using System;
using System.Collections.Generic;
using System.Text;
using SFML.Graphics;
using SFML.Window;
using SFML.Audio;
using System.Threading.Tasks;
using System.Threading;
using SFML.System;

namespace chip8SFML
{
    class Emu
    {
        //emu flags
        bool debug = true;
        bool ramView = true;

        //keys
        Keyboard.Key F12 = Keyboard.Key.F12;
        Keyboard.Key F4 = Keyboard.Key.F4;
        Keyboard.Key PGUP = Keyboard.Key.PageUp;
        Keyboard.Key PGDN = Keyboard.Key.PageDown;
        Keyboard.Key N1 = Keyboard.Key.Num1;
        Keyboard.Key N2 = Keyboard.Key.Num2;
        Keyboard.Key N3 = Keyboard.Key.Num3;
        Keyboard.Key C = Keyboard.Key.Num4;
        Keyboard.Key N4 = Keyboard.Key.Q;
        Keyboard.Key N5 = Keyboard.Key.W;
        Keyboard.Key N6 = Keyboard.Key.E;
        Keyboard.Key D = Keyboard.Key.R;
        Keyboard.Key N7 = Keyboard.Key.A;
        Keyboard.Key N8 = Keyboard.Key.S;
        Keyboard.Key N9 = Keyboard.Key.D;
        Keyboard.Key E = Keyboard.Key.F;
        Keyboard.Key A = Keyboard.Key.Z;
        Keyboard.Key N0 = Keyboard.Key.X;
        Keyboard.Key B = Keyboard.Key.C;
        Keyboard.Key F = Keyboard.Key.V;


        //These flags are to ensure holding down a key doesn't act as holding it down
        bool F12pressed = false;
        bool F12wasprsd = false;
        bool F4pressed = false;
        bool F4wasprsd = false;
        bool PGUPpressed = false;
        bool PGUPwasprsd = false;
        bool PGDNpressed = false;
        bool PGDNwasprsd = false;

        //vars for ram viewer
        int pos = 0;
        int maxPos = 4095 - 416;

        //vars for sfml
        uint scale = 4;
        uint width = 64;
        uint height = 32;
        VideoMode mode;
        RenderWindow window;
        VideoMode rvMode;
        RenderWindow ramViewWin;
        Font rvFont;

        //vars for the CPU
        CPU cpu;
        byte[] ram = new byte[4096]; //CHIP8 only has access to 4kB of RAM
        byte[] font =
        {
            0xF0, 0x90, 0x90, 0x90, 0xF0, // 0
            0x20, 0x60, 0x20, 0x20, 0x70, // 1
            0xF0, 0x10, 0xF0, 0x80, 0xF0, // 2
            0xF0, 0x10, 0xF0, 0x10, 0xF0, // 3
            0x90, 0x90, 0xF0, 0x10, 0x10, // 4
            0xF0, 0x80, 0xF0, 0x10, 0xF0, // 5
            0xF0, 0x80, 0xF0, 0x90, 0xF0, // 6
            0xF0, 0x10, 0x20, 0x40, 0x40, // 7
            0xF0, 0x90, 0xF0, 0x90, 0xF0, // 8
            0xF0, 0x90, 0xF0, 0x10, 0xF0, // 9
            0xF0, 0x90, 0xF0, 0x90, 0x90, // A
            0xE0, 0x90, 0xE0, 0x90, 0xE0, // B
            0xF0, 0x80, 0x80, 0x80, 0xF0, // C
            0xE0, 0x90, 0x90, 0x90, 0xE0, // D
            0xF0, 0x80, 0xF0, 0x80, 0xF0, // E
            0xF0, 0x80, 0xF0, 0x80, 0x80  // F
        };

        bool[] display = new bool[64 * 32];
        Color dispOff = Color.Black;
        Color dispOn = Color.White;
        RectangleShape dispPixel;

        public Emu()
        {
            //init
            mode = new VideoMode(scale * width, scale * height);
            window = new RenderWindow(mode, "SFML Chip8 test");

            rvMode = new VideoMode(640, 480); //may not need this much, or may need more
            ramViewWin = new RenderWindow(rvMode, "Ram Viewer");
            ramViewWin.Position = new Vector2i((int)(window.Position.X + (width * scale) + 2), window.Position.Y);

            window.Closed += Close;
            ramViewWin.Closed += rvClose;

            try
            {
                rvFont = new Font(@"C:\Windows\Fonts\Consola.ttf");
            }
            catch(Exception e)
            {
                ErrorMsg(e.Message);
            }

            SystemMsg("Setting up CPU ...");
            cpu = new CPU();
            cpu.SetRAM(ref ram);
            SystemMsg("Done");

            //initialize ram
            for (int rI = 0; rI < 4096; rI++)
            {
                if (rI < 0x50 || rI > 0x9F)
                {
                    //null bytes
                    ram[rI] = 0x0;
                }
            }

            //load the font into ram[$50]
            Array.Copy(font, 0, ram, 50, font.Length);

            SystemMsg("Ram Initialized.  Font Loaded.");

            SystemMsg("Display init beginning");
            for(int dI = 0; dI < display.Length; dI++)
            {
                display[dI] = false;
            }


            SystemMsg("Drawing a 3x5 TEST at 1,1.");
            //T
            display[1 + (1 * 64)] = true;
            display[2 + (1 * 64)] = true;
            display[3 + (1 * 64)] = true;
            display[2 + (2 * 64)] = true;
            display[2 + (3 * 64)] = true;
            display[2 + (4 * 64)] = true;
            display[2 + (5 * 64)] = true;

            //E
            display[5 + (1 * 64)] = true;
            display[6 + (1 * 64)] = true;
            display[7 + (1 * 64)] = true;
            display[5 + (2 * 64)] = true;
            display[5 + (3 * 64)] = true;
            display[6 + (3 * 64)] = true;
            display[5 + (4 * 64)] = true;
            display[5 + (5 * 64)] = true;
            display[6 + (5 * 64)] = true;
            display[7 + (5 * 64)] = true;

            //S
            display[9 + (1 * 64)] = true;
            display[10 + (1 * 64)] = true;
            display[11 + (1 * 64)] = true;
            display[9 + (2 * 64)] = true;
            display[9 + (3 * 64)] = true;
            display[10 + (3 * 64)] = true;
            display[11 + (3 * 64)] = true;
            display[11 + (4 * 64)] = true;
            display[11 + (5 * 64)] = true;
            display[10 + (5 * 64)] = true;
            display[9 + (5 * 64)] = true;

            //T
            display[13 + (1 * 64)] = true;
            display[14 + (1 * 64)] = true;
            display[15 + (1 * 64)] = true;
            display[14 + (2 * 64)] = true;
            display[14 + (3 * 64)] = true;
            display[14 + (4 * 64)] = true;
            display[14 + (5 * 64)] = true;

            dispPixel = new RectangleShape(new Vector2f(1 * scale, 1 * scale));
            dispPixel.FillColor = dispOn;
            SystemMsg("Display initialized.");
        }

        /// <summary>
        /// this is the loop of the program
        /// </summary>
        public void Update()
        {
            while(window.IsOpen)
            {
                window.DispatchEvents();
                window.Clear(Color.Black);

                F12pressed = Keyboard.IsKeyPressed(Keyboard.Key.F12);
                F4pressed = Keyboard.IsKeyPressed(Keyboard.Key.F4);
                PGUPpressed = Keyboard.IsKeyPressed(Keyboard.Key.PageUp);
                PGDNpressed = Keyboard.IsKeyPressed(Keyboard.Key.PageDown);


                if (F4pressed && !F4wasprsd)
                {
                    
                    Close(this, EventArgs.Empty);
                    break;
                }

                if(F12pressed && !F12wasprsd)
                {
                    if (ramViewWin.IsOpen)
                    {
                        rvClose(this, EventArgs.Empty);
                    }
                    else
                    {
                        DebugMsg("Ram Viewer Opened");
                        ramViewWin = new RenderWindow(rvMode, "Ram Viewer");
                        ramViewWin.Position = new Vector2i((int)(window.Position.X + (width * scale) + 2), window.Position.Y);
                    }
                }



                //Draw the display
                for (int y = 0; y < 32; y++)
                {
                    for (int x = 0; x < 64; x++)
                    {
                        if (display[(y*64)+x])
                        {
                            dispPixel.Position = new Vector2f(x * scale, y * scale);
                            window.Draw(dispPixel);
                        }
                    }
                }

                window.Display();

                if(ramViewWin.IsOpen)
                {
                    string ramOut = " ";

                    ramViewWin.DispatchEvents();
                    ramViewWin.Clear(Color.Black);

                    for(int rI = pos; rI < pos+416; rI ++) //416 is the amount of bytes i can display, 417 is to make sure it doesn't cut off the last byte
                    {
                        ramOut += ram[rI].ToString("X2") + " | ";
                        if((rI+1) % 16 == 0)
                        {
                            ramOut += "\n ";
                        }
                    }

                    if(PGUPpressed && !PGUPwasprsd)
                    {
                        //scroll the data up
                        pos -= 416;
                        if (pos < 0) pos = 0;
                    }
                    else if (PGDNpressed && !PGDNwasprsd)
                    {
                        //scroll the data down
                        pos += 416;
                        if (pos > maxPos) pos = maxPos+1;
                    }


                    //TODO: draw strings to represent ram here
                    string posString = (pos + 416) == 4096 ? "4095" : (pos + 416).ToString();
                    Text posText = new Text(str: "pos:" + pos.ToString() + ":" + posString, font: rvFont, characterSize: 14);
                    Text text = new Text(str: ramOut, font: rvFont, characterSize: 14);
                    text.Color = new Color(red:0x20, green:0xAA, blue:0x20);
                    ramViewWin.Draw(text);
                    posText.Color = new Color(red: 0x20, green: 0xAA, blue: 0x20);
                    posText.Position = new Vector2f(2.0f, (float)(480-16));
                    ramViewWin.Draw(posText);

                    RectangleShape scrollBG = new RectangleShape(new Vector2f(8, 480));
                    scrollBG.FillColor = new Color(red: 0x4f, green: 0x4f, blue: 0x4f);
                    scrollBG.Position = new Vector2f(640-8, 0);
                    ramViewWin.Draw(scrollBG);

                    RectangleShape scrollBar = new RectangleShape(new Vector2f(8f, 48.75f));
                    scrollBar.FillColor = new Color(red: 0x00, green: 0x00, blue:0xAA);

                    scrollBar.Position = new Vector2f(640 - 8, (pos * (480 - 48.75f) / maxPos));
                    ramViewWin.Draw(scrollBar);


                    ramViewWin.Display();
                }

                F12wasprsd = F12pressed;
                F4wasprsd = F4pressed;
                PGUPwasprsd = PGUPpressed;
                PGDNwasprsd = PGDNpressed;
            }
        }

        //events

        private void Close(Object sender, EventArgs e)
        {
            SystemMsg("Graphical window exited, terminating.");
            window.Close();
            window.Dispose();
            ramViewWin.Dispose(); //this is done here because we are closing the program.
            Environment.Exit(0);
        }

        private void rvClose(Object sender, EventArgs e)
        {
            DebugMsg("Ram View closed.");
            ramViewWin.Close();
        }

        //Helper functions

        private async Task DebugMsg(string msg)
        {
            if (debug)
            {
                Console.Write("[");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("DEBUG");
                Console.ResetColor();
                Console.Write("]: ");
                Console.WriteLine(msg);
            }
        }

        private async Task SystemMsg(string msg)
        {
            Console.Write("[");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("SYSTEM");
            Console.ResetColor();
            Console.Write("]: ");
            Console.WriteLine(msg);
        }

        private async Task ErrorMsg(string msg)
        {
            Console.Write("[");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("ERROR");
            Console.ResetColor();
            Console.Write("]: ");
            Console.WriteLine(msg);
        }

        private async Task CPUMsg(string msg)
        {
            Console.Write("[");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("CPU");
            Console.ResetColor();
            Console.Write("]: ");
            Console.WriteLine(msg);
        }
    }
}
