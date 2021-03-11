# chip8SFML
A chip 8 emulator using C# and SFML.

# Compiling
Currently, until I figured out how to fix it, you must create a new C# Core 3.1 console project, and import these files into it to compile.  
This also requires using NuGet Package Manager to install the requirements to that project (which should only be SFML.NET v2.5 from Laurent Gomila)  

# Loading a ROM
place the .ch8 file in the same folder as your .exe  
Change line 185 in Emu.cs:  
```csharp
string romName = "romName.ch8";
```
Recompile and run the .exe

# Controls
Chip8:
```   
keyboard - chip8 keypad  
1 2 3 4  - 1 2 3 C  
q w e r  - 4 5 6 D  
a s d f  - 7 8 9 E  
z x c v  - A 0 B F  
```
Note that these can (and usually should) be held down.


Emulator specific:  
PGUP, PGDN - scroll the RAM Viewer up or down respectively (per press, holding wont work)  
F4 - Exit the emulator  
F12- toggle the RAM viewer  
P  - pause the emulator.  This literally just prevents cpu.EmulatorCycle() from getting called,   
	and any messages that are pending from the CPU will NOT be displayed to the console, and  
	the soundTimer will not be checked.  Press again to unpause.  
	WARNING: the way this is currently implemented, this *should* cause a sound that's played  
	to play again after unpausing, if soundTimer was > 0 when pausing.  



# Current Version
Implemented some classes to programmatically generate a byte array that contains a wav sound.  
Implemented a class to generate a sine wave.  
Added sound to the emulator.  This properly checks the soundTimer from the CPU, and once it is 0, sets playing to false and stops the beep.  
Commented out the CPU message "BEEP!" - since sound works this is no longer necessary.  Sound is played through SFML functions, not the  
.NET builtin system functions.  

Known issues:  
Collision is wonky on breakout (1979 version), the ball will sometimes collide with blocks that   
have been removed and put blocks in their place.