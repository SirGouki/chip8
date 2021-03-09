using System;

namespace chip8SFML
{
    class Program
    {
        public static void Main()
        {
            Emu emu = new Emu();

            emu.Update();
        }


    }
}
