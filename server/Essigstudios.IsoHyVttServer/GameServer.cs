// (C)2023 Essigstudios Austria, Kaiser A.

#pragma warning disable 8602
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
    public class GameServer
    {
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
            catch (HttpListenerException ex)
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
                            $"<br>";

                        // List sessions

                        responseString += $"</body></html>";

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

                        RemoveGameElement(assetName, user);

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


        private void RemoveGameElement(string assetName, string owner)
        {
            foreach (var gameElement in m_Session.GameElements)
            {
                if (gameElement.Identifier.Replace(".", "_") == assetName)
                {
                    m_Session.GameElements.Remove(gameElement);

                    GenerateGameElementHash();

                    Console.WriteLine($"[Info] Removed game element {assetName}!");
                    break;
                }
            }
        }


        private string AddGameElement(string assetName, string owner, int windowX, int windowY)
        {
            var asset = Path.Combine(m_WebRoot, m_ServerName, assetName);

            if (!File.Exists(asset))
            {
                Console.WriteLine($"[Error] Unable to spawn asset {asset} because it does not exist!");
                return (string.Empty);
            }

            float[] spawnLocation = new float[]
            {
                windowX / 2.0f,
                windowY / 2.0f
            };

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

            Console.WriteLine($"[Info] Spawned asset: {assetName} ({identifier})!");

            return (identifier);
        }


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


        private void WriteToChatLog(string text)
        {
            m_Session.ChatWindowLog.Add(text);

            MD5 hash = MD5.Create();
            m_Session.ChatHash = BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(m_Session.ChatWindowLog.Count.ToString()))).ToLower().Replace("-", string.Empty);
        }


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


        private void CheckAddPlayerToSession(string user)
        {
            if (!m_Session.PlayerList.Contains(user))
            {
                m_Session.PlayerList.Add(user);
            }
        }


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