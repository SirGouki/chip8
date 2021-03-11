# chip8SFML
A chip 8 emulator using C# and SFML.

# Compiling
Currently, until I figured out how to fix it, you must create a new C# Core 3.1 console project, and import these files into it to compile.
This also requires using NuGet Package Manager to install the requirements to that project (which should only be SFML.NET v2.5 from Laurent Gomila)

# Current Version
Implemented some classes to programmatically generate a byte array that contains a wav sound.
Implemented a class to generate a sine wave.
Added sound to the emulator.  This properly checks the soundTimer from the CPU, and once it is 0, sets playing to false and stops the beep.
Commented out the CPU message "BEEP!" - since sound works this is no longer necessary.  Sound is played through SFML functions, not the
.NET builtin system functions.

Known issues:
Collision is wonky on breakout (1979 version), the ball will sometimes collide with blocks that 
have been removed and put blocks in their place.