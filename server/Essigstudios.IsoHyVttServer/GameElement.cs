// (C)2023 Essigstudios Austria, Kaiser A.

namespace Essigstudios.IsoHyVttServer
{
    public class GameElement
    {
        public GameElement(string name, string identifier, string owner, float[] location, float[] size)
        {
            Name = name;
            Identifier = identifier;
            Owner = owner;
            Location = location;
            Size = size;
        }

        public string Name { get; set; }

        public string Identifier { get; set; }

        public string Owner { get; set; }

        public float[] Location { get; set; }

        public float[] Size { get; set; }
    }
}