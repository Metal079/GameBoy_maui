using System;
using System.Collections.Generic;

namespace GameBoy_maui
{
	public partial class MainPage : ContentPage
	{
		int count = 0;

		public MainPage()
		{
			InitializeComponent();
            string romPath = @"C:\Users\metal\source\repos\Gameboy\Roms\test\cpu_instrs\individual\06-ld r,r.gb";
            byte[] bytes = GB.LoadRom(romPath);
        }

    }

    public static class GB
    {
        // Create system hardware
        public struct Registers
        {
            byte A;
            byte B;
            byte C;
            byte D;
            byte E;
            byte F;
            byte H;
            byte L;
            public static ushort PC;
            ushort SP;

            // Constructor
            public Registers()
            {
                PC = 0x0100;
            }
        }
        //byte[] memory= new byte[0xFFFF]; // 16 bit long memory, each cell 8 bits
        static byte[] memory = new byte[0xFFFF];

        // Load ROM file as array of bytes (8 bit each)
        public static byte[] LoadRom(string romPath)
        {

            byte[] Bytes = File.ReadAllBytes(romPath);

            return Bytes;
        }

        // Return next 8 bits and increment PC
        public static byte fetchNextByte(byte memory)
        {
            byte nextByte = GB.memory[Registers.PC];
            Registers.PC += 1;
            return nextByte;
        }

        public static void RunOpcode(byte opcode)
        {
            //Dictionary<int, Action> opcodeTable = new Dictionary<int, Action>();

            switch (opcode)
            {
                //NOP
                // Do nothing
                case 0x00:
                    break;

                //LD BC,u16 - 0x01
                case 0x01:
                    break;
            }
        }
    }
}

namespace graphics
{
    public class GraphicsDrawable : IDrawable
    {
        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            // Drawing code goes here
            canvas.FillColor = Colors.Black;
            canvas.FillRectangle(10, 10, 160, 144);
        }
    }
}

