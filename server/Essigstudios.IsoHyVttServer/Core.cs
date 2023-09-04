// (C)2023 Essigstudios Austria, Kaiser A.

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