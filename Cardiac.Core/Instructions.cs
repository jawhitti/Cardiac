using System;

namespace Cardiac.Core
{
    public enum OpCode
    {
        INP = 0,
        CLA = 1,
        ADD = 2,
        TAC = 3,
        SFT = 4,
        OUT = 5,
        STO = 6,
        SUB = 7,
        JMP = 8,
        HRS = 9
    };

    public struct Instruction
    {
        int _Value;
        int Value { get { return _Value; }
            set
            {
                Validate(value);
                _Value = value;
            }
        }

        public Instruction(int value)
        {
            Validate(value);
            _Value = value;
        }

        public Instruction(OpCode opcode, int operand)
        {
            int value = ((int)opcode) * 100 + operand;
            Validate(value);
            this._Value = value;
        }

        private static void Validate(int instruction)
        {
            if (instruction < 0 || instruction >= 1000)
                throw new ArgumentOutOfRangeException(string.Format("'{0}' is not a valid instruction", instruction));

        }
        public int Operand
        {
            get
            {
                return Value % 100;
            }
        }

        public OpCode Opcode
        {
            get
            {
                return (OpCode)(Value / 100);
            }
        }

        public override string ToString()
        {
            return String.Format("{0} {1}", this.Opcode, this.Operand);
        }

        public int ToInt32()
        {
            return _Value;
        }
    }
}
