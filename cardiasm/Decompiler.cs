using Cardiac.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cardiac.Core
{
    class Decompiler
    {
        //002
        //800
        // .
        // .	address-data card pairs
        // .
        //002
        //8xx where xx is address of the first instruction
        // .
        // .data cards
        // .

        //TODO: It would be nice if we could feed symbols to this
        //and get a better-fidelity program
        public static string Decompile(IEnumerable<int> binary)
        {
            StringBuilder retval = new StringBuilder();

            var rawInstructions = binary.ToList();

            if (rawInstructions[0] == 2 && rawInstructions[1] == 800)
            {
                int firstInstruction = 2;
                int lastInstruction = rawInstructions.Count - 1;

                for (int i = firstInstruction; i < lastInstruction; i += 1)
                {
                    if (rawInstructions[i] < 100 && rawInstructions[i] != 2)
                    {
                        //This is an INP so the next card should be an instruction...
                        Instruction parsed = new Instruction(rawInstructions[i + 1]);
                        retval.Append(parsed.ToString());
                        i++;
                    }
                    else if (rawInstructions[i] == 2 &&
                        rawInstructions[i + 1] / 100 == 8 &&
                        rawInstructions[i + 1] % 100 > 0)
                    {
                        lastInstruction = i - 1;
                        break;
                    }
                }

                //everything after lastInstruction is just read out as DATA

                if (rawInstructions.Count > lastInstruction + 3)
                {
                    retval.Append(Environment.NewLine);
                    retval.Append("DATA:");
                    retval.Append(Environment.NewLine);
                    for (int i = lastInstruction + 3; i < rawInstructions.Count; i++)
                    {
                        retval.Append(string.Format("DAT {0}{1}", rawInstructions[i], Environment.NewLine));
                    }
                }
            }
            else
            {
                retval.Append("Invalid image!");
            }

            return retval.ToString();
        }
    }
}
