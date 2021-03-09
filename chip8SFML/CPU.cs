using System;
using System.Collections.Generic;
using System.Text;

namespace chip8SFML
{
    /// <summary>
    /// This class handles the CPU operations...
    /// May need to move RAM to here
    /// </summary>
    
    class CPU
    {
        //vars
        int PC; //Program Counter
        int SP; //Stack pointer
        Int16[] stack;
        byte[] ram;

        //constructor - init
        public CPU()
        {
            Reset();
        }

        public void Reset()
        {
            PC = 0; //this should be the program entry point, probably around $200
            SP = 0;
            stack = new Int16[16];
        }

        public void SetRAM(ref byte[] newRAM)
        {
            ram = newRAM;
        }

        public void SetRAM(uint index, byte input)
        {
            ram[index] = input;
        }

        public byte GetRAM(uint index)
        {
            return ram[index];
        }
    }
}
