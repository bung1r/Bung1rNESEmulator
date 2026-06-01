// See https://aka.ms/new-console-template for more information
class Program
{
    static void Main()
    {
        Bus nes = new Bus();

        Cartridge cartridge = new Cartridge("nestest.nes");
        nes.InsertCartridge(cartridge);

        nes.reset();

        while (true)
        {
            nes.clock();
        }
    }
}