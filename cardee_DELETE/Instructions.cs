using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CARDIAC
{
    public abstract class Instruction
    {
        private static TraceSource Trace { get; }
        private string[] OpCodes = { "INP", "CLA", "ADD", "TAC", "SFT", "OUT", "STO", "SUB", "JMP", "HRS" };

        public string OpCode {  get { return OpCodes[Value / 100]; } }

        protected int Value;

        public Instruction(int raw)
        {
            this.Value = raw;
        }
        static Instruction()
        {
            Trace = new TraceSource("CARDIAC");
        }

        public abstract void Execute(CardiacMachine machine);

        public static Instruction FromInt(int val)
        {
            switch(val / 100)
            {
                case 0:
                    return new LoadInstruction(val);
                case 1:
                    return new ClearAccumulatorAndAddInstruction(val);
                case 2:
                    return new AddInstruction(val);
                case 3:
                    return new TestAndJumpInstruction(val);
                case 4:
                    return new ShiftAccumulatorInstruction(val);
                case 5:
                    return new OutputInstruction(val);
                case 6:
                    return new StoreInstruction(val);
                case 7:
                    return new SubtractInstruction(val);
                case 8:
                    return new JumpInstruction(val);
                case 9:
                    return new HaltInstruction(val);
                default:
                    throw new Exception(string.Format("Invalid opcode %d", val));
            }
        }
    }

    public abstract class OperandInstruction : Instruction
    {
        public OperandInstruction(int value) : base(value){ }

        public int Operand { get { return Value % 100;  } }

        public override string ToString()
        {
            return string.Format("{0} {1} {2}", Value, OpCode, Operand);
        }


    }
    //INP Read a card into memory
    //The INP instruction reads a single card from the input and stores 
    //the contents of that card into the memory location identified by the operand address. (MEM[a] ← INPUT)
    public class LoadInstruction : OperandInstruction {

        public LoadInstruction(int value) : base(value){}

        public override void Execute(CardiacMachine machine)
        {
            Trace.TraceInformation("{0} {1} LOAD ({2}); MEM[{3}] <== {2}", machine.ProgramCounter, Value, machine.Input.Current, Operand);
            machine.Memory[Operand] = machine.Input.Current;

            machine.Input.MoveNext();
        }
    }

    //CLA Clear accumulator and add from memory (load)
    //This instruction causes the contents of the memory location
    // specified by the operand address to be loaded into the accumulator. (ACC ← MEM[a])
    public class ClearAccumulatorAndAddInstruction : OperandInstruction
    {
        public ClearAccumulatorAndAddInstruction(int value) : base(value){ }
        public override void Execute(CardiacMachine machine)
        {
            Trace.TraceInformation("{0} {1} ACC <== MEM[{2}] ({3})", machine.ProgramCounter, Value, Operand, machine.Memory[Operand]);

            machine.Accumulator = machine.Memory[Operand];
        }
    }

    // ADD Add from memory to accumulator
    // The ADD instruction takes the contents of the accumulator, adds it to the 
    // contents of the memory location identified by the operand address and stores 
    // the sum into the accumulator. (ACC ← ACC + MEM[a])
    public class AddInstruction : OperandInstruction
    {
        public AddInstruction(int value) : base(value) { }
        public override void Execute(CardiacMachine machine)
        {
            Trace.TraceInformation("{0} {1} ACC <== ACC + MEM[{2}] ({3})", machine.ProgramCounter,Value, Operand, machine.Memory[Operand]);

            machine.Accumulator += machine.Memory[Operand];
        }
    }

    //TAC Test accumulator and jump if negative
    //The TAC instruction is the CARDIAC's only conditional branch instruction. 
    //It tests the accumulator, and if the accumulator is negative, then the 
    //PC is loaded with the operand address. Otherwise, the PC is not modified 
    //and the program continues with the instruction following the TAC. (If ACC < 0, PC ← a)

    public class TestAndJumpInstruction : OperandInstruction
    {
        public TestAndJumpInstruction(int value) : base(value) { }
        public override void Execute(CardiacMachine machine)
        {
            if(machine.Accumulator < 0)
            {
                Trace.TraceInformation("{0} {1} PC <== {2}", machine.ProgramCounter, Value, Operand);

                machine.ProgramCounter = Operand;
            }
            else
            {
                machine.ProgramCounter++;
                Trace.TraceInformation("{0} {1} Advancing Program Counter to {2}", machine.ProgramCounter, Value, machine.ProgramCounter);
            }
        }
    }

    //	SFT Shift accumulator
    // This instruction causes the accumulator to be shifted to the 
    // left by some number of digits and then back to the right some 
    // number of digits. The amounts by which it is shifted are shown 
    // above in the encoding for the SFT instruction. (ACC ← (ACC × 10^l) / 10^r)
    public class ShiftAccumulatorInstruction : OperandInstruction
    {
        public ShiftAccumulatorInstruction(int value) : base(value) { }
        public override void Execute(CardiacMachine machine)
        {
            int leftshift = (int) Math.Pow(10, this.Operand / 10);
            int rightshift = (int) Math.Pow(10, this.Operand % 10);

            machine.Accumulator *= leftshift;
            machine.Accumulator /= rightshift;
        }
    }


    //	OUT Write memory location to output card
    //The OUT instruction takes the contents of the memory location specified by the
    //operand address and writes them out to an output card. (OUTPUT ← MEM[a])
    public class OutputInstruction : OperandInstruction
    {
        public OutputInstruction(int value) : base(value) { }
        public override void Execute(CardiacMachine machine)
        {
            Trace.TraceInformation("{0} {1} OUTPUT <== MEM[{2}] ({3})", machine.ProgramCounter, Value, Operand, machine.Memory[Operand] );
          
            machine.Emit(Operand);
           
        }
    }

    //	STO Store accumulator to memory
    // This is the inverse of the CLA isntruction. The accumulator is
    // copied to the memory location given by the operand Operand. (MEM[a] ← ACC)
    public class StoreInstruction : OperandInstruction
    {
        public StoreInstruction(int value) : base(value) { }
        public override void Execute(CardiacMachine machine)
        {
            Trace.TraceInformation("{0} {1} MEM[{2}] <== ACC ({3})", machine.ProgramCounter, Value, Operand, machine.Accumulator);


            // The CARDIAC accumulator holds a signed 4 - digit number, which seems odd given 
            //that everything else is oriented around 3 - digit numbers.The manual includes the statement:
            //  Since CARDIAC's memory can store only 3-digit numbers, you may be puzzled by the inclusion
            //  of an extra square in the accumulator. It is there to handle the overflow that will result
            //  when two 3-digit numbers whose sum exceeds 999 are added.

            //What's not clear is under what conditions that overflow /carry digit is kept or discarded. 
            //From the discussion of the SFT instruction in Section 12 of the manual, exactly four digits 
            //are kept for the intermediate value between the left and right shift operations. However, the
            //manual doesn't state whether all four digits are kept between instructions nor what happens
            //when storing the accumulator to memory if the accumulator contains a number whose magnitude
            //is greater than 999.In the case of our simulator, we retain all four digits, effectively
            //implementing a 4 - digit ALU.However, when storing the accumulator to memory, we discard
            //the fourth digit. I.e.the number stored in memory is a mod 1000, 
            //where a is the contents of the accumulator.            
            machine.Memory[Operand] = machine.Accumulator % 1000;
        }
    }

    //	SUB Subtract memory from accumulator
    // In the SUB instruction the contents of the memory location identified by the
    // operand address is subtracted from the contents of the accumulator and the 
    // difference is stored in the accumulator. (ACC ← ACC − MEM[a])
    public class SubtractInstruction : OperandInstruction
    {
        public SubtractInstruction(int value) : base(value) { }
        public override void Execute(CardiacMachine machine)
        {
            Trace.TraceInformation("{0} {1} ACC <== [ACC ({2}) - MEM[{3}] ({4})", machine.ProgramCounter, Value, machine.Accumulator, Operand, machine.Memory[Operand]);
            machine.Accumulator -= machine.Memory[Operand];
        }
    }

    //	JMP Jump and save PC
    //The JMP instruction first copies the PC into the operand part of the 
    // instruction at address 99. So if the CARDIAC is executing a JMP 
    // instruction stored in memory location 42, then the value 843 
    // will be stored in location 99. Then the operand address is copied 
    // into the PC, causing the next instruction to be executed to be the one 
    // at the operand address. (MEM[99] ← 800 + PC; PC ← a)
    public class JumpInstruction : OperandInstruction
    {
        public JumpInstruction(int value) : base(value) { }
        public override void Execute(CardiacMachine machine)
        {
            int dest = 800 + machine.ProgramCounter + 1;
            Trace.TraceInformation("{0} {1} JMP MEM[99] <== {2}", machine.ProgramCounter, Value, dest);
            machine.Memory[99] = dest;

            Trace.TraceInformation("{0} {1} JMP PC <== {2}", machine.ProgramCounter, Value, Operand);
            machine.ProgramCounter = Operand;
        }
    }

    //	HRS Halt and reset
    //The HRS instruction halts the CARDIAC and puts the operand address into the PC. (PC ← a; HALT)
    public class HaltInstruction : OperandInstruction
    {
        public HaltInstruction(int value) : base(value) { }
        public override void Execute(CardiacMachine machine)
        {
            Trace.TraceInformation("{0} {1} PC <== {2}", machine.ProgramCounter, Value, Operand);
            machine.ProgramCounter = Operand;
            machine.Halt();
        }
    }
}
