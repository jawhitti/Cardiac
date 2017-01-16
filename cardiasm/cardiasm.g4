grammar cardiasm;

options {
    language=CSharp3;
}

@lexer::namespace{cardiasm}
@parser::namespace{cardiasm}


/*
 * Parser Rules
 */

program  :	(inp | cla | add | tac | sft | out | sto | sub | jmp | hrs | def | COMMENT)*;
label : IDENTIFIER ':' COMMENT*;
inp : label? 'INP' (NUMBER|IDENTIFIER) COMMENT?;
cla : label? 'CLA' (NUMBER|IDENTIFIER) COMMENT?;
add : label? 'ADD' (NUMBER|IDENTIFIER) COMMENT?;
tac : label? 'TAC' (NUMBER|IDENTIFIER) COMMENT?;
sft : label? 'SFT' (NUMBER|IDENTIFIER) COMMENT?;
out : label? 'OUT' (NUMBER|IDENTIFIER) COMMENT?;
sto : label? 'STO' (NUMBER|IDENTIFIER) COMMENT?;
sub : label? 'SUB' (NUMBER|IDENTIFIER) COMMENT?;
jmp : label? 'JMP' (NUMBER|IDENTIFIER) COMMENT?;
hrs : label? 'HRS' (NUMBER|IDENTIFIER) COMMENT?;
def : label? 'DEF' IDENTIFIER (NUMBER|IDENTIFIER) COMMENT?;


/*
 * Lexer Rules
 */
NUMBER  :  '-'?('0' .. '9')+;
NEWLINE : '\r\n' -> skip;
WS      : [ \t\r\n] -> skip ;
COMMENT :   '//' ~[\r\n]* '\r'? '\n';
IDENTIFIER : ('a' .. 'z' | 'A' .. 'Z' | '0' .. '9' | '_' )+;


