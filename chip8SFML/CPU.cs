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
        bool[,] display;
        byte[] v; // Registers V0 through VF; VF is also used for some checks such as carry.
        short I; //index register, used to point at locations in RAM
        string message; //used to send messages to the emu for printing to the console window.
        bool superChip = true;
        Dictionary<int, Delegate> instructions = new Dictionary<int, Delegate>();

        bool[] pressed = new bool[16];

        //constructor - init
        public CPU()
        {
            for (int i = 0; i < 16; i++)
            {
                pressed[i] = false;
            }

            //load the dictionary
            //no args
            instructions[0x00E0] = new Action(clearScreen);
            instructions[0x00EE] = new Action(ret);

            //1 arg
            instructions[0xE09E] = new Action<byte>(skipIfPressed);
            instructions[0xE0A1] = new Action<byte>(skipIfNotPressed);
            instructions[0xF007] = new Action<byte>(getDelay);
            instructions[0xF00A] = new Action<byte>(blockInput);
            instructions[0xF015] = new Action<byte>(setDelay);
            instructions[0xF018] = new Action<byte>(setSound);
            instructions[0xF01E] = new Action<byte>(incI);
            instructions[0xF029] = new Action<byte>(getChar);
            instructions[0xF033] = new Action<byte>(storeBCD);
            instructions[0xF055] = new Action<byte>(store);
            instructions[0xF065] = new Action<byte>(load);

            //2 arg
            instructions[0x5000] = new Action<byte, byte>(skipIfRegEQ);
            instructions[0x8000] = new Action<byte, byte>(setV2V);
            instructions[0x8001] = new Action<byte, byte>(setVOR);
            instructions[0x8002] = new Action<byte, byte>(setVAND);
            instructions[0x8003] = new Action<byte, byte>(setVXOR);
            instructions[0x8004] = new Action<byte, byte>(setVADD);
            instructions[0x8005] = new Action<byte, byte>(setVSubXY);
            instructions[0x8006] = new Action<byte, byte>(RightShiftX);
            instructions[0x8007] = new Action<byte, byte>(setVSubYX);
            instructions[0x800E] = new Action<byte, byte>(LeftShiftX);
            instructions[0x9000] = new Action<byte, byte>(skipIfRegNE);

            //3 arg
            instructions[0x1000] = new Action<byte, byte, byte>(jump);
            instructions[0x2000] = new Action<byte, byte, byte>(callSub);
            instructions[0x3000] = new Action<byte, byte, byte>(skipIfEQ);
            instructions[0x4000] = new Action<byte, byte, byte>(skipIfNE);
            instructions[0x6000] = new Action<byte, byte, byte>(setV2Value);
            instructions[0x7000] = new Action<byte, byte, byte>(incV);
            instructions[0xA000] = new Action<byte, byte, byte>(setI);
            instructions[0xB000] = new Action<byte, byte, byte>(jumpOffset);
            instructions[0xC000] = new Action<byte, byte, byte>(rand);
            instructions[0xD000] = new Action<byte, byte, byte>(draw);

            Reset();
        }

        private void Decode(byte hi, byte lo)
        {
            //break down the opcode into its parts
            byte i = (byte)(hi >> 4 & 0xF);
            byte x = (byte)(hi & 0xF);
            byte y = (byte)(lo >> 4 & 0xF);
            byte N = (byte)(lo & 0xF);

            UInt16 opcode = 0;
            opcode = (UInt16)(hi << 8 | lo);

            UInt16 nOpcode = 0; //normalized opcode


            if (i == 0)
            {
                //expected format: 0xIXYN - no args
                nOpcode = opcode;
            }
            else if (i == 0xE || i == 0xF)
            {
                //expected format: 0xI0YN - 1 arg
                nOpcode = (UInt16)((i << 12) | (0 << 8) | (y << 4) | (N));
            }
            else if (i == 0x5 || i == 0x8 || i == 0x9)
            {
                //expected format: 0xI00N - 2 args
                nOpcode = (UInt16)((i << 12) | (0 << 8) | (0 << 4) | (N));
            }
            else
            {
                //expected format: 0xI000 - 3 args
                nOpcode = (UInt16)((i << 12) | (0 << 8) | (0 << 4) | (0));
            }

            if (instructions.ContainsKey(nOpcode))
            {
                //do it
                if (i == 0) //no argument opcodes
                {
                    instructions[nOpcode].DynamicInvoke();
                }
                else if (i == 0xE || i == 0xF) //1 argument opcodes
                {
                    instructions[nOpcode].DynamicInvoke(x);
                }
                else if (i == 0x5 || i == 0x8 || i == 0x9) //2 argument opcodes
                {
                    instructions[nOpcode].DynamicInvoke(x, y);
                }
                else //3 argument opcodes
                {
                    instructions[nOpcode].DynamicInvoke(x, y, N);
                }
            }
            else
            {
                message = $"Error: {opcode.ToString("X4")}  Inavlid opcode";
            }

            
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
        public void InitDisplay(ref bool[,] newDisplay)
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

            if (delayTimer > 0)
            {
                delayTimer--;
            }

            if (soundTimer > 0)
            {
                soundTimer--;
                //debug message for no sound implementation
                //message = "BEEP!";
            }
        }

        private void Fetch()
        {
            

            //get 2 bytes, sent the instruction to Decode()
            byte lo, hi;

            hi = ram[PC];
            lo = ram[PC + 1];

            PC += 2;

            Decode(hi, lo);

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
            for (int dIY = 0; dIY < 64; dIY++)
            {
                for (int dIX = 0; dIX < 128; dIX++)
                {
                    display[dIX, dIY] = false;
                }
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
                byte pixels = ram[I + dIY];
                //v[0xF] = 0;

                for (uint dIX = 0; dIX < 8; dIX++)
                {
                    //example of draw code
                    //display[x + (y * width)];


                    /*
                    bool bitCheck = ((byte)Math.Pow(2, 8 - (dIX + 1)) & pixels) == 0 ? true : false;

                    if (display[dIX + dX, dIY + dY])
                    {
                        //this pixel will be turning off, set the v[f] to 1 when done

                        //message = $"setting {(dIX + dX).ToString("X2")},{(dIY + dY).ToString("X2")} to {bitCheck} and vF to 1";
                        display[dIX + dX, dIY + dY] = bitCheck;
                        v[0xF] = 1;
                        
                    }
                    else
                    {
                        //message = $"setting {(dIX + dX).ToString("X2")},{(dIY + dY).ToString("X2")} to {bitCheck}";
                        display[dIX + dX, dIY + dY] = !bitCheck;
                        //v[0xF] = 0;
                    }*/


                    //test
                    if ((ram[I + dIY] & (0x80 >> (int)dIX)) != 0)
                    {
                        v[0xF] |= (display[dIX + dX, dIY + dY]) ? (byte)1 : (byte)0;
                        display[dIX + dX, dIY + dY] ^= true;
                    }

                    if (dIX + dX > 127) break;
                }
                if (dIY + dY > 63) break;
            }
        }

        /// <summary>
        /// sets the register v[x] to the value yN
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="N"></param>
        private void setV2Value(byte x, byte y, byte N)
        {
            //set register Vx to yn
            v[x] = (byte)(y << 4 | N);
        }

        /// <summary>
        /// sets register v[x] to the value of register v[y]
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        private void setV2V(byte x, byte y)
        {
            v[x] = v[y];
        }

        private void setVADD(byte x, byte y)
        {
            bool setCarry = false;

            if (v[x] + v[y] > 255) setCarry = true;

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
            I = (byte)((v[x] * 5) + 0x32);
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
            if (superChip)
            {
                for (int i = 0; i <= x; i++)
                {
                    ram[I + i] = v[i];
                }
            }
            else
            {
                //do the same thing, but increment I instead of using the iterator
                for (int i = 0; i <= x; i++)
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


        public int DelayTimer
        {
            get { return delayTimer; }
        }

        public int SoundTimer
        {
            get { return soundTimer; }
        }
    }
}
