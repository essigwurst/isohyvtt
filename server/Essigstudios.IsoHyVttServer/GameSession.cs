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

namespace Essigstudios.IsoHyVttServer
{
    /// <summary>
    /// Represents a game session
    /// </summary>
    public class GameSession
    {
        /// <summary>
        /// Creates a new instance of the <see cref="GameSession"/> class. Will be serialized to a json, then written to the game client.
        /// </summary>
        /// <param name="sessionName">Name of the session, which should be created</param>
        public GameSession(string sessionName)
        {
            SessionName = sessionName;
            BackgroundAsset = string.Empty;

            AssetHash = string.Empty;
            ChatHash = string.Empty;
            GameElementsHash = string.Empty;

            AssetList = new List<string>();
            PlayerList = new List<string>();
            ChatWindowLog = new List<string>();
            GameElements = new List<GameElement>();
        }

        /// <summary>
        /// Name of this session
        /// </summary>
        public string SessionName { get; private set; }

        /// <summary>
        /// Represents the background asset. Currently not in use!
        /// </summary>
        public string BackgroundAsset { get; private set; }

        /// <summary>
        /// MD5 sum of all assets uploaded to the server
        /// </summary>
        public string AssetHash { get; set; }

        /// <summary>
        /// MD5 sum of all chat entries
        /// </summary>
        public string ChatHash { get; set; }

        /// <summary>
        /// MD5 sum of all game elements
        /// </summary>
        public string GameElementsHash { get; set; }

        /// <summary>
        /// Holds all chat text of this session
        /// </summary>
        public List<string> ChatWindowLog { get; private set; }

        /// <summary>
        /// A list of all assets uploaded to this session
        /// </summary>
        public List<string> AssetList { get; private set; }

        /// <summary>
        /// A list of all players, who joined this session at any time
        /// </summary>
        public List<string> PlayerList { get; private set; }

        /// <summary>
        /// Represents the current state of all game elements in this session
        /// </summary>
        public List<GameElement> GameElements { get; set; }
    }
}