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

    //byte[] memory= new byte[0xFFFF]; // 16 bit long memory, each cell 8 bits
    public static byte[] memory = new byte[0xFFFF];

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
            RegF = (byte)(RegF | 0b0100_0000);
        else
            RegF = (byte)(RegF & 0b1011_0000);
    }

    // Set H flag in F register
    private static void SetFlagH(bool value)
    {
        if (value)
            RegF = (byte)(RegF | 0b0010_0000);
        else
            RegF = (byte)(RegF & 0b1101_0000);
    }

    // Set C flag in F register
    private static void SetFlagC(bool value)
    {
        if (value)
            RegF = (byte)(RegF | 0b0001_0000);
        else
            RegF = (byte)(RegF & 0b1110_0000);
    }

    // Load ROM file to memory (only 32kb for now)
    public static void LoadRom(string romPath)
    {

        byte[] Bytes = File.ReadAllBytes(romPath);
        for(var i = 0; i < Bytes.Count(); i++)
        {
            memory[i] = Bytes[i];
        }
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
        // Check half-carry flag
        if ((((register & 0xf) + 1) & 0x10) == 0x10)
            SetFlagH(true);
        else
            SetFlagH(false);

        // Increment
        register++;

        // Set flags
        if (register == 0)
            SetFlagZ(true);
        else
            SetFlagZ(false);

        SetFlagN(false);

        return 1;
    }

    // Decrement 16-bit registers (ex. BC)
    private static int DecrementPseudoRegister(ref byte upperByte, ref byte lowerByte)
    {
        ushort pseudo16Bit = (ushort)((upperByte << 8) + lowerByte);
        pseudo16Bit--;

        upperByte = (byte)(pseudo16Bit >> 8);
        lowerByte = (byte)(pseudo16Bit & 0xFF);
        return 2;
    }

    // Decrement 8-bit registers
    private static int DecrementRegister(ref byte register)
    {
        // Check half-carry flag
        if ((((register & 0xf) - 1) & 0x10) == 0x10)
            SetFlagH(true);
        else
            SetFlagH(false);

        // Decrement
        register--;

        // Set flags
        if (register == 0)
            SetFlagZ(true);
        else
            SetFlagZ(false);

        SetFlagN(true);

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
    private static int LoadRegToPseudoReg(ref byte regTargetUpper, ref byte regTargetLower, ref byte regSource)
    {
        regTargetUpper = 0;
        regTargetLower = regSource;
        return 2;
    }

    // Load from mem to 8-bit reg (ex. u8 -> regB)
    private static int LoadU8ToReg(ref byte reg)
    {
        reg = FetchNextByte();
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
                return DecrementRegister(ref RegB);

            // LD B,u8 - 0x06
            case 0x06:
                System.Diagnostics.Debug.WriteLine("LD B,u8 - 0x06");
                return LoadU8ToReg(ref RegB);

            // RLCA - 0x07 Rotate left A, highest bit is put in carry
            case 0x07:
                System.Diagnostics.Debug.WriteLine("RLCA - 0x07");
                
                if ((RegA & 0b1000_0000) == 0b1000_0000)
                    SetFlagC(true);
                else
                    SetFlagC(false);

                SetFlagH(false);
                SetFlagN(false);
                SetFlagZ(false);

                RegA = (byte) (RegA << 1);
                return 1;

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
                return IncrementRegister(ref RegC);

            // DEC C - 0x0D
            case 0x0D:
                System.Diagnostics.Debug.WriteLine("DEC C - 0x0D");
                return DecrementRegister(ref RegC);

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

            // INC D - 0x14
            case 0x14:
                return DecrementRegister(ref RegD);

            // RLA - 0x17
            case 0x17:
                System.Diagnostics.Debug.WriteLine("RLA - 0x17");

                bool old_c;
                if (((1 << 4) & RegF) != 0)
                    old_c = true;
                else
                    old_c= false;

                if ((RegA & 0b1000_0000) == 0b1000_0000)
                    SetFlagC(true);
                else
                    SetFlagC(false);

                SetFlagH(false);
                SetFlagN(false);
                SetFlagZ(false);

                if (old_c)
                    RegA = (byte)((RegA << 1) | 0b0000_0001);
                else
                    RegA = (byte)(RegA << 1);

                return 1;

            // LD HL,u16 - 0x21
            case 0x21:
                RegH = FetchNextByte();
                RegL = FetchNextByte();
                return 3;

            // INC H - 0x24
            case 0x24:
                return IncrementRegister(ref RegH);

            // DEC H - 0x25
            case 0x25:
                return DecrementRegister(ref RegH);

            // LD SP,u16 - 0x31
            case 0x31:
                var upper_byte = FetchNextByte();
                var lower_byte = FetchNextByte();
                SP = (ushort)(upper_byte << 8 | lower_byte);
                return 3;

            // LD B,B - 0x40 // IS this right?
            case 0x40:
                RegB = RegB;
                return 1;

            // LD B,C - 0x41
            case 0x41:
                RegB = RegC;
                return 1;

            // LD B,D - 0x42
            case 0x42:
                RegB = RegD;
                return 1;

            // LD B,E - 0x43
            case 0x43:
                RegB = RegE;
                return 1;

            // LD B,H - 0x44
            case 0x44:
                RegB = RegH;
                return 1;

            // LD B,L - 0x45
            case 0x45:
                RegB = RegL;
                return 1;

            // LD B,(HL) - 0x46
            case 0x46:
                RegB = RegL;
                return 2;

            // LD B,A - 0x47
            case 0x47:
                RegB = RegA;
                return 1;

            // LD C,B - 0x48
            case 0x48:
                RegC = RegB;
                return 1;

            // LD C,C - 0x49
            case 0x49:
                RegC = RegC;
                return 1;

            // LD C,D - 0x4A
            case 0x4A:
                RegC = RegD;
                return 1;

            // LD C,E - 0x4B
            case 0x4B:
                RegC = RegE;
                return 1;

            // LD C,H - 0x4C
            case 0x4C:
                RegC = RegH;
                return 1;

            // LD C,L - 0x4D
            case 0x4D:
                RegC = RegL;
                return 1;

            // LD C,(HL) - 0x4E
            case 0x4E:
                RegC = RegL;
                return 2;

            // LD C,A - 0x4F
            case 0x4F:
                RegC = RegA;
                return 1;

            // LD D,B - 0x50
            case 0x50:
                RegD = RegB;
                return 1;

            // LD D,C - 0x51
            case 0x51:
                RegD = RegC;
                return 1;

            // LD D,D - 0x52
            case 0x52:
                RegD = RegD;
                return 1;

            // LD D,E - 0x53
            case 0x53:
                RegD = RegE;
                return 1;

            // LD D,H - 0x54
            case 0x54:
                RegD = RegH;
                return 1;

            // LD D,L - 0x55
            case 0x55:
                RegD = RegL;
                return 1;

            // LD D,(HL) - 0x56
            case 0x56:
                RegD = RegL;
                return 2;

            // LD D,A - 0x57
            case 0x57:
                RegD = RegA;
                return 1;

            // LD E,B - 0x58
            case 0x58:
                RegE = RegB;
                return 1;

            // LD E,C - 0x59
            case 0x59:
                RegE = RegC;
                return 1;

            // LD E,D - 0x5A
            case 0x5A:
                RegE = RegD;
                return 1;

            // LD E,E - 0x5B
            case 0x5B:
                RegE = RegE;
                return 1;

            // LD E,H - 0x5C
            case 0x5C:
                RegE = RegH;
                return 1;

            // LD E,L - 0x5D
            case 0x5D:
                RegE = RegL;
                return 1; 

            // LD E,(HL) - 0x5E
            case 0x5E:
                RegE = RegL;
                return 2; 

            // LD E,A - 0x5F
            case 0x5F:
                RegE = RegA;
                return 1;

            // LD H,B - 0x60
            case 0x60:
                RegH = RegB;
                return 1;

            // LD H,C - 0x61
            case 0x61:
                RegH = RegC;
                return 1;

            // LD H,D - 0x62
            case 0x62:
                RegH = RegD;
                return 1;

            // LD H,E - 0x63
            case 0x63:
                RegH = RegE;
                return 1;

            // LD H,H - 0x64
            case 0x64:
                RegH = RegH;
                return 1;

            // LD H,L - 0x65
            case 0x65:
                RegH = RegL;
                return 1;

            // LD H,(HL) - 0x66
            case 0x66:
                RegH = RegL;
                return 1;

            // LD H,A - 0x67
            case 0x67:
                RegH = RegA;
                return 1;

            // LD L,B - 0x68
            case 0x68:
                RegL = RegB;
                return 1;

            // LD L,C - 0x69
            case 0x69:
                RegL = RegC;
                return 1;

            // LD L,D - 0x6A
            case 0x6A:
                RegL = RegD;
                return 1;

            // LD L,E - 0x6B
            case 0x6B:
                RegL = RegE;
                return 1;

            // LD A,H - 0x7C
            case 0x6C:
                RegA = RegH;
                return 1;

            // LD L,L - 0x6D
            case 0x6D:
                RegL = RegL;
                return 1;

            // LD L,(HL) - 0x6E
            case 0x6E:
                RegL = RegL;
                return 2;

            // LD L,A - 0x6F
            case 0x6F:
                RegL = RegA;
                return 1;

            // LD (HL),B - 0x70
            case 0x70:
                return LoadRegToPseudoReg(ref RegH, ref RegL, ref RegB);

            // LD (HL),C - 0x71
            case 0x71:
                return LoadRegToPseudoReg(ref RegH, ref RegL, ref RegC);

            // LD (HL),D - 0x72
            case 0x72:
                return LoadRegToPseudoReg(ref RegH, ref RegL, ref RegD);

            // LD (HL),E - 0x73
            case 0x73:
                return LoadRegToPseudoReg(ref RegH, ref RegL, ref RegE);

            // LD (HL),H - 0x74
            case 0x74:
                return LoadRegToPseudoReg(ref RegH, ref RegL, ref RegH);

            // LD (HL),L - 0x75
            case 0x75:
                return LoadRegToPseudoReg(ref RegH, ref RegL, ref RegL);

            // TODO
            // HALT - 0x76
            case 0x76:
                return -1;

            // LD (HL),A - 0x77
            case 0x77:
                return LoadRegToPseudoReg(ref RegH, ref RegL, ref RegA);

            // LD A,B - 0x78
            case 0x78:
                RegA = RegB;
                return 1;

            // LD A,C - 0x79
            case 0x79:
                RegA = RegC;
                return 1;

            // LD A,D - 0x7A
            case 0x7A:
                RegA = RegD;
                return 1;

            // LD A,E - 0x7B
            case 0x7B:
                RegA = RegE;
                return 1;

            // LD A,H - 0x7C
            case 0x7C:
                RegA = RegH;
                return 1;

            // LD A,L - 0x7D
            case 0x7D:
                RegA = RegL;
                return 1;

            // LD A,(HL) - 0x7E
            case 0x7E:
                RegA = RegL;
                return 2;

            // LD A,A - 0x7F
            case 0x7F:
                RegA = RegA;
                return 1;

            default:
                System.Diagnostics.Debug.WriteLine("OPCODE: " + opcode + " not implemented!");
                return 0;
        }
    }
}