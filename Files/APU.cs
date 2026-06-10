using System.ComponentModel.DataAnnotations;
using System.Windows.Forms.VisualStyles;

public class APU
{
    public struct PulseChannel
    {
        // envelope generator
        public byte volume;
        public bool constantVolume;
        public bool startFlag;
        public bool loopFlag;
        public byte envelopeDivider;
        public byte envelopeDecay;

        // sweep unit
        public byte sweepDivider;
        public byte sweepShift;
        public byte sweepPeriod;
        public bool sweepEnabledFlag;
        public bool negateFlag;
        public bool reloadFlag;
        public bool sweepMute;

        // timer
        public ushort timer;
        public ushort timerReload;

        // 8-step sequencer
        public byte duty;
        public byte sequence;
        public byte sequenceBuffer;
        public byte sequencePos;

        // length counter
        public ushort length; // counts down
    }
    public struct TriangleChannel
    {
        // timer, length counter, linear counter, linear counter reload flag, control flag, sequencer.
        public ushort timer;
        public byte linearCounter;
        public bool linearCounterFlag;
        public bool controlFlag;
        public byte sequencePos;
        public byte length;
    }
    public struct NoiseChannel
    {
        // envelope generator
        public byte volume;
        public bool constantVolume;
        public bool startFlag;
        public bool loopFlag;
        public bool loopNoiseFlag;
        public byte envelopeDivider;
        public byte envelopeDecay;
        public byte timerPeriod;
        public ushort shiftRegister;
        // 
    }
    public struct DMCChannel
    {
        
    }
    PulseChannel[] pulseChannels = new PulseChannel[]
    {
        new PulseChannel(), new PulseChannel()
    };
    TriangleChannel triangleChannel = new TriangleChannel();
    NoiseChannel noiseChannel = new NoiseChannel();
    DMCChannel dmcChannel = new DMCChannel();
    private byte apuStatus = 0x00;
    public byte cpuRead(ushort mappedAddress, bool rdOnly)
    {
        byte data = 0x00;

        switch(mappedAddress)
        {
            // $4000–$4003	Pulse 1	Timer, length counter, envelope, sweep
            // $4004–$4007	Pulse 2	Timer, length counter, envelope, sweep
            // $4008–$400B	Triangle	Timer, length counter, linear counter
            // $400C–$400F	Noise	Timer, length counter, envelope, linear feedback shift register
            // $4010–$4013	DMC	Timer, memory reader, sample buffer, output unit
            // $4015	All	Channel enable and length counter status
            // $4017	All	Frame counter

            // case 0:
            // break;
            // case 1:
            // break;
            // case 2:
            // break;
            // case 3:
            // break;
            // case 4:
            // break;
            // case 5:
            // break;
            // case 6:
            // break;
            // case 7:
            // break;
            // case 8:
            // break;
            // case 9:
            // break;
            // case 0xA:
            // break;
            // case 0xB:
            // break;
            // case 0xC:
            // break;
            // case 0xD:
            // break;
            // case 0xE:
            // break;
            // case 0xF:
            // break;
            // case 0x10:
            // break;
            // case 0x11:
            // break;
            // case 0x12:
            // break;
            // case 0x13:
            // break;
            case 0x15: // only one that is read + write
            data = apuStatus;
            break;
            // case 0x17:
            // break;
        }

        return data;
    }
    public byte cpuWrite(ushort mappedAddress, byte data)
    {
        switch(mappedAddress)
        {
            // $4000–$4003	Pulse 1	Timer, length counter, envelope, sweep
            // $4004–$4007	Pulse 2	Timer, length counter, envelope, sweep
            // $4008–$400B	Triangle	Timer, length counter, linear counter
            // $400C–$400F	Noise	Timer, length counter, envelope, linear feedback shift register
            // $4010–$4013	DMC	Timer, memory reader, sample buffer, output unit
            // $4015	All	Channel enable and length counter status
            // $4017	All	Frame counter

            // pulse channel 1 and 2 are competitive, but I'm pretty sure this is efficient
            // because of the switch case

            // --------------------- PULSE CHANNEL 1     --------------------------------------------
            case 0x0: // DDLC NNNN
                pulseChannels[0].duty = (byte)(data >> 6);
                switch(pulseChannels[0].duty)
                {
                    case 0:
                        pulseChannels[0].sequenceBuffer = 0b01000000;
                        break;
                    case 1:
                        pulseChannels[0].sequenceBuffer = 0b01100000;
                        break;
                    case 2:
                        pulseChannels[0].sequenceBuffer = 0b01111000;
                        break;
                    case 3:
                        pulseChannels[0].sequenceBuffer = 0b01111110;
                        break;
                }
                pulseChannels[0].sequence = pulseChannels[0].sequenceBuffer;
                pulseChannels[0].loopFlag = (data & 0x20) != 0 ? true : false;
                pulseChannels[0].constantVolume = (data & 0x10) != 0 ? true : false;
                pulseChannels[0].volume = (byte)(data & 0x0F);
                //The duty cycle is changed (see table below), but the sequencer's current position isn't affected.
                break;
            case 0x1: // EPPP NSSS	
                pulseChannels[0].sweepEnabledFlag = (data & 0x80) != 0 ? true : false;
                pulseChannels[0].sweepPeriod = (byte)((data & 0x70) >> 4);
                pulseChannels[0].negateFlag = (data & 0x08) != 0 ? true : false;
                pulseChannels[0].sweepShift = (byte)(data & 0x07);
                pulseChannels[0].reloadFlag = true;
                break;
            case 0x2: // LLLL LLLL
                pulseChannels[0].timerReload = (byte)((pulseChannels[0].timer & 0xFF00) | data);
                break;
            case 0x3: // LLLL LHHH
                pulseChannels[0].sequence = pulseChannels[0].sequenceBuffer;
                pulseChannels[0].length = (byte)(data >> 3);
                pulseChannels[0].timerReload = (byte)((pulseChannels[0].timer & 0x00FF) | (((ushort)data & 0x07) << 8));
                pulseChannels[0].timer = pulseChannels[0].timerReload;
                pulseChannels[0].startFlag = true;
                //The sequencer is immediately restarted at the first value of the current sequence. The envelope is also restarted. The period divider is not reset.[1]
                break;
            // --------------------- PULSE CHANNEL 2 --------------------------------------------
            case 0x4: // DDLC NNNN
                pulseChannels[1].duty = (byte)(data >> 6);
                switch(pulseChannels[1].duty)
                {
                    case 0:
                        pulseChannels[1].sequenceBuffer = 0b01000000;
                        break;
                    case 1:
                        pulseChannels[1].sequenceBuffer = 0b01100000;
                        break;
                    case 2:
                        pulseChannels[1].sequenceBuffer = 0b01111000;
                        break;
                    case 3:
                        pulseChannels[1].sequenceBuffer = 0b10011111;
                        break;
                }
                pulseChannels[1].sequence = pulseChannels[1].sequenceBuffer;
                pulseChannels[1].loopFlag = (data & 0x20) != 0 ? true : false;
                pulseChannels[1].constantVolume = (data & 0x10) != 0 ? true : false;
                pulseChannels[1].volume = (byte)(data & 0x0F);
                break;
            case 0x5: // EPPP NSSS	
                pulseChannels[1].sweepEnabledFlag = (data & 0x80) != 0 ? true : false;
                pulseChannels[1].sweepPeriod = (byte)((data & 0x70) >> 4);
                pulseChannels[1].negateFlag = (data & 0x08) != 0 ? true : false;
                pulseChannels[1].sweepShift = (byte)(data & 0x07);
                pulseChannels[1].reloadFlag = true;
                break;
            case 0x6: // LLLL LLLL
                pulseChannels[1].timer = (byte)((pulseChannels[0].timer & 0xFF00) | data);
                break;
            case 0x7: // LLLL LHHH
                pulseChannels[1].length = (byte)(data >> 3);
                pulseChannels[1].timer = (byte)((pulseChannels[0].timer & 0x00FF) | (((ushort)data & 0x07) << 8));
                pulseChannels[1].startFlag = true;
                pulseChannels[1].timer = pulseChannels[0].timerReload;
                pulseChannels[1].startFlag = true;
                break;

            case 0x8: // CRRR RRRR	
                triangleChannel.controlFlag = (data & 0x80) != 0 ? true : false;
                triangleChannel.linearCounter = (byte)(data & 0xEF);
                break;
            case 0xA: // LLLL LLLL
                triangleChannel.timer = (byte)((triangleChannel.timer & 0xFF00) | data);
                break;
            case 0xB: // LLLL LHHH	
                triangleChannel.length = (byte)(data >> 3);
                triangleChannel.timer = (byte)((triangleChannel.timer & 0x00FF) | (((ushort)data & 0x07) << 8));
                triangleChannel.linearCounterFlag = true;   
                break;
            // --------------------- NOISE CHANNEL --------------------------------------------
            case 0xC: // --LC NNNN	
                noiseChannel.loopFlag = (data & 0x20) != 0 ? true : false;
                noiseChannel.constantVolume = (data & 0x10) != 0 ? true : false;
                noiseChannel.volume = (byte)(data & 0x0F);
                break; 
            case 0xE: // L--- PPPP
                noiseChannel.loopFlag = (data & 0x80) != 0 ? true : false;
                noiseChannel.timerPeriod = (byte)(data & 0xF0);
                break;
            case 0xF: // LLLL L---	

            break;
            // --------------------- DMC CHANNEL --------------------------------------------
            case 0x10:
            break;
            case 0x11:
            break;
            case 0x12:
            break;
            case 0x13:
            break;
            case 0x15: // CONTROL (read + write? what a deal!)
            
            break;
            case 0x17: // FRAME COUNTER
            break;
        }

        return data;
    }
    public void reset()
    {
        // reset the varibles   
    }
    public void ClockEnvelope(ref PulseChannel pulseChannel)
    {
        if (pulseChannel.startFlag)
        {
            pulseChannel.startFlag = false;
            pulseChannel.envelopeDecay = 15;
            pulseChannel.envelopeDivider = pulseChannel.volume;
        } else
        {
            if (pulseChannel.envelopeDivider > 0)
            {
                pulseChannel.envelopeDivider--;
            } else
            {
                pulseChannel.envelopeDivider = pulseChannel.volume;

                if (pulseChannel.envelopeDecay > 0)
                {
                    pulseChannel.envelopeDecay--;
                } 

                if (pulseChannel.loopFlag)
                {
                    pulseChannel.envelopeDecay = 15;
                }
            }
        }
    }

    public void ClockEnvelope(ref NoiseChannel pulseChannel)
    {
        if (pulseChannel.startFlag)
        {
            pulseChannel.startFlag = false;
            pulseChannel.envelopeDecay = 15;
            pulseChannel.envelopeDivider = pulseChannel.volume;
        } else
        {
            if (pulseChannel.envelopeDivider > 0)
            {
                pulseChannel.envelopeDivider--;
            } else
            {
                pulseChannel.envelopeDivider = pulseChannel.volume;

                if (pulseChannel.envelopeDecay > 0)
                {
                    pulseChannel.envelopeDecay--;
                } 

                if (pulseChannel.loopFlag)
                {
                    pulseChannel.envelopeDecay = 15;
                }
            }
        }
    }
    
    public void ClockTriangleCounter()
    {
        
    }   
    public void ClockLengthCounters()
    {
        
    }
    // sorry for the genuinely horrible if statemtents
    public void ClockSweep(int i)
    {
        int change = pulseChannels[i].timerReload >> pulseChannels[i].sweepShift;

        int targetPeriod = pulseChannels[i].negateFlag ? pulseChannels[i].timerReload - change - (i == 0 ? 1 : 0) : pulseChannels[i].timerReload + change;
        pulseChannels[i].sweepMute = pulseChannels[i].timerReload < 8 || targetPeriod > 0x7FF;

        if (pulseChannels[i].reloadFlag)
        {
            pulseChannels[i].sweepDivider = pulseChannels[i].sweepPeriod;
            pulseChannels[i].reloadFlag = false;
        }
        else
        {
            if (pulseChannels[i].sweepDivider > 0)
            {
                pulseChannels[i].sweepDivider--;
            }
            else
            {
                pulseChannels[i].sweepDivider = pulseChannels[i].sweepPeriod;

                if (pulseChannels[i].sweepEnabledFlag && pulseChannels[i].sweepShift > 0 && !pulseChannels[i].sweepMute)
                {
                    pulseChannels[i].timerReload = (ushort)targetPeriod;

                    change = pulseChannels[i].timerReload >> pulseChannels[i].sweepShift;

                    targetPeriod = pulseChannels[i].negateFlag
                            ? pulseChannels[i].timerReload - change - (i == 0 ? 1 : 0)
                            : pulseChannels[i].timerReload + change;

                    pulseChannels[i].sweepMute = pulseChannels[i].timerReload < 8 || targetPeriod > 0x7FF;
                }
            }
        }
    }
    // calculate the output of everything, basically
    public float GetOutput()
    {
 
        // --------------------------------- PULSE CHANNELS ----------------------------------
        PulseChannel pch1 = pulseChannels[0];
        int p1SequencerBit = (pch1.sequence & (1 << (7 - pch1.sequencePos))) != 0 ? 1 : 0;
        int p1Volume = pch1.length == 0 ? 0 : pch1.sweepMute ? 0 : (pch1.constantVolume ? pch1.volume : pch1.envelopeDecay);
        int p1 = p1SequencerBit * p1Volume;

      
        PulseChannel pch2 = pulseChannels[1];
        int p2SequencerBit = (pch2.sequence & (1 << (7 - pch2.sequencePos))) != 0 ? 1 : 0;
        int p2Volume = pch2.length == 0 ? 0 : pch2.sweepMute ? 0 : (pch2.constantVolume ? pch2.volume : pch2.envelopeDecay);
        int p2 = p2SequencerBit * p2Volume;
        // --------------------------------- TND!!! ----------------------------------
        int t =0;
        int n =0;
        int d =0;


        float pulseOut = 95.88f / (8128f / (float)(p1 + p2) + 100f);
        float tndOut = 159.79f / (1f / (t/8227f + n/12241f + d/22638f) + 100f); 

        return pulseOut + tndOut;
    }
    int frameCounter = 0;
    bool quarterFrame;
    bool halfFrame;
    public void clock()
    {
        // clock cycles happen once every 6 ppu of once 
        frameCounter++;

        // -------------- PulseChannels Clocking ----------------------------------
        for (int i = 0; i < 2; i++)
        {
            ref PulseChannel pulseChannel = ref pulseChannels[i];
            pulseChannel.timer--;
            if (pulseChannel.timer == 0)
            {
                pulseChannel.timer = pulseChannel.timerReload;
                pulseChannel.sequencePos = (byte)((pulseChannel.sequencePos + 1) % 7);
            }

        }
        // -------------- Triangles Clocking ----------------------------------


        // --------------- THE FRAME THINGIES??? I'm not sure, QuaterFrame HalfFrame type stuff
        if (frameCounter == 3729)
        {
            quarterFrame = true;
        }

        if (frameCounter == 7457)
        {
            quarterFrame = true;
            halfFrame = true;
        }

        if (frameCounter == 11186)
        {
            quarterFrame = true;
        }

        if (frameCounter == 14915)
        {
            quarterFrame = true;
            halfFrame = true;
            frameCounter = 0;
        }


        if (quarterFrame)
        {
            ClockEnvelope(ref pulseChannels[0]);
            ClockEnvelope(ref pulseChannels[1]);
            ClockEnvelope(ref noiseChannel);

            ClockTriangleCounter();
        }

        if (halfFrame)
        {
            ClockLengthCounters();
            ClockSweep(0);
            ClockSweep(1);
        }

        quarterFrame = false;
        halfFrame = false;
    }
}

