using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
        int PCMax; //this is to stop a rom from running into the rest of ram.
        int SP; //Stack pointer
        Int16[] stack;
        byte[] ram;
        byte delayTimer = 0; //counts down when set, 1 per 60 frames.
        byte soundTimer = 0; //counts down when set, 1 per 60 frames. At 1? sounds a beep
        bool debug = false;
        bool disasm = false; //setting this to true will step through the code and disassemble what the program thinks the instruction is.
        bool[] display;
        byte[] v; // Registers V0 through VF; VF is also used for some checks such as carry.
        short I; //index register, used to point at locations in RAM
        string message; //used to send messages to the emu for printing to the console window.

        //Dictionary<int, Action> instructions = new Dictionary<int, Action>();

        //constructor - init
        public CPU()
        {
            Reset();
        }

        public void Reset()
        {
            PC = 0x200; //this should be the program entry point, probably around $200
            SP = 0;
            stack = new Int16[16];
            v = new byte[16];
            message = "";

            //init v
            for (int i = 0; i < 16; i++)
            {
                v[i] = 0;
            }

            //init I
            I = 0;
        }

        /// <summary>
        /// Loads the rom from filename into the ram
        /// </summary>
        /// <param name="filename"></param>
        public void LoadROM(string filename)
        {
            byte[] rom;

            rom = File.ReadAllBytes(filename);

            PCMax = PC + rom.Length - 1;
            Array.Copy(rom, 0, ram, 0x200, rom.Length);
        }

        /// <summary>
        /// Gets a reference of the RAM from the Emu so that changes can be made in either place.
        /// DOES NOT create a new set of RAM.
        /// </summary>
        /// <param name="newRAM"></param>
        public void InitRAM(ref byte[] newRAM)
        {
            ram = newRAM;
        }

        /// <summary>
        /// Sets a specific RAM address to a specific value
        /// </summary>
        /// <param name="index">The address of the byte to change</param>
        /// <param name="input">The value to store in the address</param>
        public void SetRAM(uint index, byte input)
        {
            ram[index] = input;
        }

        /// <summary>
        /// Gets a reference to the display (pixel flags) from the Emu.
        /// This is done so the CPU running the ROM can actually update the display.
        /// </summary>
        /// <param name="newDisplay"></param>
        public void InitDisplay(ref bool[] newDisplay)
        {
            display = newDisplay;
        }

        /// <summary>
        /// Returns the value stored in RAM[index].
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public byte GetRAM(uint index)
        {
            return ram[index];
        }

        //returns the program counter (this is a debuging thing)
        public int GetPC()
        {
            return PC;
        }

        /// <summary>
        /// Returns the value of the register v[index].
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public byte GetRegister(uint index)
        {
            return v[index];
        }

        // due to already having a loop for SFML graphics, this will get called by EMU once every loop
        public void EmulateCycle()
        {
            Fetch();
        }

        private void Fetch()
        {
            
            //get 2 bytes, sent the instruction to Decode()
            byte lo, hi;

            //TODO: confirm endianess, this could be reversed
            hi = ram[PC];
            lo = ram[PC + 1];

            PC += 2;

            Decode(hi, lo);
            
        }

        private void Decode(byte hi, byte lo)
        {
            //break down the opcode into its parts
            byte i = (byte)(hi >> 4 & 0xF);
            byte x = (byte)(hi & 0xF);
            byte y = (byte)(lo >> 4 & 0xF);
            byte N = (byte)(lo & 0xF);

            short opcode = (short)(hi << 8 | lo);

            
            if (disasm)
            {

            }
            else
            {
                switch (i)
                {
                    case 0x0:
                    {
                        switch (x)
                        {
                            case 0x0:
                            {
                                switch (y)
                                {
                                    case 0xE:
                                    {
                                        switch (N)
                                        {
                                            case 0x0:
                                            {
                                                clearScreen();
                                                break;
                                            }
                                            default:
                                            {
                                                message = $"Invalid numeral {N} for instruction 0x{i}{x}{y}{N}";
                                                break;
                                            }
                                        }
                                        break;
                                    }
                                    default:
                                    {
                                        message = $"Invalid argument y: {y}";
                                        break;
                                    }
                                }
                                break;
                            }
                            default:
                            {
                                message = $"Invalid arguement x: {x}";
                                break;
                            }
                        }
                        break;
                    }
                    case 0x1:
                    {
                        jump(x, y, N);
                        break;
                    }
                    case 0x2:
                    {
                        //call subroutine

                        break;
                    }
                    case 0x6:
                    {
                        setV(x, y, N);
                        break;
                    }
                    case 0x7:
                    {
                        incV(x, y, N);
                        break;
                    }
                    case 0xA:
                    {
                        setI(x, y, N);
                        break;
                    }
                    case 0xD:
                    {
                        draw(x, y, N);
                        break;
                    }
                    default:
                    {
                        message = $"Invalid instruction: {i}";
                        break;
                    }

                }
            }
            
        }

        public void SetMessage(string newmsg)
        {
            message = newmsg;
        }

        public string GetMessage()
        {
            return message;
        }

        private void clearScreen()
        {
            //00E0 - clear screen
            for (int dI = 0; dI < display.Length; dI++)
            {
                display[dI] = false;
            }
        }

        private void jump(byte x, byte y, byte N)
        {
            //JMP
            PC = x << 8 | y << 4 | N;
        }

        private void draw(byte x, byte y, byte N)
        {
            //draw sprite n pixels tall to x,y from location I
            //position is stored in register v[x], v[y]

            //if width and height change, change these
            uint dX = (uint)v[x] % 128; //128 is super chip width
            uint dY = (uint)v[y] % 64;  //64 is super chip height
            v[0xf] = 0;
            int maxHeight = I + N;

            for (uint dIY = 0; dIY < N; dIY++)
            {
                byte pixels = ram[I];

                for (uint dIX = 0; dIX < 8; dIX++)
                {
                    //example of draw code
                    //display[x + (y * width)];


                    if (display[(dIX + dX) + ((dIY + dY) * 128)])
                    {
                        //this pixel will be turning off, set the v[f] to 1 when done
                        display[(dIX + dX) + ((dIY + dY) * 128)] = ((byte)Math.Pow(2, 8 - (dIX + 1)) & pixels) == 0 ? true : false;
                        v[0xF] = 1;
                    }
                    else
                    {
                        display[(dIX + dX) + ((dIY + dY) * 128)] = ((byte)Math.Pow(2, 8 - (dIX + 1)) & pixels) == 0 ? false : true;
                    }


                    if (dIX > 63) break;
                }
                if (dIY > 31) break;

                if (I < maxHeight)
                {
                    I++;
                }
                else
                {
                    break; //done drawing
                }
            }
        }

        private void setV(byte x, byte y, byte N)
        {
            //set register Vx to yn
            v[x] = (byte)(y << 4 | N);
        }

        private void incV(byte x, byte y, byte N)
        {
            //add yn to Vx - does NOT set the carry flag
            v[x] += (byte)(y << 4 | N);
        }

        private void setI(byte x, byte y, byte N)
        {
            //set index register I to nnn
            I = (short)((x << 8) | (y << 4) | N);
        }
    }
}
