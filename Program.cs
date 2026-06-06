// See https://aka.ms/new-console-template for more information
using System.Diagnostics;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.ComponentModel;

class Program
{

    Bus nes = null!;
    Cartridge catridge = null!;


    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        Bus nes = new Bus();

        Cartridge cartridge = new Cartridge("tests/test.nes");
        nes.InsertCartridge(cartridge);


        nes.reset();
        Thread thread = new Thread(() =>
        {
            while (true)
            {
                nes.clock();
            }
        });

        thread.IsBackground = true;
        thread.Start();

        MainForm form = new MainForm(nes.ppu.displayScreen, nes);


        Thread guiThread = new Thread(() => {
            while (true)
            {
                form.GameLoop();
            }
        });

        guiThread.IsBackground = true;
        guiThread.Start();

        Application.Run(form);
    }

}

public class MainForm : Form
{
    int width = 0;
    int height = 0;
    Bitmap screen;
    Bitmap oldScreen;
    Bus nes;
    
    int currentPalette = 0;
    int frameCount = 0;

    Panel paletteColor1 = new Panel();
    Panel paletteColor2 = new Panel();
    Panel paletteColor3 = new Panel();
    Panel paletteColor4 = new Panel();
    Stopwatch stopwatch = Stopwatch.StartNew();

    private string iconName = "bung1rIcon.ico";
    private string fullPath;
    private const int targetFPS = 60;
    private long targetTicksPerFrame = Stopwatch.Frequency / targetFPS;
    public MainForm(Pixel[,] pixels, Bus nes)
    {
        ClientSize = new Size(512, 512);
        DoubleBuffered = true;
        Text = "Bung1r's NES Emulator";
        fullPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, iconName);
        if (System.IO.File.Exists(fullPath)) this.Icon = new System.Drawing.Icon(fullPath);



        screen = PixelsToBitmap(pixels);
        this.nes = nes;

        Button button = new Button();
        button.Text = "Switch Palettes";
        button.Name = "Palette Switcher";
        button.Size = new Size(100,40);
        button.Location = new Point(590,50);
        button.Click += new EventHandler(SwitchPalette);
        this.Controls.Add(button);

        paletteColor1.Location = new Point(550,100);
        paletteColor1.Size = new Size(30,30);
        paletteColor2.Location = new Point(580,100);
        paletteColor2.Size = new Size(30,30);
        paletteColor3.Location = new Point(610,100);
        paletteColor3.Size = new Size(30,30);
        paletteColor4.Location = new Point(640,100);
        paletteColor4.Size = new Size(30,30);

        Pixel color1 = nes.ppu.GetColorFromPalette((byte)0, 0);
        paletteColor1.BackColor = Color.FromArgb(color1.R, color1.G, color1.B);
        Pixel color2 = nes.ppu.GetColorFromPalette((byte)0, 1);
        paletteColor2.BackColor = Color.FromArgb(color2.R, color2.G, color2.B);
        Pixel color3 = nes.ppu.GetColorFromPalette((byte)0, 2);
        paletteColor3.BackColor = Color.FromArgb(color3.R, color3.G, color3.B);
        Pixel color4 = nes.ppu.GetColorFromPalette((byte)0, 3);
        paletteColor4.BackColor = Color.FromArgb(color4.R, color4.G, color4.B);
        
        this.Controls.Add(paletteColor1);
        this.Controls.Add(paletteColor2);
        this.Controls.Add(paletteColor3);
        this.Controls.Add(paletteColor4);
    }
    private void SwitchPalette(object sender, EventArgs e)
    {
        currentPalette++;
        currentPalette %= 8;

        Pixel color1 = nes.ppu.GetColorFromPalette((byte)currentPalette, 0);
        paletteColor1.BackColor = Color.FromArgb(color1.R, color1.G, color1.B);
        Pixel color2 = nes.ppu.GetColorFromPalette((byte)currentPalette, 1);
        paletteColor2.BackColor = Color.FromArgb(color2.R, color2.G, color2.B);
        Pixel color3 = nes.ppu.GetColorFromPalette((byte)currentPalette, 2);
        paletteColor3.BackColor = Color.FromArgb(color3.R, color3.G, color3.B);
        Pixel color4 = nes.ppu.GetColorFromPalette((byte)currentPalette, 3);
        paletteColor4.BackColor = Color.FromArgb(color4.R, color4.G, color4.B);
        // File.AppendAllText("log.txt",$"Palette: {currentPalette}\n");
        // File.AppendAllText("log.txt",$"{color1.R}, {color1.G}, {color1.B}, {(nes.ppu.ppuRead((ushort)(0x3F00 + (currentPalette << 2) + 0)) & 0x3F):X4}\n");
        // File.AppendAllText("log.txt",$"{color2.R}, {color2.G}, {color2.B}, {(nes.ppu.ppuRead((ushort)(0x3F00 + (currentPalette << 2) + 1)) & 0x3F):X4}\n");
        // File.AppendAllText("log.txt",$"{color3.R}, {color3.G}, {color3.B}, {(nes.ppu.ppuRead((ushort)(0x3F00 + (currentPalette << 2) + 2)) & 0x3F):X4}\n");
        // File.AppendAllText("log.txt",$"{color4.R}, {color4.G}, {color4.B}, {(nes.ppu.ppuRead((ushort)(0x3F00 + (currentPalette << 2) + 3)) & 0x3F):X4}\n");

    }
    private Bitmap PixelsToBitmap(Pixel[,] pixels)
    {
        width = pixels.GetLength(0);
        height = pixels.GetLength(1);

        Bitmap bitmap = new Bitmap(
            width,
            height,
            PixelFormat.Format32bppArgb);

        BitmapData data = bitmap.LockBits(
            new Rectangle(0, 0, width, height),
            ImageLockMode.WriteOnly,
            PixelFormat.Format32bppArgb);

        unsafe
        {
            byte* ptr = (byte*)data.Scan0;

            for (int y = 0; y < height; y++)
            {
                byte* row = ptr + y * data.Stride;

                for (int x = 0; x < width; x++)
                {
                    Pixel p = pixels[x, y];

                    int offset = x * 4;

                    row[offset + 0] = p.B;     // Blue
                    row[offset + 1] = p.G;     // Green
                    row[offset + 2] = p.R;     // Red
                    row[offset + 3] = 255;     // Alpha

                    // if (p.B != 0 || p.G != 0 || p.R != 0)
                    // {
                    //     File.WriteAllText("log.txt","I'm not black.");
                    // }
                }
            }
        }

        bitmap.UnlockBits(data);
        return bitmap;
    }
    public void GameLoop()
    {
        while(nes.ppu.frameComplete == false)
        {
            Thread.Sleep(0);
        }
    
        long frameStartTicks = stopwatch.ElapsedTicks;

        // File.AppendAllText("nestest_output.log", "i am looping the rooms help me\n");
        oldScreen = screen;
        
        // if (screen != null) screen.Dispose(); 
        // screen = PixelsToBitmap(nes.ppu.GetPatternTable(0, currentPalette)); // change this to the screen later
        screen = PixelsToBitmap(nes.ppu.displayScreen);
        Invalidate();

        oldScreen.Dispose();
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.InterpolationMode =
            InterpolationMode.NearestNeighbor;

        e.Graphics.DrawImage(
            screen,
            0,
            0,
            width * 2,
            height * 2);
    }
}