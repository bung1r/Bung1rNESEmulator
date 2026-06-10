using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.VisualBasic;

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
    private Mapper mapper;
    private ROMHeader romHeader;
    private ushort mapperID;
    public byte scrolling; // 0 for horizontal, 1 for vertical
    private byte[] vPRGMemory = [];
    private byte[] vCHRMemory = []; 
    private byte[] vRAM = []; // there is usually 8kb of extra RAM for the CPU to use. 

    byte PRGBanks = 0;
    byte CHRBanks = 0;

    // is the cpu/ppu interested in reading? Apparentally cartridge gets first serve!
    public bool cpuRead(ushort address, out byte data)
    {
        uint mapped;
        if (mapper.cpuMapRead(address, out mapped))
        {
            data = vPRGMemory[mapped];
            return true;
        }

        data = 0;
        return false;
    }
    public bool cpuWrite(ushort address, byte data)
    {
        uint mapped;
        if (mapper.cpuMapRead(address, out mapped))
        {
            vPRGMemory[mapped] = data;
            return true;
        }

        return false;
    }
    public bool ppuRead(ushort address, out byte data)
    {
        uint mapped;
        if (mapper.ppuMapRead(address, out mapped))
        {
            data = vCHRMemory[mapped];
            return true;
        }

        data = 0;
        return false;
    }
    
    public bool ppuWrite(ushort address, byte data)
    {
        uint mapped;
        if (mapper.ppuMapWrite(address, out mapped))
        {
            vCHRMemory[mapped] = data;
            return true;
        }
        return false;
        
    }
    
    public Cartridge(string fileName)
    {
        byte[] file = File.ReadAllBytes(fileName);
        int offset = 0;

        byte[] header = file.Take(16).ToArray(); // take only the first 16 bytes for the header
        offset += 16;
        romHeader = new ROMHeader(header);

        if ((romHeader.romCrtl1 & 0x0004) != 0) offset += 512; // trainer exists
        mapperID = (ushort)(((romHeader.romCrtl2 >> 4) << 4) | (romHeader.romCrtl1 >> 4));
        scrolling = (byte)(romHeader.romCrtl1 & 0x01);

        byte fileType = 1;

        if (fileType == 0)
        {

        } else if (fileType == 1)
        {
            PRGBanks = romHeader.prgRomSize;
            CHRBanks = romHeader.chrRomSize;
            vPRGMemory = file[offset..(offset + PRGBanks * 16384)];
            offset += PRGBanks * 16384;


            if (CHRBanks == 0)
            {
                vCHRMemory = new byte[8192];
            } else {
                vCHRMemory = file[offset..(offset + CHRBanks * 8192)];
                offset += CHRBanks * 8192;
            }
            
        } else if (fileType == 2)
        {
            
        } else
        {
            // not implemented, just for now. 
        }

        // switch case for mappers or something
        switch(mapperID)
        {
            case 0:
                mapper = new Mapper000(PRGBanks, CHRBanks);
                break;
        }
    }
}