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
        //Registers Register = new Registers();
        //Flags flags = new Flags();
    }

    public static MainPageViewModel viewModel;

    // Create system hardware
    static byte RegA = 0;
    static byte RegB = 0;
    static byte RegC = 0;
    static byte RegD = 0;
    static byte RegE = 0;
    static byte RegF = 0;
    static byte RegH = 0;
    static byte RegL = 0;
    static ushort SP = 0;
    static ushort PC = 0;

    // Flags
    public struct Flags
    {
        public static bool Z { get; set; }
        public static bool N { get; set; }
        public static bool H { get; set; }
        public static bool C { get; set; }

        public Flags()
        {
            Z = false;
            N = false;
            H = false;
            C = false;
        }
    }

    // Set Z flag in F register
    private static void SetFlagZ(bool value)
    {
        if (value)
            RegF = (byte)(RegF | 0b1000_0000);
        else
            RegF = (byte)(RegF & 0b0111_0000);
    }

    // Set N flag in F register
    private static void SetFlagN(bool value)
    {
        if (value)
            RegF = (byte)(RegF | 0b1000_0000);
        else
            RegF = (byte)(RegF & 0b0111_0000);
    }

    // Set H flag in F register
    private static void SetFlagH(bool value)
    {
        if (value)
            RegF = (byte)(RegF | 0b1000_0000);
        else
            RegF = (byte)(RegF & 0b0111_0000);
    }

    // Set C flag in F register
    private static void SetFlagC(bool value)
    {
        if (value)
            RegF = (byte)(RegF | 0b1000_0000);
        else
            RegF = (byte)(RegF & 0b0111_0000);
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
        byte nextByte = memory[PC];
        PC += 1;
        return nextByte;
    }

    // Increment 16-bit registers (ex. BC)
    private static int IncrementPseudoRegister(ref byte upperByte, ref byte lowerByte)
    {
        ushort pseudo16Bit = (ushort)((upperByte << 8) + lowerByte);
        pseudo16Bit++;

        upperByte = (byte) (pseudo16Bit >> 8);
        lowerByte = (byte) (pseudo16Bit & 0xFF);
        return 2;
    }

    // Increment 8-bit registers
    private static int IncrementRegister(ref byte register)
    {
        register++;

        // Set flags
        if (register == 0)
            SetFlagZ(true);
        else
            SetFlagZ(false);

        SetFlagN(false);

        return 1;
    }

    // Set UI register values 
    public static void SetViewModelRegisters()
    {
        viewModel.A = RegA;
        viewModel.B = RegB;
        viewModel.C = RegC;
        viewModel.D = RegD;
        viewModel.E = RegE;
        viewModel.F = RegF;
        viewModel.H = RegH;
        viewModel.L = RegL;
        viewModel.SP = SP;
        viewModel.PC = PC;

        viewModel.BC = (ushort)((RegB << 8) + RegC);
    }

    // Load into pseudo registers from memory (ex. nn -> BC)
    private static void LoadMemToPseudoReg(ref byte reg1, ref byte reg2)
    {
        reg1 = FetchNextByte();
        reg2 = FetchNextByte();
    }

    // Load into pseudo registers from other reg (ex. A -> BC), reg1 -> B, reg2 -> C, reg3 -> A
    private static int LoadRegToPseudoReg(ref byte reg1, ref byte reg2, ref byte reg3)
    {
        reg1 = 0;
        reg2 = reg3;
        return 2;
    }

    // Run inputted opcode, return m-cycles opcode takes
    public static int RunOpcode(byte opcode)
    {
        switch (opcode)
        {
            //NOP
            // Do nothing
            case 0x00:
                return 1;

            // LD BC,u16 - 0x01
            case 0x01:
                System.Diagnostics.Debug.WriteLine("LD BC,u16 - 0x01");
                LoadMemToPseudoReg(ref RegC, ref RegB);
                return 3;

            // LD (BC),A - 0x02
            case 0x02:
                System.Diagnostics.Debug.WriteLine("LD (BC),A - 0x02");
                return LoadRegToPseudoReg(ref RegB, ref RegC, ref RegA);

            // INC BC - 0x03
            case 0x03:
                System.Diagnostics.Debug.WriteLine("INC BC - 0x03");
                return IncrementPseudoRegister(ref RegB, ref RegC);

            // INC B - 0x04
            case 0x04:
                System.Diagnostics.Debug.WriteLine("INC B - 0x04");
                return  IncrementRegister(ref RegB);

            // DEC B - 0x05
            case 0x05:
                System.Diagnostics.Debug.WriteLine("DEC B - 0x05");
                RegB--;
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

            // LD DE,u16 - 0x11
            case 0x11:
                RegE = FetchNextByte();
                RegD = FetchNextByte();
                return 3;

            // INC DE - 0x13
            case 0x13:
                return IncrementPseudoRegister(ref RegD, ref RegE);

            default:
                System.Diagnostics.Debug.WriteLine("OPCODE: " + opcode + " not implemented!");
                return 0;
        }
    }
}