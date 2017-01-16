using Cardiac.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace cardbg
{
    class Debugger
    {
        List<int> Instructions;
        string[] SourceLines;
        Dictionary<int, string> Symbols;
        Dictionary<int, int> Statements;
        List<int> OutputItems = new List<int>();
        CardiacInterpreter Interpreter; 
        

        public Debugger(List<int> instructions)
        {
            Instructions = instructions;
            SourceLines = null;
            Statements = new Dictionary<int, int>();
            Symbols = new Dictionary<int, string>();

            Interpreter = new CardiacInterpreter(instructions);
            Interpreter.Output += (o, e) => { OutputItems.Add(e);  };
        }
        
        public Debugger(List<int> instructions, string symbols) : this(instructions)
        {
            LoadSymbols(symbols);
        }

        private void LoadSymbols(string symbols)
        {
            Symbols = new Dictionary<int, string>();
            Statements = new Dictionary<int, int>();

            string[] newline = new string[] { Environment.NewLine };
            string[] split = symbols.Split(newline, StringSplitOptions.None);

            bool IsSymbols = false;
            bool IsStatements = false;

            for (int i = 0; i < split.Length; i++)
            {
                if (!string.IsNullOrEmpty(split[i]))
                {
                    if (split[i].Contains("SOURCE:"))
                    {
                        string sourcePath = split[i].Substring("SOURCE:".Length);
                        SourceLines = File.ReadAllLines(sourcePath);

                    }
                    else if (split[i] == "SYMBOLS")
                    {
                        IsSymbols = true;
                        IsStatements = false;

                    }
                    else if (split[i] == "STATEMENTS")
                    {
                        IsSymbols = false;
                        IsStatements = true;

                    }
                    else if (IsStatements)
                    {
                        var s = split[i].Split(':');
                        Statements[int.Parse(s[0])] = int.Parse(s[1]);
                    }
                    else if (IsSymbols)
                    {
                        var s = split[i].Split(':');
                        Symbols[int.Parse(s[0])] = s[1];
                    }
                }
            }
        }

        internal void Start()
        {
            int lastModified = -1;

            Load(Instructions);

            while (!Interpreter.IsHalted)
            {
                int operand = -1;
                var nextInstruction = Interpreter.NextInstruction;
                    operand = nextInstruction.Operand;

                RenderDisplayWindow(lastModified, operand);

                string command = Console.ReadLine();

                if (Interpreter.NextInstruction.Opcode == OpCode.STO)
                    lastModified = operand;
                else
                    lastModified = -1;

                Interpreter.Step();
            }
        }

        private void RenderDisplayWindow(int lastModified, int operand)
        {
            Console.Clear();
             
            RenderMachineState(operand, lastModified);

            Console.WriteLine();
            Console.WriteLine();

            if (this.SourceLines != null && this.Statements.ContainsKey(Interpreter.ProgramCounter))
            {
                RenderSourceCode();
            }
            else
            {
                RenderDisassembly();
            }
        }

        private void Load(List<int> instructions)
        {
            Interpreter.Cards = instructions;

            Console.WriteLine("Loading...");
            do
            {
                Interpreter.Step();
            } while(Interpreter.NextInstruction.Opcode != OpCode.JMP || 
                    Interpreter.NextInstruction.Opcode == OpCode.JMP && 
                    Interpreter.NextInstruction.Operand < 3);

            //One last step needed to land on the first instruction
            Interpreter.Step();
        }

        private void RenderDisassembly()
        {
            var lineNumber = Interpreter.ProgramCounter;

            int window_bottom = Math.Max(1, lineNumber - 5);
            int window_top = Math.Min(window_bottom + 11, 99);

            for (int i = window_bottom; i <= window_top; i++)
            {
                if (i != lineNumber)
                {
                    Console.WriteLine("   [{0:000}] {1}", i, new Instruction(Interpreter.Memory[i]).ToString());
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("==>[{0:000}] {1}", i, new Instruction(Interpreter.Memory[i]).ToString());
                    Console.ForegroundColor = ConsoleColor.Gray;
                }
            }
        }

        private void RenderSourceCode()
        {
            var lineNumber = this.Statements[Interpreter.ProgramCounter];

            int window_bottom = Math.Max(1, lineNumber - 5);
            int window_top = Math.Min(window_bottom + 11, SourceLines.Length);

            for (int i = window_bottom; i <= window_top; i++)
            {
                if (i != lineNumber)
                {
                    Console.WriteLine("   [{0}] {1}", i, SourceLines[i - 1]);
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("==>[{0}] {1}", lineNumber, SourceLines[lineNumber - 1]);
                    Console.ForegroundColor = ConsoleColor.Gray;
                }
            }
        }

        private void RenderMachineState(int operand, int lastModified)
        {
            if(Interpreter.IsHalted)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[HALT]");
                Console.ForegroundColor = ConsoleColor.Gray;
            }
            else
            {
                Console.WriteLine();
            }

            for (int i = 0; i < 5; i++)
            {
                for (int j = 0; j < 20; j++)
                {
                    int address = i * 20 + j;
                    int val = Interpreter.Memory[address];
                    string s = string.Format("{0:000}", val);
                    if (val >= 0)
                    {
                        s = " " + s;
                    }
           
                    if (address == Interpreter.ProgramCounter)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write(s);
                        Console.ForegroundColor = ConsoleColor.Gray;
                    }
                    else if (address == operand)
                    {
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        Console.Write(s);

                    }
                    else if (address == lastModified)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write(s);
                    }
                    else if(Statements.ContainsKey(address))
                    {
                        Console.ForegroundColor = ConsoleColor.Gray;
                        Console.Write(s);
                    }
                    else if (Symbols.ContainsKey(address))
                    {
                        Console.ForegroundColor = ConsoleColor.DarkMagenta;
                        Console.Write(s);
                        Console.ForegroundColor = ConsoleColor.Gray;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.Write(s);
                        Console.ForegroundColor = ConsoleColor.Gray;
                    }
                }

                Console.WriteLine();
            }
            Console.ForegroundColor = ConsoleColor.Green;

            string acc = string.Format("{0:000}", Interpreter.Accumulator);
            if (Interpreter.Accumulator >= 0)
            {
                acc = " " + acc;
            }
            Console.Write("[ACC: {0}] ", acc);

            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("OUT:");
            var displayItems = OutputItems.Skip(OutputItems.Count - 15).Take(15);
            foreach(var displayItem in displayItems)
            {
                Console.Write(" {0}", displayItem);
            }
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Gray;
        }
    }
}
