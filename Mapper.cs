public abstract class Mapper
{
    public virtual ushort cpuMapRead(ushort address){return 0;}
    public virtual ushort cpuMapWrite(ushort address){return 0;}
    public virtual ushort ppuMapRead(ushort address){return 0;}
    public virtual ushort ppuMapWrite(ushort address){return 0;}
    public byte prgBanks; 
    public byte chrBanks;
    public Mapper(byte prgBanks, byte chrBanks)
    {
        this.prgBanks = prgBanks;
        this.chrBanks = chrBanks;
    }
}