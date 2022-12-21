using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel;

namespace GameBoy_maui;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}


public static class GB 
{
    static GB()
    {
        Registers Register = new Registers();
    }

    public static MainPageViewModel viewModel;

    // Create system hardware
    public struct Registers
    {
        public static byte A { get; set; }
        public static byte B { get; set; }
        public static byte C { get; set; }
        public static byte D { get; set; }
        public static byte E { get; set; }
        public static byte F { get; set; }
        public static byte H { get; set; }
        public static byte L { get; set; }
        public static ushort PC { get; set; }
        public static ushort SP { get; set; }

        // Constructor
        public Registers()
        {
            Registers.A = 17;
            Registers.PC = 0x0100;
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
    public static byte FetchNextByte()
    {
        byte nextByte = memory[Registers.PC];
        Registers.PC += 1;
        return nextByte;
    }

    /// Templates for different opcodes
    // LD 8-bit Load/Store/Move
    //private static void LD8Template(var B)
    //{
        //System.Diagnostics.Debug.WriteLine("LD BC,u16 - 0x01");
    //}

    // Increment registers
    private static void Increment(byte upperByte, byte lowerByte)
    {
        ushort pseudo16Bit = (ushort)upperByte;
        System.Diagnostics.Debug.WriteLine("LD (BC),A - 0x02");
        //ushort pseudo16Bit = BitConverter.ToInt16(upperByte, lowerByte);

    }

    // Set UI register values 
    private static void SetRegisters()
    {
        viewModel.A = Registers.A;
        viewModel.B = Registers.B;
        viewModel.C = Registers.C;
        viewModel.D = Registers.D;
        viewModel.E = Registers.E;
        viewModel.F = Registers.F;
        viewModel.H = Registers.H;
        viewModel.L = Registers.L;
        viewModel.SP = Registers.SP;
        viewModel.PC = Registers.PC;
    }

    // Run inputted opcode, return m-cycles opcode takes
    public static int RunOpcode(byte opcode)
    {
        //Dictionary<int, Action> opcodeTable = new Dictionary<int, Action>();
        Registers.A += 1;
        SetRegisters();
        switch (opcode)
        {
            //NOP
            // Do nothing
            case 0x00:
                return 1;

            // LD BC,u16 - 0x01
            case 0x01:
                System.Diagnostics.Debug.WriteLine("LD BC,u16 - 0x01");
                Registers.C = FetchNextByte();
                Registers.B = FetchNextByte();
                return 3;

            // LD (BC),A - 0x02
            case 0x02:
                System.Diagnostics.Debug.WriteLine("LD (BC),A - 0x02");
                return 0;

            // INC BC - 0x03
            case 0x03:
                System.Diagnostics.Debug.WriteLine("INC BC - 0x03");
                Increment(Registers.B, Registers.C);
                return 0;

            // INC B - 0x04
            case 0x04:
                System.Diagnostics.Debug.WriteLine("INC B - 0x04");
                return 0;

            // DEC B - 0x05
            case 0x05:
                System.Diagnostics.Debug.WriteLine("DEC B - 0x05");
                return 0;

            // LD B,u8 - 0x06
            case 0x06:
                System.Diagnostics.Debug.WriteLine("LD B,u8 - 0x06");
                return 0;

            // RLCA - 0x07
            case 0x07:
                System.Diagnostics.Debug.WriteLine("RLCA - 0x07");
                return 0;

            // LD (u16),SP - 0x08
            case 0x08:
                System.Diagnostics.Debug.WriteLine("LD (u16),SP - 0x08");
                return 0;

            // ADD HL,BC - 0x09
            case 0x09:
                System.Diagnostics.Debug.WriteLine("ADD HL,BC - 0x09");
                return 0;

            // LD A,(BC) - 0x0A
            case 0x0A:
                System.Diagnostics.Debug.WriteLine("LD A,(BC) - 0x0A");
                return 0;

            // DEC BC - 0x0B
            case 0x0B:
                System.Diagnostics.Debug.WriteLine("DEC BC - 0x0B");
                return 0;

            // INC C - 0x0C
            case 0x0C:
                System.Diagnostics.Debug.WriteLine("INC C - 0x0C");
                return 0;

            // DEC C - 0x0D
            case 0x0D:
                System.Diagnostics.Debug.WriteLine("DEC C - 0x0D");
                return 0;

            // LD C,u8 - 0x0E
            case 0x0E:
                System.Diagnostics.Debug.WriteLine("LD C,u8 - 0x0E");
                return 0;

            // RRCA - 0x0F
            case 0x0F:
                System.Diagnostics.Debug.WriteLine("RRCA - 0x0F");
                return 0;

            default:
                System.Diagnostics.Debug.WriteLine("OPCODE: " + opcode + " not implemented!");
                return 0;
        }
    }
}