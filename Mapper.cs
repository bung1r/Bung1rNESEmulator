public abstract class Mapper
{
    public virtual bool cpuMapRead(ushort address, out uint mapped){mapped = 0; return false;}
    public virtual bool cpuMapWrite(ushort address, out uint mapped){mapped = 0; return false;}
    public virtual bool ppuMapRead(ushort address, out uint mapped){mapped = 0; return false;}
    public virtual bool ppuMapWrite(ushort address, out uint mapped){mapped = 0; return false;}
    public byte prgBanks; 
    public byte chrBanks;
    public Mapper(byte prgBanks, byte chrBanks)
    {
        this.prgBanks = prgBanks;
        this.chrBanks = chrBanks;
    }
}