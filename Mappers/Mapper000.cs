public class Mapper000 : Mapper
{
    
    public Mapper000(byte prgBanks, byte chrBanks) : base(prgBanks, chrBanks)
    {
        
    }
    public override ushort cpuMapWrite(ushort address)
    {
        ushort mappedAddress = address;
        if (address >= 0x8000 && address <= 0xFFFF)
        {
            mappedAddress = (ushort)(address & (prgBanks> 1 ? 0x7FFF : 0x3FFF));
            return mappedAddress;
        }
        return mappedAddress;
    }
    public override ushort cpuMapRead(ushort address)
    {
        ushort mappedAddress = address;
        if (address >= 0x8000 && address <= 0xFFFF)
        {
            mappedAddress = (ushort)(address & (prgBanks> 1 ? 0x7FFF : 0x3FFF));
            return mappedAddress;
        }
        return mappedAddress;
    }
    public override ushort ppuMapWrite(ushort address)
    {
        ushort mappedAddress = address;
        if (address >= 0x0000 && address <= 0x1FFF)
        {
            return mappedAddress;
        }
        return 0;
    }
    public override ushort ppuMapRead(ushort address)
    {
        ushort mappedAddress = address;
        if (address >= 0x0000 && address <= 0x1FFF)
        {
            return mappedAddress;
        }
        return 0;
    }
 }