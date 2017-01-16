using System;
using System.Text;

namespace Cardiac.Core
{
    public class MemoryEventArgs : EventArgs
    {
        public int Address { get; set; }
    }

    public class MemoryUnit
    {
        public event EventHandler<MemoryEventArgs> Changing;
        public event EventHandler<MemoryEventArgs> Changed;

        public int[] _Contents = new int[100];

        public MemoryUnit()
        {
            _Contents[0] = 1;
        }

        public int this[int idx]
        {
            get
            {
                return _Contents[idx];
            }
            set
            {
                if (idx > 0)
                {
                    var arg = new MemoryEventArgs() { Address = idx };
                    Changing?.Invoke(this, arg );
                    _Contents[idx] = value;
                    Changed?.Invoke(this, arg );
                }
                else
                {
                    //ignore or throw an exception
                }
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    int val = this[i * 10 + j];
                    string s = string.Format("{0:000}", val);
                    if (val >= 0)
                    {
                        s = " " + s;
                    }
                    sb.Append(s);
                }

                sb.Append(Environment.NewLine);
            }

            return sb.ToString();
        }
    }
}
