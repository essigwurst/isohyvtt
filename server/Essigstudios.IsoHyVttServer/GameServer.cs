/* ------------------------------------------------------------------------
 * Copyright 2023 Essigstudios Austria / Kaiser A.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0

 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 * --------------------------------------------------------------------- */

#pragma warning disable 8600
#pragma warning disable 8602
#pragma warning disable 8604
#pragma warning disable 8618

using System;
using System.IO;
using System.Net;
using System.Text;
using System.Drawing;
using System.Net.Http;
using System.Threading;
using System.Reflection;
using System.Security.Cryptography;

using Newtonsoft.Json;

namespace Essigstudios.IsoHyVttServer
{
    /// <summary>
    /// Base GameServer class
    /// </summary>
    public class GameServer
    {
        /// <summary>
        /// Creates a new instance of the <see cref="GameServer"/> class.
        /// Multiple instances may run on the same machine. Modify the constant LISTEN_PORT for this.
        /// </summary>
        /// <param name="serverName">Target instance name of this server</param>
        public GameServer(string serverName)
        {
            m_WebRoot = Environment.CurrentDirectory;
            m_ServerName = serverName;

            m_HttpListener = new HttpListener();
            m_HttpListener.Prefixes.Add($"http://+:{Constants.LISTEN_PORT}/");

            try
            {
                m_HttpListener.Start();
            }
            catch (HttpListenerException)
            {
                Console.WriteLine($"[Error]\tAdmin rights required - unable to start server! Press any key to exit!");
                Console.ReadKey(true);

                return;
            }

            m_ListenThread = new Thread(RunLoop);
        }

        private string m_WebRoot;

        private string m_ServerName;

        private Thread m_ListenThread;

        private GameSession m_Session;

        private HttpListener m_HttpListener;


        /// <summary>
        /// Returns true, if the server thread is running. Otherwise returns false.
        /// </summary>
        public bool IsAlive
        {
            get
            {
                if (m_ListenThread == null)
                {
                    return (false);
                }

                return (m_ListenThread.ThreadState == ThreadState.Running ? true : false);
            }
        }


        /// <summary>
        /// Creates a new server instance (if it doesn't exist) and starts the Http listener.
        /// </summary>
        public void RunLoop()
        {
            Console.WriteLine($"[Info]\tHttp listener running!");

            CreateSession();


            while (m_HttpListener.IsListening)
            {
                var context = m_HttpListener.GetContext();

                try
                {
                    var request = context.Request;

                    byte[] requestBody = null, responseData = null;

                    if (request.HasEntityBody)
                    {
                        using (Stream bodyStream = request.InputStream)
                        {
                            requestBody = ReadFullStream(bodyStream);
                        }
                    }

                    Console.WriteLine($"[Info]\tRequest from {request.RemoteEndPoint.Address + " (" + request.UserAgent + ")"} to endpoint {request.RawUrl}");

                    if (request.RawUrl == "/")
                    {
                        string responseString =
                            $"<html>" +
                            $"<head><title>Essigstudios IsoHyVtt Server</title></head>" +
                            $"<body>" +
                            $"Server time is {DateTime.Now.ToString("HH:mm:ss.ffff")}<br>" +
                            $"Prefixes: {string.Join("<br>", m_HttpListener.Prefixes)}" +
                            $"<br>" +
                            $"</body></html>";

                        responseData = Encoding.UTF8.GetBytes(responseString);
                    }
                    else if (request.RawUrl.Contains($"/{m_ServerName}/getsession"))
                    {
                        string requestingUser = Encoding.UTF8.GetString(requestBody) + "@" + request.RemoteEndPoint.Address.ToString();

                        CheckAddPlayerToSession(requestingUser);
                        GenerateAssetHash();

                        responseData = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(m_Session));
                    }
                    else if (request.RawUrl.Contains($"/{m_ServerName}/postasset?name="))
                    {
                        string instancePath = Path.Combine(m_WebRoot, m_Session.SessionName);
                        string filePath = Path.Combine(instancePath, request.RawUrl.Split("name=").Last().Replace("'", string.Empty));
                        string fileName = request.RawUrl.Split("name=").Last().Replace("'", string.Empty).Split('.').First();

                        WriteAsset(instancePath, filePath, requestBody);

                        responseData = Encoding.UTF8.GetBytes("200");
                    }
                    else if (request.RawUrl.Contains($"/{m_ServerName}/getasset?name="))
                    {
                        string instancePath = Path.Combine(m_WebRoot, m_Session.SessionName);
                        string filePath = Path.Combine(instancePath, request.RawUrl.Split("name=").Last().Replace("'", string.Empty));
                        string fileName = request.RawUrl.Split("name=").Last().Replace("'", string.Empty).Split('.').First();

                        if (File.Exists(filePath))
                        {
                            responseData = File.ReadAllBytes(filePath);
                        }
                        else
                        {
                            responseData = new byte[0];

                            m_Session.AssetList.Remove(fileName);
                        }
                    }
                    else if (request.RawUrl.Contains($"/{m_ServerName}/askassetsize?name="))
                    {
                        string instancePath = Path.Combine(m_WebRoot, m_Session.SessionName);
                        string filePath = Path.Combine(instancePath, request.RawUrl.Split("name=").Last().Replace("'", string.Empty));
                        string fileName = request.RawUrl.Split("name=").Last().Replace("'", string.Empty).Split('.').First();

                        if (File.Exists(filePath))
                        {
                            var size = new FileInfo(filePath).Length;
                            responseData = BitConverter.GetBytes(size);
                        }
                        else
                        {
                            responseData = new byte[0];

                            m_Session.AssetList.Remove(fileName);
                        }
                    }
                    else if (request.RawUrl.Contains($"/{m_ServerName}/addchat?user="))
                    {
                        string user = request.RawUrl.Split("user=").Last().Replace("'", string.Empty);

                        WriteToChatLog($"[{user} @ {DateTime.Now.ToString("HH:mm:ss")}] " + Encoding.UTF8.GetString(requestBody));
                        responseData = Encoding.UTF8.GetBytes("200");
                    }
                    else if (request.RawUrl.Contains($"/{m_ServerName}/spawnasset?user="))
                    {
                        string user = request.RawUrl.Split("user=").Last().Replace("'", string.Empty);
                        string[] requirement = Encoding.UTF8.GetString(requestBody).Split('\t');

                        string assetName = requirement[0];
                        int windowX = Convert.ToInt32(requirement[1]);
                        int windowY = Convert.ToInt32(requirement[2]);

                        string id = AddGameElement(assetName, user, windowX, windowY);

                        responseData = Encoding.UTF8.GetBytes(id);
                    }
                    else if (request.RawUrl.Contains($"/{m_ServerName}/removeasset?user="))
                    {
                        string user = request.RawUrl.Split("user=").Last().Replace("'", string.Empty);
                        string[] requirement = Encoding.UTF8.GetString(requestBody).Split('\t');

                        string assetName = requirement[0];

                        RemoveGameElement(assetName);

                        responseData = Encoding.UTF8.GetBytes("200");
                    }
                    else if (request.RawUrl.Contains($"/{m_ServerName}/updateassetlocation?user="))
                    {
                        string user = request.RawUrl.Split("user=").Last().Replace("'", string.Empty);
                        string[] requirement = Encoding.UTF8.GetString(requestBody).Split('\t');

                        string assetIdentifier = requirement[0];
                        int newPosX = Convert.ToInt32(requirement[1]);
                        int newPosY = Convert.ToInt32(requirement[2]);

                        UpdateGameElementLocation(assetIdentifier, newPosX, newPosY);

                        responseData = Encoding.UTF8.GetBytes("200");
                    }
                    else if (request.RawUrl.Contains($"/{m_ServerName}/updateassetsize?user="))
                    {
                        string user = request.RawUrl.Split("user=").Last().Replace("'", string.Empty);
                        string[] requirement = Encoding.UTF8.GetString(requestBody).Split('\t');

                        string assetIdentifier = requirement[0];
                        int newSizeX = Convert.ToInt32(requirement[1].Replace(".", ",").Split(',')[0]);
                        int newSizeY = Convert.ToInt32(requirement[2].Replace(".", ",").Split(',')[0]);

                        UpdateGameElementSize(assetIdentifier, newSizeX, newSizeY);

                        responseData = Encoding.UTF8.GetBytes("200");
                    }
                    else if (request.RawUrl.Contains($"/{m_ServerName}/roll?user="))
                    {
                        string user = request.RawUrl.Split("user=").Last().Replace("'", string.Empty);
                        string[] requirement = Encoding.UTF8.GetString(requestBody).Split('\t');

                        string dice = requirement[0];
                        int dval = Convert.ToInt32(requirement[0].Replace("D", string.Empty));

                        Random random = new Random();
                        int result = random.Next(1, dval + 1);

                        WriteToChatLog($"{user} rolls a {dice}: {result}");

                        responseData = Encoding.UTF8.GetBytes("200");
                    }

                    if (responseData != null)
                    {
                        context.Response.ContentLength64 = responseData.Length;
                        context.Response.OutputStream.WriteAsync(responseData, 0, responseData.Length);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Error]\t{ex.ToString()}");

                    var response = Encoding.UTF8.GetBytes(ex.ToString());

                    context.Response.ContentLength64 = response.Length;
                    context.Response.OutputStream.WriteAsync(response, 0, response.Length);
                }
            }

            Console.WriteLine("[Info]\tHttp listener shutdown!");
        }


        /// <summary>
        /// Converts the request body stream into a byte array
        /// </summary>
        /// <param name="input">Stream</param>
        /// <returns>Request body data</returns>
        public static byte[] ReadFullStream(Stream input)
        {
            byte[] buffer = new byte[16 * 1024];

            using (MemoryStream memoryStream = new MemoryStream())
            {
                int read;

                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    memoryStream.Write(buffer, 0, read);
                }
                return memoryStream.ToArray();
            }
        }


        /// <summary>
        /// Updates the location (e.g. position) of a game element from a client request
        /// </summary>
        /// <param name="assetIdentifier">Asset identifier, e.g. test_png1</param>
        /// <param name="newPosX">Target position, X</param>
        /// <param name="newPosY">Target position, Y</param>
        private void UpdateGameElementLocation(string assetIdentifier, int newPosX, int newPosY)
        {
            foreach (var gameElement in m_Session.GameElements)
            {
                if (gameElement.Identifier.Replace(".", "_") == assetIdentifier)
                {
                    gameElement.Location[0] = newPosX;
                    gameElement.Location[1] = newPosY;

                    break;
                }
            }
        }


        /// <summary>
        /// Updates the size of an asset from a client request
        /// </summary>
        /// <param name="assetIdentifier">Asset identifier, e.g. test_png1</param>
        /// <param name="newSizeX">Target size, X</param>
        /// <param name="newSizeY">Target size, Y</param>
        private void UpdateGameElementSize(string assetIdentifier, int newSizeX, int newSizeY)
        {
            foreach (var gameElement in m_Session.GameElements)
            {
                if (gameElement.Identifier.Replace(".", "_") == assetIdentifier)
                {
                    gameElement.Size[0] = newSizeX;
                    gameElement.Size[1] = newSizeY;

                    break;
                }
            }
        }


        /// <summary>
        /// Removes an element from the game by a client request
        /// </summary>
        /// <param name="assetName">Asset identifier, e.g. test_png1</param>
        private void RemoveGameElement(string assetName)
        {
            foreach (var gameElement in m_Session.GameElements)
            {
                if (gameElement.Identifier.Replace(".", "_") == assetName)
                {
                    m_Session.GameElements.Remove(gameElement);

                    GenerateGameElementHash();

                    Console.WriteLine($"[Info]\tRemoved game element {assetName}!");
                    break;
                }
            }
        }


        /// <summary>
        /// Adds a new element to the game by a client request
        /// </summary>
        /// <param name="assetName">Asset name, e.g. test.png</param>
        /// <param name="owner">User, who spawns and owns the asset</param>
        /// <param name="windowX">Viewport width, used to determine the spawn position (center screen)</param>
        /// <param name="windowY">Viewport height, used to determine the spawn position (center screen)</param>
        /// <returns>Asset identifier, e.g. test_png1</returns>
        private string AddGameElement(string assetName, string owner, int windowX, int windowY)
        {
            var asset = Path.Combine(m_WebRoot, m_ServerName, assetName);

            if (!File.Exists(asset))
            {
                Console.WriteLine($"[Error]\tUnable to spawn asset {asset} because it does not exist!");
                return (string.Empty);
            }

            float[] spawnLocation = new float[]
            {
                windowX / 2.0f,
                windowY / 2.0f
            };

            // ToDo: Make it cross- platform compatible.
            var image = Image.FromFile(asset);

            int highestId = 0;

            List<GameElement> existingAssets = m_Session.GameElements.Where(x => x.Name == assetName).ToList();

            foreach (var existingAsset in existingAssets)
            {
                int existingId = Convert.ToInt32(existingAsset.Identifier.Replace(assetName.Replace(".", "_"), string.Empty));

                if (existingId > highestId)
                {
                    highestId = existingId;
                }
            }

            var id = highestId + 1;
            string identifier = (assetName + id).Replace(".", "_");

            GameElement element = new GameElement(assetName, identifier, owner, spawnLocation, new float[] { (float)image.Width, (float)image.Height } );
            m_Session.GameElements.Add(element);

            image.Dispose();

            GenerateGameElementHash();

            Console.WriteLine($"[Info]\tSpawned asset: {assetName} ({identifier})!");

            return (identifier);
        }


        /// <summary>
        /// Creates the asset on the server, as received in bytes from a client
        /// </summary>
        /// <param name="instancePath">Instance name, specifies where to store the asset</param>
        /// <param name="filePath">Target file path</param>
        /// <param name="fileContent">Fily bytes</param>
        private void WriteAsset(string instancePath, string filePath, byte[] fileContent)
        {
            if (!Directory.Exists(instancePath))
            {
                Directory.CreateDirectory(instancePath);
            }

            File.WriteAllBytes(filePath, fileContent);

            string assetName = Path.GetFileName(filePath);

            if (!m_Session.AssetList.Contains(assetName))
            {
                m_Session.AssetList.Add(assetName);
            }

            GenerateAssetHash();
            WriteToChatLog($"Asset '{assetName}' was added!");
        }


        /// <summary>
        /// Writes text to the game chat log
        /// </summary>
        /// <param name="text">Text, which should be written</param>
        private void WriteToChatLog(string text)
        {
            m_Session.ChatWindowLog.Add(text);

            MD5 hash = MD5.Create();
            m_Session.ChatHash = BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(m_Session.ChatWindowLog.Count.ToString()))).ToLower().Replace("-", string.Empty);
        }


        /// <summary>
        /// Generates a hash out of all uploaded assets.
        /// Used by the client to determine, if anything has been changed
        /// </summary>
        private void GenerateAssetHash()
        {
            string buffer = string.Empty;

            foreach (var asset in m_Session.AssetList)
            {
                buffer += asset.ToString();
            }

            MD5 hash = MD5.Create();
            var result = hash.ComputeHash(Encoding.UTF8.GetBytes(buffer));

            m_Session.AssetHash = BitConverter.ToString(result).ToLower().Replace("-", string.Empty);
        }


        /// <summary>
        /// Generates a hash out of all active game elements.
        /// Used by the client to determine, if anything has been changed
        /// </summary>
        private void GenerateGameElementHash()
        {
            string buffer = string.Empty;

            foreach (var asset in m_Session.GameElements)
            {
                buffer += asset.Identifier + asset.Owner + asset.Location[0].ToString() + asset.Location[1].ToString();
            }

            MD5 hash = MD5.Create();
            var result = hash.ComputeHash(Encoding.UTF8.GetBytes(buffer));

            m_Session.GameElementsHash = BitConverter.ToString(result).ToLower().Replace("-", string.Empty);
        }


        /// <summary>
        /// Adds the identification of a player to the session.
        /// Not used for anything specific right now
        /// </summary>
        /// <param name="user">Name of the new player with the according IP</param>
        private void CheckAddPlayerToSession(string user)
        {
            if (!m_Session.PlayerList.Contains(user))
            {
                m_Session.PlayerList.Add(user);
            }
        }


        /// <summary>
        /// Creates a new session on the server (or loads it, if there are existing assets).
        /// </summary>
        public void CreateSession()
        {
            Console.WriteLine($"[Info]\tGenerating new session '{m_ServerName}'  . . .");

            m_Session = new GameSession(m_ServerName);

            WriteToChatLog($"Session {m_ServerName} created at {DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss")}!");

            Console.WriteLine($"[Info]\tChecking for existing assets for this session . . .");

            string instanceRoot = Path.Combine(m_WebRoot, m_ServerName);

            if (!Directory.Exists(instanceRoot))
            {
                Directory.CreateDirectory(instanceRoot);
            }

            foreach (var file in Directory.GetFiles(instanceRoot))
            {
                var assetName = Path.GetFileName(file);

                m_Session.AssetList.Add(assetName);
                Console.WriteLine($"[Info]\tFound asset {assetName}!");
            }

            if (m_Session.AssetList.Count > 0)
            {
                WriteToChatLog($"Found {m_Session.AssetList.Count} asssets for session {m_ServerName}!");
                GenerateAssetHash();
            }
            else
            {
                Console.WriteLine($"[Info]\tNo existing assets found!");
            }

            GenerateGameElementHash();

            Console.WriteLine($"[Info]\tServer init complete!");
        }
    }
}