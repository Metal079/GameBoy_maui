namespace GameBoy_maui
{
	public partial class MainPage : ContentPage
	{
		int count = 0;

		public MainPage()
		{
			InitializeComponent();
            byte[] Bytes = GB.loadRom();
            GB GB_Sys = new GB();
        }

    }

    public class GB
    {
        // Create system hardware
        struct Registers
        {
            byte A;
            byte B;
            byte C;
            byte D;
            byte E;
            byte F;
            byte H;
            byte L;
        }
        private ushort[] memory= null;

        public static byte[] loadRom()
        {
            string romPath = @"C:\Users\metal\source\repos\Gameboy\Roms\test\cpu_instrs\individual\06-ld r,r.gb";
            byte[] Bytes1 = File.ReadAllBytes(romPath);

            Console.Write("hello");

            return Bytes1;
        }

        public void opcodes()
        {

        }
    }
}

namespace graphics
{
    public class GraphicsDrawable : IDrawable
    {
        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            // Drawing code goes here
            canvas.FillColor = Colors.Black;
            canvas.FillRectangle(10, 10, 160, 144);
        }
    }
}

