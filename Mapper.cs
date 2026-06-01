public abstract class Mapper
{
    public virtual void cpuMapRead(ushort address, int mappedAddress){}
    public virtual void cpuMapWrite(ushort address, int mappedAddress){}
    public virtual void ppuMapRead(ushort address, int mappedAddress){}
    public byte prgBanks; 
    public byte chrBanks;
    public Mapper(byte prgBanks, byte chrBanks)
    {
        this.prgBanks = prgBanks;
        this.chrBanks = chrBanks;
    }
}