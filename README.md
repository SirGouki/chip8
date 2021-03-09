# chip8SFML
A chip 8 emulator using C# and SFML.

# Compiling
Currently, until I figured out how to fix it, you must create a new C# Core 3.1 console project, and import these files into it to compile.
This also requires using NuGet Package Manager to install the requirements to that project (which should only be SFML.NET v2.5 from Laurent Gomila)

# Current Version
Initial, just a bare bones skeleton that I set up to test the variables and such.  You can manually display things on the "screen" by setting the display bools
to true like the following:
`display[x + (y * 64)] = true;`

doing the following will display a 3x3 pixel T at screen pos 1,1:

```cpp
display[1 + (1 * 64)] = true;
display[2 + (1 * 64)] = true;
display[3 + (1 * 64)] = true;
display[2 + (2 * 64)] = true;
display[2 + (3 * 64)] = true;
```
