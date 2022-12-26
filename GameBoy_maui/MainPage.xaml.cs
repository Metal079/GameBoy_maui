using __XamlGeneratedCode__;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace GameBoy_maui
{
	public partial class MainPage : ContentPage
	{
		public MainPage()
		{
            InitializeComponent();

            //string romPath = @"C:\Users\metal\source\repos\Gameboy\Roms\test\cpu_instrs\individual\06-ld r,r.gb";
            //byte[] bytes = GB.LoadRom(romPath);

            var viewModel = new MainPageViewModel();
            BindingContext = viewModel;
            GB.viewModel = viewModel;
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
            GB.SetViewModelRegisters();
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

// GB View Model for registers and other stats
namespace GameBoy_maui
{
    public class MainPageViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private byte _regA;
        private byte _regB;
        private byte _regC;
        private byte _regD;
        private byte _regE;
        private byte _regF;
        private byte _regH;
        private byte _regL;

        private ushort _regAF;
        private ushort _regBC;
        private ushort _regDE;
        private ushort _regHL;

        private ushort _regSP;
        private ushort _regPC;

        public byte A
        {
            get => _regA;
            set
            {
                _regA = value;
                OnPropertyChanged();
            }
        }

        public byte B
        {
            get => _regB;
            set
            {
                _regB = value;
                OnPropertyChanged();
            }
        }

        public byte C
        {
            get => _regC;
            set
            {
                _regC = value;
                OnPropertyChanged();
            }
        }

        public byte D
        {
            get => _regD;
            set
            {
                _regD = value;
                OnPropertyChanged();
            }
        }

        public byte E
        {
            get => _regE;
            set
            {
                _regE = value;
                OnPropertyChanged();
            }
        }

        public byte F
        {
            get => _regF;
            set
            {
                _regF = value;
                OnPropertyChanged();
            }
        }

        public byte H
        {
            get => _regH;
            set
            {
                _regH = value;
                OnPropertyChanged();
            }
        }

        public byte L
        {
            get => _regL;
            set
            {
                _regL = value;
                OnPropertyChanged();
            }
        }

        public ushort AF
        {
            get => _regAF;
            set
            {
                _regAF = value;
                OnPropertyChanged();
            }
        }

        public ushort BC
        {
            get => _regBC;
            set
            {
                _regBC = value;
                OnPropertyChanged();
            }
        }

        public ushort DE
        {
            get => _regDE;
            set
            {
                _regDE = value;
                OnPropertyChanged();
            }
        }

        public ushort HL
        {
            get => _regHL;
            set
            {
                _regHL = value;
                OnPropertyChanged();
            }
        }

        public ushort SP
        {
            get => _regSP;
            set
            {
                _regSP = value;
                OnPropertyChanged();
            }
        }

        public ushort PC
        {
            get => _regPC;
            set
            {
                _regPC = value;
                OnPropertyChanged();
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}