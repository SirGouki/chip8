# chip8SFML
A chip 8 emulator using C# and SFML.

# Compiling
Currently, until I figured out how to fix it, you must create a new C# Core 3.1 console project, and import these files into it to compile.
This also requires using NuGet Package Manager to install the requirements to that project (which should only be SFML.NET v2.5 from Laurent Gomila)

# Current Version
Now loads roms.  Currently, only enough opcodes to run the IBM Logo ROM are implemented.  Also, to change the rom, you have to change a line
in Emu.cs
Implemented CPU.
Implemented the following opcodes:
  0x00e0
  0x1NNN
  0x6XNN
  0x7XNN
  0xDXYN

Program will run the IBM Logo ROM, which iterates through drawing "sprites" until IBM is displayed, and then executes an endless jmp loop to $228 (that's how the rom is programmed)

Changed graphics mode from chip8 to super chip (from 64x32, to 128x64).  This is currently only editable by editing the source (change width and height in Emu.cs).
If you change this, there's a few areas where you'll need to change a magic number to match!

Implemented a RAM Viewer, complete with scrolling (PGUP and PGDN keys).
Implemented/Finished a message system that shows a message to the console, and indicates where or what is causing it (System, Debug, Error, or CPU).
Implemented SFML side of controls, controls are as follows:
F4 - close the emulator window (also tells the whole program to exit).
F12 - toggle the RAM viewer
PGUP - scroll the RAM Viewer up by 416 bytes (its what fit on the original design).
PGDN - scroll the RAM Viewer down by 416 bytes.
keys - chip 8 input
1 2 3 4 - 1 2 3 C
q w e r - 4 5 6 D
a s d f - 7 8 9 E
z x c v - A 0 B F

see: https://tobiasvl.github.io/blog/write-a-chip-8-emulator/#keypad

Implemented a variable pair to slow down emulation cycles.
Current setup is 1 emulation cycle per 60 frames. This was done to make it easier to see the draw function working.
Took care of some various bugs I caused.
