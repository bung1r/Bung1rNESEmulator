using System.Buffers;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks.Dataflow;

    

public class CPU
{
    // the registers that are used 
    public byte A;
    public byte X;
    public byte Y;

    public ushort PC; // program counter
    public byte SP; // stack pointer
    public byte SR; // status register
    private Bus bus = null!; // trust me bro it's gonna be initialized soon
    Instruction[] instructions; // because 16x16 table of opcodes
    public CPU()
    {
        instructions =
        [
            new Instruction("BRK", BRK, imm, 7 ),new Instruction("ORA", ORA, inX, 6 ),new Instruction("???", XXX, imp, 2 ),new Instruction("???", XXX, imp, 8 ),new Instruction("???", NOP, imp, 3 ),new Instruction("ORA", ORA, zpg, 3 ),new Instruction("ASL", ASL, zpg, 5 ),new Instruction("???", XXX, imp, 5 ),new Instruction("PHP", PHP, imp, 3 ),new Instruction("ORA", ORA, imm, 2 ),new Instruction("ASL", ASL, imp, 2 ),new Instruction("???", XXX, imp, 2 ),new Instruction("???", NOP, imp, 4 ),new Instruction("ORA", ORA, abs, 4 ),new Instruction("ASL", ASL, abs, 6 ),new Instruction("???", XXX, imp, 6 ),
            new Instruction("BPL", BPL, rel, 2 ),new Instruction("ORA", ORA, inY, 5 ),new Instruction("???", XXX, imp, 2 ),new Instruction("???", XXX, imp, 8 ),new Instruction("???", NOP, imp, 4 ),new Instruction("ORA", ORA, zpX, 4 ),new Instruction("ASL", ASL, zpX, 6 ),new Instruction("???", XXX, imp, 6 ),new Instruction("CLC", CLC, imp, 2 ),new Instruction("ORA", ORA, abY, 4 ),new Instruction("???", NOP, imp, 2 ),new Instruction("???", XXX, imp, 7 ),new Instruction("???", NOP, imp, 4 ),new Instruction("ORA", ORA, abX, 4 ),new Instruction("ASL", ASL, abX, 7 ),new Instruction("???", XXX, imp, 7 ),
            new Instruction("JSR", JSR, abs, 6 ),new Instruction("AND", AND, inX, 6 ),new Instruction("???", XXX, imp, 2 ),new Instruction("???", XXX, imp, 8 ),new Instruction("BIT", BIT, zpg, 3 ),new Instruction("AND", AND, zpg, 3 ),new Instruction("ROL", ROL, zpg, 5 ),new Instruction("???", XXX, imp, 5 ),new Instruction("PLP", PLP, imp, 4 ),new Instruction("AND", AND, imm, 2 ),new Instruction("ROL", ROL, imp, 2 ),new Instruction("???", XXX, imp, 2 ),new Instruction("BIT", BIT, abs, 4 ),new Instruction("AND", AND, abs, 4 ),new Instruction("ROL", ROL, abs, 6 ),new Instruction("???", XXX, imp, 6 ),
            new Instruction("BMI", BMI, rel, 2 ),new Instruction("AND", AND, inY, 5 ),new Instruction("???", XXX, imp, 2 ),new Instruction("???", XXX, imp, 8 ),new Instruction("???", NOP, imp, 4 ),new Instruction("AND", AND, zpX, 4 ),new Instruction("ROL", ROL, zpX, 6 ),new Instruction("???", XXX, imp, 6 ),new Instruction("SEC", SEC, imp, 2 ),new Instruction("AND", AND, abY, 4 ),new Instruction("???", NOP, imp, 2 ),new Instruction("???", XXX, imp, 7 ),new Instruction("???", NOP, imp, 4 ),new Instruction("AND", AND, abX, 4 ),new Instruction("ROL", ROL, abX, 7 ),new Instruction("???", XXX, imp, 7 ),
            new Instruction("RTI", RTI, imp, 6 ),new Instruction("EOR", EOR, inX, 6 ),new Instruction("???", XXX, imp, 2 ),new Instruction("???", XXX, imp, 8 ),new Instruction("???", NOP, imp, 3 ),new Instruction("EOR", EOR, zpg, 3 ),new Instruction("LSR", LSR, zpg, 5 ),new Instruction("???", XXX, imp, 5 ),new Instruction("PHA", PHA, imp, 3 ),new Instruction("EOR", EOR, imm, 2 ),new Instruction("LSR", LSR, imp, 2 ),new Instruction("???", XXX, imp, 2 ),new Instruction("JMP", JMP, abs, 3 ),new Instruction("EOR", EOR, abs, 4 ),new Instruction("LSR", LSR, abs, 6 ),new Instruction("???", XXX, imp, 6 ),
            new Instruction("BVC", BVC, rel, 2 ),new Instruction("EOR", EOR, inY, 5 ),new Instruction("???", XXX, imp, 2 ),new Instruction("???", XXX, imp, 8 ),new Instruction("???", NOP, imp, 4 ),new Instruction("EOR", EOR, zpX, 4 ),new Instruction("LSR", LSR, zpX, 6 ),new Instruction("???", XXX, imp, 6 ),new Instruction("CLI", CLI, imp, 2 ),new Instruction("EOR", EOR, abY, 4 ),new Instruction("???", NOP, imp, 2 ),new Instruction("???", XXX, imp, 7 ),new Instruction("???", NOP, imp, 4 ),new Instruction("EOR", EOR, abX, 4 ),new Instruction("LSR", LSR, abX, 7 ),new Instruction("???", XXX, imp, 7 ),
            new Instruction("RTS", RTS, imp, 6 ),new Instruction("ADC", ADC, inX, 6 ),new Instruction("???", XXX, imp, 2 ),new Instruction("???", XXX, imp, 8 ),new Instruction("???", NOP, imp, 3 ),new Instruction("ADC", ADC, zpg, 3 ),new Instruction("ROR", ROR, zpg, 5 ),new Instruction("???", XXX, imp, 5 ),new Instruction("PLA", PLA, imp, 4 ),new Instruction("ADC", ADC, imm, 2 ),new Instruction("ROR", ROR, imp, 2 ),new Instruction("???", XXX, imp, 2 ),new Instruction("JMP", JMP, ind, 5 ),new Instruction("ADC", ADC, abs, 4 ),new Instruction("ROR", ROR, abs, 6 ),new Instruction("???", XXX, imp, 6 ),
            new Instruction("BVS", BVS, rel, 2 ),new Instruction("ADC", ADC, inY, 5 ),new Instruction("???", XXX, imp, 2 ),new Instruction("???", XXX, imp, 8 ),new Instruction("???", NOP, imp, 4 ),new Instruction("ADC", ADC, zpX, 4 ),new Instruction("ROR", ROR, zpX, 6 ),new Instruction("???", XXX, imp, 6 ),new Instruction("SEI", SEI, imp, 2 ),new Instruction("ADC", ADC, abY, 4 ),new Instruction("???", NOP, imp, 2 ),new Instruction("???", XXX, imp, 7 ),new Instruction("???", NOP, imp, 4 ),new Instruction("ADC", ADC, abX, 4 ),new Instruction("ROR", ROR, abX, 7 ),new Instruction("???", XXX, imp, 7 ),
            new Instruction("???", NOP, imp, 2 ),new Instruction("STA", STA, inX, 6 ),new Instruction("???", NOP, imp, 2 ),new Instruction("???", XXX, imp, 6 ),new Instruction("STY", STY, zpg, 3 ),new Instruction("STA", STA, zpg, 3 ),new Instruction("STX", STX, zpg, 3 ),new Instruction("???", XXX, imp, 3 ),new Instruction("DEY", DEY, imp, 2 ),new Instruction("???", NOP, imp, 2 ),new Instruction("TXA", TXA, imp, 2 ),new Instruction("???", XXX, imp, 2 ),new Instruction("STY", STY, abs, 4 ),new Instruction("STA", STA, abs, 4 ),new Instruction("STX", STX, abs, 4 ),new Instruction("???", XXX, imp, 4 ),
            new Instruction("BCC", BCC, rel, 2 ),new Instruction("STA", STA, inY, 6 ),new Instruction("???", XXX, imp, 2 ),new Instruction("???", XXX, imp, 6 ),new Instruction("STY", STY, zpX, 4 ),new Instruction("STA", STA, zpX, 4 ),new Instruction("STX", STX, zpY, 4 ),new Instruction("???", XXX, imp, 4 ),new Instruction("TYA", TYA, imp, 2 ),new Instruction("STA", STA, abY, 5 ),new Instruction("TXS", TXS, imp, 2 ),new Instruction("???", XXX, imp, 5 ),new Instruction("???", NOP, imp, 5 ),new Instruction("STA", STA, abX, 5 ),new Instruction("???", XXX, imp, 5 ),new Instruction("???", XXX, imp, 5 ),
            new Instruction("LDY", LDY, imm, 2 ),new Instruction("LDA", LDA, inX, 6 ),new Instruction("LDX", LDX, imm, 2 ),new Instruction("???", XXX, imp, 6 ),new Instruction("LDY", LDY, zpg, 3 ),new Instruction("LDA", LDA, zpg, 3 ),new Instruction("LDX", LDX, zpg, 3 ),new Instruction("???", XXX, imp, 3 ),new Instruction("TAY", TAY, imp, 2 ),new Instruction("LDA", LDA, imm, 2 ),new Instruction("TAX", TAX, imp, 2 ),new Instruction("???", XXX, imp, 2 ),new Instruction("LDY", LDY, abs, 4 ),new Instruction("LDA", LDA, abs, 4 ),new Instruction("LDX", LDX, abs, 4 ),new Instruction("???", XXX, imp, 4 ),
            new Instruction("BCS", BCS, rel, 2 ),new Instruction("LDA", LDA, inY, 5 ),new Instruction("???", XXX, imp, 2 ),new Instruction("???", XXX, imp, 5 ),new Instruction("LDY", LDY, zpX, 4 ),new Instruction("LDA", LDA, zpX, 4 ),new Instruction("LDX", LDX, zpY, 4 ),new Instruction("???", XXX, imp, 4 ),new Instruction("CLV", CLV, imp, 2 ),new Instruction("LDA", LDA, abY, 4 ),new Instruction("TSX", TSX, imp, 2 ),new Instruction("???", XXX, imp, 4 ),new Instruction("LDY", LDY, abX, 4 ),new Instruction("LDA", LDA, abX, 4 ),new Instruction("LDX", LDX, abY, 4 ),new Instruction("???", XXX, imp, 4 ),
            new Instruction("CPY", CPY, imm, 2 ),new Instruction("CMP", CMP, inX, 6 ),new Instruction("???", NOP, imp, 2 ),new Instruction("???", XXX, imp, 8 ),new Instruction("CPY", CPY, zpg, 3 ),new Instruction("CMP", CMP, zpg, 3 ),new Instruction("DEC", DEC, zpg, 5 ),new Instruction("???", XXX, imp, 5 ),new Instruction("INY", INY, imp, 2 ),new Instruction("CMP", CMP, imm, 2 ),new Instruction("DEX", DEX, imp, 2 ),new Instruction("???", XXX, imp, 2 ),new Instruction("CPY", CPY, abs, 4 ),new Instruction("CMP", CMP, abs, 4 ),new Instruction("DEC", DEC, abs, 6 ),new Instruction("???", XXX, imp, 6 ),
            new Instruction("BNE", BNE, rel, 2 ),new Instruction("CMP", CMP, inY, 5 ),new Instruction("???", XXX, imp, 2 ),new Instruction("???", XXX, imp, 8 ),new Instruction("???", NOP, imp, 4 ),new Instruction("CMP", CMP, zpX, 4 ),new Instruction("DEC", DEC, zpX, 6 ),new Instruction("???", XXX, imp, 6 ),new Instruction("CLD", CLD, imp, 2 ),new Instruction("CMP", CMP, abY, 4 ),new Instruction("NOP", NOP, imp, 2 ),new Instruction("???", XXX, imp, 7 ),new Instruction("???", NOP, imp, 4 ),new Instruction("CMP", CMP, abX, 4 ),new Instruction("DEC", DEC, abX, 7 ),new Instruction("???", XXX, imp, 7 ),
            new Instruction("CPX", CPX, imm, 2 ),new Instruction("SBC", SBC, inX, 6 ),new Instruction("???", NOP, imp, 2 ),new Instruction("???", XXX, imp, 8 ),new Instruction("CPX", CPX, zpg, 3 ),new Instruction("SBC", SBC, zpg, 3 ),new Instruction("INC", INC, zpg, 5 ),new Instruction("???", XXX, imp, 5 ),new Instruction("INX", INX, imp, 2 ),new Instruction("SBC", SBC, imm, 2 ),new Instruction("NOP", NOP, imp, 2 ),new Instruction("???", SBC, imp, 2 ),new Instruction("CPX", CPX, abs, 4 ),new Instruction("SBC", SBC, abs, 4 ),new Instruction("INC", INC, abs, 6 ),new Instruction("???", XXX, imp, 6 ),
            new Instruction("BEQ", BEQ, rel, 2 ),new Instruction("SBC", SBC, inY, 5 ),new Instruction("???", XXX, imp, 2 ),new Instruction("???", XXX, imp, 8 ),new Instruction("???", NOP, imp, 4 ),new Instruction("SBC", SBC, zpX, 4 ),new Instruction("INC", INC, zpX, 6 ),new Instruction("???", XXX, imp, 6 ),new Instruction("SED", SED, imp, 2 ),new Instruction("SBC", SBC, abY, 4 ),new Instruction("NOP", NOP, imp, 2 ),new Instruction("???", XXX, imp, 7 ),new Instruction("???", NOP, imp, 4 ),new Instruction("SBC", SBC, abX, 4 ),new Instruction("INC", INC, abX, 7 ),new Instruction("???", XXX, imp, 7 ),
        ];
    }
    public void ConnectBus(Bus bus)
    {
        this.bus = bus;
    }

    /* 
    read + write kinda just wraps about the bus's functions. 
    this makes it a little easier to conceptualize each component reading/writing, 
    rather than the bus doing all the work, persay 
    */
    byte read(ushort address, bool bReadOnly = false)
    {
        return bus.cpuRead(address, bReadOnly); // read from the bus
    }

    void write(ushort address, byte data)
    {
        bus.cpuWrite(address, data); 
    }

    void SetFlag(StatusFlag flag, bool value)
    {
        if (value)
        {
            SR |= (byte)flag; // sets flag bit to 1 (? | 1 == 1)
        } else
        {
            SR &= (byte)~flag; // set flag bit to 0 (? & 0 == 0)
        }
    }
    byte GetFlag(StatusFlag flag)
    {
        if ((SR & (byte)flag) == 0) return 0;
        return 1;
    }
    byte GetStatus()
    {
        return SR;
    }
    
    void Push(byte value)
    {
        write((ushort)(0x0100 + SP), value);
        SP--;
    }

    byte Pull()
    {
        SP++;
        return read((ushort)(0x0100 + SP));
    }
    /*
    COPIED FROM THE 6502 Instruction Set at https://www.masswerk.at/6502/6502_instruction_set.html
    
    ADDRESS MODES: 

        A	Accumulator	OPC A	operand is AC (implied single byte instruction)
        abs	absolute	OPC $LLHH	operand is address $HHLL *
        abs,X	absolute, X-indexed	OPC $LLHH,X	operand is address; effective address is address incremented by X with carry **
        abs,Y	absolute, Y-indexed	OPC $LLHH,Y	operand is address; effective address is address incremented by Y with carry **
        #	immediate	OPC #$BB	operand is byte BB
        impl	implied	OPC	operand implied
        ind	indirect	OPC ($LLHH)	operand is address; effective address is contents of word at address: C.w($HHLL)
        X,ind	X-indexed, indirect	OPC ($LL,X)	operand is zeropage address; effective address is word in (LL + X, LL + X + 1), inc. without carry: C.w($00LL + X)
        ind,Y	indirect, Y-indexed	OPC ($LL),Y	operand is zeropage address; effective address is word in (LL, LL + 1) incremented by Y with carry: C.w($00LL) + Y
        rel	relative	OPC $BB	branch target is PC + signed offset BB ***
        zpg	zeropage	OPC $LL	operand is zeropage address (hi-byte is zero, address = $00LL)
        zpg,X	zeropage, X-indexed	OPC $LL,X	operand is zeropage address; effective address is address incremented by X without carry **
        zpg,Y	zeropage, Y-indexed	OPC $LL,Y	operand is zeropage address; effective address is address incremented by Y without carry **
    
    */

    // list of address modes
    byte acc()
    {
        return 0;
    } 
    byte abs() 
    {
        ushort low = read(PC);
        PC++;
        ushort high = read(PC);
        PC++;

        addrAbs = (ushort)(high << 8 | low);
        return 0;
    } // combine because can only read 1 byte at a time, but the address is 2 bytes.
    byte abX()
    {
        ushort low = read(PC);
        PC++;
        ushort high = read(PC);
        PC++;

        addrAbs = (ushort)(high << 8 | low);
        addrAbs += X;

        if ((addrAbs & 0xFF00) != (high << 8)) return 1; // if high byte different after adding X (page switch)
        return 0;
    }
    byte abY()
    {
        ushort low = read(PC);
        PC++;
        ushort high = read(PC);
        PC++;

        addrAbs = (ushort)(high << 8 | low);
        addrAbs += Y;

        if ((addrAbs & 0xFF00) != (high << 8)) return 1; // if high byte different after adding X (page switch)
        return 0;
    } 
    byte imm()
    {
        addrAbs = PC++;
        return 0;
    } 
    byte imp()
    {
        fetched = A;
        return 0;
    }
    byte ind() // to make this as similar to the NES system, implement the page overflow bug too
    {
        byte ptrLow = read(PC);
        PC++;
        byte ptrHigh = read(PC);
        PC++;
        ushort pointer = (ushort)(ptrHigh << 8 | ptrLow);

        ushort low = read(pointer);
        ushort high;

        if ((pointer & 0x00FF) == 0x00FF)
        {
            high = read((ushort)(pointer & 0xFF00));
        } else
        {
            high = read((ushort)(pointer + 1));
        }

        addrAbs = (ushort)(high << 8 | low);
        return 0;
    }
    byte inX()
    {
        byte zpAddr = read(PC);
        PC++;
        byte ptr = (byte)(zpAddr + X); // add X to the low byte, but wrap around if it goes over 0xFF
        
        byte low = read(ptr);
        byte high = read((byte)(ptr + 1));
        addrAbs = (ushort)(high << 8 | low);
        return 0;
    } 
    byte inY()
    {
        byte zpAddr = read(PC);
        PC++;
        byte ptr = (byte)(zpAddr);

        ushort baseAddr = (ushort)(read(ptr) | (read((byte)(ptr + 1)) << 8));
        addrAbs = (ushort)(baseAddr + Y);
        return 0;
    } 
    byte rel()
    {
        addrRel = read(PC);
        PC++;
        if ((addrRel & 0x80) != 0) addrRel |= 0xFF00; // if the value is negative, sign extend it to 16 bits by setting the high byte to 0xFF
        return 0;
    }
    byte zpg()
    {
        addrAbs = read(PC);
        PC++;
        addrAbs &= 0x00FF;
        return 0;
    } 
    byte zpX()
    {
        addrAbs = (ushort)(read(PC) + X);
        PC++;
        addrAbs &= 0x00FF;
        return 0;
    } 
    byte zpY()
    {
        addrAbs = (ushort)(read(PC) + Y);
        PC++;
        addrAbs &= 0x00FF;
        return 0;
    }

    byte XXX()
    {
        return 0;
    } // all illegal opcodes, might implement some useful ones later. 

    // all 56 operates in alphabetical order. yay!
    byte ADC()
    {
        fetch();
        ushort temp = (ushort)(A + fetched + GetFlag(StatusFlag.C));
        SetFlag(StatusFlag.C, temp > 255);
        SetFlag(StatusFlag.Z, (temp & 0x00FF) == 0);
        // i'm not going to lie, just copied this because I was not going to derive this on my own
        SetFlag(StatusFlag.V, (~(A ^ fetched) & (A ^ temp) & 0x0080) != 0);
        SetFlag(StatusFlag.N, (temp & 0x80) != 0);
        A = (byte)temp;
        return 0;
    } // add with carry
    byte AND()
    {
        fetch();
        A = (byte)(A & fetched);
        SetFlag(StatusFlag.N, (A & 0b10000000) != 0);
        SetFlag(StatusFlag.Z, A == 0);
        return 0;
    } // and (with accumulator)
    byte ASL()
    {
        fetch();
        byte temp = (byte)(fetched << 1);
        SetFlag(StatusFlag.C, (fetched & 0b10000000) != 0);
        SetFlag(StatusFlag.Z, temp == 0);
        SetFlag(StatusFlag.N, (temp & 0b10000000) != 0);
        if (instructions[opcode].addrMode == imp)
        {
            A = temp;
        } else
        {
            write(addrAbs, temp);
        }

        return 0;
    } // arith shft left
    byte BCC()
    {
        if (GetFlag(StatusFlag.C) == 0)
        {
            cycles++;
            addrAbs = (ushort)(PC + addrRel);

            if ((addrAbs & 0b11110000) != (PC & 0b11110000)) cycles++; // checks if different page
            
            PC = addrAbs;
        }
        return 0;
    } // branch on carry clear
    byte BCS()
    {
        if (GetFlag(StatusFlag.C) == 1)
        {
            cycles++;
            addrAbs = (ushort)(PC + addrRel);

            if ((addrAbs & 0b11110000) != (PC & 0b11110000)) cycles++; // checks if different page
            
            PC = addrAbs;
        }
        return 0;
    } // branch on carry set 
    byte BEQ()
    {
        if (GetFlag(StatusFlag.Z) == 1)
        {
            cycles++;
            addrAbs = (ushort)(PC + addrRel);

            if ((addrAbs & 0b11110000) != (PC & 0b11110000)) cycles++; // checks if different page
            
            PC = addrAbs;
        }
        return 0;
    } // branch on equal
    byte BIT()
    {
        fetch();
        SetFlag(StatusFlag.V, (fetched & 0b01000000) != 0);
        SetFlag(StatusFlag.N, (fetched & 0b10000000) != 0);
        SetFlag(StatusFlag.Z, (fetched & A) == 0);
        return 0;
    } // bit test
    byte BMI()
    {
        if (GetFlag(StatusFlag.N) == 1)
        {
            cycles++;
            addrAbs = (ushort)(PC + addrRel);

            if ((addrAbs & 0b11110000) != (PC & 0b11110000)) cycles++; // checks if different page
            
            PC = addrAbs;
        }
        return 0;
    } // branch on minus (negative set)
    byte BNE()
    {
        if (GetFlag(StatusFlag.Z) == 0)
        {
            cycles++;
            addrAbs = (ushort)(PC + addrRel);

            if ((addrAbs & 0b11110000) != (PC & 0b11110000)) cycles++; // checks if different page
            
            PC = addrAbs;
        }
        return 0;
    } // branch on not equal (zero clear)
    byte BPL()
    {
        if (GetFlag(StatusFlag.N) == 0)
        {
            cycles++;
            addrAbs = (ushort)(PC + addrRel);

            if ((addrAbs & 0b11110000) != (PC & 0b11110000)) cycles++; // checks if different page
            
            PC = addrAbs;
        }
        return 0;
    } // branch on plus (negative clear)
    byte BRK()
    {
        
        PC++; 
        
        SetFlag(StatusFlag.I, true);
        Push((byte)((PC >> 8) & 0x00FF));
        Push((byte)(PC & 0x00FF));

        SetFlag(StatusFlag.B, true);
        Push(GetStatus());
        SetFlag(StatusFlag.B, false);

        addrAbs = 0xFFFE;
        ushort low = read(addrAbs);
        ushort high = read((ushort)(addrAbs + 1));
        PC = (ushort)((high << 8) | low);
        return 0;
    } // break / interrupt, same functionality as NMI
    byte BVC()
    {
        if (GetFlag(StatusFlag.V) == 0)
        {
            cycles++;
            addrAbs = (ushort)(PC + addrRel);

            if ((addrAbs & 0b11110000) != (PC & 0b11110000)) cycles++; // checks if different page
            
            PC = addrAbs;
        }
        return 0;
    } // branch on overflow clear
    byte BVS()
    {
        if (GetFlag(StatusFlag.V) == 1)
        {
            cycles++;
            addrAbs = (ushort)(PC + addrRel);

            if ((addrAbs & 0b11110000) != (PC & 0b11110000)) cycles++; // checks if different page
            
            PC = addrAbs;
        }
        return 0;
    } // branch on overflow set
    byte CLC()
    {
        SetFlag(StatusFlag.C, false);
        return 0;
    } // clear carry
    byte CLD()
    {
        SetFlag(StatusFlag.D, false);
        return 0;
    } // clear decimal
    byte CLI()
    {
        SetFlag(StatusFlag.I, false);
        return 0;
    } // clear interrupt disable
    byte CLV()
    {
        SetFlag(StatusFlag.V, false);
        return 0;
    } // clear overflow
    byte CMP()
    {
        fetch();
        SetFlag(StatusFlag.C, A >= fetched);
        SetFlag(StatusFlag.Z, A == fetched);
        SetFlag(StatusFlag.N, (byte)((A - fetched) & 0b10000000) != 0);
        return 0;
    } // compare (with accumulator)
    byte CPX()
    {
        fetch();
        SetFlag(StatusFlag.C, X >= fetched);
        SetFlag(StatusFlag.Z, X == fetched);
        SetFlag(StatusFlag.N, (byte)((X - fetched) & 0b10000000) != 0);
        return 0;
    } // compare with X
    byte CPY()
    {
        fetch();
        SetFlag(StatusFlag.C, Y >= fetched);
        SetFlag(StatusFlag.Z, Y == fetched);
        SetFlag(StatusFlag.N, (byte)((Y - fetched) & 0b10000000) != 0);
        return 0;
    } // compare with Y
    byte DEC()
    {
        fetch();
        byte value = (byte)(fetched - 1);
        write(addrAbs, value);
        SetFlag(StatusFlag.N, (value & 0b10000000) != 0);
        SetFlag(StatusFlag.Z, value == 0);
        return 0;
    } // decrement
    byte DEX()
    {
        X = (byte)(X - 1);
        SetFlag(StatusFlag.N, (X & 0b10000000) != 0);
        SetFlag(StatusFlag.Z, X == 0);
        return 0;
    } // decrement X
    byte DEY()
    {
        Y = (byte)(Y - 1);
        SetFlag(StatusFlag.N, (Y & 0b10000000) != 0);
        SetFlag(StatusFlag.Z, Y == 0);
        return 0;
    } // decrement Y
    byte EOR()
    {
        fetch();
        A = (byte)(A ^ fetched);
        SetFlag(StatusFlag.N, (A & 0b10000000) != 0);
        SetFlag(StatusFlag.Z, A == 0);
        return 0;
    } // exclusive or (with accumulator)
    byte INC()
    {
        fetch();
        byte value = (byte)(fetched + 1);
        write(addrAbs, value);
        SetFlag(StatusFlag.N, (value & 0b10000000) != 0);
        SetFlag(StatusFlag.Z, value == 0);
        return 0;
    } // increment
    byte INX()
    {
        X = (byte)(X + 1);
        SetFlag(StatusFlag.N, (X & 0b10000000) != 0);
        SetFlag(StatusFlag.Z, X == 0);
        return 0;
    } // increment X
    byte INY()
    {
        Y = (byte)(Y + 1);
        SetFlag(StatusFlag.N, (Y & 0b10000000) != 0);
        SetFlag(StatusFlag.Z, Y == 0);
        return 0;
    } // increment Y
    byte JMP()
    {
        PC = addrAbs;
        return 0;
    } // jump
    byte JSR()
    {
        PC--;

        Push((byte)((PC >> 8) & 0x00FF));    
        Push((byte)((PC & 0x00FF)));

        PC = addrAbs;
        return 0;
    } // jump subroutine
    byte LDA()
    {
        fetch();
        A = fetched;
        SetFlag(StatusFlag.N, (A & 0b10000000) != 0);
        SetFlag(StatusFlag.Z, A == 0);
        return 0;
    } // load accumulator
    byte LDX()
    {
        fetch();
        X = fetched;
        SetFlag(StatusFlag.N, (X & 0b10000000) != 0);
        SetFlag(StatusFlag.Z, X == 0);
        return 0;
    } // load X
    byte LDY()
    {
        fetch();
        Y = fetched;
        SetFlag(StatusFlag.N, (Y & 0b10000000) != 0);
        SetFlag(StatusFlag.Z, Y == 0);
        return 0;
    } // load Y
    byte LSR()
    {
        fetch();
        byte temp = (byte)(fetched >> 1);
        SetFlag(StatusFlag.N, false);
        SetFlag(StatusFlag.Z, temp == 0);
        SetFlag(StatusFlag.C, (fetched & 0b00000001) != 0);
        if (instructions[opcode].addrMode == imp)
        {
            A = temp;
        } else
        {
            write(addrAbs, temp);
        }
        return 0;
    } // logical shift right
    byte NOP()
    {
        return 0; 
    } // no operation, ths is cray cray yo!
    byte ORA()
    {
        fetch();
        A = (byte)(A | fetched);
        SetFlag(StatusFlag.N, (A & 0b10000000) != 0);
        SetFlag(StatusFlag.Z, A == 0);
        return 0;
    } // or with accumulator
    byte PHA()
    {
        Push(A);
        return 0;
    } // push accumulator
    byte PHP()
    {
        Push((byte)(SR | 0b0011_0000));
        return 0;
    } // push processor status (SR)
    byte PLA()
    {
        A = Pull();
        SetFlag(StatusFlag.N, (A & 0b10000000) != 0);
        SetFlag(StatusFlag.Z, A == 0);
        return 0;
    } // pull accumulator
    byte PLP()
    {
        byte temp = Pull();
        SR = (byte)(temp &= 0b1100_1111);
        SetFlag(StatusFlag.U, true);
        return 0;
    } // pull processor status (SR)
    byte ROL()
    {
        fetch();
        byte temp = (byte)((fetched << 1) | GetFlag(StatusFlag.C));
        
        SetFlag(StatusFlag.C, (fetched & 0b10000000) != 0);
        SetFlag(StatusFlag.Z, temp == 0);
        SetFlag(StatusFlag.N, (temp & 0b10000000) != 0);
        if (instructions[opcode].addrMode == imp)
        {   
            A = temp;
        } else
        {
            write(addrAbs, temp);
        }
        return 0;
    } // rotate left
    byte ROR()
    {
        fetch();
        byte temp = (byte)((GetFlag(StatusFlag.C) << 7) | ((fetched & 0b11111110) >> 1));
        SetFlag(StatusFlag.C, (fetched & 0b00000001) != 0);
        SetFlag(StatusFlag.Z, temp == 0);
        SetFlag(StatusFlag.N, (temp & 0b10000000) != 0);
        if (instructions[opcode].addrMode == imp)
        {
            A = temp;
        } else
        {   
            write(addrAbs, temp);
        }
        return 0;
    } // rotate right
    byte RTI()
    {
        SR = Pull();
        // SR &= (byte)~StatusFlag.B;
        // SR &=  (byte)~StatusFlag.U;
        SetFlag(StatusFlag.B, false);
        SetFlag(StatusFlag.U, true);

        PC = Pull();
        PC |= (ushort)(Pull() << 8);
        return 0;
    } // return from interrupt
    byte RTS()
    {
        PC = Pull();
        PC |= (ushort)(Pull() << 8);
        PC++;
        return 0;
    } // return from subroutine
    byte SBC()
    {
        fetch();
        ushort val = (ushort)(fetched ^ 0x00FF);
        ushort temp = (ushort)(A + val + (ushort)GetFlag(StatusFlag.C)); 
        SetFlag(StatusFlag.C, (temp & 0xFF00) > 0);
        SetFlag(StatusFlag.Z, ((temp & 0x00FF) == 0));
        SetFlag(StatusFlag.V, ((temp ^ A) & (temp ^ val) & 0x0080) != 0);
        SetFlag(StatusFlag.N, (temp & 0x0080) != 0);
        A = (byte)(temp & 0x00FF);
        return 0;
    } // subtract with carry
    byte SEC()
    {
        SetFlag(StatusFlag.C, true);
        return 0;
    } // set carry
    byte SED()
    {
        SetFlag(StatusFlag.D, true);
        return 0;
    } // set decimal
    byte SEI()
    {
        SetFlag(StatusFlag.I, true);
        return 0;
    } // set interrupt disable
    byte STA()
    {
        write(addrAbs, A);
        return 0;
    } // store accumulator
    byte STX()
    {
        write(addrAbs, X);
        return 0;
    } // store X
    byte STY()
    {
        write(addrAbs, Y);
        return 0;
    } // store Y
    byte TAX()
    {
        X = A;
        SetFlag(StatusFlag.N, (X & 0b10000000) != 0); // simpler to visualize 0b than 0x
        SetFlag(StatusFlag.Z, X == 0);
        return 0;
    } // transfer accumulator to X
    byte TAY()
    {
        Y = A;
        SetFlag(StatusFlag.N, (Y & 0b10000000) != 0);
        SetFlag(StatusFlag.Z, Y == 0);
        return 0;
    } // transfer accumulator to Y
    byte TSX() {
        X = SP;
        SetFlag(StatusFlag.N, (X & 0b10000000) != 0);
        SetFlag(StatusFlag.Z, X == 0);
        return 0;
    } // transfer stack pointer to X
    byte TXA()
    {
        A = X;
        SetFlag(StatusFlag.N, (A & 0b10000000) != 0);
        SetFlag(StatusFlag.Z, A == 0);
        return 0;
    } // transfer X to accumulator
    byte TXS()
    {
        SP = X;
        return 0;
    } // transfer X to stack pointer
    byte TYA()
    {
        A = Y;
        SetFlag(StatusFlag.N, (A & 0b10000000) != 0);
        SetFlag(StatusFlag.Z, A == 0);
        return 0;
    } // transfer Y to accumulator


    // The Clock
    private int totalCycles;
    public void clock()
    {
        // once the cycles reaches 0, do the whole process
        if (cycles == 0)
        {
            

            opcode = read(PC);
            LogTest();
            PC++; // progress the program counter always
            cycles = instructions[opcode].cycles;
           
            byte addCycleAddr = instructions[opcode].addrMode();
            byte addCycleOp = instructions[opcode].operate();

            if (addCycleAddr + addCycleOp > 1) cycles++; // if both the address mode and the operate function add a cycle, then add one more cycle.
        }
        totalCycles++;
        cycles--; // decrement the cycles.
    } // one clock cycle 

    // Interrupts
    void NMI()
    {
        Push((byte)((PC >> 8) & (0x00FF)));
        Push((byte)(PC & 0x00FF));

        SetFlag(StatusFlag.B, false);
        SetFlag(StatusFlag.U, true);
        SetFlag(StatusFlag.I, true);
        Push(GetStatus());

        addrAbs = 0xFFFE;
        ushort low = read(addrAbs);
        ushort high = read((ushort)(addrAbs + 1));
        PC = (ushort)((high << 8) | low);

        cycles = 8;
    } // non maskable (can't stop the NMI train!!!)
    void IRQ()
    {
        if (GetFlag(StatusFlag.I) == 0)
        {
            Push((byte)((PC >> 8) & (ushort)(0x00FF)));
            Push((byte)(PC & (ushort)0x00FF));

            SetFlag(StatusFlag.B, false);
            SetFlag(StatusFlag.U, true);
            SetFlag(StatusFlag.I, true);
            Push(GetStatus());

            addrAbs = 0xFFFE;
            ushort low = read(addrAbs);
            ushort high = read((ushort)(addrAbs + 1));
            PC = (ushort)((high << 8) | low);
            cycles = 7;
        }
    } // maskable, only happens if the I flag is clear. 
    public void RES(){

        A = 0;
        X = 0;
        Y = 0;
        SP = 0xFD;
        PC = 0xC000;
        SetFlag(StatusFlag.U, true);
        SetFlag(StatusFlag.I, true);
        
        ushort low = (ushort)read(0xFFFC);
        ushort high = (ushort)read(0xFFFD);
        
        addrRel = 0x0000;
        addrAbs = 0x0000;
        fetched = 0x00;

        cycles = 8; // arbitrary number I suppose, resets take time
    } // reset

    byte fetch()
    {
        if (instructions[opcode].addrMode != imp)
        {
            fetched = read(addrAbs);
        }
        return fetched;
    }
    private void LogTest()
    {

        string line = $"{PC:X4} {opcode:X2} {instructions[opcode].name} {fetched:X2}         A:{A:X2} X:{X:X2} Y:{Y:X2} P:{SR:X2} SP:{SP:X2} CYC:{totalCycles}\n";

        File.AppendAllText("nestest_output.log", line.ToString());
    }
    byte fetched = 0x00; 
    ushort addrAbs = 0x0000;
    ushort addrRel = 0x00;
    byte opcode = 0x00;
    byte cycles = 0;
    
}

    
// each enum represents a different bit in the status register, 8 because it is a byte!
public enum StatusFlag
{
    C = (1 << 0), // Carry Bit
    Z = (1 << 1), // Zero
    I = (1 << 2), // Disable Interrupts
    D = (1 << 3), // Decimal Mode 
    B = (1 << 4), // Break
    U = (1 << 5), // USELESS!!!!
    V = (1 << 6), // Overflow
    N = (1 << 7), // Negative
}

public struct Instruction
{
    public string name; // for readability and debugging purposes 
    public Func<byte> operate; // duh
    public Func<byte> addrMode; // duh
    public byte cycles; // num of cycles to complete the instruction
    public Instruction(string name, Func<byte> operate, Func<byte> addrMode, byte cycles)
    {
        this.name = name;
        this.operate = operate;
        this.addrMode = addrMode;
        this.cycles = cycles;
    }
}

