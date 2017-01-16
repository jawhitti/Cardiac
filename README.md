# Cardiac
C# / .NET implementation of the CARDIAC Cardboard computer

## Overview
This project is a compiler, runtime and debugger for the [CARDIAC - A **Cardboard Illustrative Aid to Computation**](https://en.wikipedia.org/wiki/CARDboard_Illustrative_Aid_to_Computation). 
The goal of the project is to implement a simple, but realistic compiler and interpreter.  The compiler implements a simple language I call CARDIASM which
is a simple assembly.  

## Compiling the code
The code is a Visual Studio 201S solution primarily providing three applications


## Applications

### cardiasm.exe
This compiler takes CARDIASM files as input and produces compiled images (".cardimg").  These images can be run via cardiac.exe or
debugged via cardb.exe.  The compiler will optionally produce a simple program database (".cardb") if you pass it a "/debug+" flag on the
command-line.  This application uses the ANTLR library to parse cardiasm source files and produces executable images as described 
[here](https://www.cs.drexel.edu/~bls96/museum/cardiac.html).
  
### cardiac.exe  
This is cardiac interpreter.  Most of the "real" code is in Cardiac.Core.dll; cardiac.exe mostly just accepts command-line parameters
and invokes the Interpreter.  The Interpreter is availalbe as a general-purpose class and is designed to be easily hosted and "tweaked".
cardiac.exe can optionally emit a profile trace via the "/profile+" switch.
  
### cardbg.exe
This is an interactive debugger. Right now it only supports single-stepping through the code.  If no .cardb file is found it will display
disassembly of the raw CARDIAC opcodes; otherwise it will display source code and an enhanced view of CARDIAC memory during debugging.

## Quick tour

The CARDIASM language is built around the opcodes defined in the [manual](https://www.cs.drexel.edu/~bls96/museum/CARDIAC_manual.pdf). 
However I did not want developers to worry about memory addresses so the language also allows DEF statements to define constants 
and variables.For instance, a very simple cardiac program to add 1+1 and output the result is shown below: This program defines
a and b as 1 then loads a into the Accumulator register and adds b.  The result is stored into "result" and result is then output. 
The final instruction (HRS 0) is a Halt-Reset which just ends the program.

```
DEF a 1  
DEF b a  
DEF result 0  
  
CLA a  
ADD b  
STO result  
OUT result  
HRS 0  
```

If you save the above to **add.cardiasm extension you can compile it via the command line below:
  
``
cardiasm /debug+ add.cardiasm
``
  
This will produce two files:
* **add.cardimg** - an executable image suitable for executing with cardiac.exe
* **add.cardb**   - a cardiac program database (roughly analogous to a ".pdb") useful for debugging

### Running the program
To run the program you can just run it like this:
```
C:\> cardiac add.cardimg
2
```
### Using the debugger
To debug the program run the debugger as follows:
```
cardbg add.cardimg
```


## Other Resources
The CARDIAC instruction Manual can be found [here](https://www.cs.drexel.edu/~bls96/museum/cardiac.html).
The best page for CARDIAC is probably [this page at cs.drexel.edu](https://www.cs.drexel.edu/~bls96/museum/cardiac.html)
