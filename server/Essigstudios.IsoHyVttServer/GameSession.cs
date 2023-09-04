// (C)2023 Essigstudios Austria, Kaiser A.

namespace Essigstudios.IsoHyVttServer
{
    public class GameSession
    {
        public GameSession(string sessionName)
        {
            SessionName = sessionName;
            BackgroundAsset = string.Empty;

            AssetList = new List<string>();
            PlayerList = new List<string>();
            ChatWindowLog = new List<string>();
            GameElements = new List<GameElement>();
        }

        public string SessionName { get; private set; }

        public string BackgroundAsset { get; private set; }

        public string AssetHash { get; set; }

        public string ChatHash { get; set; }

        public string GameElementsHash { get; set; }

        public List<string> ChatWindowLog { get; private set; }

        public List<string> AssetList { get; private set; }

        public List<string> PlayerList { get; private set; }

        public List<GameElement> GameElements { get; set; }
    }
}