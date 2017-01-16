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

### The CARDIASM language

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

#### Notes
* Comments in CARDIASM begin with two slashes.  They can be standalone or at the end of a line. 
* Labels can be used anywhere and are denoted via labels.  For example the follow program displays
the values 1 through 100
```
/This program counts to 100

DEF n    100  
DEF cntr 000 

CLA	00		//Initialize the counter to 1 (address 00 always contains 1)
STO	cntr	

loop:
	CLA	n	//Load n into accumulator
	TAC	exit    //If n < 0, exit
	OUT	cntr	//Output cntr
	
	CLA	cntr	//Load cntr into accumulator
	ADD	00		//address 0 always holds 1 so this is a clever way to increment
	STO	cntr    //store counter
	
	CLA	n	    
	SUB	00      //Decrement n
	STO	n
	
	JMP	loop

exit:	
	HRS	00
```

* CARDIAC has only one register and no stack so self-modifying code can be useful.  For this reason the language
supports storing values to addresses denoted by labels. The following function from fizzbuzz.cardiasm uses this 
capability in the line STO divides_exit.  This provides a crude way of implementing subroutines by replacing
the "Halt-Reset" at the end of the function with a jump back to the calling location.

```
/////////////////////////////////////////////////////////////
//DIVIDES
//
// expects inputs in m and n
// returns true or false in the accumulator
////////////////////////////////////////////////////////////
divides_entry:
	CLA 99
	STO divides_exit
        ...
	HRS 0  //return from "divides"
```


### Running a program
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

You should see a display like the following:
```
 001 002 803 198 297 696 596 900 000 000 000 000 000 000 000 000 000 000 000 000
 000 000 000 000 000 000 000 000 000 000 000 000 000 000 000 000 000 000 000 000
 000 000 000 000 000 000 000 000 000 000 000 000 000 000 000 000 000 000 000 000
 000 000 000 000 000 000 000 000 000 000 000 000 000 000 000 000 000 000 000 000
 000 000 000 000 000 000 000 000 000 000 000 000 000 000 000 000 000 001 001 803
[ACC:  000] OUT:


   [1] DEF a 1
   [2] DEF b 1
   [3] DEF result 0
==>[4] CLA a
   [5] ADD b
   [6] STO result
   [7] OUT result
   [8] HRS 0
```

The top view displays the memory contents of the CARDIAC (all 100 memory cells) along with the accumulator
and any output.  The bottom display is a source code view.  The debugger is currently very limited; simply
press enter to single-step over the code. The debugger will use color to highlight certain areas, namely:
  
* **Dark Gray** for unused memory  
* **Yellow** for highlighting the current instruction
* **Light Gray** for memory cells containing code
* **Dark Magenta** for memory cells containing constants/variables
* **Light Magenta** to highlight the current Operand address if the current instruction references memory.
For example if the current instruction is "CLA a" and "a" is mapped to address 98 then address 98 will be
highlighted in magenta
* **Red** to highlight the most recently-changed value (so if you step-over you can easily see what changed
(if anything).
  
## Other Resources
The CARDIAC instruction Manual can be found [here](https://www.cs.drexel.edu/~bls96/museum/cardiac.html).
The best page for CARDIAC is probably [this page at cs.drexel.edu](https://www.cs.drexel.edu/~bls96/museum/cardiac.html)
