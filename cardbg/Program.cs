using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace cardbg
{
    class Program
    {
        static void Main(string[] args)
        {
            Banner();

            if(args.Length == 0)
            {
                Console.WriteLine("Error: Input file not specified");
                return;
            }
            else if (args.Length == 1 &&  args[0] == "/?")
            {
                DisplayHelp();
                return;
            }

            string fileName = args[0];

            if (File.Exists(fileName))
            {
                var inputText = File.ReadAllLines(fileName);
                var instructions = inputText.Select(line => int.Parse(line)).ToList();

                string symbolFile = Path.ChangeExtension(fileName, "cardb");
                Debugger debugger = null;

                if (File.Exists(symbolFile))
                {
                    var symbols = File.ReadAllText(symbolFile);
                    debugger = new Debugger(instructions, symbols);
                }
                else
                {
                    debugger = new Debugger(instructions);
                }

                debugger.Start();
            }
            else
            {
                Console.WriteLine("Error: File '{0}' not found", fileName);
            }
        }

        private static void Banner()
        {
            Console.WriteLine("Cardiac Debugger version {0}",
                Assembly.GetExecutingAssembly().GetName().Version);

            Console.WriteLine("Copyright (C) 2017. All rights reserved");
            Console.WriteLine();
        }

        private static void DisplayHelp()
        {
            Console.WriteLine("                    Cardiac Interactive Debugger Options");
            Console.WriteLine();
            Console.WriteLine("cardbg <image_file>");
            Console.WriteLine();
            Console.WriteLine("                        -INPUT FILES-");
            Console.WriteLine("<image_file> Specify compiled image (.cardimg) to debug. Symbols (.cardb)");
            Console.WriteLine("             will be loaded automatically if they are found in the same");
            Console.WriteLine("             directory as the target image.");
        }

    }
}
