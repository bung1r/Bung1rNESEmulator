// See https://aka.ms/new-console-template for more information
using System.Diagnostics;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.ComponentModel;
using System.Configuration;

public class Program
{

    public Bus nes = null!;
    public Cartridge catridge = null!;
    public MainForm form = null!;

    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        // Cartridge cartridge = new Cartridge("tests/donkey_kong.nes");
        // nes.InsertCartridge(cartridge);


        // nes.reset();

        // Thread thread = new Thread(() =>
        // {

        // });
        

        // thread.IsBackground = true;
        // thread.Start();

        
        MainForm form = new MainForm();


        // Thread guiThread = new Thread(() => {
            
        // });

        // guiThread.IsBackground = true;
        // guiThread.Start();

        Application.Run(form);
    }
    
}

public class MainForm : Form
{    
    int width = 0;
    int height = 0;
    float pixelScaling = 2f;
    Bitmap screen;
    Bitmap oldScreen;
    Bus nes;
    public Cartridge catridge = null!;
    Thread NESThread = null!;
    Thread GUIThread = null!;
    bool ROMInserted = false;
    bool debugPatternTable = false;
    int debugPalette = 0;
    MenuStrip menuStrip = new MenuStrip();
    ToolStripMenuItem fileMenu = new ToolStripMenuItem("File");
    ToolStripMenuItem openRom = new ToolStripMenuItem("Open ROM");
    ToolStripMenuItem exitRom = new ToolStripMenuItem("Exit ROM");

    ToolStripMenuItem viewMenu = new ToolStripMenuItem("View");
    ToolStripMenuItem scaleMenu = new ToolStripMenuItem("Scale");

    ToolStripMenuItem debugMenu = new ToolStripMenuItem("Debug");
    ToolStripMenuItem displayPatternMenu = new ToolStripMenuItem("Show Pattern Table");

    ToolStripMenuItem scale1x = new ToolStripMenuItem("1x");
    ToolStripMenuItem scale15x = new ToolStripMenuItem("1.5x");
    ToolStripMenuItem scale2x = new ToolStripMenuItem("2x");
    ToolStripMenuItem scale25x = new ToolStripMenuItem("2.5x");
    ToolStripMenuItem scale3x = new ToolStripMenuItem("3x");
    ToolStripMenuItem scale35x = new ToolStripMenuItem("3.5x");
    ToolStripMenuItem scale4x = new ToolStripMenuItem("4x");
    Button button = new Button();
    Panel paletteColor1 = new Panel();
    Panel paletteColor2 = new Panel();
    Panel paletteColor3 = new Panel();
    Panel paletteColor4 = new Panel();
    private CancellationTokenSource cts = new CancellationTokenSource();

    public MainForm()
    {
        
        BackColor = Color.AliceBlue;
        DoubleBuffered = true;
        Text = "Bung1r's NES Emulator";
        KeyPreview = true;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;

        KeyDown += new KeyEventHandler(InputKeyDown);
        KeyUp += new KeyEventHandler(InputKeyUp);     

        if (File.Exists("Other/bung1rIcon.ico")) this.Icon = new Icon("Other/bung1rIcon.ico");

        screen = PixelsToBitmap(new Pixel[256,240]);


        button.Text = "Switch Palettes";
        button.Name = "Palette Switcher";
        button.Size = new Size(100,40);
        button.Location = new Point(590,50);
        button.Click += new EventHandler(SwitchPalette);
        // this.Controls.Add(button);
        
        paletteColor1.Location = new Point(550,100);
        paletteColor1.Size = new Size(30,30);
        paletteColor2.Location = new Point(580,100);
        paletteColor2.Size = new Size(30,30);
        paletteColor3.Location = new Point(610,100);
        paletteColor3.Size = new Size(30,30);
        paletteColor4.Location = new Point(640,100);
        paletteColor4.Size = new Size(30,30);

        scaleMenu.DropDownItems.Add(scale1x);
        scaleMenu.DropDownItems.Add(scale15x);
        scaleMenu.DropDownItems.Add(scale2x);
        scaleMenu.DropDownItems.Add(scale25x);
        scaleMenu.DropDownItems.Add(scale3x);
        scaleMenu.DropDownItems.Add(scale35x);
        scaleMenu.DropDownItems.Add(scale4x);

        scale1x.Click += (s, e) => SetScale(1);
        scale15x.Click += (s, e) => SetScale(1.5f);
        scale2x.Click += (s, e) => SetScale(2);
        scale25x.Click += (s, e) => SetScale(2.5f);
        scale3x.Click += (s, e) => SetScale(3);
        scale35x.Click += (s, e) => SetScale(3.5f);
        scale4x.Click += (s, e) => SetScale(4f);


        exitRom.Click += (s,e) =>
        {
            CancelThreads();
            Invalidate();
         
            if (debugPatternTable)
            {
                this.Controls.Remove(button);
                this.Controls.Remove(paletteColor1);
                this.Controls.Remove(paletteColor2);
                this.Controls.Remove(paletteColor3);
                this.Controls.Remove(paletteColor4);
                debugPatternTable = !debugPatternTable;
                displayPatternMenu.Checked = debugPatternTable;
                debugPalette = 0;
            }
            ROMInserted = false;
        };

        openRom.Click += (s, e) =>
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "NES ROMs (*.nes)|*.nes";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                
                string path = openFileDialog.FileName;
                MessageBox.Show(path);

                CancelThreads();
                
                nes = new Bus();
                Cartridge cartridge = new Cartridge(path);
                nes.InsertCartridge(cartridge);
                nes.reset();

                ROMInserted = true;

                NESThread = new Thread(() => NESThreadLoop());
                GUIThread = new Thread(() => GUIThreadLoop());
                
                NESThread.IsBackground = true;
                NESThread.Start();
                
                GUIThread.IsBackground = true;
                GUIThread.Start();
            }
        };

        displayPatternMenu.Click += (s,e) =>
        {
            if (!ROMInserted) return;
            debugPatternTable = !debugPatternTable;
            displayPatternMenu.Checked = debugPatternTable;
            debugPalette = 0;

            if (debugPatternTable)
            {
                this.Controls.Add(button);
                this.Controls.Add(paletteColor1);
                this.Controls.Add(paletteColor2);
                this.Controls.Add(paletteColor3);
                this.Controls.Add(paletteColor4);
            } else
            {
                this.Controls.Remove(button);
                this.Controls.Remove(paletteColor1);
                this.Controls.Remove(paletteColor2);
                this.Controls.Remove(paletteColor3);
                this.Controls.Remove(paletteColor4);
            }
        };
        fileMenu.DropDownItems.Add(openRom);
        fileMenu.DropDownItems.Add(exitRom);
        viewMenu.DropDownItems.Add(scaleMenu);
        debugMenu.DropDownItems.Add(displayPatternMenu);
        menuStrip.Items.Add(fileMenu);
        menuStrip.Items.Add(viewMenu);
        menuStrip.Items.Add(debugMenu);


        Controls.Add(menuStrip);

        SetScale(2f);
    }
    private void CancelThreads()
    {
        cts.Cancel();
        if (NESThread != null)
        {
            NESThread.Join();
        } 
        if (GUIThread != null)
        {
            GUIThread.Join();
        }
        cts.Dispose();
        cts = new CancellationTokenSource();
    }
    private void NESThreadLoop()
    {
        const double NES_CLOCK_RATE = 5369318.0;

        Stopwatch sw = Stopwatch.StartNew();

        double previousTime = sw.Elapsed.TotalSeconds;
        double accumulatedCycles = 0;

        while (!cts.Token.IsCancellationRequested)
        {
            double currentTime = sw.Elapsed.TotalSeconds;
            double deltaTime = currentTime - previousTime;
            previousTime = currentTime;

            accumulatedCycles += deltaTime * NES_CLOCK_RATE;

            while (accumulatedCycles >= 1.0)
            {
                nes.clock();
                accumulatedCycles--;
            }

            Thread.Sleep(0);
        }
    }
    private void GUIThreadLoop()
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        int targetFPS = 60;
        long lastUpdateMilli = 0;
        long targetMilliPerFrame = 1000 / targetFPS;
        while (!cts.Token.IsCancellationRequested)
        {
            if (stopwatch.ElapsedMilliseconds - lastUpdateMilli > targetMilliPerFrame) {
                lastUpdateMilli = stopwatch.ElapsedMilliseconds;
                GameLoop();
            }
        }
    }
    private void SwitchPalette(object sender, EventArgs e)
    {
        debugPalette++;
        debugPalette %= 8;

        Pixel color1 = nes.ppu.GetColorFromPalette((byte)debugPalette, 0);
        paletteColor1.BackColor = Color.FromArgb(color1.R, color1.G, color1.B);
        Pixel color2 = nes.ppu.GetColorFromPalette((byte)debugPalette, 1);
        paletteColor2.BackColor = Color.FromArgb(color2.R, color2.G, color2.B);
        Pixel color3 = nes.ppu.GetColorFromPalette((byte)debugPalette, 2);
        paletteColor3.BackColor = Color.FromArgb(color3.R, color3.G, color3.B);
        Pixel color4 = nes.ppu.GetColorFromPalette((byte)debugPalette, 3);
        paletteColor4.BackColor = Color.FromArgb(color4.R, color4.G, color4.B);
    }
    private void SetScale(float scale)
    {
    
        this.pixelScaling = scale;
        ClientSize = new Size((int)(256 * pixelScaling), (int)(240 * pixelScaling) + menuStrip.Height);


        scale1x.Checked = false;
        scale15x.Checked = false;
        scale2x.Checked = false;
        scale25x.Checked = false;
        scale3x.Checked = false;
        scale35x.Checked = false;
        scale4x.Checked = false;

        button.Location = new Point(20, (int)(128 * pixelScaling + 60));
        paletteColor1.Location = new Point(140, (int)(128 * pixelScaling + 60));
        paletteColor2.Location = new Point(170, (int)(128 * pixelScaling + 60));
        paletteColor3.Location = new Point(200, (int)(128 * pixelScaling + 60));
        paletteColor4.Location = new Point(230, (int)(128 * pixelScaling + 60));

        switch (scale)
        {
            case 1f:
                scale1x.Checked = true;
                break;
            case 1.5f:
                scale15x.Checked = true;
                break;
            case 2f:
                scale2x.Checked = true;
                break;
            case 2.5f:
                scale25x.Checked = true;
                break;
            case 3f:
                scale3x.Checked = true;
                break;
            case 3.5f:  
                scale35x.Checked = true;
                break;
            case 4f:
                scale4x.Checked = true;
                break;
        }
        
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
        while(!nes.ppu.frameComplete)
        {
            Thread.Sleep(0);
        }

        // File.AppendAllText("nestest_output.log", "i am looping the rooms help me\n");
        oldScreen = screen;
        
        // if (screen != null) screen.Dispose(); 
        // screen = PixelsToBitmap(nes.ppu.GetPatternTable(0, currentPalette)); // change this to the screen later
        if (!debugPatternTable) {
            screen = PixelsToBitmap(nes.ppu.displayScreen);
        } else
        {
            screen = PixelsToBitmap(nes.ppu.GetPatternTable(0, debugPalette));
        }
        Invalidate();

        oldScreen.Dispose();
    }
    private void InputKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.KeyCode)
        {
            case Keys.X: nes.controllers[0] |= 0x80; break; // A
            case Keys.Z: nes.controllers[0] |= 0x40; break; // B
            case Keys.N: nes.controllers[0] |= 0x20; break; // Select
            case Keys.M: nes.controllers[0] |= 0x10; break; // Start
            case Keys.W: nes.controllers[0] |= 0x08; break;
            case Keys.S: nes.controllers[0] |= 0x04; break;
            case Keys.A: nes.controllers[0] |= 0x02; break;
            case Keys.D: nes.controllers[0] |= 0x01; break;
        }
        // File.AppendAllText("Logs/log.txt", $"{nes.controllers[0]:B8}\n");
    }
    private void InputKeyUp(object sender, KeyEventArgs e)
    {
        switch (e.KeyCode)
        {
            case Keys.X: nes.controllers[0] &= 0x7F; break; // clear bit 7
            case Keys.Z: nes.controllers[0] &= 0xBF; break; // clear bit 6
            case Keys.N: nes.controllers[0] &= 0xDF; break; // clear bit 5
            case Keys.M: nes.controllers[0] &= 0xEF; break; // clear bit 4
            case Keys.W: nes.controllers[0] &= 0xF7; break; // clear bit 3
            case Keys.S: nes.controllers[0] &= 0xFB; break; // clear bit 2
            case Keys.A: nes.controllers[0] &= 0xFD; break; // clear bit 1
            case Keys.D: nes.controllers[0] &= 0xFE; break; // clear bit 0
        }
        // File.AppendAllText("Logs/log.txt", $"{nes.controllers[0]:B8}\n");
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        if (!ROMInserted) return;
        e.Graphics.InterpolationMode =
            InterpolationMode.NearestNeighbor;

        e.Graphics.DrawImage(
            screen,
            0,
            menuStrip.Height,
            (int)(width * pixelScaling),
            (int)(height * pixelScaling));
    }

}