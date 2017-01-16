using Cardiac.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Cardiac
{

    class Options
    {
        public bool IsValid;
        public string ImageFile;
        public List<int> args;
        public bool profile;
    }

    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Banner();
                Console.WriteLine("Error: Input file not specified");
                return;
            }
            else if (args[0] == "/?")
            {
                Banner();
                DisplayHelp();
                return;
            }

            else
            {
                var options = ParseArguments(args);

                if (!options.IsValid)
                    return;

                string fileName = options.ImageFile;

                if (File.Exists(fileName))
                {
                    var inputText = File.ReadAllLines(fileName);
                    var instructions = inputText.Select(line => int.Parse(line)).ToList();

                    if (options.args.Count > 0)
                        instructions.AddRange(options.args);

                    CardiacInterpreter c = new CardiacInterpreter(instructions);

                    StreamWriter writer = null;

                    if (options.profile)
                    {
                        DateTime dt = DateTime.Now;
                        string dateTimeString = string.Format("{0}{1}{2}{3}{4}{5}",
                            dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second);
                        string profileFileName = Path.GetFileNameWithoutExtension(options.ImageFile) + "_" + dateTimeString + ".cardiprof";
                        writer = File.CreateText(profileFileName);
                        TraceSource ts = new TraceSource("cardiac");
                        ts.Switch.Level = SourceLevels.Information;
                        ts.Listeners.Add(new TextWriterTraceListener(writer));
                        c.Trace = ts;
                    }


                    c.Output += (o, e) => { Console.WriteLine(e); };
                    c.Start();

                    if(writer != null)
                    {
                        writer.Close();
                    }

                }
                else
                {
                    Console.WriteLine("Error: File '{0}' does not exist", fileName);
                }
            }
        }

        private static void Banner()
        {
            Console.WriteLine("Cardiac Interpreter version {0}",
                Assembly.GetExecutingAssembly().GetName().Version);

            Console.WriteLine("Copyright (C) 2017. All rights reserved");
            Console.WriteLine();
        }

        private static void DisplayHelp()
        {
            Console.WriteLine("                    Cardiac Interpreter Options");
            Console.WriteLine();
            Console.WriteLine("cardiac <image_file> [/profile[+|-]]  [<input_args>]");
            Console.WriteLine();
            Console.WriteLine("                        -OPTIONS-");
            Console.WriteLine("<image_file>  Specify compiled image  (.cardexe) to execute. Symbols");
            Console.WriteLine("              will be loaded automatically if they are found in the");
            Console.WriteLine("              same directory as the target image.");
            Console.WriteLine();
            Console.WriteLine("/profile[+|-] Emit a raw profile trace to a .cardiprof file.");
            Console.WriteLine();
            Console.WriteLine("<input_args>  Space-delimited list of arguments to be passed in via ");
            Console.WriteLine("              the input tape. Arguments should be integers in the");
            Console.WriteLine("              range [-999...999].");
            Console.WriteLine();
        }


        private static Options ParseArguments(string[] args)
        {
            Options retval = new Options() { IsValid = true };

            retval.args = new List<int>();

            var numericArgs = args.Where(arg => { int nextArg = 0; return int.TryParse(arg, out nextArg);  });
            var optionsArgs = args.Where(arg => { int nextArg = 0; return !int.TryParse(arg, out nextArg); });

            foreach (var arg in optionsArgs)
            {
                if(arg.ToLower() == "/profile+")
                {
                    retval.profile = true;
                }
                else if(arg == "/?")
                {
                    Banner();
                    DisplayHelp();
                    retval.IsValid = false;
                }
                else if(arg.StartsWith("/"))
                {
                    Console.WriteLine("Error: Unrecognized option: '{0}'", arg);
                    retval.IsValid = false;
                }
                else
                {
                    retval.ImageFile = arg;
                }
            }


            foreach (var arg in numericArgs)
            {
                int nextArg = 0;
                if (int.TryParse(arg, out nextArg) && nextArg > 0 && nextArg < 1000)
                {
                    retval.args.Add(nextArg);
                }
                else
                {
                    retval.IsValid = false;
                    Console.WriteLine("Error: Invalid input argument '{0}'", arg);
                    return retval;
                }
            }


            return retval;
        }
    }
}
