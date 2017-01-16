using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Cardiac.Core
{
    public enum ExecuteAction
    {
        Continue,
        Break
    }
    public class InstructionEventArgs : EventArgs
    {
        public Instruction Instruction { get; set; }
        public ExecuteAction Action { get; set; }
    }

    public class InputEventArgs : EventArgs
    {
        public int? Input { get; set; }
    }

    delegate void InstructionProc(int operand);
    public class CardiacInterpreter
    {
        public event EventHandler<int> Output;
        public event EventHandler<InstructionEventArgs> Executing;
        public event EventHandler<InstructionEventArgs> Executed;
        public event EventHandler Starting;
        public event EventHandler Breaking;
        public event EventHandler Halting;
        public event EventHandler Resetting;
        public event EventHandler Halted;
        public event EventHandler Reset;

        private Dictionary<OpCode, InstructionProc> Operations;
        public MemoryUnit Memory { get; set; }
        public int Accumulator { get; set; }

        private int _ProgramCounter;
        public int ProgramCounter
        {
            get { return _ProgramCounter; }

            set
            {
                if (value >= 0 && value < 100)
                {
                    _ProgramCounter = value;
                }
                else
                {
                    throw new InvalidOperationException(string.Format("Invalid Program counter value '{0}'", value));
                }
            }
        }
        public IEnumerable<int> Cards { get; set; }
        private IEnumerator<int> Input;
        public TraceSource Trace { get; set; }
        public bool IsHalted { get; set; }

        private bool InputAvailable = false;
         
        public CardiacInterpreter(IEnumerable<int> cards)
        {
            this.Cards = cards;

            this.Operations = new Dictionary<OpCode, InstructionProc> 
            {
                { OpCode.INP,  o => Load(o) },
                { OpCode.CLA,  o => Accumulator  = Memory[o] },
                { OpCode.ADD,  o => Accumulator += Memory[o] },
                { OpCode.TAC,  o => ProgramCounter = (Accumulator < 0)? o : ProgramCounter + 1 },
                { OpCode.SFT,  o => Shift(o) },
                { OpCode.OUT,  o => Output?.Invoke(this, Memory[o]) },
                { OpCode.STO,  o => Memory[o] = Accumulator % 1000 },
                { OpCode.SUB,  o => Accumulator -= Memory[o] },
                { OpCode.JMP,  o => Jump(o) },
                { OpCode.HRS,  o => Halt(o) }
            };

            ResetState();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(string.Format("Accumulator:{0}", Accumulator));
            sb.Append(string.Format("   Program Counter:{0}", ProgramCounter));
            sb.Append(Environment.NewLine);
            sb.Append(Memory.ToString());

            return sb.ToString();
        }

        public void ResetState()
        {
            Resetting?.Invoke(this, EventArgs.Empty);

            Memory = new MemoryUnit();
            Accumulator = 0;
            if (Cards != null)
            {
                Input = Cards.GetEnumerator();
                InputAvailable = Input.MoveNext();
            }

            Reset?.Invoke(this, EventArgs.Empty);
        }

        public Instruction NextInstruction
        {
            get
            {
                return new Instruction(Memory._Contents[ProgramCounter]);
            }
        }

        public ExecuteAction Step()
        {
            Instruction nextInstruction = NextInstruction;

            Trace?.TraceInformation("[{0}] {1}", ProgramCounter, nextInstruction);

            var args = new InstructionEventArgs() { Instruction = nextInstruction };

            Executing?.Invoke(this, args);

            if (args.Action == ExecuteAction.Break)
                return args.Action;
          
            this.Operations[nextInstruction.Opcode](nextInstruction.Operand);

            Executed?.Invoke(this, args);

            if (!IsHalted)
            {
                if (nextInstruction.Opcode != OpCode.JMP && 
                    nextInstruction.Opcode != OpCode.TAC)
                {
                    ProgramCounter++;
                }
            }
            else
            {
                Halted?.Invoke(this, EventArgs.Empty);
            }
            
            return args.Action;
        }

        public void Start()
        {
            Starting?.Invoke(this, EventArgs.Empty);

            while (!IsHalted)
            {
                var result = Step();
                if (result == ExecuteAction.Break)
                {
                    Breaking?.Invoke(this, EventArgs.Empty);
                    break;
                }
            }
        }

        internal void Load(int address)
        {
            if (InputAvailable)
            {
                Memory[address] = Input.Current;
                InputAvailable = Input.MoveNext();
            }

            else
            {
                throw new InvalidOperationException("The program requires input but no input available.");
            }
        }

        void Shift(int o)
        {
            int leftshift = (int)Math.Pow(10, o / 10);
            int rightshift = (int)Math.Pow(10, o % 10);

            Accumulator *= leftshift;
            Accumulator /= rightshift;
        }

        void Jump(int address)
        {
            int dest = 800 + ProgramCounter + 1;
            Memory[99] = dest;
            ProgramCounter = address;
        }

        public void Halt(int address)
        {
            this.Halting?.Invoke(this, EventArgs.Empty);
            this.ProgramCounter = address;
            Trace?.TraceInformation("HALT");
            IsHalted = true;
        }
    }
}

