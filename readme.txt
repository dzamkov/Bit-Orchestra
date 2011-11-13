Bit Orchestra is a tool for making some combination of sound, noise and music with c expressions. It functions as a (very small) IDE for writing expressions and interpreting their output as a pcm waveform. To learn how to write programs for Bit Orchestra, look at the included samples.

Shortcuts:
* Ctrl-S	Save
* Ctrl-L	Load
* Ctrl-E	Export
* F5		Play/Stop/Update

Directives:
#rate		Sample rate
#length		Export length
#offset		Initial parameter (t) value
#resolution	Output resolution in bits

Binary Operations:
+	Add
-	Subtract
*	Multiply
/	Divide
%	Modulus
|	Bitwise Or
&	Bitwise And
^	Bitwise Xor
<<	Left shift
>>	Right shift

Unary Operations:
-	Negate
~	Complement
saw	Saw generator
sin	Sin generator
square	Square generator
tri	Triangle generator

Additional operations
[x, y, z]a	Sequencer

