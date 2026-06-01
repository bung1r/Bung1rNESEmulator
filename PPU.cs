using System.Net.Mail;

public class PPU
{
    private Cartridge cartridge = null!;
    private Bus bus;
    public byte[] patternTable = new byte[8 * 1024];
    public byte[,] nameTable = new byte[2, 1024];
    public byte[] paletteRAM = new byte[32];
    public byte cpuRead(ushort mappedAddress, bool b)
    {
        byte data = 0x00;
        switch(mappedAddress)
        {
            case 0x0000: // Controls rendering, NMI interrupts, and base nametable addresses.
                break;
            case 0x0001: // Enables/disables rendering and sprite display.
                break;
            case 0x0002: // Reads current PPU status flags (like Vertical Blanking) and resets the address latch.
                break;
            case 0x0003: // Sets the Sprite Object Attribute Memory (OAM) address
                break;
            case 0x0004: // Reads/writes sprite attributes.
                break;
            case 0x0005: // Fine X and Y scrolling coordinates. Requires consecutive writes.
                break;
            case 0x0006: //Sets the target 14 bit PPU bus VRAM address. Requires consecutive writes for high and low bytes.
                break;
            case 0x0007: // Reads/writes video memory (VRAM) at the address set by PPUADDR
                break;
        }  
        return data;
    }
    public void cpuWrite(ushort mappedAddress, byte data)
    {
        switch(mappedAddress)
        {
            case 0x0000: // Controls rendering, NMI interrupts, and base nametable addresses.
                break;
            case 0x0001: // Enables/disables rendering and sprite display.
                break;
            case 0x0002: // Reads current PPU status flags (like Vertical Blanking) and resets the address latch.
                break;
            case 0x0003: // Sets the Sprite Object Attribute Memory (OAM) address
                break;
            case 0x0004: // Reads/writes sprite attributes.
                break;
            case 0x0005: // Fine X and Y scrolling coordinates. Requires consecutive writes.
                break;
            case 0x0006: //Sets the target 14 bit PPU bus VRAM address. Requires consecutive writes for high and low bytes.
                break;
            case 0x0007: // Reads/writes video memory (VRAM) at the address set by PPUADDR
                break;
        }  
    }
    
    public byte ppuRead(ushort address, bool rdonly)
    {
        byte data = 0x00;
        address &= 0x3FFF; // basically limit the range to an actually valid one

        if (address >= 0x0000 && address <= 0x1FFF) // Pattern Tables
        {
            
        } else if (address >= 0x2000 && address <= 0x3EFF) // Nametables
        {
            // note that address range $3000 -> $3EFF mirrors $2000 -> $2EFF
            if (address >= 0x3000) address -= 0x1000;

        } else if (address >= 0x3F00 && address <= 0x3FFF)
        {
            if (address >= 0x3F20) address &= 0x001F;

            if (address == 0x10) address = 0x00;
            if (address == 0x14) address = 0x04;
            if (address == 0x18) address = 0x08;
            if (address == 0x1C) address = 0x0C;
        }

        return data;
    }
    public void ppuWrite(ushort address, byte data)
    {

        address &= 0x3FFF; // basically limit the range to an actually valid one

        if (address >= 0x0000 && address <= 0x1FFF) // Pattern Tables
        {
            
        } else if (address >= 0x2000 && address <= 0x3EFF) // Nametables
        {
            // note that address range $3000 -> $3EFF mirrors $2000 -> $2EFF
            if (address >= 0x3000) address -= 0x1000;   
            
            // there should probably be other logic, or something like that. 
            if (address >= 0x2000 && address <= 0x23FF)
            {
                // name table 0
            } else if (address >= 0x2400 && address <= 0x27FF)
            {
                // name table 1
            }
        } else if (address >= 0x3F00 && address <= 0x3FFF)
        {
            if (address >= 0x3F20) address &= 0x001F; // w mask!            
            if (address == 0x10) address = 0x00;
            if (address == 0x14) address = 0x04;
            if (address == 0x18) address = 0x08;
            if (address == 0x1C) address = 0x0C;
        }
    }
    public void connectCartToPPU(Cartridge cartridge)
    {
        this.cartridge = cartridge;
    }
    public void clock()
    {
        // do something for the PPU clock
    }
    public PPU()
    {
        
    }
}