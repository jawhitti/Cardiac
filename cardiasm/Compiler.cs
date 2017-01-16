using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using Cardiac.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace cardiasm
{
    public class Statement
    {
        public int LineNumber { get; set; }
        public OpCode OpCode { get; set; }
        public int Address { get; set; }
        public string Label { get; set; }

        //Operand is a string because it may refer to 
        //a variable and not a constant int value
        public string Operand { get; set; }
    }

    public class Variable
    {
        public int Address { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }

        public Variable()
        {
            Address = -1;
        }

    }

    public class Program
    {
        const int MIN_STATEMENT = 3;
        const int MAX_VAR = 98;
        const int MAX_ITEMS = 98; //can't override 0, can't override 99

        public string SourceFileName { get; set; }
        public List<Statement> Statements { get; set; }
        public Dictionary<string, Variable> Variables { get; set; }
        public int ReferencedVariableCount { get; set; }

        public void EmitStatement(int lineNumber, string label, OpCode opCode, string operand)
        {
            CheckAvailableMemory();
            Statement statement = new Statement() { LineNumber = lineNumber, Address = MIN_STATEMENT + Statements.Count, Label = label, OpCode = opCode, Operand = operand };
            Statements.Add(statement);
        }

        public void EmitDefinition(string identifier, string value)
        {
            if(!Variables.ContainsKey(identifier))
            {
                Variable var = new Variable() { Name = identifier, Value = value };
                Variables[identifier] = var;
            }
            else
            {
                throw new CompileException(string.Format("Variable '{0}' already defined", identifier));
            }
        }

        private int AvailableMemory
        {
            get
            {
                return MAX_ITEMS - Variables.Count - Statements.Count;
            }
        }

        public Program(cardiasmParser.ProgramContext ast) : this()
        {
            var visitor = new CardiasmVisitor(this);
            visitor.Visit(ast);
            ResolveVariables();
        }
        public Program()
        {
            Statements = new List<Statement>();
            Variables = new Dictionary<string, Variable>();
        }
        public void CheckAvailableMemory()
        {
            if (AvailableMemory <= 0)
            {
                throw new OutOfMemoryException(string.Format("Out of memory! ({0} statements and {1} variables)", Statements.Count, Variables.Count));
            }
        }

        public List<int> CreateExecutable()
        {
            List<int> retval = new List<int>();

            //Emit standard prologue
            retval.Add(2);
            retval.Add(800);

            for (int i = 0; i < Statements.Count; i++)
            {
                Statement statement = Statements[i];
                Instruction output = new Instruction(statement.OpCode, int.Parse(statement.Operand));
                
                retval.Add(statement.Address);
                retval.Add(output.ToInt32());
            }


            //Console.WriteLine("VARIABLES");
            //Console.WriteLine("---------");
            foreach (var var in Variables)
            {
                CheckAvailableMemory();

                if (var.Value.Address >= 0)
                {
                    retval.Add(var.Value.Address);
                    retval.Add(Resolve(var.Value.Value));
                }
            }

            //Add code to jump to entry point
            retval.Add(2);
            retval.Add(800 + Statements[0].Address);

            //Console.WriteLine("Statements:   {0}", Statements.Count + 4);

            var skippedVariables = Variables.Values.Where(v => v.Address < 0).Count();

            //Console.WriteLine("Variables:    {0}", Variables.Count - skippedVariables);
            //Console.WriteLine("Memory used:  {0}", (Statements.Count + 4 + Variables.Count) / 100.0);

            return retval;
        }
        private int Resolve(string identifier)
        {
            int retval = 0;
            if (int.TryParse(identifier, out retval))
            {
                return retval;
            }

            else if (Variables.ContainsKey(identifier))
                return Resolve(Variables[identifier].Value);

            else
                throw new CompileException("Unable to resolve identifier '{0}'", identifier);

        }

        public string CreateSymbols()
        {
            StringBuilder retval = new StringBuilder();

            retval.Append(string.Format("SOURCE:{0}\r\n", Path.GetFullPath(SourceFileName)));
  
            retval.Append("SYMBOLS\r\n");

            foreach (var variable in this.Variables)
            {
                retval.Append(string.Format("{0}:{1}\r\n", variable.Value.Address, variable.Value.Name));
            }

            retval.Append("STATEMENTS\r\n");

            foreach (var statement in this.Statements)
            {
                retval.Append(string.Format("{0}:{1}\r\n", statement.Address, statement.LineNumber));
            }


            return retval.ToString();
        }

        private void ResolveVariables()
        {
            for (int i = 0; i < Statements.Count; i++)
            {
                Statement s = Statements[i];

                int operandVal = 0;

                if (!int.TryParse(s.Operand, out operandVal))
                {
                    Variable targetVar = null;
                    Variables.TryGetValue(s.Operand, out targetVar);

                    if (targetVar != null)
                    {
                        if (targetVar.Address < 0)
                        {
                            targetVar.Address = MAX_VAR - ReferencedVariableCount;
                            ReferencedVariableCount++;
                        }

                        s.Operand = targetVar.Address.ToString();
                    }
                    else
                    {
                        //yes, that's right -- you can manipulate labels to
                        //make self-modifying code.  That's critical to enabling
                        //subroutines. Ordinary stack-based subroutine calls would
                        //be too expensive to implement.
                        var targetStatement = Statements.Where(o => o.Label == s.Operand).FirstOrDefault();
                        if (targetStatement != null)
                        {
                            s.Operand = targetStatement.Address.ToString();
                        }
                        else
                        {
                            throw new CompileException(string.Format("Unknown label '{0}'", s.Operand));
                        }

                    }
                }
            }
        }
    }

    internal class CardiasmVisitor : cardiasmBaseVisitor<Program>
    {
        Program TargetProgram = new Program();

        public CardiasmVisitor(Program target)
        {
            this.TargetProgram = target;
        }

        public override Program VisitDef([NotNull] cardiasmParser.DefContext context)
        {
            TargetProgram.EmitDefinition(context.GetChild(1).GetText(), context.GetChild(2).GetText());
            return TargetProgram;
        }

        Program VisitStatement(ParserRuleContext context, OpCode opCode)
        {
            string operand = "";
            string label = "";
            TerminalNodeImpl sourceLineContext;

            if (context.GetChild(0) is cardiasmParser.LabelContext)
            {
                label = context.GetChild(0).GetChild(0).GetText();
            }

            if (string.IsNullOrEmpty(label))
            {
                operand = context.GetChild(1).GetText();
                sourceLineContext = context.GetChild(1) as TerminalNodeImpl;

            }
            else
            {
                operand = context.GetChild(2).GetText();
                sourceLineContext = context.GetChild(2) as TerminalNodeImpl;
            }

            TargetProgram.EmitStatement(sourceLineContext.Payload.Line, label, opCode, operand);

            return TargetProgram;
        }
        public override Program VisitInp([NotNull] cardiasmParser.InpContext context)
        {
            return VisitStatement(context, OpCode.INP);
        }

        public override Program VisitCla([NotNull] cardiasmParser.ClaContext context)
        {
            return VisitStatement(context, OpCode.CLA);
        }

        public override Program VisitAdd([NotNull] cardiasmParser.AddContext context)
        {
            return VisitStatement(context, OpCode.ADD);
        }

        public override Program VisitTac([NotNull] cardiasmParser.TacContext context)
        {
            return VisitStatement(context, OpCode.TAC);
        }

        public override Program VisitSft([NotNull] cardiasmParser.SftContext context)
        {
            return VisitStatement(context, OpCode.SFT);
        }

        public override Program VisitOut([NotNull] cardiasmParser.OutContext context)
        {
            return VisitStatement(context, OpCode.OUT);
        }

        public override Program VisitSto([NotNull] cardiasmParser.StoContext context)
        {
            return VisitStatement(context, OpCode.STO);
        }

        public override Program VisitSub([NotNull] cardiasmParser.SubContext context)
        {
            return VisitStatement(context, OpCode.SUB);
        }

        public override Program VisitJmp([NotNull] cardiasmParser.JmpContext context)
        {
            return VisitStatement(context, OpCode.JMP);
        }

        public override Program VisitHrs([NotNull] cardiasmParser.HrsContext context)
        {
            return VisitStatement(context, OpCode.HRS);
        }
    }

    class Compiler
    {
        const int MAX_ADDRESS = 98;

        public Program Compile(string fileName)
        {
            var ast = Parse(fileName);
            Program program = Compile(ast);
            program.SourceFileName = fileName;
            return program;
        }

        public Program Compile(cardiasmParser.ProgramContext ast)
        {
            return new Program(ast);
        }


        private cardiasmParser.ProgramContext Parse(string fileName)
        {
            using (StreamReader fileStream = new StreamReader(fileName))
            {
                AntlrInputStream inputStream = new AntlrInputStream(fileStream);

                cardiasmLexer lexer = new cardiasmLexer(inputStream);
                CommonTokenStream commonTokenStream = new CommonTokenStream(lexer);
                cardiasmParser parser = new cardiasmParser(commonTokenStream);

                var ast = parser.program();
                return ast;
            }
        }

    }
}
