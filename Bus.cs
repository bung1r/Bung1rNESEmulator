

using System.Collections;
using System.Security.Cryptography;


public class Bus
{
    public Cartridge cartridge;
    public CPU cpu; 
    public PPU ppu;
    public byte[] ram = new byte[2048]; // 2KB of memory, real RAM for now!. 
    private int clockCounter = 0; // counts the amount of time passed total
    public byte cpuRead(ushort address, bool bReadOnly = false) // not sure why, but this boolean is here. 
    {
        byte data = 0x00;
        if (address >= 0x0000 && address <= 0x1FFF)// 8 bytes, but the last 6 bytes just map onto the first 2. (for some reason)
        {
            data = (byte)ram[address & 0x07FF]; // mirror the 6kb with the first 2kb
        } else if (address >= 0x2000 && address <= 0x3FFF)
        {
            // real range is only 2000 -> 2007, anything else in this range repeats 
            // 0010 0000 0000 0000 -> 0010 0000 0000 0111 (only last 3 numbers matter then)
            ushort mappedAddress = (ushort)(address & 0x0007);
            data = ppu.cpuRead(mappedAddress, bReadOnly);
        }
        else if (address >= 0x4020 && address <= 0xFFFF)
        {
            // do something with the cartridge
            if (address >= 0x8000 && address <= 0xFFFF)
            {
                return cartridge.cpuRead(address);
            }
        }
        return data; // fallback in case of an invalid address
    }
    public void cpuWrite(ushort address, byte data)
    {
        if (address >= 0x0000 && address <= 0x1FFF) 
        {
            ram[address] = data; // sets the value of the address to the data
        } 
        else if (address >= 0x2000 && address <= 0x3FFF)
        {
            // real range is only 2000 -> 2007, anything else in this range repeats 
            // 0010 0000 0000 0000 -> 0010 0000 0000 0111 (only last 3 numbers matter then)
            ushort mappedAddress = (ushort)(address & 0x0007);
            ram[mappedAddress] = data;
        }
        else if (address >= 0x4020 && address <= 0xFFFF)
        {
            // Cartridge stuff
            if (address >= 0x8000) // the PRG-ROM of the Catridge
            {
                cartridge.cpuWrite(address, data);
            }
        }
    }
    public byte ppuRead(ushort address, bool bReadOnly = false)
    {
        return 0x00;
    } 
    public void ppuWrite(ushort address)
    {
        return;
    }

    private void ClearRAM()
    {
        for (int address = 0; address < ram.Length; address++)
        {
            ram[address] = 0x00; // set all the values to 0 for clearing.
        }
    }
    public void InsertCartridge(Cartridge cartridge)
    {
        this.cartridge = cartridge;
        ppu.connectCartToPPU(cartridge);
    }
    public void reset()
    {
        cpu.RES();
        clockCounter = 0;
    }
    public void clock()
    {
        clockCounter++;
        ppu.clock();
        if (clockCounter % 3 == 0) cpu.clock();
    }

    public Bus()
    {
        cpu = new CPU();
        ppu = new PPU();
        cpu.ConnectBus(this);
        ClearRAM();
    }
}

 

