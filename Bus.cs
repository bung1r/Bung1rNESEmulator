

public class Bus
{
    public CPU cpu; 
    public UInt16[] ram = new UInt16[65536]; // 64KB of memory, fake RAM for now. 

    byte read(UInt16 address, bool bReadOnly = false) // not sure why, but this boolean is here. 
    {
        if (address >= 0x0000 && address <= 0xFFFF)
        {
            return ram[address]; // do ram[address] when you get the chance
        }
        return 0x00; // fallback in case of an invalid address
    }
    void write(UInt16 address, byte data)
    {
        if (address >= 0x0000 && address <= 0xFFFF)
        {
            ram[address] = data; // sets the value of the address to the data
        }
    }


    private void ClearRAM()
    {
        for (int address = 0; i < ram.Length; i++)
        {
            ram[address] = 0x00; // set all the values to 0 for clearing.
        }
    }
    public Bus()
    {
        cpu = new CPU();
        ClearRAM();
    }
}

