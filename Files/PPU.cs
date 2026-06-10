using System.Data.Common;
using System.DirectoryServices.ActiveDirectory;
using System.Drawing;
using System.Net.Mail;
using System.Net.NetworkInformation;
using System.Numerics;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices.Marshalling;
using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;

public class PPU
{
    
    private Cartridge cartridge = null!;
    private Bus bus = null!;
    // tile is 8x8 pixels, takes 16 bytes, 256 tiles per table (so 512 total)
    // public byte[] patternTable = new byte[8 * 1024]; // 0x0000 -> 0x0FFF is sprites, 0x1000 -> 0x1FFF is bg
    public byte[,] nameTable = new byte[2, 1024]; // name table 0 and 1 obviously 
    public byte[] paletteRAM = new byte[32]; // 4 bg and 4 sprite palettes at once, baby!!
    // public Pixel[,] screen = new Pixel[256, 240]; // represents the screen, obviously. 
    public Pixel[] palScreen = new Pixel[100]; // for the 54 unique colors!
    public Pixel[,] displayScreen = new Pixel[256,240];

    // sprite stuff
    public byte[] OAM = new byte[256]; // 64 sprites, 4 bytes per sprite 
    public byte[] OAMscanline = new byte[32]; // 8 sprites per scanline or something
    private byte[] spriteShiftPatLo = new byte[8];
    private byte[] spriteShiftPatHi = new byte[8];
    private byte spriteCount = 0;
    private bool spriteZeroRender = false;
    private bool spriteZeroHit = false;
    public bool frameComplete = false;
    
    private struct ppuDebugStruct {

        public bool hardcodedPalette = false;
        public ppuDebugStruct()
        {
            
        }
    } 
    private ppuDebugStruct debugStruct = new ppuDebugStruct();
    private byte ppuCtrl = 0x00;
    private byte ppuMask = 0x00;
    private byte ppuScroll = 0x00;
    private byte ppuStatus = 0x00;
    
    public bool nmiTriggered = false;
    public bool ranNMI = false;
    public byte addressLatch = 0x00;
    public byte ppuDataBuffer = 0x00;

    // loopy registers
    private ushort currentVRAMAddr = 0x0000; // completed, just display this 0yyy NN YYYYY XXXXX
    private ushort tempVRAMAddr = 0x0000; // work on this before sending over to current
    private byte fineX = 0x00; // who is he?

    // stuff and things for things and stuff, or something. 
    private ushort bgShiftPatLo = 0x0000;
    private ushort bgShiftPatHi = 0x0000;
    private ushort bgShiftAttrLo = 0x0000;
    private ushort bgShiftAttrHi = 0x0000;  
    private byte bgNextTileID = 0x00;
    private byte bgNextAttr = 0x00;
    private byte bgNextTileLo = 0x00;
    private byte bgNextTileHi = 0x00;


    // sprite stuff
    byte oamAddress = 0x00;
    // just gets the value, then shifts it all the way over the right
    private ushort GetVRAMValue(ushort vram, VRAMMasks mask)
    {
        return (ushort)((vram & (ushort)mask) >> BitOperations.TrailingZeroCount((ushort)mask));
    }
    // same as above except does not shfit to the right
    private ushort GetTrueVRAMValue(ushort vram, VRAMMasks mask)
    {
        return (ushort)(vram & (ushort)mask);
    }
    // simple set and return VRAMValue using byte (shifted all the way to the right)
    private ushort SetVRAMValue(ushort vram, VRAMMasks mask, byte data)
    {
        return (ushort)((vram & ~((ushort)mask)) | (data << BitOperations.TrailingZeroCount((ushort)mask)));
    }
    // same as above except this one takes data that is NOT shifted to the right (must be ushort)
    private ushort SetTrueVRAMValue(ushort vram, VRAMMasks mask, ushort data)
    {
        return (ushort)((vram & ~((ushort)mask)) | data);
    }

    
    public byte cpuRead(ushort mappedAddress, bool rdOnly)
    {
        byte data = 0x00;
        if (rdOnly)
        {
            switch(mappedAddress)
            {
                case 0x0000:
                    data = ppuCtrl;
                    break;
                case 0x0001:
                    data = ppuMask;
                    break;
                case 0x0002:
                    data = ppuStatus;
                    break;
                case 0x0003:
                    break;
                case 0x0004:
                    break;
                case 0x0005:
                    break;
                case 0x0006:
                    break;
                case 0x0007:
                    break;
            }
        } else
        {
            switch(mappedAddress)
            {
                case 0x0000: // Controls rendering, NMI interrupts, and base nametable addresses.

                    break;
                case 0x0001: // Enables/disables rendering and sprite display.
    
                    break;
                case 0x0002: // Reads current PPU status flags (like Vertical Blanking) and resets the address latch.
                    data = (byte)((ppuStatus & 0xE0) | (ppuDataBuffer & 0x1F));
                    SetPPURegister(PPUSTATUS.VerticalBlank, false);
                    addressLatch = 0;
                    break;
                case 0x0003: // Sets the Sprite Object Attribute Memory (OAM) address
                    break;
                case 0x0004: // Reads/writes sprite attributes.
                    data = OAM[oamAddress];
                    break;
                case 0x0005: // Fine X and Y scrolling coordinates. Requires consecutive writes.
                    break;
                case 0x0006: //Sets the target 14 bit PPU bus VRAM address. Requires consecutive writes for high and low bytes.
                    break;
                case 0x0007: // Reads/writes video memory (VRAM) at the address set by PPUADDR
                    data = ppuDataBuffer; // need buffer for the delay
                    ppuDataBuffer = ppuRead(currentVRAMAddr); 
                    if (currentVRAMAddr >= 0x3F00) data = ppuDataBuffer; //palettes don't have the buffer

                    if (GetPPURegister(PPUCTRL.IncrementMode))
                    {
                        currentVRAMAddr+=32;
                    } else
                    {
                        currentVRAMAddr++;
                    }
                    break;
            }  
        }
        
        return data;
    }
    public void cpuWrite(ushort mappedAddress, byte data)
    {
        switch(mappedAddress)
        {
            case 0x0000: // Controls rendering, NMI interrupts, and base nametable addresses. PPUCRTL
                tempVRAMAddr = SetVRAMValue(tempVRAMAddr, VRAMMasks.nametable, (byte)(data & 0b0000_0011));
                ppuCtrl = data;
                break;
            case 0x0001: // Enables/disables rendering and sprite display. PPUMASK
                ppuMask = data;
                break;
            case 0x0002: // Reads current PPU status flags (like Vertical Blanking) and resets the address latch. PPUSTATUS
                ppuStatus = data;
                break;
            case 0x0003: // Sets the Sprite Object Attribute Memory (OAM) address OAMADDR
                oamAddress = data;
                break;
            case 0x0004: // Reads/writes sprite attributes. OAMDATA
                OAM[oamAddress] = data;
                break;
            case 0x0005: // Fine X and Y scrolling coordinates. Requires consecutive writes. PPUSCROLL
                if (addressLatch == 0) // coarseX and fineX
                {
                    tempVRAMAddr = SetVRAMValue(tempVRAMAddr, VRAMMasks.coarseX, (byte)(data >> 3));
                    fineX = (byte)(data & 0b0000_0111);
                    addressLatch = 1;
                } else // coarseY and finey
                {
                    tempVRAMAddr = SetVRAMValue(tempVRAMAddr, VRAMMasks.coarseY, (byte)(data >> 3));
                    tempVRAMAddr = SetVRAMValue(tempVRAMAddr, VRAMMasks.fineY, (byte)(data & 0b0000_0111));
                    addressLatch = 0;
                }
                break;
            case 0x0006: //Sets the target 14 bit PPU bus VRAM address. Requires consecutive writes for high and low bytes.
                if (addressLatch == 0) // high byte (special vramvalues for somereason)
                {
                    // ppuAddress = (ushort)((ppuAddress & 0x00FF) | (data << 8));
                    // addressLatch = 1;
                    // File.AppendAllText("log.txt",$"0: {data:X2}\n");
                    tempVRAMAddr = SetVRAMValue(tempVRAMAddr, VRAMMasks.ppuAddr0, (byte)(data & 0b0011_1111));
                    tempVRAMAddr = (ushort)(tempVRAMAddr & 0b0111_1111_1111_1111); // clear top bit
                    addressLatch = 1;

                } else // low byte
                {
                    tempVRAMAddr = (ushort)(tempVRAMAddr & 0xFF00 | data);
                    currentVRAMAddr = tempVRAMAddr;
                    addressLatch = 0;
                    // ppuAddress = (ushort)((ppuAddress & 0xFF00) | data); 
                    // File.AppendAllText("log.txt",$"1: {data:X2}\n");
                }

                break;
            case 0x0007: // Reads/writes video memory (VRAM) at the address set by PPUADDR
                ppuWrite(currentVRAMAddr, data);
                if (GetPPURegister(PPUCTRL.IncrementMode))
                {
                    currentVRAMAddr+=32;
                } else
                {
                    currentVRAMAddr++;
                }
                
                // File.AppendAllText("log.txt",$"{currentVRAMAddr:X4}\n");
                break;
        }  
    }
    
    public byte ppuRead(ushort address, bool rdonly = false)
    {
        byte data = 0x00;
        address &= 0x3FFF; // basically limit the range to an actually valid one

        if (cartridge.ppuRead(address, out data)) // Pattern Tables on the Cartridge
        {
            
        } else if (address >= 0x2000 && address <= 0x3EFF) // Nametables
        {
            // note that address range $3000 -> $3EFF mirrors $2000 -> $2EFF
            address &= 0x0FFF;   // so now it's a proper range starting from 0 

            if (cartridge.scrolling == 0    ) // horizontal
            {
                if (address >= 0x0000 && address <= 0x03FF) data = nameTable[0, address & 0x03FF];
                if (address >= 0x0400 && address <= 0x07FF) data = nameTable[0, address & 0x03FF];
                if (address >= 0x0800 && address <= 0x0BFF) data = nameTable[1, address & 0x03FF];
                if (address >= 0x0C00 && address <= 0x0FFF) data = nameTable[1, address & 0x03FF];
            } else if (cartridge.scrolling == 1) // vertical
            {
                if (address >= 0x0000 && address <= 0x03FF) data = nameTable[0, address & 0x03FF];
                if (address >= 0x0400 && address <= 0x07FF) data = nameTable[1, address & 0x03FF];
                if (address >= 0x0800 && address <= 0x0BFF) data = nameTable[0, address & 0x03FF];
                if (address >= 0x0C00 && address <= 0x0FFF) data = nameTable[1, address & 0x03FF];
            }
        } else if (address >= 0x3F00 && address <= 0x3FFF)
        {
            address &= 0x001F;

            if (address == 0x0010) address = 0x0000;
            if (address == 0x0014) address = 0x0004;
            if (address == 0x0018) address = 0x0008;
            if (address == 0x001C) address = 0x000C;

            data = (byte)(paletteRAM[address] & (GetPPURegister(PPUMASK.Grayscale) ? 0x30 : 0x3F));

        }

        return data;
    }   
    public void ppuWrite(ushort address, byte data)
    {
        address &= 0x3FFF; // basically limit the range to an actually valid one

        if (cartridge.ppuWrite(address, data)) // Pattern Tables on the Cartridge
        {
            
        } else if (address >= 0x2000 && address <= 0x3EFF) // Nametables
        {
            // note that address range $3000 -> $3EFF mirrors $2000 -> $2EFF
            address &= 0x0FFF;   // so now it's a proper range starting from 0 

            if (cartridge.scrolling == 0) // horizontal
            {
                if (address >= 0x0000 && address <= 0x03FF) nameTable[0, address & 0x03FF] = data;
                if (address >= 0x0400 && address <= 0x07FF) nameTable[0, address & 0x03FF] = data;
                if (address >= 0x0800 && address <= 0x0BFF) nameTable[1, address & 0x03FF] = data;
                if (address >= 0x0C00 && address <= 0x0FFF) nameTable[1, address & 0x03FF] = data;
            } else if (cartridge.scrolling == 1) // vertical
            {
                if (address >= 0x0000 && address <= 0x03FF) nameTable[0, address & 0x03FF] = data;
                if (address >= 0x0400 && address <= 0x07FF) nameTable[1, address & 0x03FF] = data;
                if (address >= 0x0800 && address <= 0x0BFF) nameTable[0, address & 0x03FF] = data;
                if (address >= 0x0C00 && address <= 0x0FFF) nameTable[1, address & 0x03FF] = data;
            }
        } else if (address >= 0x3F00 && address <= 0x3FFF)
        {
            address &= 0x001F;

            if (address == 0x0010) address = 0x0000;
            if (address == 0x0014) address = 0x0004;
            if (address == 0x0018) address = 0x0008;
            if (address == 0x001C) address = 0x000C;
            
            // File.AppendAllText("log.txt",$"Step 2: {address:X4}, {data:X2}\n");
            paletteRAM[address] = data;
        }
    }
    public void reset()
    {
        scanline = -1;
        cycle = 0;
        ppuStatus = 0;
        ppuMask = 0;
        ppuCtrl = 0;
        currentVRAMAddr = 0;
        tempVRAMAddr = 0;
        fineX = 0;
        bgShiftPatLo = bgShiftPatHi = 0;
        bgShiftAttrLo = bgShiftAttrHi = 0;
        bgNextTileID = bgNextAttr = bgNextTileLo = bgNextTileHi = 0;
        addressLatch = 0;
        ppuDataBuffer = 0;
        frameComplete = false;
        nmiTriggered = false;
        ranNMI = false;
    }
    public void connectCartToPPU(Cartridge cartridge)
    {
        this.cartridge = cartridge;
    }
    public Pixel GetColorFromPalette(byte palette, byte pixel) // where pixel is 0, 1, 2, or 3
    {
        // File.AppendAllText("log.txt", $"{pixel:X2}\n");
        // if (pixel == 0)
        // {
        //     return palScreen[0x0];
        // } else if (pixel == 1)
        // {
        //     return palScreen[0x01];
        // } else if (pixel == 2)
        // {
        //     return palScreen[0x02];
        // } else
        // {
        //     return palScreen[0x03];
        // }
    
        return palScreen[(ppuRead((ushort)(0x3F00 + (palette << 2) + pixel)) & 0x3F)];
    }
    bool didPrint = false;
    public Pixel[,] GetPatternTable(ushort offset, int palette)
    {
        Pixel[,] pixels = new Pixel[256,128];
        // because 16x16, obviously. 
        for (int tileY = 0; tileY < 16; tileY++)
        {
            for (int tileX = 0; tileX < 32; tileX++)
            {
                for (int row = 0; row < 8; row++)
                {
                    // each tile has 2 bytes per row, each representing a different bitmap. 
                    byte lowByte = ppuRead((ushort)(offset * 0x1000 + row + tileY * 256 + tileX * 16));
                    byte highByte = ppuRead((ushort)(offset * 0x1000 + row + 8 + tileY * 256 + tileX * 16));
                    for (int bitPair = 0; bitPair < 8; bitPair++)
                    {
                        // should be a number 0, 1, 2, or 3. 
                        int bit = 7 - bitPair;
                        byte newLow = (byte)((lowByte >> (bit)) & 0x01); 
                        byte newHigh = (byte)(((highByte >> (bit)) & 0x01) << 1);
                        byte pixelPair = (byte)(newLow | newHigh); //
                        // if (!didPrint) File.AppendAllText("log.txt", $"{pixelPair}\n");
                        // gets the correct color asscoaited with the palette and adds it to the thing
                        // Pixel pixel = palScreen[paletteRAM[palette * 4 + pixelPair]];
                        Pixel pixel = GetColorFromPalette((byte)palette, pixelPair);
                        pixels[tileX * 8 + bitPair, tileY * 8 + row] = new Pixel(pixel);
                    }
                }
            }
        }
        didPrint = true;
        return pixels; //place holder so I don't get a billino errors. 
    }
    private int scanline = 0;
    private int cycle = 0;
    private void IncrementY()
    {
        if (GetPPURegister(PPUMASK.RenderBG) || GetPPURegister(PPUMASK.RenderSprites))
        {
            if (GetVRAMValue(currentVRAMAddr, VRAMMasks.fineY) < 7)
            {
                currentVRAMAddr += 0x1000; // if fineY < 7, increment by 1
            } else
            {
                currentVRAMAddr = SetVRAMValue(currentVRAMAddr, VRAMMasks.fineY, 0x00); // else, set back to 0 (loop)
                int coarseY = GetVRAMValue(currentVRAMAddr, VRAMMasks.coarseY);
                if (coarseY == 29)
                {
                    coarseY = 0;
                    currentVRAMAddr ^= 0b0000_1000_0000_0000; // the nametable y, we gotta switch it!
                } else if (coarseY == 31)
                {
                    coarseY = 0;
                } else
                {
                    coarseY++;
                }
                currentVRAMAddr = SetVRAMValue(currentVRAMAddr, VRAMMasks.coarseY, (byte)coarseY);
            }
        }
    }
    private void IncrementX()
    {
        if (GetPPURegister(PPUMASK.RenderBG) || GetPPURegister(PPUMASK.RenderSprites))
        {
            int coarseX = GetVRAMValue(currentVRAMAddr, VRAMMasks.coarseX);
            // File.AppendAllText("log.txt", "I'M INCREMENTING X!!!")
            if (coarseX == 31)
            {
                coarseX = 0;
                currentVRAMAddr ^= 0b0000_0100_0000_0000; // switch horizontal nametable
            } else
            {
                coarseX++;
            }
            currentVRAMAddr = SetVRAMValue(currentVRAMAddr, VRAMMasks.coarseX, (byte)coarseX);  
        }
    }
    const ushort transferXMask = 0b0000_0100_0001_1111; // use binary because it looks better!
    private void TransferXAddress()
    {
        // transfer coarseX and nametable x of temp to current
        if (GetPPURegister(PPUMASK.RenderBG) || GetPPURegister(PPUMASK.RenderSprites))
        {
            currentVRAMAddr = (ushort)((currentVRAMAddr & ~transferXMask) | (tempVRAMAddr & transferXMask));
        }
    }
    const ushort transferYMask = 0b0111_1011_1110_0000; 
    private void TransferYAddress()
    {
        // fineY, coarseY, and one of the nametable bits
        if (GetPPURegister(PPUMASK.RenderBG) || GetPPURegister(PPUMASK.RenderSprites)   )
        {
            currentVRAMAddr = (ushort)((currentVRAMAddr & ~transferYMask) | (tempVRAMAddr & transferYMask));
        }
    }
    private ushort FetchTileAddr()
    {
        // taken from https://www.nesdev.org/wiki/PPU_scrolling
        return (ushort)(0x2000 | (currentVRAMAddr & 0x0FFF));
    }
    private ushort FetchAttributeAddr()
    {
        // taken from https://www.nesdev.org/wiki/PPU_scrolling
        return (ushort)(0x23C0 | (currentVRAMAddr & 0x0C00) | ((currentVRAMAddr >> 4) & 0x38) | ((currentVRAMAddr >> 2) & 0x07));
    } 
    private ushort FetchNextTileLoAddr()
    {
        return (ushort)(((GetPPURegister(PPUCTRL.PatternBackground) ? 1 : 0) << 12) + ((ushort)(bgNextTileID) << 4) + (GetVRAMValue(currentVRAMAddr, VRAMMasks.fineY)));
    }
    private ushort FetchnextTileHiAddr()
    {
        return (ushort)(FetchNextTileLoAddr() + 8);
    }
    private void LoadBGShifters()
    {
        bgShiftPatLo = (ushort)((bgShiftPatLo & 0xFF00) | bgNextTileLo);
        bgShiftPatHi = (ushort)((bgShiftPatHi & 0xFF00) | bgNextTileHi);  

        bgShiftAttrLo = (ushort)((bgShiftAttrLo & 0xFF00) | ((bgNextAttr & 0b01) != 0 ? 0xFF : 0x00));
        bgShiftAttrHi = (ushort)((bgShiftAttrHi & 0xFF00) | ((bgNextAttr & 0b10) != 0 ? 0xFF : 0x00));
    }
    private void UpdateShifters()
    {
        if (GetPPURegister(PPUMASK.RenderBG))
        {
            bgShiftPatLo <<=1;
            bgShiftPatHi <<=1;
            bgShiftAttrLo <<=1;
            bgShiftAttrHi <<=1;
        }

        if (GetPPURegister(PPUMASK.RenderSprites) && cycle >= 1 && cycle < 258)
        {
            for (int i = 0; i < spriteCount; i++)
            {
                if (OAMscanline[i * 4 + 3] > 0)
                {
                    OAMscanline[i * 4 + 3] -= 1;
                } else
                {   
                    spriteShiftPatLo[i] <<= 1;
                    spriteShiftPatHi[i] <<= 1;
                }
            }
        }
    }
    // shoutout to OLC and then a guy from stack overflow for this solution!
    private byte flipByte(byte b)
    {
        b = (byte)((b & 0xF0) >> 4 | (b & 0x0F) << 4);
        b = (byte)((b & 0xCC) >> 2 | (b & 0x33) << 2);
        b = (byte)((b & 0xAA) >> 1 | (b & 0x55) << 1);
        return b;
    }
    public void clock()
    {
        // do something for the PPU clock


        if (scanline >= -1 && scanline < 240) // AKA the visible period (except -1)
        {
            if (scanline == -1 && cycle == 1) // congrats! another cycle has begun. no more vertical blanking. 
            {
                SetPPURegister(PPUSTATUS.VerticalBlank, false);

                SetPPURegister(PPUSTATUS.Overflow, false);

                SetPPURegister(PPUSTATUS.Sprite0HitFlag, false);

                for (int i = 0; i < 8; i++)
                {
                    spriteShiftPatLo[i] = spriteShiftPatHi[i] = 0; // wow so efficient!
                }

            }
            
            if (scanline == -1 && cycle >= 280 && cycle <= 304)
            {
                TransferYAddress();
            }

            if (scanline == 0 && cycle == 0)
            {
                // oddframe implementation
                cycle = 1;
            }

            if (cycle >= 65 && cycle <= 256)
            {
                // sprite eval (primary -> secondary)
            }

            if (cycle == 256)
            {
                IncrementY();
            }

            if (cycle == 257)
            {
                TransferXAddress();
                LoadBGShifters();
            }

            if (cycle == 338 || cycle == 340)
            {
                bgNextTileID = ppuRead(FetchTileAddr());
            }

            if ((cycle >= 2 && cycle < 258) || (cycle >= 321 && cycle < 338))
            {
                UpdateShifters();
                switch((cycle - 1)% 8)
                {
                    case 0:
                        LoadBGShifters();
                        bgNextTileID = ppuRead(FetchTileAddr());
                        break;
                    case 1:
                        break;
                    case 2:
                        bgNextAttr = ppuRead(FetchAttributeAddr());

                        if ((GetVRAMValue(currentVRAMAddr, VRAMMasks.coarseY) & 0x02) > 0) bgNextAttr >>=4;
                        if ((GetVRAMValue(currentVRAMAddr, VRAMMasks.coarseX) & 0x02) > 0) bgNextAttr >>=2;
                        bgNextAttr &= 0x03; 
                        break;
                    case 3:
                        break;
                    case 4:
                        bgNextTileLo = ppuRead(FetchNextTileLoAddr());
                        break;
                    case 5:
                        break;
                    case 6:
                        bgNextTileHi = ppuRead(FetchnextTileHiAddr());
                        break;
                    case 7: 
                        IncrementX();
                        break;
                }
            }
        
        // -------------------------- SPRITE STUFF!!!!!!!!! ----------------------------------------------
        
            if (cycle == 257) // do it all in one cycle
            {
                // sprite pattern fetches (secondary OAM -> shifters)
                for (int i = OAMscanline.Length - 1; i >= 0; i--)
                {
                    OAMscanline[i] = 0; // clear everything in the scanline
                }

                for (int i = 0; i < 8; i++)
                {
                    spriteShiftPatLo[i] = spriteShiftPatHi[i] = 0; // wow so efficient!
                }
                    
                spriteCount = 0;
                spriteZeroHit = false;

                for (byte i = 0; i < 64; i++)
                {
                    byte address = (byte)(i << 2); // i * 4 basically
                    byte scanlineAddress = (byte)(spriteCount << 2);
                    byte OAMy = OAM[address];
                    byte spriteHeight = (byte)(GetPPURegister(PPUCTRL.SpriteSize) ? 16 : 8);
                    if (scanline >= OAMy && scanline < OAMy + spriteHeight && OAMy != 0xFF) // aka is it visible?/
                    {
                        if (spriteCount <= 7)
                        {
                            OAMscanline[scanlineAddress] = OAMy; // y pos
                            OAMscanline[scanlineAddress + 1] = OAM[address + 1]; // tile index
                            OAMscanline[scanlineAddress + 2] = OAM[address + 2]; // attributes
                            OAMscanline[scanlineAddress + 3] = OAM[address + 3]; // x position
                            if (i == 0)
                            {
                                spriteZeroHit = true;
                            }
                        }

                        spriteCount++;
                    }

                    if (spriteCount >= 9) break; // break outta this loop
                }

                if (spriteCount >= 9) {
                    spriteCount = 8;
                    SetPPURegister(PPUSTATUS.Overflow, true);
                }

            }

            if (cycle == 340) // end of the scanline, 
            {
                for (byte i = 0; i < spriteCount; i++)
                {
                    byte spritePatBitsLo = 0;
                    byte spritePatBitsHi = 0;
                    ushort spritePatAddrLo = 0;
                    ushort spritePatAddrHi = 0; 

                    // calculating spritePatAddrLo 
                    bool flippedVert = (OAMscanline[((i * 4) + 2)] & 0x80) != 0;
                    if (!GetPPURegister(PPUCTRL.SpriteSize)) // 8 x 8
                    {
                        if (flippedVert)
                        {
                            spritePatAddrLo = (ushort)(
                                ((GetPPURegister(PPUCTRL.PatternSprite) != false ? 1 : 0) << 12) |
                                (OAMscanline[(i * 4) + 1] << 4) | // tile ID 
                                (7 - ((scanline - OAMscanline[(i * 4)]) & 0x07)) // which row in cell using y, but inverse!! wow.
                            ); // switch pattern tables
                        } else
                        {
                            spritePatAddrLo = (ushort)(
                                ((GetPPURegister(PPUCTRL.PatternSprite) != false ? 1 : 0) << 12) |
                                (OAMscanline[(i * 4) + 1] << 4) | // tile ID 
                                ((scanline - OAMscanline[(i * 4)]) & 0x07) // which row in cell using y
                            ); // switch pattern tables
                        }
                    } else // 8 x 16
                    {
                        if (flippedVert)
                        {
                            if (scanline - OAMscanline[(i * 4)] < 8)  // top half of the tile 
                            {
                                spritePatAddrLo = (ushort)(
                                    ((OAMscanline[(i * 4) + 1] & 0x01) << 12) | // which pattern table depends on first bit of tileID
                                    ((OAMscanline[(i * 4) + 1] & 0xFE) << 4) | // tile ID 
                                    ((scanline - OAMscanline[(i * 4)]) & 0x07) // which row in cell using y, but inverse!! wow.
                                ); // switch pattern tables
                            } else // bottom half of tile, just one 1 to the tileID
                            {
                                spritePatAddrLo = (ushort)(
                                    ((OAMscanline[(i * 4) + 1] & 0x01) << 12) | // which pattern table depends on first bit of tileID
                                    (((OAMscanline[(i * 4) + 1] & 0xFE) + 1) << 4) | // tile ID 
                                    ((scanline - OAMscanline[(i * 4)]) & 0x07) // which row in cell using y, but inverse!! wow.
                                ); // switch pattern tables
                            }
                        } else
                        {
                            if (scanline - OAMscanline[(i * 4)] < 8)
                            {
                                spritePatAddrLo = (ushort)(
                                    ((OAMscanline[(i * 4) + 1] & 0x01) << 12) | // which pattern table depends on first bit of tileID
                                    (((OAMscanline[(i * 4) + 1] & 0xFE) + 1) << 4) | // tile ID 
                                    (7 - ((scanline - OAMscanline[(i * 4)]) & 0x07)) // which row in cell using y, but inverse!! wow.
                                ); // switch pattern tables
                            } else
                            {
                                spritePatAddrLo = (ushort)(
                                    ((OAMscanline[(i * 4) + 1] & 0x01) << 12) | // which pattern table depends on first bit of tileID
                                    ((OAMscanline[(i * 4) + 1] & 0xFE) << 4) | // tile ID 
                                    (7 - ((scanline - OAMscanline[(i * 4)]) & 0x07)) // which row in cell using y, but inverse!! wow.
                                ); // switch pattern tables
                            }
                        }
                    }
                
                    spritePatAddrHi = (ushort)(spritePatAddrLo + 8);

                    spritePatBitsLo = ppuRead(spritePatAddrLo);
                    spritePatBitsHi = ppuRead(spritePatAddrHi);

                    if ((OAMscanline[i * 4 + 2] & 0x40) != 0) // horizonatl mirror
                    {
                        spritePatBitsLo = flipByte(spritePatBitsLo);
                        spritePatBitsHi = flipByte(spritePatBitsHi);
                    } 

                    // now bytes have been successfully vertically and horizontally mirrored
                    // thus they shall be put in the register! 

                    spriteShiftPatLo[i] = spritePatBitsLo;
                    spriteShiftPatHi[i] = spritePatBitsHi;
                }
            }
        }
        if (scanline >= 241 && scanline < 261) // AKA the NOT visible period 
        {
            if (scanline == 241 && cycle == 1)
            {
                SetPPURegister(PPUSTATUS.VerticalBlank, true);

                if (GetPPURegister(PPUCTRL.EnableNMI)) nmiTriggered = true;
            }
        }

        // ------------------------------ BG FINAL CALCULATIONS ----------------------------
        byte bgPixel = 0x00;
        byte bgPalette = 0x00;

        if (GetPPURegister(PPUMASK.RenderBG))
        {

            ushort bit_mux = (ushort)(0x8000 >> fineX);

            // Select Plane pixels by extracting from the shifter 
            // at the required location. 
            byte p0_pixel = (byte)((bgShiftPatLo & bit_mux) != 0 ? 1 : 0);
            byte p1_pixel = (byte)((bgShiftPatHi & bit_mux) != 0 ? 1 : 0);

            // Combine to form pixel index
            bgPixel = (byte)((p1_pixel << 1) | p0_pixel);

            // Get palette
            byte bg_pal0 = (byte)((bgShiftAttrLo & bit_mux) != 0 ? 1 : 0);
            byte bg_pal1 = (byte)((bgShiftAttrHi & bit_mux) != 0 ? 1 : 0);
            bgPalette = (byte)((bg_pal1 << 1) | bg_pal0);
        }
        // ----------------------------- SPRITE FINAL CALCULATIONS --------------------------
        spriteZeroRender = false;
        byte fgPixel = 0b00; // ahh yes, the classic pixel
        byte fgPalette = 0b000; // the palette
        byte fgPriority = 0b0; // ARE YOU IMPORTNAT??? HUH????
    
        if (GetPPURegister(PPUMASK.RenderSprites))
        {
            for (int i = 0; i < spriteCount; i++)
            {
                if (OAMscanline[i * 4 + 3] == 0) // scanline has finally reached the x value of the sprite
                {
                    byte fgPixelLo = (byte)((spriteShiftPatLo[i] & 0x80) > 0 ? 1 : 0); 
                    byte fgPixelHi = (byte)((spriteShiftPatHi[i] & 0x80) > 0 ? 1 : 0); 
                    fgPixel = (byte)((fgPixelHi << 1) | fgPixelLo);

                    fgPalette = (byte)((OAMscanline[i * 4 + 2] & 0x03) + 4); // +4 because sprites are last 4 palettes
                    fgPriority = (byte)(OAMscanline[i * 4 + 2] & 0x20); // 1 = bg wins, 0 = fg wins

                    if (fgPixel != 0)
                    {
                        if (i == 0) spriteZeroRender = true;
                        break;
                    }
                }
            }
        }
        // --------------------- ITS THE FINAL PIXEL (DOO DOO DOO DOOOO) ----------------------

        byte pixelPair = 0x00;
        byte palette = 0x00;

        // NOW THE FG AND BG PIXELS MUST BATTLE TO THE DEATH!! WINNER TAKES A SPOT ON THE SCREEN!
        if (fgPixel == 0 && bgPixel == 0) // EVERYBODY LOSES
        {
            // change nothing
        } else if (fgPixel > 0 && bgPixel == 0) // FG WINS
        {
            pixelPair = fgPixel;
            palette = fgPalette;
        } else if (fgPixel == 0 && bgPixel > 0) // BG WINS 
        {
            pixelPair = bgPixel;
            palette = bgPalette;   
        } else if (fgPixel > 0 && bgPixel > 0) // WINNER COMES DOWN TO PRIORITY!!!
        {
            if (fgPriority == 1) // BG WINS!!
            {
                pixelPair = bgPixel;
                palette = bgPalette; 
            } else // FG WINS!!
            {
                pixelPair = fgPixel;
                palette = fgPalette;
            }
        } 

        // Sprite Zero Hit Detection
        if (spriteZeroHit && spriteZeroRender)
        {
            if (GetPPURegister(PPUMASK.RenderSprites) && GetPPURegister(PPUMASK.RenderBG))
            {
                byte renderSpritesLeft = (byte)((!GetPPURegister(PPUMASK.RenderSpritesLeft)) ? 1 : 0);
                byte renderBGLeft = (byte)((!GetPPURegister(PPUMASK.RenderBGLeft)) ? 1 : 0);

                if ((renderSpritesLeft | renderBGLeft) == 0)
                {
                    if (cycle >= 9 && cycle < 258)
                    {
                        SetPPURegister(PPUSTATUS.Sprite0HitFlag, true);
                    }
                } else
                {
                    if (cycle >= 1 && cycle < 258)
                    {
                        SetPPURegister(PPUSTATUS.Sprite0HitFlag, true);
                    }
                }
            }
        }


        if (cycle - 1 >= 0 && cycle - 1 <= 255 && scanline >= 0 && scanline <= 239)
        {
            displayScreen[cycle - 1, scanline] = GetColorFromPalette(palette, pixelPair);
        }

        cycle++;
        if (cycle >= 341)
        {
            cycle = 0;
            scanline++;
            if (scanline >= 261)
            {
                scanline = -1;
                frameComplete = true;
                
                nmiTriggered = false; // reset the nmi thingy
                ranNMI = false;
            }
        }
    }

    public PPU()
    { // all possible colors that the NES can display apparentally. 

        palScreen[0x00] = new Pixel(84, 84, 84); // just for now
        palScreen[0x01] = new Pixel(0, 30, 116);
        palScreen[0x02] = new Pixel(8, 16, 144);
        palScreen[0x03] = new Pixel(48, 0, 136);

        palScreen[0x04] = new Pixel(68, 0, 100);
        palScreen[0x05] = new Pixel(92, 0, 48);
        palScreen[0x06] = new Pixel(84, 4, 0);
        palScreen[0x07] = new Pixel(60, 24, 0);

        palScreen[0x08] = new Pixel(32, 42, 0);
        palScreen[0x09] = new Pixel(8, 58, 0);
        palScreen[0x0A] = new Pixel(0, 64, 0);
        palScreen[0x0B] = new Pixel(0, 60, 0);

        palScreen[0x0C] = new Pixel(0, 50, 60);
        palScreen[0x0D] = new Pixel(0, 0, 0);
        palScreen[0x0E] = new Pixel(0, 0, 0);
        palScreen[0x0F] = new Pixel(0, 0, 0);

        palScreen[0x10] = new Pixel(152, 150, 152);
        palScreen[0x11] = new Pixel(8, 76, 196);
        palScreen[0x12] = new Pixel(48, 50, 236);
        palScreen[0x13] = new Pixel(92, 30, 228);

        palScreen[0x14] = new Pixel(136, 20, 176);
        palScreen[0x15] = new Pixel(160, 20, 100);
        palScreen[0x16] = new Pixel(152, 34, 32);
        palScreen[0x17] = new Pixel(120, 60, 0);

        palScreen[0x18] = new Pixel(84, 90, 0);
        palScreen[0x19] = new Pixel(40, 114, 0);
        palScreen[0x1A] = new Pixel(8, 124, 0);
        palScreen[0x1B] = new Pixel(0, 118, 40);

        palScreen[0x1C] = new Pixel(0, 102, 120);
        palScreen[0x1D] = new Pixel(0, 0, 0);
        palScreen[0x1E] = new Pixel(0, 0, 0);
        palScreen[0x1F] = new Pixel(0, 0, 0);

        palScreen[0x20] = new Pixel(236, 238, 236);
        palScreen[0x21] = new Pixel(76, 154, 236);
        palScreen[0x22] = new Pixel(120, 124, 236);
        palScreen[0x23] = new Pixel(176, 98, 236);

        palScreen[0x24] = new Pixel(228, 84, 236);
        palScreen[0x25] = new Pixel(236, 88, 180);
        palScreen[0x26] = new Pixel(236, 106, 100);
        palScreen[0x27] = new Pixel(212, 136, 32);

        palScreen[0x28] = new Pixel(160, 170, 0);
        palScreen[0x29] = new Pixel(116, 196, 0);
        palScreen[0x2A] = new Pixel(76, 208, 32);
        palScreen[0x2B] = new Pixel(56, 204, 108);

        palScreen[0x2C] = new Pixel(56, 180, 204);
        palScreen[0x2D] = new Pixel(60, 60, 60);
        palScreen[0x2E] = new Pixel(0, 0, 0);
        palScreen[0x2F] = new Pixel(0, 0, 0);

        palScreen[0x30] = new Pixel(236, 238, 236);
        palScreen[0x31] = new Pixel(168, 204, 236);
        palScreen[0x32] = new Pixel(188, 188, 236);
        palScreen[0x33] = new Pixel(212, 178, 236);
        palScreen[0x34] = new Pixel(236, 174, 236);
    
        palScreen[0x35] = new Pixel(236, 174, 212);
        palScreen[0x36] = new Pixel(236, 180, 176);
        palScreen[0x37] = new Pixel(228, 196, 144);
        palScreen[0x38] = new Pixel(204, 210, 120);
        palScreen[0x39] = new Pixel(180, 222, 120);
        palScreen[0x3A] = new Pixel(168, 226, 144);
        palScreen[0x3B] = new Pixel(152, 226, 180);
        palScreen[0x3C] = new Pixel(160, 214, 228);
        palScreen[0x3D] = new Pixel(160, 162, 160);
        palScreen[0x3E] = new Pixel(0, 0, 0);
        palScreen[0x3F] = new Pixel(0, 0, 0);
    }
    private void SetPPURegister(PPUMASK flag, bool value)
    {
        if (value) { ppuMask |= (byte)flag; } else
            { ppuMask &= (byte)~flag; } // set flag bit to 0 (? & 0 == 0)   
    }
    private void SetPPURegister(PPUCTRL flag, bool value)
    {
        if (value) { ppuCtrl |= (byte)flag; } else
            { ppuCtrl &= (byte)~flag; } // set flag bit to 0 (? & 0 == 0)   
    }
    private void SetPPURegister(PPUSCROLL flag, bool value)
    {
        if (value) { ppuScroll |= (byte)flag; } else
            { ppuScroll &= (byte)~flag; } // set flag bit to 0 (? & 0 == 0)   
    }
    private void SetPPURegister(PPUSTATUS flag, bool value)
    {
        if (value) { ppuStatus |= (byte)flag; } else
            { ppuStatus &= (byte)~flag; } // set flag bit to 0 (? & 0 == 0)   
    }
    private bool GetPPURegister(PPUMASK flag)
    {
        return (ppuMask & (byte)flag) >= 1;
    }
    private bool GetPPURegister(PPUCTRL flag)
    {
        return (ppuCtrl & (byte)flag) >= 1;
    }
    private bool GetPPURegister(PPUSTATUS flag)
    {
        return (ppuStatus & (byte)flag) >= 1;
    }
    private bool GetPPURegister(PPUSCROLL flag)
    {
        return (ppuScroll & (byte)flag) >= 1;
    }

    
}

// will likely be replaced later (it's a little more inefficient than just having a ushort value)
public struct Pixel
{
    public byte R;
    public byte G;
    public byte B;
    public byte A = 0;
    public Pixel(byte R, byte G, byte B, byte A = 255)
    {
        this.R = R;
        this.B = B;
        this.G = G;
        this.A = A;
    }
    public Pixel(Pixel pixel)
    {
        R = pixel.R;
        G = pixel.G;
        B = pixel.B;
        A = pixel.A;
    }
}

// for OAM



// emums for the love of the game (so I can read it better)
public enum VRAMMasks // in binary so little 'ol me can visualize it
{
    coarseX = 0b0000_0000_0001_1111, // or (0b11111 << 0)
    coarseY = 0b0000_0011_1110_0000, // or (0b11111 << 5)
    nametable = 0b0000_1100_0000_0000, // or (0b11 << 10)
    fineY = 0b0111_0000_0000_0000, // or (0b111 << 12)
    unused = 0b1000_0000_0000_0000,
    ppuAddr0 = 0b0011_1111_0000_0000,
    ppuAddr1 = 0b0000_0000_1111_1111,
}
public enum PPUCTRL
{
    NametableX = (1 << 0),
    NametableY = (1 << 1),
    IncrementMode = (1 << 2),
    PatternSprite = (1 << 3),
    PatternBackground = (1 << 4),
    SpriteSize = (1 << 5),
    SlaveMode = (1 << 6),
    EnableNMI = (1 << 7),
}
public enum PPUMASK
{
    Grayscale = (1 << 0),
    RenderBGLeft = (1 << 1),
    RenderSpritesLeft = (1 << 2),
    RenderBG = (1 << 3),
    RenderSprites = (1 << 4),
    EnhanceRed = (1 << 5),
    EnhanceBlue = (1 << 6),
    EnhanceGreen = (1 << 7),
}
public enum PPUSTATUS
{
    Overflow = (1 << 5),
    Sprite0HitFlag = (1 << 6),
    VerticalBlank = (1 << 7)
}
public enum PPUSCROLL
{
    
}
