# chip8SFML
A chip 8 emulator using C# and SFML.

# Compiling
Currently, until I figured out how to fix it, you must create a new C# Core 3.1 console project, and import these files into it to compile.
This also requires using NuGet Package Manager to install the requirements to that project (which should only be SFML.NET v2.5 from Laurent Gomila)

# Current Version
Fully implemented CPU instructions (that I know about).
Can now play games!

Known issues:
Collision is wonky on breakout (1979 version), the ball will sometimes collide with blocks that 
have been removed and put blocks in their place.