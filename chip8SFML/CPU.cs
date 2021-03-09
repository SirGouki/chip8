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
        List<Int16> stack;
        byte[] ram;
        byte delayTimer = 0; //counts down when set, 1 per 60 frames.
        byte soundTimer = 0; //counts down when set, 1 per 60 frames. At 1? sounds a beep
        bool debug = false;
        bool disasm = false; //setting this to true will step through the code and disassemble what the program thinks the instruction is.
        bool[] display;
        byte[] v; // Registers V0 through VF; VF is also used for some checks such as carry.
        short I; //index register, used to point at locations in RAM
        string message; //used to send messages to the emu for printing to the console window.
        bool superChip = true;
        //Dictionary<int, Action> instructions = new Dictionary<int, Action>();

        bool[] pressed = new bool[16];

        //constructor - init
        public CPU()
        {
            for (int i = 0; i < 16; i++)
            {
                pressed[i] = false;
            }

            Reset();
        }

        public void Reset()
        {
            PC = 0x200; //this should be the program entry point, probably around $200
            SP = 0;
            stack = new List<Int16>();
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

        public void SetSCHIP(ref bool schip)
        {
            superChip = schip;
        }

        public void SetPressed(uint index, bool press)
        {
            pressed[index] = press;
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
            if (delayTimer > 0)
            {
                delayTimer--;
            }

            if (soundTimer > 0)
            {
                soundTimer--;
                message = "BEEP!";
            }

            //get 2 bytes, sent the instruction to Decode()
            byte lo, hi;

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

            //for debugging
            //message = $"Last Opcode: {opcode.ToString("x4")}";

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
                                switch (lo)
                                {
                                    case 0xE0:
                                    {
                                        clearScreen();
                                        break;
                                    }
                                    case 0xEE:
                                    {
                                        ret();
                                        break;
                                    }
                                    default:
                                    {
                                        message = $"{opcode.ToString("X4")}: Invalid machine code: {lo.ToString("x2")}";
                                        break;
                                    }
                                }
                                break;
                            }
                            default:
                            {
                                message = $"{opcode.ToString("X4")}: Invalid arguement x: {x.ToString("X1")}";
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
                        callSub(x, y, N);
                        break;
                    }
                    case 0x3:
                    {
                        //skip if vx == nn
                        skipIfEQ(x, y, N);
                        break;
                    }
                    case 0x4:
                    {
                        //skip if vx != nn
                        skipIfNE(x, y, N);
                        break;
                    }
                    case 0x5:
                    {
                        //skip if vx == vy
                        skipIfRegEQ(x, y);
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
                    case 0x8:
                    {
                        switch (N)
                        {
                            case 0x0:
                            {
                                setV(x, y);
                                break;
                            }
                            case 0x1:
                            {
                                setVOR(x, y);
                                break;
                            }
                            case 0x2:
                            {
                                setVAND(x, y);
                                break;
                            }
                            case 0x3:
                            {
                                setVXOR(x, y);
                                break;
                            }
                            case 0x4:
                            {
                                setVADD(x, y);
                                break;
                            }
                            case 0x5:
                            {
                                setVSubXY(x, y);
                                break;
                            }
                            case 0x6:
                            {
                                RightShiftX(x, y);
                                break;
                            }
                            case 0x7:
                            {
                                setVSubYX(x, y);
                                break;
                            }
                            case 0xE:
                            {
                                LeftShiftX(x, y);
                                break;
                            }
                            default:
                            {
                                message = $"Invalid opcode: {opcode.ToString("X4")}";
                                break;
                            }
                        }

                        break;
                    }
                    case 0x9:
                    {
                        //skip if vx != vy
                        skipIfRegNE(x, y);
                        break;
                    }
                    case 0xA:
                    {
                        setI(x, y, N);
                        break;
                    }
                    case 0xB:
                    {
                        jumpOffset(x, y, N);
                        break;
                    }
                    case 0xC:
                    {
                        rand(x, y, N);
                        break;
                    }
                    case 0xD:
                    {
                        draw(x, y, N);
                        break;
                    }
                    case 0xE:
                    {
                        switch (lo)
                        {
                            case 0x9e:
                            {
                                skipIfPressed(x);
                                break;
                            }
                            case 0xA1:
                            {
                                skipIfNotPressed(x);
                                break;
                            }
                            default:
                            {
                                message = $"{opcode.ToString("X4")} : Invalid argument {lo.ToString("X2")}";
                                break;
                            }
                        }

                        break;
                    }
                    case 0xF:
                    {
                        switch (lo)
                        {
                            case 0x07:
                            {
                                getDelay(x);
                                break;
                            }
                            case 0x0A:
                            {
                                blockInput(x);
                                break;
                            }
                            case 0x15:
                            {
                                setDelay(x);
                                break;
                            }
                            case 0x18:
                            {
                                setSound(x);
                                break;
                            }
                            case 0x1E:
                            {
                                incI(x);
                                break;
                            }
                            case 0x29:
                            {
                                getChar(x);
                                break;
                            }
                            case 0x33:
                            {
                                storeBCD(x);
                                break;
                            }
                            case 0x55:
                            {
                                store(x);
                                break;
                            }
                            case 0x65:
                            {
                                load(x);
                                break;
                            }
                            default:
                            {
                                message = $"{opcode.ToString("X4")} : Invalid argument {lo.ToString("X2")}";
                                break;
                            }
                        }

                        break;
                    }
                    default:
                    {
                        message = $"Invalid instruction: {opcode.ToString("X4")}";
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

        private void jumpOffset(byte x, byte y, byte N)
        {
            // WARNING: the superchip behavior is abnormal
            // the commented out code is how the superchip behaves normally

            /*
             * PC = ((x << 8) | (y  << 4) | N) + v[x];
             */

            PC = v[0] + ((x << 8) | (y << 4) | N);

        }

        private void draw(byte x, byte y, byte N)
        {
            //draw sprite n pixels tall to x,y from location I
            //position is stored in register v[x], v[y]

            //if width and height change, change these
            uint dX = (uint)v[x] % 128; //128 is super chip width
            uint dY = (uint)v[y] % 64;  //64 is super chip height
            v[0xf] = 0;
            
            for (uint dIY = 0; dIY < N; dIY++)
            {
                byte pixels = ram[I+dIY];

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
                        //v[0xF] = 0;
                    }


                    if (dIX+dX > 127) break;
                }
                if (dIY+dY > 63) break;

                
            }
        }

        /// <summary>
        /// sets the register v[x] to the value yN
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="N"></param>
        private void setV(byte x, byte y, byte N)
        {
            //set register Vx to yn
            v[x] = (byte)(y << 4 | N);
        }

        /// <summary>
        /// sets register v[x] to the value of register v[y]
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        private void setV(byte x, byte y)
        {
            v[x] = v[y];
        }

        private void setVADD(byte x, byte y)
        {
            bool setCarry = false;

            if (v[x] + v[y] > 255)  setCarry = true;
            
            v[x] += v[y];

            v[0xF] = (byte)((setCarry) ? 1 : 0);
        }

        private void setVOR(byte x, byte y)
        {
            v[x] |= v[y];
        }

        private void setVAND(byte x, byte y)
        {
            v[x] &= v[y];
        }

        private void setVXOR(byte x, byte y)
        {
            v[x] ^= v[y];
        }

        private void setVSubXY(byte x, byte y)
        {
            bool setCarry = false;

            if (v[x] > v[y]) setCarry = true;
            
            v[x] -= v[y];

            v[0xF] = (byte)((setCarry) ? 1 : 0);
        }

        private void setVSubYX(byte x, byte y)
        {
            bool setCarry = false;
            if (v[y] > v[x]) 
                setCarry = true;
            

            v[x] = (byte)(v[y] - v[x]);

            v[0xF] = (byte)((setCarry) ? 1 : 0);
        }


        private void LeftShiftX(byte x, byte y)
        {
            if (!superChip)
            {
                v[x] = v[y];
            }

            byte c = (byte)((v[x] >> 7) & 0b1);
            v[x] = (byte)(v[x] << 1);
            v[0xF] = c;

        }

        private void RightShiftX(byte x, byte y)
        {
            if (!superChip)
            {
                v[x] = v[y];
            }

            byte c = (byte)(v[x] & 0b1);
            v[x] = (byte)(v[x] >> 1);
            v[0xF] = c;
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

        private void callSub(byte x, byte y, byte N)
        {
            //store current pc in the stack
            stack.Add((Int16)PC);

            //jump to NNN
            PC = (x << 8) | (y << 4) | N;
        }

        private void ret()
        {
            PC = stack[stack.Count - 1];
            stack.RemoveAt(stack.Count - 1);
        }

        private void skipIfEQ(byte x, byte y, byte N)
        {
            if (v[x] == ((y << 4) | N))
            {
                PC += 2;
            }
        }

        private void skipIfNE(byte x, byte y, byte N)
        {
            if (v[x] != ((y << 4) | N))
            {
                PC += 2;
            }
        }

        private void skipIfRegEQ(byte x, byte y)
        {
            if (v[x] == v[y])
            {
                PC += 2;
            }
        }

        private void skipIfRegNE(byte x, byte y)
        {
            if (v[x] != v[y])
            {
                PC += 2;
            }
        }

        private void rand(byte x, byte y, byte N)
        {
            //generate a random number
            Random r = new Random();

            byte ran = (byte)r.Next(0, 256);

            v[x] = (byte)(ran & ((y << 4) | N));
        }

        private void skipIfPressed(byte x)
        {
            //if the key in v[x] is pressed, increment PC by 2
            if (pressed[v[x]])
            {
                PC += 2;
            }
        }

        private void skipIfNotPressed(byte x)
        {
            //if the key in v[x] is not pressed, increment PC by 2
            if (!pressed[v[x]])
            {
                PC += 2;
            }
        }

        private void getDelay(byte x)
        {
            v[x] = delayTimer;
        }

        private void setDelay(byte x)
        {
            delayTimer = v[x];
        }

        private void setSound(byte x)
        {
            soundTimer = v[x];
        }

        private void incI(byte x)
        {
            I += v[x];
        }

        private void blockInput(byte x)
        {
            bool key = false;

            for (int i = 0; i < 16; i++)
            {
                key = pressed[i];

                if (pressed[i])
                {
                    v[x] = (byte)i;
                    break;
                }
            }

            if (!key)
            {
                //block if no key pressed
                PC -= 2;
            }
        }

        private void getChar(byte x)
        {
            I = (byte)((v[x] * 0xF)+0x32);
        }

        private void storeBCD(byte x)
        {
            int a, b, c;

            a = (v[x] / 100);
            b = ((v[x] / 10) % 10);
            c = (v[x] % 10);


            ram[I] = (byte)a;
            ram[I + 1] = (byte)b;
            ram[I + 2] = (byte)c;
        }

        private void store(byte x)
        {
            if (x > 16) x = 16;

            //store from v0 to vx INCLUSIVE into ram at I+index
            if(superChip)
            {   
                for(int i = 0; i <= x; i++)
                {
                    ram[I + i] = v[i];
                }
            }
            else
            {
                //do the same thing, but increment I instead of using the iterator
                for(int i = 0; i <= x; i++)
                {
                    ram[I] = v[i];
                    I++;
                }
            }
        }

        private void load(byte x)
        {
            if (x > 16) x = 16;

            //load from ram[I+index] INCLUSIVE into v[0] to v[x]
            if (superChip)
            {
                for (int i = 0; i <= x; i++)
                {
                    v[i] = ram[I + i];
                }
            }
            else
            {
                //do the same thing, but increment I instead of using the iterator
                for (int i = 0; i <= x; i++)
                {
                    v[i] = ram[I];
                    I++;
                }
            }
        }
    }
}
