using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

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
    public static byte[] memory = new byte[0x10000];

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
    public static void LoadRom(string bootRomPath, string romPath)
    {
        byte[] Bytes = File.ReadAllBytes(romPath);
        for (var i = 0; i < Bytes.Count(); i++)
        {
            memory[i] = Bytes[i];
        }

        byte[] bootRom = File.ReadAllBytes(bootRomPath);
        for (var i = 0; i < bootRom.Count(); i++)
        {
            memory[i] = bootRom[i];
        }
    }

    // Return next 8 bits and increment PC
    public static byte FetchNextByte()
    {
        byte nextByte = memory[PC];
        PC += 1;
        return nextByte;
    }

    // Add two 8-bit registers
    private static int Add8bitRegisters(ref byte target, byte source)
    {
        uint carry = ((uint)target + (uint)source) >> 9;

        // Check half-carry flag
        if ((((target & 0xf) + source) & 0x10) == 0x10)
            SetFlagH(true);
        else
            SetFlagH(false);

        target += source;

        if (target == 0)
            SetFlagZ(true);
        else
            SetFlagZ(false);

        SetFlagN(false);

        if (carry >= 1)
            SetFlagC(true);
        else
            SetFlagC(false);

        return 1;
    }

    // Add two 16-bit registers
    private static int AddRegisters(ref byte regH, ref byte regL, ref byte sourceUpper, ref byte sourceLower)
    {
        uint pseudoHL = (ushort)((regH << 8) + regL);
        uint pseudoSource = (ushort)((sourceUpper << 8) + sourceLower);

        uint carry = ((uint)pseudoHL + (uint)pseudoSource) >> 17;

        // Check half-carry flag
        if ((((pseudoHL & 0xf) + pseudoSource) & 0x10) == 0x10)
            SetFlagH(true);
        else
            SetFlagH(false);

        pseudoHL += pseudoSource;

        regH = (byte)(pseudoHL >> 8);
        regL = (byte)(pseudoHL & 0b0000_0000_1111_1111);

        SetFlagN(false);

        if (carry >= 1)
            SetFlagC(true);
        else
            SetFlagC(false);

        return 2;
    }

    // Sub two 8-bit registers
    private static int Sub8BitRegisters(ref byte target, byte source)
    {
        int result = target - source;
        if (result < 0)
        {
            // Set the Carry flag (CF)
            // CF = 1 indicates a carry occurred
            // CF = 0 indicates a carry did not occur
            SetFlagC(true);
        }
        else
        {
            SetFlagC(false);
        }

        // Check half-carry flag
        if ((((target & 0xf) - source) & 0x10) == 0x10)
            SetFlagH(true);
        else
            SetFlagH(false);

        target -= source;

        if (target == 0)
            SetFlagZ(true);
        else
            SetFlagZ(false);

        SetFlagN(true);

        return 1;
    }

    // Increment 16-bit registers (ex. BC)
    private static int IncrementPseudoRegister(ref byte upperByte, ref byte lowerByte)
    {
        ushort pseudo16Bit = (ushort)((upperByte << 8) + lowerByte);
        pseudo16Bit++;

        upperByte = (byte)(pseudo16Bit >> 8);
        lowerByte = (byte)(pseudo16Bit & 0xFF);
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

    // Load into pseudo registers memory address from other reg (ex. A -> (BC)), reg1 -> B, reg2 -> C, reg3 -> A
    private static int LoadRegToPseudoReg(ushort target, byte regSource)
    {
        memory[target] = regSource;
        return 2;
    }

    // Load from mem to 8-bit reg (ex. u8 -> regB)
    private static int LoadU8ToReg(ref byte reg)
    {
        reg = FetchNextByte();
        return 2;
    }

    // XOR 8-bit registers
    private static int XorRegister(ref byte target, byte source)
    {
        target = (byte)(target ^ source);

        if (target == 0)
            SetFlagZ(true);
        else
            SetFlagZ(true);

        SetFlagN(false);
        SetFlagH(false);
        SetFlagC(false);
        return 1;
    }

    // Rotate register to the left (most sig bit wraps around to least sig bit)
    private static int RotateLeftRegister(ref byte reg)
    {
        reg = (byte)((reg << 1) | (reg >> (32 - 1)));

        if (reg == 0)
            SetFlagZ(true);
        else
            SetFlagZ(false);

        SetFlagN(false);
        SetFlagH(false);

        if ((reg & 0b0000_0001) == 1)
            SetFlagC(true);
        else
            SetFlagC(false);

        return 2;
    }

    // Rotate register to the Right (Least sig bit becomes most sig bit)
    private static int RotateRightRegister(ref byte reg)
    {
        reg = (byte)((reg >> 1) | (reg << 7));

        if (reg == 0)
            SetFlagZ(true);
        else
            SetFlagZ(false);

        SetFlagN(false);
        SetFlagH(false);

        if ((reg & 0b1000_0000) == 1)
            SetFlagC(true);
        else
            SetFlagC(false);

        return 2;
    }

    // Rotate register to the left through carry (most sig bit wraps becomes Carry bit, old carry bit becomes least sig bit)
    private static int RotateLeftRegisterThroughCarry(ref byte reg)
    {
        // Store old carry bit
        byte carryBit;
        if ((RegF & 0b0001_0000) == 0b0001_0000)
            carryBit = 1;
        else
            carryBit = 0;

        int tempReg = ((reg << 1) | carryBit);
        reg = (byte)(tempReg);

        // Check if left bit was carry
        if (tempReg >> 8 == 1)
            SetFlagC(true);
        else
            SetFlagC(false);

        if (reg == 0)
            SetFlagZ(true);
        else
            SetFlagZ(false);

        SetFlagN(false);
        SetFlagH(false);

        return 2;
    }

    // Rotate register to the Right through carry (least sig bit becomes carry, old carry is put into most sig bit.)
    private static int RotateRightRegisterThroughCarry(ref byte reg)
    {
        // Store old carry bit
        byte carryBit;
        if ((RegF & 0b0001_0000) == 0b0001_0000)
            carryBit = 0b1000_0000;
        else
            carryBit = 0;

        reg = (byte)((reg >> 1) | carryBit);

        // Check if Right bit was carry
        if (reg >> 7 == 1)
            SetFlagC(true);
        else
            SetFlagC(false);

        if (reg == 0)
            SetFlagZ(true);
        else
            SetFlagZ(false);

        SetFlagN(false);
        SetFlagH(false);

        return 2;
    }

    // Reset bit n of register R
    private static int ResetBit(byte n, byte reg)
    {
        reg = (byte)(reg ^ n);

        return 2;
    }

    // Set bit n of register R
    private static int SetBit(byte n, byte reg)
    {
        reg = (byte)(reg | n);

        return 2;
    }

    // BIT Opcode (it sets Z if bit N of R is not set and resets it otherwise)
    private static void TestBit(byte n, byte reg)
    {
        if ((reg & n) != n)
            SetFlagZ(true);
        else
            SetFlagZ(false);

        SetFlagN(false);
        SetFlagH(true);
    }

    // SLA : Shift left arithmetic (discard most sig bit and set least sig to 0)
    private static void ShiftLeftArithmetic(ref byte reg)
    {
        // Check if left bit was carry
        if (reg >> 7 == 1)
            SetFlagC(true);
        else
            SetFlagC(false);

        reg = (byte) (reg << 1);

        if (reg == 0)
            SetFlagZ(true);
        else
            SetFlagZ(false);

        SetFlagN(false);
        SetFlagH(false);
    }

    // SRA : Shift right arithmetic (shifts right but keeps most sig bit the same)
    private static void ShiftRightArithmetic(ref byte reg)
    {
        // Check if Right bit was carry
        if ((reg & 0b0000_0001) == 0b0000_0001)
            SetFlagC(true);
        else
            SetFlagC(false);

        // Stores value of most sig bit
        byte oldSig = (byte)((reg >> 7) << 7);

        reg = (byte)(reg >> 1);
        reg = (byte)(reg | oldSig);
        
        if (reg == 0)
            SetFlagZ(true);
        else
            SetFlagZ(false);

        SetFlagN(false);
        SetFlagH(false);
    }

    // SRL : Shift right arithmetic (discard least sig bit, most sig bit is set to 0)
    private static void ShiftRightLogical(ref byte reg)
    {
        // Check if Right bit was carry
        if ((reg & 0b0000_0001) == 0b0000_0001)
            SetFlagC(true);
        else
            SetFlagC(false);

        reg = (byte)(reg >> 1);

        if (reg == 0)
            SetFlagZ(true);
        else
            SetFlagZ(false);

        SetFlagN(false);
        SetFlagH(false);
    }

    // SWAP : exchange low/hi-nibble
    private static void Swap(ref byte reg)
    {
        byte upperNibble = (byte)(reg >> 4);
        byte lowerNibble = (byte)(reg << 4);

        reg = (byte)(upperNibble | lowerNibble);

        // Set flags
        if (reg == 0)
            SetFlagZ(true);
        else
            SetFlagZ(false);

        SetFlagN(false);
        SetFlagH(false);
        SetFlagC(false);
    }

    // CP OPCODE (compare registers, subtract the second register from the first to set flags in the Status Register. However, cp leaves the registers themselves unmodified.)
    private static void CP(byte target, byte source)
    {
        byte result = (byte)(target - source);

        if (result == 0)
            SetFlagZ(true);
        else
            SetFlagZ(false);

        SetFlagN(true);

        // Check half-carry flag
        if ((((target & 0xf) - source) & 0x10) == 0x10)
            SetFlagH(true);
        else
            SetFlagH(false);

        if (result < 0)
        {
            // Set the Carry flag (CF)
            // CF = 1 indicates a carry occurred
            // CF = 0 indicates a carry did not occur
            SetFlagC(true);
        }
        else
        {
            SetFlagC(false);
        }
    }

    // OR 8-bit register
    private static void OR8BitRegisters(ref byte target, byte source)
    {
        target = (byte)(target | source);

        if (target == 0) SetFlagZ(true);
        else SetFlagZ(false);

        SetFlagN(false);
        SetFlagH(false);
        SetFlagC(false);
    }

    // XOR 8-bit register
    private static void XOR8BitRegisters(ref byte target, byte source)
    {
        target = (byte)(target ^ source);

        if (target == 0) SetFlagZ(true);
        else SetFlagZ(false);

        SetFlagN(false);
        SetFlagH(false);
        SetFlagC(false);
    }

    // AND 8-bit register
    private static void AND8BitRegisters(ref byte target, byte source)
    {
        target = (byte)(target & source);

        if (target == 0) SetFlagZ(true);
        else SetFlagZ(false);

        SetFlagN(false);
        SetFlagH(true);
        SetFlagC(false);
    }

    // ADC 2 8 bit bytes
    private static void ADC8BitRegisters(ref byte target, byte source)
    {
        var carry = RegF & 0b0001_0000;

        ushort result = (ushort)(target + source + carry);
        target = (byte) result;

        // Check zero flag
        if (target == 0) SetFlagZ(true);
        else SetFlagZ(false);

        SetFlagN(false);

        // Check half-carry flag
        if ((((target & 0xf) + source) & 0x10) == 0x10)
            SetFlagH(true);
        else
            SetFlagH(false);

        if ((result >> 8) >= 1)
            SetFlagC(true);
        else
            SetFlagC(false);
    }

    // SBC 2 8 bit bytes
    private static void SBC8BitRegisters(ref byte target, byte source)
    {
        var carry = RegF & 0b0001_0000;

        int result = target - source - carry;
        target = (byte) result;


        // Check zero flag
        if (target == 0) SetFlagZ(true);
        else SetFlagZ(false);

        SetFlagN(true);

        // Check half-carry flag
        if ((((target & 0xf) - source) & 0x10) == 0x10)
            SetFlagH(true);
        else
            SetFlagH(false);

        if (result < 0)
            SetFlagC(true);
        else
            SetFlagC(false);
    }

    // CALL (call to nn, SP=SP-2, (SP)=PC, PC=nn)
    private static void CALL(byte upperByte, byte lowerByte)
    {
        SP = (ushort) (SP - 2);

        byte PC_upper = (byte)(PC >> 8);
        byte PC_lower = (byte)PC;

        memory[SP] = PC_upper;
        memory[SP+1] = PC_lower;
        PC = (ushort)((upperByte << 8) | lowerByte);
    }

    // PUSH
    private static void PUSH(byte upperReg, byte lowerReg)
    {
        SP = (ushort)(SP - 2);

        memory[SP] = upperReg;
        memory[SP+1] = lowerReg;
    }

    // POP
    private static void POP(byte upperReg, byte lowerReg)
    {
        upperReg = memory[SP];
        lowerReg= memory[SP+1];

        SP = (ushort)(SP + 2);
    }

    // Get pseudo register from 2 8-bit registers
    private static ushort GetPseudoRegister(byte upperByte, byte lowerByte)
    {
        return (ushort)((upperByte << 8) | lowerByte);
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
                return LoadRegToPseudoReg((ushort)((RegB << 8) | RegC), RegA);

            // INC BC - 0x03
            case 0x03:
                System.Diagnostics.Debug.WriteLine("INC BC - 0x03");
                return IncrementPseudoRegister(ref RegB, ref RegC);

            // INC B - 0x04
            case 0x04:
                System.Diagnostics.Debug.WriteLine("INC B - 0x04");
                return IncrementRegister(ref RegB);

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

                RegA = (byte)(RegA << 1);
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

            // STOP - 0x10
            case 0x10:


            // LD DE,u16 - 0x11
            case 0x11:
                RegE = FetchNextByte();
                RegD = FetchNextByte();
                return 3;

            // LD (DE),A - 0x12
            case 0x12:

            // INC DE - 0x13
            case 0x13:
                return IncrementPseudoRegister(ref RegD, ref RegE);

            // INC D - 0x14
            case 0x14:
                return DecrementRegister(ref RegD);

            // DEC D - 0x15
            case 0x15:

            // LD D,u8 - 0x16
            case 0x16:

            // RLA - 0x17
            case 0x17:
                System.Diagnostics.Debug.WriteLine("RLA - 0x17");

                bool old_c;
                if (((1 << 4) & RegF) != 0)
                    old_c = true;
                else
                    old_c = false;

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

            // JR i8 - 0x18
            case 0x18:

            // ADD HL,DE - 0x19
            case 0x19:

            // LD A,(DE) - 0x1A
            case 0x1A:

            // DEC DE - 0x1B
            case 0x1B:

            // INC E - 0x1C
            case 0x1C:

            // DEC E - 0x1D
            case 0x1D:

            // LD E,u8 - 0x1E
            case 0x1E:

            // RRA - 0x1F
            case 0x1F:

            // JR NZ,i8 - 0x20
            case 0x20:
                if (RegF >> 7 == 1)
                {
                    PC = (ushort)(PC + FetchNextByte());
                    return 3;
                }
                else
                    return 2;

            // LD HL,u16 - 0x21
            case 0x21:
                RegH = FetchNextByte();
                RegL = FetchNextByte();
                return 3;

            // LD (HL+),A - 0x22
            case 0x22:
                
                return 2;

            // INC HL - 0x23
            case 0x23:

            // INC H - 0x24
            case 0x24:
                return IncrementRegister(ref RegH);

            // DEC H - 0x25
            case 0x25:
                return DecrementRegister(ref RegH);

            // LD H,u8 - 0x26
            case 0x26:

            // DAA - 0x27
            case 0x27:

            // JR Z,i8 - 0x28
            case 0x28:

            // ADD HL,HL - 0x29
            case 0x29:

            // LD A,(HL+) - 0x2A
            case 0x2A:

            // DEC HL - 0x2B
            case 0x2B:

            // INC L - 0x2C
            case 0x2C:

            // DEC L - 0x2D
            case 0x2D:

            // LD L,u8 - 0x2E
            case 0x2E:

            // CPL - 0x2F
            case 0x2F:

            // LD SP,u16 - 0x31
            case 0x31:
                var upper_byte = FetchNextByte();
                var lower_byte = FetchNextByte();
                SP = (ushort)(upper_byte << 8 | lower_byte);
                return 3;

            // LD (HL-),A - 0x32
            case 0x32:
                LoadRegToPseudoReg((ushort)((RegH << 8) | RegL), RegA);
                if (RegL == 0)
                    RegH--;
                RegL--;
                return 2;

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
                return LoadRegToPseudoReg((ushort)((RegH << 8) | RegL), RegB);

            // LD (HL),C - 0x71
            case 0x71:
                return LoadRegToPseudoReg((ushort)((RegH << 8) | RegL), RegC);

            // LD (HL),D - 0x72
            case 0x72:
                return LoadRegToPseudoReg((ushort)((RegH << 8) | RegL), RegD);

            // LD (HL),E - 0x73
            case 0x73:
                return LoadRegToPseudoReg((ushort)((RegH << 8) | RegL), RegE);

            // LD (HL),H - 0x74
            case 0x74:
                return LoadRegToPseudoReg((ushort)((RegH << 8) | RegL), RegH);

            // LD (HL),L - 0x75
            case 0x75:
                return LoadRegToPseudoReg((ushort)((RegH << 8) | RegL), RegL);

            // TODO
            // HALT - 0x76
            case 0x76:
                return -1;

            // LD (HL),A - 0x77
            case 0x77:
                return LoadRegToPseudoReg((ushort)((RegH << 8) | RegL), RegA);

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

            // ADD A,B - 0x80
            case 0x80:
                Add8bitRegisters(ref RegA, RegB);
                return 1;

            // ADD A,C - 0x81
            case 0x81:
                Add8bitRegisters(ref RegA, RegC);
                return 1;

            // ADD A,D - 0x82
            case 0x82:
                Add8bitRegisters(ref RegA, RegD);
                return 1;

            // ADD A,E - 0x83
            case 0x83:
                Add8bitRegisters(ref RegA, RegE);
                return 1;

            // ADD A,H - 0x84
            case 0x84:
                Add8bitRegisters(ref RegA, RegH);
                return 1;

            // ADD A,L - 0x85
            case 0x85:
                Add8bitRegisters(ref RegA, RegL);
                return 1;

            // ADD A,(HL) - 0x86
            case 0x86:
                Add8bitRegisters(ref RegA, memory[GetPseudoRegister(RegH, RegL)]);
                return 2;

            // ADD A,A - 0x87
            case 0x87:
                Add8bitRegisters(ref RegA, RegA);
                return 1;

            // ADC A,B - 0x88
            case 0x88:
                ADC8BitRegisters(ref RegA, RegB);
                return 1;

            // ADC A,C - 0x89
            case 0x89:
                ADC8BitRegisters(ref RegA, RegC);
                return 1;

            // ADC A,D - 0x8A
            case 0x8A:
                ADC8BitRegisters(ref RegA, RegD);
                return 1;

            // ADC A,E - 0x8B
            case 0x8B:
                ADC8BitRegisters(ref RegA, RegE);
                return 1;

            // ADC A,H - 0x8C
            case 0x8C:
                ADC8BitRegisters(ref RegA, RegH);
                return 1;

            // ADC A,L - 0x8D
            case 0x8D:
                ADC8BitRegisters(ref RegA, RegL);
                return 1;

            // ADC A,(HL) - 0x8E
            case 0x8E:
                ADC8BitRegisters(ref RegA, memory[GetPseudoRegister(RegH, RegL)]);
                return 2;

            // ADC A,A - 0x8F
            case 0x8F:
                ADC8BitRegisters(ref RegA, RegA);
                return 1;

            // SUB A,B - 0x90
            case 0x90:
                Sub8BitRegisters(ref RegA, RegB);
                return 1;

            // SUB A,C - 0x91
            case 0x91:
                Sub8BitRegisters(ref RegA, RegC);
                return 1;

            // SUB A,D - 0x92
            case 0x92:
                Sub8BitRegisters(ref RegA, RegD);
                return 1;

            // SUB A,E - 0x93
            case 0x93:
                Sub8BitRegisters(ref RegA, RegE);
                return 1;

            // SUB A,H - 0x94
            case 0x94:
                Sub8BitRegisters(ref RegA, RegH);
                return 1;

            // SUB A,L - 0x95
            case 0x95:
                Sub8BitRegisters(ref RegA, RegL);
                return 1;

            // SUB A,(HL) - 0x96
            case 0x96:
                Sub8BitRegisters(ref RegA, memory[GetPseudoRegister(RegH, RegL)]);
                return 2;

            // SUB A,A - 0x97
            case 0x97:
                Sub8BitRegisters(ref RegA, RegA);
                return 1;

            // SBC A,B - 0x98
            case 0x98:
                SBC8BitRegisters(ref RegA, RegB);
                return 1;

            // SBC A,C - 0x99
            case 0x99:
                SBC8BitRegisters(ref RegA, RegC);
                return 1;

            // SBC A,D - 0x9A
            case 0x9A:
                SBC8BitRegisters(ref RegA, RegD);
                return 1;

            // SBC A,E - 0x9B
            case 0x9B:
                SBC8BitRegisters(ref RegA, RegE);
                return 1;

            // SBC A,H - 0x9C
            case 0x9C:
                SBC8BitRegisters(ref RegA, RegH);
                return 1;

            // SBC A,L - 0x9D
            case 0x9D:
                SBC8BitRegisters(ref RegA, RegL);
                return 1;

            // SBC A,(HL) - 0x9E
            case 0x9E:
                SBC8BitRegisters(ref RegA, memory[GetPseudoRegister(RegH, RegL)]);
                return 2;

            // SBC A,A - 0x9F
            case 0x9F:
                SBC8BitRegisters(ref RegA, RegA);
                return 1;

            // AND A,B - 0xA0
            case 0xA0:
                AND8BitRegisters(ref RegA, RegB);
                return 1;

            // AND A,C - 0xA1
            case 0xA1:
                AND8BitRegisters(ref RegA, RegC);
                return 1;

            // AND A,D - 0xA2
            case 0xA2:
                AND8BitRegisters(ref RegA, RegD);
                return 1;

            // AND A,E - 0xA3
            case 0xA3:
                AND8BitRegisters(ref RegA, RegE);
                return 1;

            // AND A,H - 0xA4
            case 0xA4:
                AND8BitRegisters(ref RegA, RegH);
                return 1;

            // AND A,L - 0xA5
            case 0xA5:
                AND8BitRegisters(ref RegA, RegL);
                return 1;

            // AND A,(HL) - 0xA6
            case 0xA6:
                AND8BitRegisters(ref RegA, memory[GetPseudoRegister(RegH, RegL)]);
                return 1;

            // AND A,A - 0xA7
            case 0xA7:
                AND8BitRegisters(ref RegA, RegA);
                return 1;

            // XOR A,B - 0xA8
            case 0xA8:
                return XorRegister(ref RegA, RegB);

            // XOR A,C - 0xA9
            case 0xA9:
                return XorRegister(ref RegA, RegC);

            // XOR A,D - 0xAA
            case 0xAA:
                return XorRegister(ref RegA, RegD);

            // XOR A,E - 0xAB
            case 0xAB:
                return XorRegister(ref RegA, RegE);

            // XOR A,H - 0xAC
            case 0xAC:
                return XorRegister(ref RegA, RegH);

            // XOR A,L - 0xAD
            case 0xAD:
                return XorRegister(ref RegA, RegL);

            // XOR A,A - 0xAF
            case 0xAF:
                return XorRegister(ref RegA, RegA);

            // OR A,B - 0xB0
            case 0x

            // OR A,C - 0xB1
            case 0x

            // OR A,D - 0xB2
            case 0x

            // OR A,E - 0xB3
            case 0x

            // OR A,H - 0xB4
            case 0x

            // OR A,L - 0xB5
            case 0x

            // OR A,(HL) - 0xB6
            case 0x

            // OR A,A - 0xB7
            case 0x

            // CP A,B - 0xB8
            case 0x

            // CP A,C - 0xB9
            case 0x

            // CP A,D - 0xBA
            case 0x

            // CP A,E - 0xBB
            case 0x

            // CP A,H - 0xBC
            case 0x

            // CP A,L - 0xBD
            case 0x

            // CP A,(HL) - 0xBE
            case 0x

            // CP A,A - 0xBF
            case 0x

            // RET NZ - 0xC0
            case 0x

            // POP BC - 0xC1
            case 0x

            // JP NZ,u16 - 0xC2
            case 0xC2:
                {
                    if (RegF >> 7 == 1)
                    {
                        ushort u16 = (ushort)((FetchNextByte() << 8) + FetchNextByte());
                        PC = u16;
                        return 4;
                    }
                    else
                        return 3;
                }

            // JP u16 - 0xC3
            case 0xC3:
                {
                    ushort u16 = (ushort)((FetchNextByte() << 8) + FetchNextByte());
                    PC = u16;
                    return 4;
                }

            // PREFIX CB
            case 0xCB:
                opcode = FetchNextByte();
                switch (opcode)
                {
                    // RLC B - 0x00
                    case 0x00:
                        return RotateLeftRegister(ref RegB); 

                    // RLC C - 0x01
                    case 0x01:
                        return RotateLeftRegister(ref RegC);

                    // RLC D - 0x02
                    case 0x02:
                        return RotateLeftRegister(ref RegD);

                    // RLC E - 0x03
                    case 0x03:
                        return RotateLeftRegister(ref RegE);

                    // RLC H - 0x04
                    case 0x04:
                        return RotateLeftRegister(ref RegH);

                    // RLC L - 0x05
                    case 0x05:
                        return RotateLeftRegister(ref RegL);

                    // RLC (HL) - 0x06
                    case 0x06:
                        {
                            // Define HL from RegH and RegL
                            ushort HL = (ushort)((RegH << 8) | RegL);

                            HL = (ushort)((HL << 1) | (HL >> (32 - 1)));

                            // Convert back to RegH and RegL
                            RegH = (byte)(HL >> 8);
                            RegL = (byte) HL;

                            if (HL == 0)
                                SetFlagZ(true);
                            else
                                SetFlagZ(false);

                            SetFlagN(false);
                            SetFlagH(false);

                            if ((HL & 0b0000_0000_0000_0001) == 1)
                                SetFlagC(true);
                            else
                                SetFlagC(false);

                            return 4;
                        }

                    // RLC A - 0x07
                    case 0x07:
                        return RotateLeftRegister(ref RegA);

                    // RRC B - 0x08
                    case 0x08:
                        return RotateRightRegister(ref RegB);

                    // RRC C - 0x09
                    case 0x09:
                        return RotateRightRegister(ref RegC);

                    // RRC D - 0x0A
                    case 0x0A:
                        return RotateRightRegister(ref RegD);

                    // RRC E - 0x0B
                    case 0x0B:
                        return RotateRightRegister(ref RegE);

                    // RRC H - 0x0C
                    case 0x0C:
                        return RotateRightRegister(ref RegH);

                    // RRC L - 0x0D
                    case 0x0D:
                        return RotateRightRegister(ref RegL);

                    // RRC (HL) - 0x0E
                    case 0x0E:
                        {
                            // Define HL from RegH and RegL
                            ushort HL = (ushort)((RegH << 8) | RegL);
                            
                            HL = (byte)((HL >> 1) | (HL << 7));

                            // Convert back to RegH and RegL
                            RegH = (byte)(HL >> 8);
                            RegL = (byte)HL;

                            if (HL == 0)
                                SetFlagZ(true);
                            else
                                SetFlagZ(false);

                            SetFlagN(false);
                            SetFlagH(false);

                            if ((HL & 0b1000_0000_0000_0000) == 1)
                                SetFlagC(true);
                            else
                                SetFlagC(false);

                            return 4;
                        }

                    // RRC A - 0x0F
                    case 0x0F:
                        return RotateRightRegister(ref RegA);

                    // RL B - 0x10
                    case 0x10:
                        return RotateLeftRegisterThroughCarry(ref RegB);

                    // RL C - 0x11
                    case 0x11:
                        return RotateLeftRegisterThroughCarry(ref RegC);

                    // RL D - 0x12
                    case 0x12:
                        return RotateLeftRegisterThroughCarry(ref RegD);

                    // RL E - 0x13
                    case 0x13:
                        return RotateLeftRegisterThroughCarry(ref RegE);

                    // RL H - 0x14
                    case 0x14:
                        return RotateLeftRegisterThroughCarry(ref RegH);

                    // RL L - 0x15
                    case 0x15:
                        return RotateLeftRegisterThroughCarry(ref RegL);

                    // RL (HL) - 0x16
                    case 0x16:
                        {
                            // Store old carry bit
                            byte carryBit;
                            if ((RegF & 0b0001_0000) == 0b0001_0000)
                                carryBit = 1;
                            else
                                carryBit = 0;

                            // Define HL from RegH and RegL
                            ushort HL = (ushort)((RegH << 8) | RegL);

                            int tempReg = ((HL << 1) | carryBit);
                            HL = (ushort)(tempReg);

                            // Convert back to RegH and RegL
                            RegH = (byte)(HL >> 7);
                            RegL = (byte)HL;

                            // Check if left bit was carry
                            if (tempReg >> 7 == 1)
                                SetFlagC(true);
                            else
                                SetFlagC(false);

                            if (HL == 0)
                                SetFlagZ(true);
                            else
                                SetFlagZ(false);

                            SetFlagN(false);
                            SetFlagH(false);

                            return 4;
                        }

                    // RL A - 0x17
                    case 0x17:
                        return RotateLeftRegisterThroughCarry(ref RegA);

                    // RR B - 0x18
                    case 0x18:
                        return RotateRightRegisterThroughCarry(ref RegB);

                    // RR C - 0x19
                    case 0x19:
                        return RotateRightRegisterThroughCarry(ref RegC);

                    // RR D - 0x1A
                    case 0x1A:
                        return RotateRightRegisterThroughCarry(ref RegD);

                    // RR E - 0x1B
                    case 0x1B:
                        return RotateRightRegisterThroughCarry(ref RegE);

                    // RR H - 0x1C
                    case 0x1C:
                        return RotateRightRegisterThroughCarry(ref RegH);

                    // RR L - 0x1D
                    case 0x1D:
                        return RotateRightRegisterThroughCarry(ref RegL);

                    // RR (HL) - 0x1E
                    case 0x1E:
                        {
                            // Store old carry bit
                            byte carryBit;
                            if ((RegF & 0b0001_0000) == 0b0001_0000)
                                carryBit = 0b1000_0000;
                            else
                                carryBit = 0;

                            // Define HL from RegH and RegL
                            ushort HL = (ushort)((RegH << 8) | RegL);

                            HL = (ushort)((HL >> 1) | carryBit);

                            // Convert back to RegH and RegL
                            RegH = (byte)(HL >> 8);
                            RegL = (byte)HL;

                            // Check if Right bit was carry
                            if (HL >> 15 == 1)
                                SetFlagC(true);
                            else
                                SetFlagC(false);

                            if (HL == 0)
                                SetFlagZ(true);
                            else
                                SetFlagZ(false);

                            SetFlagN(false);
                            SetFlagH(false);

                            return 4;
                        }

                    // RR A - 0x1F
                    case 0x1F:
                        return RotateRightRegisterThroughCarry(ref RegA);

                    // SLA B - 0x20
                    case 0x20:
                        ShiftLeftArithmetic(ref RegB);
                        return 2;

                    // SLA C - 0x21
                    case 0x21:
                        ShiftLeftArithmetic(ref RegC);
                        return 2;

                    // SLA D - 0x22
                    case 0x22:
                        ShiftLeftArithmetic(ref RegD);
                        return 2;

                    // SLA E - 0x23
                    case 0x23:
                        ShiftLeftArithmetic(ref RegE);
                        return 2;

                    // SLA H - 0x24
                    case 0x24:
                        ShiftLeftArithmetic(ref RegH);
                        return 2;

                    // SLA L - 0x25
                    case 0x25:
                        ShiftLeftArithmetic(ref RegL);
                        return 2;

                    // SLA (HL) - 0x26
                    case 0x26:
                        {
                            // Define HL from RegH and RegL
                            ushort HL = (ushort)((RegH << 8) | RegL);

                            // Check if left bit was carry
                            if (HL >> 15 == 1)
                                SetFlagC(true);
                            else
                                SetFlagC(false);

                            HL = (ushort)(HL << 1);

                            // Convert back to RegH and RegL
                            RegH = (byte)(HL >> 8);
                            RegL = (byte)HL;

                            if (HL == 0)
                                SetFlagZ(true);
                            else
                                SetFlagZ(false);

                            SetFlagN(false);
                            SetFlagH(false);

                            return 4;
                        }

                    // SLA A - 0x27
                    case 0x27:
                        ShiftLeftArithmetic(ref RegA);
                        return 2;

                    //SRA B - 0x28
                    case 0x28:
                        ShiftRightArithmetic(ref RegB);
                        return 2;

                    //SRA C - 0x29
                    case 0x29:
                        ShiftRightArithmetic(ref RegC);
                        return 2;

                    //SRA D - 0x2A
                    case 0x2A:
                        ShiftRightArithmetic(ref RegD);
                        return 2;

                    //SRA E - 0x2B
                    case 0x2B:
                        ShiftRightArithmetic(ref RegE);
                        return 2;

                    //SRA H - 0x2C
                    case 0x2C:
                        ShiftRightArithmetic(ref RegH);
                        return 2;

                    //SRA L - 0x2D
                    case 0x2D:
                        ShiftRightArithmetic(ref RegL);
                        return 2;

                    //SRA (HL) - 0x2E
                    case 0x2E:
                        {
                            // Define HL from RegH and RegL
                            ushort HL = (ushort)((RegH << 8) | RegL);

                            // Check if Right bit was carry
                            if ((HL & 0b0000_0000_0000_0001) == 0b0000_0001)
                                SetFlagC(true);
                            else
                                SetFlagC(false);

                            // Stores value of most sig bit
                            ushort oldSig = (byte)((HL >> 15) << 15);

                            HL = (ushort)(HL >> 1);
                            HL = (ushort)(HL | oldSig);

                            // Convert back to RegH and RegL
                            RegH = (byte)(HL >> 8);
                            RegL = (byte)HL;

                            if (HL == 0)
                                SetFlagZ(true);
                            else
                                SetFlagZ(false);

                            SetFlagN(false);
                            SetFlagH(false);

                            return 4;
                        }

                    //SRA A - 0x2F
                    case 0x2F:
                        ShiftRightArithmetic(ref RegA);
                        return 2;

                    //SWAP B - 0x30
                    case 0x30:
                        Swap(ref RegB);
                        return 2;

                    //SWAP C - 0x31
                    case 0x31:
                        Swap(ref RegC);
                        return 2;

                    //SWAP D - 0x32
                    case 0x32:
                        Swap(ref RegD);
                        return 2;

                    //SWAP E - 0x33
                    case 0x33:
                        Swap(ref RegE);
                        return 2;

                    //SWAP H - 0x34
                    case 0x34:
                        Swap(ref RegH);
                        return 2;

                    //SWAP L - 0x35
                    case 0x35:
                        Swap(ref RegL);
                        return 2;

                    //SWAP (HL) - 0x36
                    case 0x36:
                        {
                            // Define HL from RegH and RegL
                            ushort HL = (ushort)((RegH << 8) | RegL);

                            byte upperNibble = (byte)(HL >> 12);
                            byte lowerNibble = (byte)(HL << 12);

                            HL = (byte)(upperNibble | lowerNibble);

                            // Convert back to RegH and RegL
                            RegH = (byte)(HL >> 8);
                            RegL = (byte)HL;

                            // Set flags
                            if (HL == 0)
                                SetFlagZ(true);
                            else
                                SetFlagZ(false);

                            SetFlagN(false);
                            SetFlagH(false);
                            SetFlagC(false);

                            return 4;
                        }

                    //SWAP A - 0x37
                    case 0x37:
                        Swap(ref RegA);
                        return 2;

                    //SRL B - 0x38
                    case 0x38:
                        ShiftRightLogical(ref RegB);
                        return 2;

                    //SRL C - 0x39
                    case 0x39:
                        ShiftRightLogical(ref RegC);
                        return 2;

                    //SRL D - 0x3A
                    case 0x3A:
                        ShiftRightLogical(ref RegD);
                        return 2;

                    //SRL E - 0x3B
                    case 0x3B:
                        ShiftRightLogical(ref RegE);
                        return 2;

                    //SRL H - 0x3C
                    case 0x3C:
                        ShiftRightLogical(ref RegH);
                        return 2;

                    //SRL L - 0x3D
                    case 0x3D:
                        ShiftRightLogical(ref RegL);
                        return 2;

                    //SRL (HL) - 0x3E
                    case 0x3E:
                        {
                            // Define HL from RegH and RegL
                            ushort HL = (ushort)((RegH << 8) | RegL);

                            // Check if Right bit was carry
                            if ((HL & 0b0000_0000_0000_0001) == 0b0000_0000_0000_0001)
                                SetFlagC(true);
                            else
                                SetFlagC(false);

                            HL = (ushort)(HL >> 1);

                            // Convert back to RegH and RegL
                            RegH = (byte)(HL >> 8);
                            RegL = (byte)HL;

                            if (HL == 0)
                                SetFlagZ(true);
                            else
                                SetFlagZ(false);

                            SetFlagN(false);
                            SetFlagH(false);

                            return 4;
                        }

                    //SRL A - 0x3F
                    case 0x3F:
                        ShiftRightLogical(ref RegA);
                        return 2;

                    //BIT 0,B - 0x40
                    case 0x40:
                        TestBit(0b0000_0001, RegB);
                        return 2;

                    //BIT 0,C - 0x41
                    case 0x41:
                        TestBit(0b0000_0001, RegC);
                        return 2;

                    //BIT 0,D - 0x42
                    case 0x42:
                        TestBit(0b0000_0001, RegD);
                        return 2;

                    //BIT 0,E - 0x43
                    case 0x43:
                        TestBit(0b0000_0001, RegE);
                        return 2;

                    //BIT 0,H - 0x44
                    case 0x44:
                        TestBit(0b0000_0001, RegH);
                        return 2;

                    //BIT 0,L - 0x45
                    case 0x45:
                        TestBit(0b0000_0001, RegL);
                        return 2;

                    //BIT 0,(HL) - 0x46
                    case 0x46:
                        TestBit(0b0000_0001, RegL);
                        return 2;

                    //BIT 0,A - 0x47
                    case 0x47:
                        TestBit(0b0000_0001, RegA);
                        return 2;

                    //BIT 1,B - 0x48
                    case 0x48:
                        TestBit(0b0000_0010, RegB);
                        return 2;

                    //BIT 1,C - 0x49
                    case 0x49:
                        TestBit(0b0000_0010, RegC);
                        return 2;

                    //BIT 1,D - 0x4A
                    case 0x4A:
                        TestBit(0b0000_0010, RegD);
                        return 2;

                    //BIT 1,E - 0x4B
                    case 0x4B:
                        TestBit(0b0000_0010, RegE);
                        return 2;

                    //BIT 1,H - 0x4C
                    case 0x4C:
                        TestBit(0b0000_0010, RegH);
                        return 2;

                    //BIT 1,L - 0x4D
                    case 0x4D:
                        TestBit(0b0000_0010, RegL);
                        return 2;

                    //BIT 1,(HL) - 0x4E
                    case 0x4E:
                        TestBit(0b0000_0010, RegL);
                        return 2;

                    //BIT 1,A - 0x4F
                    case 0x4F:
                        TestBit(0b0000_0010, RegA);
                        return 2;

                    //BIT 2,B - 0x50
                    case 0x50:
                        TestBit(0b0000_0100, RegB);
                        return 2;

                    //BIT 2,C - 0x51
                    case 0x51:
                        TestBit(0b0000_0100, RegC);
                        return 2;

                    //BIT 2,D - 0x52
                    case 0x52:
                        TestBit(0b0000_0100, RegD);
                        return 2;

                    //BIT 2,E - 0x53
                    case 0x53:
                        TestBit(0b0000_0100, RegE);
                        return 2;

                    //BIT 2,H - 0x54
                    case 0x54:
                        TestBit(0b0000_0100, RegB);
                        return 2;

                    //BIT 2,L - 0x55
                    case 0x55:
                        TestBit(0b0000_0100, RegL);
                        return 2;

                    //BIT 2,(HL) - 0x56
                    case 0x56:
                        TestBit(0b0000_0100, RegL);
                        return 2;

                    //BIT 2,A - 0x57
                    case 0x57:
                        TestBit(0b0000_0100, RegA);
                        return 2;

                    //BIT 3,B - 0x58
                    case 0x58:
                        TestBit(0b0000_1000, RegB);
                        return 2;

                    //BIT 3,C - 0x59
                    case 0x59:
                        TestBit(0b0000_1000, RegC);
                        return 2;

                    //BIT 3,D - 0x5A
                    case 0x5A:
                        TestBit(0b0000_1000, RegD);
                        return 2;

                    //BIT 3,E - 0x5B
                    case 0x5B:
                        TestBit(0b0000_1000, RegE);
                        return 2;

                    //BIT 3,H - 0x5C
                    case 0x5C:
                        TestBit(0b0000_1000, RegH);
                        return 2;

                    //BIT 3,L - 0x5D
                    case 0x5D:
                        TestBit(0b0000_1000, RegL);
                        return 2;

                    //BIT 3,(HL) - 0x5E
                    case 0x5E:
                        TestBit(0b0000_1000, RegL);
                        return 2;

                    //BIT 3,A - 0x5F
                    case 0x5F:
                        TestBit(0b0000_1000, RegA);
                        return 2;

                    //BIT 4,B - 0x60
                    case 0x60:
                        TestBit(0b0001_0000, RegB);
                        return 2;

                    //BIT 4,C - 0x61
                    case 0x61:
                        TestBit(0b0001_0000, RegC);
                        return 2;

                    //BIT 4,D - 0x62
                    case 0x62:
                        TestBit(0b0001_0000, RegD);
                        return 2;

                    //BIT 4,E - 0x63
                    case 0x63:
                        TestBit(0b0001_0000, RegE);
                        return 2;

                    //BIT 4,H - 0x64
                    case 0x64:
                        TestBit(0b0001_0000, RegH);
                        return 2;

                    //BIT 4,L - 0x65
                    case 0x65:
                        TestBit(0b0001_0000, RegL);
                        return 2;

                    //BIT 4,(HL) - 0x66
                    case 0x66:
                        TestBit(0b0001_0000, RegL);
                        return 2;

                    //BIT 4,A - 0x67
                    case 0x67:
                        TestBit(0b0001_0000, RegA);
                        return 2;

                    //BIT 5,B - 0x68
                    case 0x68:
                        TestBit(0b0010_0000, RegB);
                        return 2;

                    //BIT 5,C - 0x69
                    case 0x69:
                        TestBit(0b0010_0000, RegC);
                        return 2;

                    //BIT 5,D - 0x6A
                    case 0x6A:
                        TestBit(0b0010_0000, RegD);
                        return 2;

                    //BIT 5,E - 0x6B
                    case 0x6B:
                        TestBit(0b0010_0000, RegE);
                        return 2;

                    //BIT 5,H - 0x6C
                    case 0x6C:
                        TestBit(0b0010_0000, RegH);
                        return 2;

                    //BIT 5,L - 0x6D
                    case 0x6D:
                        TestBit(0b0010_0000, RegL);
                        return 2;

                    //BIT 5,(HL) - 0x6E
                    case 0x6E:
                        TestBit(0b0010_0000, RegL);
                        return 2;

                    //BIT 5,A - 0x6F
                    case 0x6F:
                        TestBit(0b0010_0000, RegA);
                        return 2;

                    //BIT 6,B - 0x70
                    case 0x70:
                        TestBit(0b0100_0000, RegB);
                        return 2;

                    //BIT 6,C - 0x71
                    case 0x71:
                        TestBit(0b0100_0000, RegC);
                        return 2;

                    //BIT 6,D - 0x72
                    case 0x72:
                        TestBit(0b0100_0000, RegD);
                        return 2;

                    //BIT 6,E - 0x73
                    case 0x73:
                        TestBit(0b0100_0000, RegE);
                        return 2;

                    //BIT 6,H - 0x74
                    case 0x74:
                        TestBit(0b0100_0000, RegH);
                        return 2;

                    //BIT 6,L - 0x75
                    case 0x75:
                        TestBit(0b0100_0000, RegL);
                        return 2;

                    //BIT 6,(HL) - 0x76
                    case 0x76:
                        TestBit(0b0100_0000, RegL);
                        return 2;

                    //BIT 6,A - 0x77
                    case 0x77:
                        TestBit(0b0100_0000, RegA);
                        return 2;

                    //BIT 7,B - 0x78
                    case 0x78:
                        TestBit(0b1000_0000, RegB);
                        return 2;

                    //BIT 7,C - 0x79
                    case 0x79:
                        TestBit(0b1000_0000, RegC);
                        return 2;

                    //BIT 7,D - 0x7A
                    case 0x7A:
                        TestBit(0b1000_0000, RegD);
                        return 2;

                    //BIT 7,E - 0x7B
                    case 0x7B:
                        TestBit(0b1000_0000, RegE);
                        return 2;

                    //BIT 7,H - 0x7C
                    case 0x7C:
                        TestBit(0b1000_0000, RegH);
                        return 2;

                    //BIT 7,L - 0x7D
                    case 0x7D:
                        TestBit(0b1000_0000, RegL);
                        return 2;

                    //BIT 7,(HL) - 0x7E
                    case 0x7E:
                        TestBit(0b1000_0000, RegL);
                        return 2;

                    //BIT 7,A - 0x7F
                    case 0x7F:
                        TestBit(0b1000_0000, RegA);
                        return 2;

                    // RES 0,B - 0x80
                    case 0x80:
                        ResetBit(0b000_0001, RegB);
                        return 2;

                    // RES 0,C - 0x81
                    case 0x81:
                        ResetBit(0b000_0001, RegC);
                        return 2;

                    // RES 0,D - 0x82
                    case 0x82:
                        ResetBit(0b000_0001, RegD);
                        return 2;

                    // RES 0,E - 0x83
                    case 0x83:
                        ResetBit(0b000_0001, RegE);
                        return 2;

                    // RES 0,H - 0x84
                    case 0x84:
                        ResetBit(0b000_0001, RegH);
                        return 2;

                    // RES 0,L - 0x85
                    case 0x85:
                        ResetBit(0b000_0001, RegL);
                        return 2;

                    // RES 0,(HL) - 0x86
                    case 0x86:
                        ResetBit(0b000_0001, RegL);
                        return 2;

                    // RES 0,A - 0x87
                    case 0x87:
                        ResetBit(0b000_0001, RegA);
                        return 2;

                    // RES 1,B - 0x88
                    case 0x88:
                        ResetBit(0b000_0010, RegB);
                        return 2;

                    // RES 1,C - 0x89
                    case 0x89:
                        ResetBit(0b000_0010, RegC);
                        return 2;

                    // RES 1,D - 0x8A
                    case 0x8A:
                        ResetBit(0b000_0010, RegD);
                        return 2;

                    // RES 1,E - 0x8B
                    case 0x8B:
                        ResetBit(0b000_0010, RegE);
                        return 2;

                    // RES 1,H - 0x8C
                    case 0x8C:
                        ResetBit(0b000_0010, RegH);
                        return 2;

                    // RES 1,L - 0x8D
                    case 0x8D:
                        ResetBit(0b000_0010, RegL);
                        return 2;

                    // RES 1,(HL) - 0x8E
                    case 0x8E:
                        ResetBit(0b000_0010, RegL);
                        return 2;

                    // RES 1,A - 0x8F
                    case 0x8F:
                        ResetBit(0b000_0010, RegA);
                        return 2;

                    // RES 2,B - 0x90
                    case 0x90:
                        ResetBit(0b000_0100, RegB);
                        return 2;

                    // RES 2,C - 0x91
                    case 0x91:
                        ResetBit(0b000_0100, RegC);
                        return 2;

                    // RES 2,D - 0x92
                    case 0x92:
                        ResetBit(0b000_0100, RegD);
                        return 2;

                    // RES 2,E - 0x93
                    case 0x93:
                        ResetBit(0b000_0100, RegE);
                        return 2;

                    // RES 2,H - 0x94
                    case 0x94:
                        ResetBit(0b000_0100, RegH);
                        return 2;

                    // RES 2,L - 0x95
                    case 0x95:
                        ResetBit(0b000_0100, RegL);
                        return 2;

                    // RES 2,(HL) - 0x96
                    case 0x96:
                        ResetBit(0b000_0100, RegL);
                        return 2;

                    // RES 2,A - 0x97
                    case 0x97:
                        ResetBit(0b000_0100, RegA);
                        return 2;

                    // RES 3,B - 0x98
                    case 0x98:
                        ResetBit(0b000_1000, RegB);
                        return 2;

                    // RES 3,C - 0x99
                    case 0x99:
                        ResetBit(0b000_1000, RegC);
                        return 2;

                    // RES 3,D - 0x9A
                    case 0x9A:
                        ResetBit(0b000_1000, RegD);
                        return 2;

                    // RES 3,E - 0x9B
                    case 0x9B:
                        ResetBit(0b000_1000, RegE);
                        return 2;

                    // RES 3,H - 0x9C
                    case 0x9C:
                        ResetBit(0b000_1000, RegH);
                        return 2;

                    // RES 3,L - 0x9D
                    case 0x9D:
                        ResetBit(0b000_1000, RegL);
                        return 2;

                    // RES 3,(HL) - 0x9E
                    case 0x9E:
                        ResetBit(0b000_1000, RegL);
                        return 2;

                    // RES 3,A - 0x9F
                    case 0x9F:
                        ResetBit(0b000_1000, RegA);
                        return 2;

                    // RES 4,B - 0xA0
                    case 0xA0:
                        ResetBit(0b0001_0000, RegB);
                        return 2;

                    // RES 4,C - 0xA1
                    case 0xA1:
                        ResetBit(0b0001_0000, RegC);
                        return 2;

                    // RES 4,D - 0xA2
                    case 0xA2:
                        ResetBit(0b0001_0000, RegD);
                        return 2;

                    // RES 4,E - 0xA3
                    case 0xA3:
                        ResetBit(0b0001_0000, RegE);
                        return 2;

                    // RES 4,H - 0xA4
                    case 0xA4:
                        ResetBit(0b0001_0000, RegH);
                        return 2;

                    // RES 4,L - 0xA5
                    case 0xA5:
                        ResetBit(0b0001_0000, RegL);
                        return 2;

                    // RES 4,(HL) - 0xA6
                    case 0xA6:
                        ResetBit(0b0001_0000, RegL);
                        return 2;

                    // RES 4,A - 0xA7
                    case 0xA7:
                        ResetBit(0b0001_0000, RegA);
                        return 2;

                    // RES 5,B - 0xA8
                    case 0xA8:
                        ResetBit(0b0010_0000, RegB);
                        return 2;

                    // RES 5,C - 0xA9
                    case 0xA9:
                        ResetBit(0b0010_0000, RegC);
                        return 2;

                    // RES 5,D - 0xAA
                    case 0xAA:
                        ResetBit(0b0010_0000, RegD);
                        return 2;

                    // RES 5,E - 0xAB
                    case 0xAB:
                        ResetBit(0b0010_0000, RegE);
                        return 2;

                    // RES 5,H - 0xAC
                    case 0xAC:
                        ResetBit(0b0010_0000, RegH);
                        return 2;

                    // RES 5,L - 0xAD
                    case 0xAD:
                        ResetBit(0b0010_0000, RegL);
                        return 2;

                    // RES 5,(HL) - 0xAE
                    case 0xAE:
                        ResetBit(0b0010_0000, RegL);
                        return 2;

                    // RES 5,A - 0xAF
                    case 0xAF:
                        ResetBit(0b0010_0000, RegA);
                        return 2;

                    // RES 6,B - 0xB0
                    case 0xB0:
                        ResetBit(0b0100_0000, RegB);
                        return 2;

                    // RES 6,C - 0xB1
                    case 0xB1:
                        ResetBit(0b0100_0000, RegC);
                        return 2;

                    // RES 6,D - 0xB2
                    case 0xB2:
                        ResetBit(0b0100_0000, RegD);
                        return 2;

                    // RES 6,E - 0xB3
                    case 0xB3:
                        ResetBit(0b0100_0000, RegE);
                        return 2;

                    // RES 6,H - 0xB4
                    case 0xB4:
                        ResetBit(0b0100_0000, RegH);
                        return 2;

                    // RES 6,L - 0xB5
                    case 0xB5:
                        ResetBit(0b0100_0000, RegL);
                        return 2;

                    // RES 6,(HL) - 0xB6
                    case 0xB6:
                        ResetBit(0b0100_0000, RegL);
                        return 2;

                    // RES 6,A - 0xB7
                    case 0xB7:
                        ResetBit(0b0100_0000, RegA);
                        return 2;

                    // RES 7,B - 0xB8
                    case 0xB8:
                        ResetBit(0b1000_0000, RegB);
                        return 2;

                    // RES 7,C - 0xB9
                    case 0xB9:
                        ResetBit(0b1000_0000, RegC);
                        return 2;

                    // RES 7,D - 0xBA
                    case 0xBA:
                        ResetBit(0b1000_0000, RegD);
                        return 2;

                    // RES 7,E - 0xBB
                    case 0xBB:
                        ResetBit(0b1000_0000, RegE);
                        return 2;

                    // RES 7,H - 0xBC
                    case 0xBC:
                        ResetBit(0b1000_0000, RegH);
                        return 2;

                    // RES 7,L - 0xBD
                    case 0xBD:
                        ResetBit(0b1000_0000, RegL);
                        return 2;

                    // RES 7,(HL) - 0xBE
                    case 0xBE:
                        ResetBit(0b1000_0000, RegL);
                        return 2;

                    // RES 7,A - 0xBF
                    case 0xBF:
                        ResetBit(0b1000_0000, RegA);
                        return 2;

                    // SET 0,B - 0xC0
                    case 0xC0:
                        SetBit(0b0000_0001, RegB);
                        return 2;

                    // SET 0,C - 0xC1
                    case 0xC1:
                        SetBit(0b0000_0001, RegC);
                        return 2;

                    // SET 0,D - 0xC2
                    case 0xC2:
                        SetBit(0b0000_0001, RegD);
                        return 2;

                    // SET 0,E - 0xC3
                    case 0xC3:
                        SetBit(0b0000_0001, RegE);
                        return 2;

                    // SET 0,H - 0xC4
                    case 0xC4:
                        SetBit(0b0000_0001, RegH);
                        return 2;

                    // SET 0,L - 0xC5
                    case 0xC5:
                        SetBit(0b0000_0001, RegL);
                        return 2;

                    // SET 0,(HL) - 0xC6
                    case 0xC6:
                        SetBit(0b0000_0001, RegL);
                        return 2;

                    // SET 0,A - 0xC7
                    case 0xC7:
                        SetBit(0b0000_0001, RegA);
                        return 2;

                    // SET 1,B - 0xC8
                    case 0xC8:
                        SetBit(0b0000_0010, RegB);
                        return 2;

                    // SET 1,C - 0xC9
                    case 0xC9:
                        SetBit(0b0000_0010, RegC);
                        return 2;

                    // SET 1,D - 0xCA
                    case 0xCA:
                        SetBit(0b0000_0010, RegD);
                        return 2;

                    // SET 1,E - 0xCB
                    case 0xCB:
                        SetBit(0b0000_0010, RegE);
                        return 2;

                    // SET 1,H - 0xCC
                    case 0xCC:
                        SetBit(0b0000_0010, RegH);
                        return 2;

                    // SET 1,L - 0xCD
                    case 0xCD:
                        SetBit(0b0000_0010, RegL);
                        return 2;

                    // SET 1,(HL) - 0xCE
                    case 0xCE:
                        SetBit(0b0000_0010, RegL);
                        return 2;

                    // SET 1,A - 0xCF
                    case 0xCF:
                        SetBit(0b0000_0010, RegA);
                        return 2;

                    // SET 2,B - 0xD0
                    case 0xD0:
                        SetBit(0b0000_0100, RegB);
                        return 2;

                    // SET 2,C - 0xD1
                    case 0xD1:
                        SetBit(0b0000_0100, RegC);
                        return 2;

                    // SET 2,D - 0xD2
                    case 0xD2:
                        SetBit(0b0000_0100, RegD);
                        return 2;

                    // SET 2,E - 0xD3
                    case 0xD3:
                        SetBit(0b0000_0100, RegE);
                        return 2;

                    // SET 2,H - 0xD4
                    case 0xD4:
                        SetBit(0b0000_0100, RegH);
                        return 2;

                    // SET 2,L - 0xD5
                    case 0xD5:
                        SetBit(0b0000_0100, RegL);
                        return 2;

                    // SET 2,(HL) - 0xD6
                    case 0xD6:
                        SetBit(0b0000_0100, RegL);
                        return 2;

                    // SET 2,A - 0xD7
                    case 0xD7:
                        SetBit(0b0000_0100, RegA);
                        return 2;

                    // SET 3,B - 0xD8
                    case 0xD8:
                        SetBit(0b0000_1000, RegB);
                        return 2;

                    // SET 3,C - 0xD9
                    case 0xD9:
                        SetBit(0b0000_1000, RegC);
                        return 2;

                    // SET 3,D - 0xDA
                    case 0xDA:
                        SetBit(0b0000_1000, RegD);
                        return 2;

                    // SET 3,E - 0xDB
                    case 0xDB:
                        SetBit(0b0000_1000, RegE);
                        return 2;

                    // SET 3,H - 0xDC
                    case 0xDC:
                        SetBit(0b0000_1000, RegH);
                        return 2;

                    // SET 3,L - 0xDD
                    case 0xDD:
                        SetBit(0b0000_1000, RegL);
                        return 2;

                    // SET 3,(HL) - 0xDE
                    case 0xDE:
                        SetBit(0b0000_1000, RegL);
                        return 2;

                    // SET 3,A - 0xDF
                    case 0xDF:
                        SetBit(0b0000_1000, RegA);
                        return 2;

                    // SET 4,B - 0xE0
                    case 0xE0:
                        SetBit(0b0001_0000, RegB);
                        return 2;

                    // SET 4,C - 0xE1
                    case 0xE1:
                        SetBit(0b0001_0000, RegC);
                        return 2;

                    // SET 4,D - 0xE2
                    case 0xE2:
                        SetBit(0b0001_0000, RegD);
                        return 2;

                    // SET 4,E - 0xE3
                    case 0xE3:
                        SetBit(0b0001_0000, RegE);
                        return 2;

                    // SET 4,H - 0xE4
                    case 0xE4:
                        SetBit(0b0001_0000, RegH);
                        return 2;

                    // SET 4,L - 0xE5
                    case 0xE5:
                        SetBit(0b0001_0000, RegL);
                        return 2;

                    // SET 4,(HL) - 0xE6
                    case 0xE6:
                        SetBit(0b0001_0000, RegL);
                        return 2;

                    // SET 4,A - 0xE7
                    case 0xE7:
                        SetBit(0b0001_0000, RegA);
                        return 2;

                    // SET 5,B - 0xE8
                    case 0xE8:
                        SetBit(0b0010_0000, RegB);
                        return 2;

                    // SET 5,C - 0xE9
                    case 0xE9:
                        SetBit(0b0010_0000, RegC);
                        return 2;

                    // SET 5,D - 0xEA
                    case 0xEA:
                        SetBit(0b0010_0000, RegD);
                        return 2;

                    // SET 5,E - 0xEB
                    case 0xEB:
                        SetBit(0b0010_0000, RegE);
                        return 2;

                    //  SET 5,H - 0xEC
                    case 0xEC:
                        SetBit(0b0010_0000, RegH);
                        return 2;

                    // SET 5,L - 0xED
                    case 0xED:
                        SetBit(0b0010_0000, RegL);
                        return 2;

                    // SET 5,(HL) - 0xEE
                    case 0xEE:
                        SetBit(0b0010_0000, RegL);
                        return 2;

                    // SET 5,A - 0xEF
                    case 0xEF:
                        SetBit(0b0010_0000, RegA);
                        return 2;

                    // SET 6,B - 0xF0
                    case 0xF0:
                        SetBit(0b0100_0000, RegB);
                        return 2;

                    // SET 6,C - 0xF1
                    case 0xF1:
                        SetBit(0b0100_0000, RegC);
                        return 2;

                    // SET 6,D - 0xF2
                    case 0xF2:
                        SetBit(0b0100_0000, RegD);
                        return 2;

                    // SET 6,E - 0xF3
                    case 0xF3:
                        SetBit(0b0100_0000, RegE);
                        return 2;

                    // SET 6,H - 0xF4
                    case 0xF4:
                        SetBit(0b0100_0000, RegH);
                        return 2;

                    // SET 6,L - 0xF5
                    case 0xF5:
                        SetBit(0b0100_0000, RegL);
                        return 2;

                    // SET 6,(HL) - 0xF6
                    case 0xF6:
                        SetBit(0b0100_0000, RegL);
                        return 2;

                    // SET 6,A - 0xF7
                    case 0xF7:
                        SetBit(0b0100_0000, RegA);
                        return 2;

                    // SET 7,B - 0xF8
                    case 0xF8:
                        SetBit(0b1000_0000, RegB);
                        return 2;

                    // SET 7,C - 0xF9
                    case 0xF9:
                        SetBit(0b1000_0000, RegC);
                        return 2;

                    // SET 7,D - 0xFA
                    case 0xFA:
                        SetBit(0b1000_0000, RegD);
                        return 2;

                    // SET 7,E - 0xFB
                    case 0xFB:
                        SetBit(0b1000_0000, RegE);
                        return 2;

                    // SET 7,H - 0xFC
                    case 0xFC:
                        SetBit(0b1000_0000, RegH);
                        return 2;

                    // SET 7,L - 0xFD
                    case 0xFD:
                        SetBit(0b1000_0000, RegL);
                        return 2;

                    // SET 7,(HL) - 0xFE
                    case 0xFE:
                        SetBit(0b1000_0000, RegL);
                        return 2;

                    // SET 7,A - 0xFF
                    case 0xFF:
                        SetBit(0b1000_0000, RegA);
                        return 2;

                    default:
                        System.Diagnostics.Debug.WriteLine("OPCODE: " + opcode + " not implemented!");
                        return 0;
                }

            // CALL Z,u16 - 0xCC
            case 0xCC:

            // CALL u16 - 0xCD
            case 0xCD:

            // ADC A,u8 - 0xCE
            case 0xCE:
                ADC8BitRegisters(ref RegA, FetchNextByte());
                return 2;

            // RST 08h - 0xCF
            case 0xCF:
                CALL(0x00, 0x08);
                return 4;

            // RET NC - 0xD0
            case 0xD0:

            // POP DE - 0xD1
            case 0xD1:

            // JP NC,u16 - 0xD2
            case 0xD2:

            // CALL NC,u16 - 0xD4
            case 0xD4:

            // PUSH DE - 0xD5
            case 0xD5:

            // SUB A,u8 - 0xD6
            case 0xD6:

            // RST 10h - 0xD7
            case 0xD7:

            // RET C - 0xD8
            case 0xD8:

            // RETI - 0xD9
            case 0xD9:

            // JP C,u16 - 0xDA
            case 0xDA:

            // CALL C,u16 - 0xDC
            case 0xDC:

            // SBC A,u8 - 0xDE
            case 0xDE:

            // RST 18h - 0xDF
            case 0xDF:

            // LD (FF00+u8),A - 0xE0
            case 0xE0:

            // POP HL - 0xE1
            case 0xE1:
                POP(RegH, RegL);
                return 3;

            // LD (FF00+C),A - 0xE2
            case 0xE2:
                memory[(byte)(0xFF00 + RegC)] = RegA;
                return 2;

            // PUSH HL - 0xE5
            case 0xE5:
                PUSH(RegH, RegL);
                return 3;

            // AND A,u8 - 0xE6
            case 0xE6:
                AND8BitRegisters(ref RegA, FetchNextByte());
                return 2;


            // RST 20h - 0xE7
            case 0xE7:
                CALL(0x00, 0x20);
                return 4;

            // ADD SP,i8 - 0xE8
            case 0xE8:
                {
                    sbyte i8 = (sbyte)FetchNextByte();
                    int carry = (SP + i8) >> 16;

                    // Check half-carry flag
                    if ((((SP & 0b0000_1111_1111_1111) + i8) & 0b0001_0000_0000_0000) == 0b0001_0000_0000_0000)
                        SetFlagH(true);
                    else
                        SetFlagH(false);

                    SP = (ushort)(SP + i8);

                    SetFlagZ(false);
                    SetFlagN(false);

                    if (carry >= 1)
                        SetFlagC(true);
                    else
                        SetFlagC(false);

                    return 4;
                }


            // JP HL - 0xE9
            case 0xE9:


            // LD (u16),A - 0xEA
            case 0xEA:
                {
                    byte upperByte = FetchNextByte();
                    byte lowerByte = FetchNextByte();

                    memory[upperByte] = (byte)(RegA >> 8);
                    memory[lowerByte] = (byte)(RegA);

                    return 4;
                }

            // XOR A,u8 - 0xEE
            case 0xEE:
                XOR8BitRegisters(ref RegA, FetchNextByte());
                return 2;


            // RST 28h - 0xEF
            case 0xEF:
                CALL(0x00, 0x38);
                return 4;


            // LD A,(FF00+u8) - 0xF0
            case 0xF0:
                {
                    byte u8 = FetchNextByte();

                    RegA = (byte) (memory[0xFF00] + u8);
                    return 3;
                }

            // POP AF - 0xF1
            case 0xF1:
                POP(RegA, RegF);
                return 4;

            // LD A,(FF00+C) - 0xF2
            case 0xF2:
                {
                    RegA = (byte)(memory[0xFF00] + RegC);
                    return 2;
                }

            // DI - 0xF3
            case 0xF3:
                memory[0xFFFF] = 0b0000;
                return 1;

            // PUSH AF - 0xF5
            case 0xF5:
                PUSH(RegA, RegF);
                return 4;

            // OR A,u8 - 0xF6
            case 0xF6:
                OR8BitRegisters(ref RegA, FetchNextByte());
                return 2;

            // RST 30h - 0xF7
            case 0xF7:
                CALL(0x00, 0x30);
                return 4;

            // LD HL,SP+i8 - 0xF8
            case 0xF8:
                {
                    ushort HL = (ushort)((RegH << 8) | RegL);
                    sbyte i8 = (sbyte) FetchNextByte();

                    // Check carry
                    int carry = (HL + i8) >> 16;

                    // Check half-carry flag
                    if ((((HL & 0b0000_1111_1111_1111) + i8) & 0b0001_0000_0000_0000) == 0b0001_0000_0000_0000)
                        SetFlagH(true);
                    else
                        SetFlagH(false);

                    HL = (ushort)(SP + i8);

                    SetFlagZ(false);
                    SetFlagN(false);

                    if (carry >= 1)
                        SetFlagC(true);
                    else
                        SetFlagC(false);

                    return 3;
                }

            // LD SP,HL - 0xF9
            case 0xF9:
                {
                    ushort HL = (ushort)((RegH << 8) | RegL);
                    SP = HL;
                    return 2;
                }

            // LD A,(u16) - 0xFA
            case 0xFA:
                {
                    ushort upperByte = FetchNextByte();
                    ushort lowerByte = FetchNextByte();
                    RegA = (byte)((upperByte << 8) | lowerByte);

                    return 4;
                }

            // EI - 0xFB
            case 0xFB:
                memory[0xFFFF] = 0b1111;
                return 1;

            // CP A,u8 - 0xFE
            case 0xFE:
                CP(RegA, FetchNextByte());
                return 4;

            // RST 38h - 0xFF
            case 0xFF:
                CALL(0x00, 0x38);
                return 4;

            default:
                System.Diagnostics.Debug.WriteLine("OPCODE: " + opcode.ToString("X") + " not implemented!");
                return 0;
        }
    }
}