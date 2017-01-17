# Cardiac
C# / .NET implementation of the CARDIAC Cardboard computer

## Overview
This project is a compiler, runtime and debugger for the [CARDIAC - A **Cardboard Illustrative Aid to Computation**](https://en.wikipedia.org/wiki/CARDboard_Illustrative_Aid_to_Computation). 
The goal of the project is to implement a simple, but realistic compiler,interpreter and debugger. 
  

## Compiling the code
The code is a Visual Studio 201S solution primarily providing three applications described below.  You should be able
to just open the project and build (Ctrl-Shift-B). Binaries will all be sent to a "bin" folder under the root directory
of the solution.


## Applications

### cardiasm.exe
**cardiasm.exe** is a compiler for a simple language I call CARDIASM. CARDIASM is a simple assembly language derived from the 
CARDIAC instruction set.  Sadly the limitations of the CARDIAC hardware platform preclude porting a more common
language like C.  

The compiler transforms source files (".cardiasm") int compiled binaries(".cardimg").  These images can be executed via **cardiac.exe** or debugged via **cardb.exe**.  The compiler will optionally produce a simple program database (".cardb") if you pass it a "/debug+" flag on the command line, which will enable integrated source debugging.  See the Quick Tour below for more information on the
CARDIASM language

#### Implementation notes
**cardiasm** uses the ANTLR library to parse cardiasm source files. Executable images follow the format described  
[here](https://www.cs.drexel.edu/~bls96/museum/cardiac.html).
  
### cardiac.exe  
**cardiac.exe** is the cardiac interpreter (analogous to "java.exe")  Most of the "real" code is in Cardiac.Core.dll; 
cardiac.exe mostly just accepts command-line parameters and invokes the Interpreter it contains.  The Interpreter 
is available as a general-purpose class and is designed to be easily hosted and "tweaked" in other host applications.
cardiac.exe can optionally emit a profile trace via the "/profile+" switch.  Currently there are no tools that consume
this profile information but it should be straightforward to build one.
  
### cardbg.exe
**cardbg.exe** is a simple interactive debugger. Right now it only supports single-stepping through the code.  
The debugger displays the contents of the memory along with a disassembly of the raw CARDIAC opcodes. Pressing ENTER
single-steps over the source code.
  
If a program database (.cardb) file is located the memory display is enhanced and the disassembly view is 
replaced with an enhanced "source code" view. 

## Quick tour

### The CARDIASM language

The CARDIASM language is built around the opcodes defined in the [manual](https://www.cs.drexel.edu/~bls96/museum/CARDIAC_manual.pdf). 
An important design goal was making it possible to develop code without worrying about address allocation, so the
language adds a few familiar "C-like" features (defining constants, variables and labels).

A very simple cardiac program to add 1+1 and output the result is shown below: This program defines
three variables (*a*, *b* and *result*). *a* is defined as 1 and *b* is defined as *a* (so, 1 by transitivity). 
  
The CLA instruction is the first executable statement in the program. CLA loads *a* into the Accumulator register; the 
ADD instruction adds *b* to *a* and stores it in the accumulator.  The CARDIAC can't output directly from the accumulator
so "STO result" saves the result into a variable that can be passed to the OUT instruction. The final instruction (HRS 0)
is a "Halt and Reset" which just ends the program.

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

#### Definitions
DEF statements can appear anywhere in the program.  They can reference each other.  For boolean operations **true** can be 
defined as 1 but I recommend defining **false** as -1 because of the way the TAC instruction works.

```
DEF true 1
DEF false -1
DEF a true
DEF b false
```
CARDIASM does not distinguish between constant values and variables.  However the compiler is 
smart enough to omit un-referenced definitions from the compiled image so you can define variables
liberally.  CARDIASM has no notion of scope; all definitions are visible everywhere.
  
(Note that expressions are not currently allowed as rvalues so expressions like "DEF c a+b" are not accepted). 

#### Comments
CARDIASM uses C++/Java-style comments. Comments can be standalone or at the end of a line, exactly like most "C-like" languages.

#### Labels
Labels are critical for any code with loops. The CARDIAC lacks a native stack, so labels are also important
for implementing subroutines. 

Labels can be places anywhere and are denoted via the familiar C syntax of "<label>: statement".   
  
The follow program displays the values 1 through 100 and uses labels as targets for the JMP and 
TAC statements. 
```
//This program counts to 100

DEF n    100  
DEF cntr 000 

CLA	00              //Initialize the counter to 1 (address 00 always contains 1)
STO	cntr	

loop:
	CLA	n       //Load n into accumulator
	TAC	exit    //If n < 0, exit
	OUT	cntr    //Output cntr
	
        // cntr++
	CLA	cntr    
	ADD	00      //clever way to implement++ (00 always contains the value 1)
	STO	cntr    
	
        // n--
	CLA	n	    
	SUB	00
	STO	n
	
	JMP	loop

exit:	
	HRS	00
```
  
#### Subroutines and Self-modifying code 
CARDIAC has only one register and no stack so subroutines are useful.  One way to implement them is
via self-modifying code. CARDIASM helps enable this via a curious construct - the language
supports storing values to *labels*.  (This construct is very similar to POKE in old-school platforms).

The following function from fizzbuzz.cardiasm demonstrates this usage in the line **STO divides_exit**.  This
line takes the value from the accumulator (an address, presumably) and stores it at the location labeled
"divides_exit".  That location contains a "Halt and reset by default" but after the STO operation the code
will be patched to hold something else (presumably a JMP instruction jumping back to the calling location.
  

```
divides_entry:
	CLA 99
	STO divides_exit //override the "HRS" at divides_exit with a jump to the return address
                         //stored in memory location 99

        ...

divides_exit:
	HRS 0  //This will be a JMP instruction by the time it executes
```

### Compiling programs
If you save the first example above to **add.cardiasm** you can compile it via the command line below:
  
``
cardiasm /debug+ add.cardiasm
``
  
This will produce two files:
* **add.cardimg** - an executable image suitable for executing with cardiac.exe
* **add.cardb**   - a cardiac program database (roughly analogous to a ".pdb") useful for debugging

The actual executable image will be in the form described at 
[the Drexel page](https://www.cs.drexel.edu/~bls96/museum/cardiac.html). **.cardimg** files are 
just text files where each line holds a single value (I chose text over binary because it's 
easier to read and manipulate). 


### Running a program
To run the program you can just pass the name of the compiled image to cardiac.exe:
```
C:\> cardiac add.cardimg
2
```

#### I/O
The CARDIAC is not very well-suited for interactive I/O.  The only I/O instruction is 
a blocking read, and the CARDIAC has no way to deal with text.  **cardiac.exe** supports 
passing arguments on the command-line.  It is up to you to pass the right number.  If
a program tries to read input and none is available cardiac.exe will crash with an
InvalidOperationException and the text "The Program requires input but none is available".
  
The following program expects two arguments and outputs their sum:
```
DEF a 1
DEF b 1
DEF result 0

INP a
INP b

CLA a
ADD b
STO result
OUT result
HRS 0
```

To run this program pass a and b on the command line:
```
C:\> cardiac add.cardimg 1 1 
2
C:\> cardiac add.cardimg 2 3 
5
C:\> cardiac add.cardimg 2 
Unhandled Exception: System.InvalidOperationException: The program requires input but no input available

```
  
**cardiac.exe** sends all output to the console. If you want something more elaborate you can host
the Interpreter in your own process and handle the *Output* event.  (See the source code to cardiac.exe
for an example).
  
### Using the debugger
To debug the program run the debugger as follows:
```
C:\> cardbg add.cardimg
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
