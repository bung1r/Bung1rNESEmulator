

using System.Security.Cryptography;


public class Bus
{
    public CPU cpu; 
    public PPU ppu;
    public ushort[] ram = new ushort[2048]; // 2KB of memory, real RAM for now!. 

    public byte cpuRead(ushort address, bool bReadOnly = false) // not sure why, but this boolean is here. 
    {
        if (address >= 0x0000 && address <= 0x1FFF)// 8 bytes, but the last 6 bytes just map onto the first 2. (for some reason)
        {
            return (byte)ram[address]; // do ram[address] when you get the chance
        }
        return 0x00; // fallback in case of an invalid address
    }
    public void cpuWrite(ushort address, byte data)
    {
        if (address >= 0x0000 && address <= 0xFFFF) 
        {
            ram[address] = data; // sets the value of the address to the data
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
    public Bus()
    {
        cpu = new CPU();
        ppu = new PPU();
        cpu.ConnectBus(this);
        ClearRAM();
        
    }
}

 

