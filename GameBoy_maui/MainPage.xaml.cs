using System;
using System.Collections.Generic;
using System.Text;

namespace GameBoy_maui
{
	public partial class MainPage : ContentPage
	{
		int count = 0;

		public MainPage()
		{
            InitializeComponent();
            string romPath = @"C:\Users\metal\source\repos\Gameboy\Roms\test\cpu_instrs\individual\06-ld r,r.gb";
            byte[] bytes = GB.LoadRom(romPath);
        }

        void OnEntryTextChanged(object sender, TextChangedEventArgs e)
        {
            string oldText = e.OldTextValue;
            string newText = e.NewTextValue;
            string myText = entry.Text;
        }

        void OnEntryCompleted(object sender, EventArgs e)
        {
            string text = ((Entry)sender).Text;
            byte command = Byte.Parse(text);
            GB.RunOpcode(command);
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

