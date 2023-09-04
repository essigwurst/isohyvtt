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

#pragma warning disable 8602

using System;
using System.IO;
using System.Net;
using System.Text;
using System.Drawing;
using System.Net.Http;
using System.Threading;
using System.Reflection;

namespace Essigstudios.IsoHyVttServer
{
    public static class Core
    {
        /// <summary>
        /// Program entry point
        /// </summary>
        /// <param name="args">Startup arguments</param>
        public static void Main(string[] args)
        {
            Console.WriteLine($"(C)2023 Essigstudios Austria / Kaiser A.\r\nIsoHyVtt Server, Version {Assembly.GetExecutingAssembly().GetName().Version.ToString()}");

            if (args.Length != 1)
            {
                Console.WriteLine("[Warning]\tInvalid argument specified! Required: ServerName");

                return;
            }

            GameServer gameServer = new GameServer(args.First());
            gameServer.RunLoop();

            while (gameServer.IsAlive)
            {
                Thread.Sleep(1000);
            }

            Console.WriteLine("[Info]\tProgram ended!");
        }
    }
}