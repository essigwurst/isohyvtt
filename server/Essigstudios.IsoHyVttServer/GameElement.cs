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
    /// Represents a game object
    /// </summary>
    public class GameElement
    {
        /// <summary>
        /// Creates a new instance of the <see cref="GameElement"/> class.
        /// </summary>
        /// <param name="name">Name of the new element, e.g. image.png</param>
        /// <param name="identifier">Identifier of the new element, e.g. image_png1</param>
        /// <param name="owner">Creator of the new element, e.g. PlayerName</param>
        /// <param name="location">Location / Position of the new element, [0] = X, [1] = Y</param>
        /// <param name="size">Size of the new element, [0] = Width, [1] = Height</param>
        public GameElement(string name, string identifier, string owner, int[] location, int[] size, int layer)
        {
            Name = name;
            Identifier = identifier;
            Owner = owner;
            Location = location;
            Size = size;
            Layer = layer;
        }

        /// <summary>
        /// Element name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Element identifier
        /// </summary>
        public string Identifier { get; set; }

        /// <summary>
        /// Element owner
        /// </summary>
        public string Owner { get; set; }

        /// <summary>
        /// Element location
        /// </summary>
        public int[] Location { get; set; }

        /// <summary>
        /// Element size
        /// </summary>
        public int[] Size { get; set; }

        /// <summary>
        /// Representation layer
        /// </summary>
        public int Layer { get; set; }
    }
}