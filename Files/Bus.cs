

using System.Collections;
using System.Security.Cryptography;


public class Bus
{
    public Cartridge cartridge = null!;
    public CPU cpu; 
    public PPU ppu;
    public APU apu;
    public byte[] controllers = new byte[2]; 
    private byte[] controllersState = new byte[2]; // snapshot when asked
    public byte[] ram = new byte[2048]; // 2KB of memory, real RAM for now!. 
    private int clockCounter = 0; // counts the amount of time passed total

    private byte dmaPage = 0x00;
    private byte dmaAddress = 0x00;
    private byte dmaData = 0x00;
    private bool dmaTransfer = false;
    private bool dmaDummy = true;
    public byte cpuRead(ushort address, bool bReadOnly = false) // not sure why, but this boolean is here. 
    {
        byte data = 0x00;
        if (address >= 0x0000 && address <= 0x1FFF)// 8 bytes, but the last 6 bytes just map onto the first 2. (for some reason)
        {
            data = (byte)ram[address & 0x07FF]; // mirror the 6kb with the first 2kb
        } else if (address >= 0x2000 && address <= 0x3FFF)
        {
            // real range is only 2000 -> 2007, anything else in this range repeats 
            // 0010 0000 0000 0000 -> 0010 0000 0000 0111 (only last 3 numbers matter then)
            ushort mappedAddress = (ushort)(address & 0x0007);
            data = ppu.cpuRead(mappedAddress, bReadOnly);
        }
        else if (cartridge.cpuRead(address, out data))
        {
            return data;
            // do something with the cartridge
            // if (address >= 0x8000 && address <= 0xFFFF)
            // {
            //     return cartridge.cpuRead(address);
            // }
        } else if (address == 0x4014) // direct memeory access
        {
        
        }
        else if (address >= 0x4016 && address <= 0x4017)
        {
            data = (byte)((controllersState[address & 0x0001] & 0x80) != 0 ? 1 : 0);
            controllersState[address & 0x0001] <<= 1;
        }
        return data; // fallback in case of an invalid address
    }
    public void cpuWrite(ushort address, byte data)
    {
        if (address >= 0x0000 && address <= 0x1FFF) 
        {
            ram[address] = data; // sets the value of the address to the data
        } 
        else if (address >= 0x2000 && address <= 0x3FFF)
        {
            // real range is only 2000 -> 2007, anything else in this range repeats 
            // 0010 0000 0000 0000 -> 0010 0000 0000 0111 (only last 3 numbers matter then)
            ushort mappedAddress = (ushort)(address & 0x0007);
            ppu.cpuWrite(mappedAddress, data);
        }
        else if (address == 0x4014)
        {
            dmaPage = data;
            dmaAddress = 0x00;
            dmaTransfer = true;
        }
        else if (address >= 0x4016 && address <= 0x4017)
        {
            controllersState[address & 0x0001] = controllers[address & 0x0001];
        }
        else if (address >= 0x4020 && address <= 0xFFFF)
        {
            // Cartridge stuff
            if (address >= 0x8000) // the PRG-ROM of the Catridge
            {
                cartridge.cpuWrite(address, data);
            }
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
    public void InsertCartridge(Cartridge cartridge)
    {
        this.cartridge = cartridge;
        ppu.connectCartToPPU(cartridge);
    }
    public void reset()
    {
        cpu.RES();
        ppu.reset();
        apu.reset();
        clockCounter = 0;
    }
    public void clock()
    {
        clockCounter++;
        ppu.clock();
        if (clockCounter % 3 == 0) {
            if (dmaTransfer)
            {
                if (dmaDummy) 
                {
                    if (clockCounter % 2 == 1) dmaDummy = false; // sync on odd clock cycle so we start with reading
                } else
                {
                    if (clockCounter % 2 == 0) // read on even
                    {
                        dmaData = cpuRead((ushort)(dmaPage << 8 | dmaAddress));
                    }
                    else // write on odd
                    {
                        dmaData = ppu.OAM[dmaAddress] = dmaData;
                        dmaAddress++; // increment, for efficiency so we need to change it

                        if (dmaAddress == 0x00) // wrapped from 255 -> 0
                        {
                            dmaTransfer = false;
                            dmaDummy = true;
                        }
                    }
                }
            } else
            {
                cpu.clock();
            }  
        }
        if (clockCounter % 6 == 0)
        {
            apu.clock(); // happens at half the rate of the cpu!
        }
        if (ppu.nmiTriggered && !ppu.ranNMI)
        {
            cpu.NMI();
            ppu.ranNMI = true;
        }
    }

    public Bus()
    {
        cpu = new CPU();
        ppu = new PPU();
        apu = new APU();
        cpu.ConnectBus(this);
        ClearRAM();
    }
}

 

