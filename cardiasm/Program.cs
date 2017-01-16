using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;
using Cardiac.Core;

namespace cardiasm
{
    class Options
    {
        public bool IsValid;
        public bool EmitSymbols;
        public string SourceFileName;
        public string OutputFileName;
    }

    class Entry
    {
        static void Main(string[] args)
        {
            Banner();
            Options o = ParseArguments(args);

            if (!o.IsValid)
                return;

            Compiler c = new Compiler();
            Program program = c.Compile(o.SourceFileName);

            var haltStatements = program.Statements.Where(statement => statement.OpCode == OpCode.HRS);
            if (haltStatements.Count() == 0)
            {
                Console.WriteLine("Warning: No halt statements found.");
            }
            else
            {
                var LastStatement = program.Statements
                                    .OrderByDescending(statement => statement.Address)
                                    .First();

                if (LastStatement.OpCode != OpCode.JMP && LastStatement.OpCode != OpCode.HRS)
                {
                    Console.WriteLine("Warning: Last statement in program is not halt or jump; adding HRS");
                }
            }

            List<int> binary = program.CreateExecutable();

            using (StreamWriter writer = new StreamWriter(o.OutputFileName))
            {
                if (binary != null)
                {
                    for (int i = 0; i < binary.Count; i++)
                    {
                        if (binary[i] >= 0)
                            writer.Write(" ");

                        writer.WriteLine("{0:000}", binary[i]);
                    }
                }
            }

            if (o.EmitSymbols)
            {
                string symbols = program.CreateSymbols();
                string symbolFile = Path.ChangeExtension(o.OutputFileName, "cardb");

                using (StreamWriter writer = new StreamWriter(symbolFile))
                {
                    writer.Write(symbols);
                }
            }
        }

        private static Options ParseArguments(string[] args)
        {
            Options retval = new Options() { IsValid = false };

            if (args.Length == 0)
            {
                Console.WriteLine("Warning: No Source file specified");
                return retval;
            }
            else if (args.Length == 1 && args[0] == "/?")
            {
                DisplayHelp();
                return retval;
            }

            foreach(var arg in args)
            {
                if (arg.ToLower() == "/debug+")
                {
                    retval.EmitSymbols = true;
                }
                else if (arg.ToLower() == "/debug-")
                {
                    retval.EmitSymbols = false;
                }
                else if (arg.ToLower().StartsWith("/out:"))
                {
                    retval.OutputFileName = arg.Substring("/out:".Length);
                }
                else
                {
                    retval.SourceFileName = arg;
                }
            }

            if(string.IsNullOrEmpty(retval.OutputFileName))
            {
                retval.OutputFileName = Path.ChangeExtension(retval.SourceFileName, ".cardimg");
            }

            if(!string.IsNullOrEmpty(retval.SourceFileName) &&
               !string.IsNullOrEmpty(retval.OutputFileName))
            {
                retval.IsValid = true;
            }

            return retval;
        }

        private static void DisplayHelp()
        {
            Console.WriteLine("                    Cardiasm Compiler Options");
            Console.WriteLine();
            Console.WriteLine("cardiasm [/out:<file>] [/debug[+/-]] <source_file>");
            Console.WriteLine();
            Console.WriteLine("                        -INPUT FILES-");
            Console.WriteLine("<source_file> Specify input file name.");
            Console.WriteLine();
            Console.WriteLine("                        -OUTPUT FILES-");
            Console.WriteLine("/out:<file>   Specify output file name(default: base name of input file)");
            Console.WriteLine();
            Console.WriteLine("                        -CODE GENERATION-");
            Console.WriteLine("/debug[+|-]   Emit debugging information");
        }

    private static void Banner()
        {
            Console.WriteLine("Cardiac Compiler version {0}",
                Assembly.GetExecutingAssembly().GetName().Version);

            Console.WriteLine("Copyright (C) 2017. All rights reserved");
            Console.WriteLine();
        }
    }
}
