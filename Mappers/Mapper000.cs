public class Mapper000 : Mapper
{
    
    public Mapper000(byte prgBanks, byte chrBanks) : base(prgBanks, chrBanks)
    {
        
    }
    public override bool cpuMapWrite(ushort address, out uint mapped)
    {
        mapped = address;
        if (address >= 0x8000 && address <= 0xFFFF)
        {
            mapped = (ushort)(address & (prgBanks > 1 ? 0x7FFF : 0x3FFF));
            return true;
        }
        return false;
    }
    public override bool cpuMapRead(ushort address, out uint mapped)
    {
        mapped = address;
        if (address >= 0x8000 && address <= 0xFFFF)
        {
            mapped = (ushort)(address & (prgBanks > 1 ? 0x7FFF : 0x3FFF));
            return true;
        }
        return false;
    }
    public override bool ppuMapWrite(ushort address, out uint mapped)
    {
        mapped = address;
        if (address >= 0x0000 && address <= 0x1FFF)
        {
            if (chrBanks == 0)
            {
                mapped = address;
                return true;
            }
        }

        return false;
    }
    public override bool ppuMapRead(ushort address, out uint mapped)
    {
        mapped = address;
        if ((address >= 0x0000 && address <= 0x1FFF))
        {
            mapped = address;
            return true;
        } 
        return false;
    }
 }