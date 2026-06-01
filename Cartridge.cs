using System.Runtime.CompilerServices;

public class Cartridge
{
    struct ROMHeader
    {
        public char[] CoolNESLabelDontUseProbably = ['N', 'E', 'S']; // 3 bytes (NES)
        public byte endOfFile = 0x1A;
        public byte prgRomSize; // game's logic
        public byte chrRomSize;
        public byte romCrtl1;
        public byte romCrtl2;
        public byte prgRamSize; // 'hey we can write here type stuff!'
        public byte tvSys1;
        public byte tvSys2;
        public byte[] padding = [0,0,0,0,0]; // 5 bytes
        // 16 bytes total by the way!!

        public ROMHeader(byte prgRomSize, byte chrRomSize, byte romCrtl1, byte romCrtl2, byte prgRamSize, byte tvSys1, byte tvSys2)
        {
            this.prgRomSize = prgRomSize;
            this.chrRomSize = chrRomSize;
            this.romCrtl1 = romCrtl1;
            this.romCrtl2 = romCrtl2;
            this.prgRamSize = prgRamSize;
            this.tvSys1 = tvSys1;
            this.tvSys2 = tvSys2;
        }
        public ROMHeader(byte[] header) 
        {
            // note: MUST be a 16 byte header, by the way
            this.prgRomSize = header[4];
            this.chrRomSize = header[5];
            this.romCrtl1 = header[6];
            this.romCrtl2 = header[7];
            this.prgRamSize = header[8];
            this.tvSys1 = header[9];
            this.tvSys2 = header[10];
        }
    } 
    private ROMHeader romHeader;
    private byte[] vPRGMemory;
    private byte[] vCHRMemory; 
    private byte[] vRAM; // there is usually 8kb of extra RAM for the CPU to use. 

    byte MapperID = 0;
    byte PRGBanks;
    byte CHRBanks;

    // is the cpu/ppu interested in reading? Apparentally cartridge gets first serve!
    public bool cpuRead()
    {
        return false;
    }
    public bool cpuWrite()
    {
        return false;
    }
    public bool ppuRead()
    {
        return false;
    }
    public bool ppuWrite()
    {
        return false;
    }
    
    
    public Cartridge(string fileName)
    {
        byte[] file = File.ReadAllBytes(fileName);
        // look at this sheer efficiency, it's INCREDIBLE. 
        byte[] header = file.Take(16).ToArray(); // take only the first 16 bytes for the header
        romHeader = new ROMHeader(header);

    }
}