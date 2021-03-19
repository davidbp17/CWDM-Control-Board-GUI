using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;
using System.IO;
using Microsoft.Win32;

namespace CWDM_Control_Board_GUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private HID_Connection usb_connection;
		private bool superuserAccess;
        private const string CONNECT_TO = "Connect";
        private const string DISCONNECT_FROM = "Disconnect";
        private const int BIN_MODE = 1;
        private const int HEX_MODE = 2;
        private const int DEC_MODE = 3;
        public SvgImageSource gfLogo;
        private BSLOutputBridge outputAdapter;
        private BoardScriptingLanguage boardScriptingLanguage;
        private bool updating = false;
        private const string commentSyntax = "//";
        private SuperuserMode passwordWindow;
        public static RoutedCommand PasswordBoxCommand = new RoutedCommand();
        private string password = "globalfoundries";
        private SolidColorBrush greenBrush;
        private SolidColorBrush orangeBrush;
        private SolidColorBrush redBrush;
        private SolidColorBrush grayBrush;
        private int curAutotuneTest;
        private bool abortFlag = false;
        private string curFirmwareName = "\\cwdm_fw-20210115.bin";
        public bool? ConnectionStatus
        {
            get
            {
                if (usb_connection == null) return false;
                return connectionStatus;
            }
        }

        public bool SuperuserAccess { get { return superuserAccess; } }

        public bool RefreshButtonEnabled
		{ get;set; }

        public int BaseMode { get; set; }

        bool connectionStatus => usb_connection.Connected;

        /*Visibility Properties
         * Allows for binding of visibility
         * Used for grids and buttons
         */
        public Visibility RefreshButtonVisibility
		{
			get
			{
                if (RefreshButtonEnabled == true)
                    return Visibility.Visible;
                else
                    return Visibility.Hidden;
			}
		}

        public Visibility SettingsGridVisibility
        {
            get
            {
                if (connectionStatus)
                    return Visibility.Collapsed;
                else
                    return Visibility.Visible;
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;
 
        public MainWindow()
        {
            InitializeComponent();
            usb_connection = new HID_Connection();
            DataContext = this;
            superuserAccess = false;
            greenBrush = new SolidColorBrush();
            orangeBrush = new SolidColorBrush();
            redBrush = new SolidColorBrush();
            grayBrush = new SolidColorBrush();
            greenBrush.Color = Color.FromRgb(50, 200, 0);
            orangeBrush.Color = Color.FromRgb(255, 165, 0);
            redBrush.Color = Color.FromRgb(255, 0, 0);
            grayBrush.Color = Color.FromRgb(128, 128, 128);
            if(OutputBox != null)
                outputAdapter = new BSLRichTextBoxOutputBridge(OutputBox);
            passwordWindow = null;
            PasswordBoxCommand.InputGestures.Add(new KeyGesture(Key.W, ModifierKeys.Control));
            PasswordCommand.Command = PasswordBoxCommand;
            RefreshButtonEnabled = true;
            ReadProgress.Maximum = Registers.adc_registers.Length + Registers.dac_registers.Length + Registers.sel_registers.Length;
            BaseMode = HEX_MODE; //Hex Mode
            OnPropertyChanged("RefreshButtonEnabled");
            OnPropertyChanged("RefreshButtonVisibility");


        }

        private void OnPropertyChanged(String info)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(info));
            }
        }
        public string ConnectionText
        {
            get
            {
                if (usb_connection == null)
                    return CONNECT_TO;
                else
                    if (!connectionStatus)
                    return CONNECT_TO;
                return DISCONNECT_FROM;
            }
        }

        private List<Registers.ADCRegister> refreshADCRegisters()
        {
            List<Registers.ADCRegister> registers = new List<Registers.ADCRegister>();
            for (int i = 0; i < Registers.adc_registers.Length;)
            {
                int len = Registers.adc_registers.Length - i >= 10 ? 10 : Registers.adc_registers.Length - i;
                string[] names = new string[len];
                ushort[] regAddr = new ushort[len];
                for (int j = 0; j < len && i + j < Registers.adc_registers.Length; j++)
                {
                    (names[j], regAddr[j]) = Registers.adc_registers[i + j];
                }
                int[] values = usb_connection.SendReadCommands(regAddr);
                for (int j = 0; j < len; j++)
                {
                    if (values.Length == 1 && values[0] < 0)
                    {
                        registers.Add(new Registers.ADCRegister { Name = names[j], RegisterValue = values[0], Mode = BaseMode }); 
                    }
                    else
                    {
                        registers.Add(new Registers.ADCRegister { Name = names[j], RegisterValue = values[j], Mode = BaseMode });
                    }
                    ReadProgress.Dispatcher.Invoke(new Action(() => ReadProgress.Value += 1));
                }
                i += len;
            }
            return registers;
        }


        private List<Registers.DACRegister> refreshDACRegisters()
        {
            List<Registers.DACRegister> registers = new List<Registers.DACRegister>();
            for (int i = 0; i < Registers.dac_registers.Length; i++)
            {
                (string name, ushort output_addr, ushort offset_addr, ushort gain_addr) = Registers.dac_registers[i];
                int[] values = usb_connection.SendReadCommands(new ushort[] { output_addr, offset_addr, gain_addr });
                int output_val;
                int offset_val;
                int gain_val;
                if (values.Length == 1 && values[0] < 0)
                {
                    output_val = values[0];
                    offset_val = values[0];
                    gain_val = values[0];
                }
                else
                {
                    output_val = values[0];
                    offset_val = values[1];
                    gain_val = values[2];
                }
                registers.Add(new Registers.DACRegister { Name = name, OutputValue = output_val, OffsetValue = offset_val, GainValue = gain_val, Mode = BaseMode });
                ReadProgress.Dispatcher.Invoke(new Action(() => ReadProgress.Value += 1));
            }
            return registers;
        }


        private List<Registers.SELRegister> refreshSELRegisters()
        {
            List<Registers.SELRegister> registers = new List<Registers.SELRegister>();
            for (int i = 0; i < Registers.sel_registers.Length;)
            {
                int len = Registers.sel_registers.Length - i >= 10 ? 10 : Registers.sel_registers.Length - i;
                string[] names = new string[len];
                ushort[] regAddr = new ushort[len];
                for (int j = 0; j < len && i + j < Registers.sel_registers.Length; j++)
                {
                    (names[j], regAddr[j]) = Registers.sel_registers[i + j];
                }
                int[] values = usb_connection.SendReadCommands(regAddr);
                for (int j = 0; j < len; j++)
                {
                    if (values.Length == 1 && values[0] < 0)
                    {
                        registers.Add(new Registers.SELRegister { Name = names[j], RegisterValue = values[0], Mode = BaseMode });
                    }
                    else
                    {
                        registers.Add(new Registers.SELRegister { Name = names[j], RegisterValue = values[j], Mode = BaseMode });
                    }
                    ReadProgress.Dispatcher.Invoke(new Action(() => ReadProgress.Value += 1));
                }
                i += len;
            }
            return registers;
        }

   

        private async Task AutoRefreshRegisters()
        {
            while (true)
            {
                if (AutoRefresh.IsChecked.Value && connectionStatus)
                {
                    ADC_Registers.ItemsSource = await Task.Run(refreshADCRegisters);

                }
                else break;
                if (AutoRefresh.IsChecked.Value && connectionStatus)
                {
                    DAC_Registers.ItemsSource = await Task.Run(refreshDACRegisters);
                }
                else break;
                if (AutoRefresh.IsChecked.Value && connectionStatus)
                {
                    SEL_Registers.ItemsSource = await Task.Run(refreshSELRegisters);

                }
                else break;
                ReadProgress.Dispatcher.Invoke(new Action(() => ReadProgress.Value = 0));
            }
        }
        public void ToggleConnection(bool currentStatus) {
            if (currentStatus)
            {
                usb_connection.DisconnectDevice();
            }
            else
            {
                usb_connection.ConnectDevice();
                if (!connectionStatus)
                {
                    string messageBoxText = "Unable to connect to selected device.";
                    string caption = "Connection Failed!";
                    MessageBoxButton button = MessageBoxButton.OK;
                    MessageBoxImage icon = MessageBoxImage.Error;
                    MessageBox.Show(messageBoxText, caption, button, icon);
                }
            }
        }
        private async void Connection_Click(object sender, RoutedEventArgs e)
        {

            bool currentStatus = connectionStatus;
            string pidStr = PID_Textbox.Text.ToLower();
            string vidStr = VID_Textbox.Text.ToLower();
            if(!currentStatus &&!(vidStr.Equals("") || pidStr.Equals("")))
			{
                int vid; int pid;
                if (pidStr.StartsWith("0x"))
                {
                    pidStr = Convert.ToInt32(pidStr, 16).ToString();
                }
                if (pidStr.StartsWith("0b"))
                {
                    pidStr = Convert.ToInt32(pidStr.Substring(2), 2).ToString();
                }
                if (vidStr.StartsWith("0x"))
                {
                    vidStr = Convert.ToInt32(vidStr, 16).ToString();
                }
                if (vidStr.StartsWith("0b"))
                {
                    vidStr = Convert.ToInt32(vidStr.Substring(2), 2).ToString();
                }
                if (int.TryParse(pidStr, out pid) && int.TryParse(vidStr, out vid))
				{
                    usb_connection = new HID_Connection(vid, pid);
                }
                else
				{
                    string messageBoxText = $"Unable to parse VID/PID. Use Default Values?\nVID: {HID_Connection.DefaultVIDString()}\nPID: {HID_Connection.DefaultPIDString()}";
                    string caption = "Parsing Failed!";
                    MessageBoxButton button = MessageBoxButton.YesNo;
                    MessageBoxImage icon = MessageBoxImage.Error;
                    MessageBoxResult result = MessageBox.Show(messageBoxText, caption, button, icon);
                    if (result == MessageBoxResult.No)
                    {
                        //do nothing, no connection attempt
                        return;
                    }
                }
            }
            ToggleConnection(currentStatus);
            OnPropertyChanged("ConnectionText");
            OnPropertyChanged("ConnectionStatus");
            OnPropertyChanged("SettingsGridVisibility");
            if (connectionStatus)
            {
                
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                CheckConnection();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                RefreshButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                //ADC_Registers.ItemsSource = await Task.Run(refreshADCRegisters);
                //DAC_Registers.ItemsSource = await Task.Run(refreshDACRegisters);
                //SEL_Registers.ItemsSource = await Task.Run(refreshSELRegisters);
            }
            
        }

        private async void CheckConnection()
        {
            if (!connectionStatus) return;
            else
            {
                while (true) {
                    bool statusUpdate = usb_connection.ConnectionStatus();
                    if (!statusUpdate)
                    {
                        usb_connection.Connected = statusUpdate;
                        OnPropertyChanged("ConnectionText");
                        OnPropertyChanged("ConnectionStatus");
                        break;
                    }
                    await Task.Delay(500);
                }
            }
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            if (!connectionStatus) return;
            RefreshButtonEnabled = false;
            OnPropertyChanged("RefreshButtonEnabled");
            ADC_Registers.ItemsSource = await Task.Run(refreshADCRegisters);
            DAC_Registers.ItemsSource = await Task.Run(refreshDACRegisters);
            SEL_Registers.ItemsSource = await Task.Run(refreshSELRegisters);
            ReadProgress.Dispatcher.Invoke(new Action(() => ReadProgress.Value = 0));
            RefreshButtonEnabled = true;
            OnPropertyChanged("RefreshButtonEnabled");
        }

        private void AutoRefresh_Unchecked(object sender, RoutedEventArgs e)
        {
            RefreshButtonEnabled = true;
            OnPropertyChanged("RefreshButtonEnabled");
            OnPropertyChanged("RefreshButtonVisibility");
        }

        private async void AutoRefresh_Checked(object sender, RoutedEventArgs e)
        {
            RefreshButtonEnabled = false;
            OnPropertyChanged("RefreshButtonEnabled");
            OnPropertyChanged("RefreshButtonVisibility");
            await AutoRefreshRegisters();
        }

        private async void WriteButton_Click(object sender, RoutedEventArgs e)
        {
            WriteButton.IsEnabled = false;
            int reg;
            int val;
            string regText = RegisterBox.Text.ToLower();
            string valText = ValueBox.Text.ToLower();
            if (regText.StartsWith("0x"))
            {
                regText = Convert.ToInt32(regText, 16).ToString();
            }
            if (regText.StartsWith("0b"))
            {
                regText = Convert.ToInt32(regText.Substring(2), 2).ToString();
            }
            if (valText.StartsWith("0x"))
            {
                valText = Convert.ToInt32(valText, 16).ToString();
            }
            if (valText.StartsWith("0b"))
            {
                valText = Convert.ToInt32(valText.Substring(2), 2).ToString();
            }
            if (!int.TryParse(regText, out reg))
                Console.WriteLine("Invalid Text");
            if (!int.TryParse(valText, out val))
                Console.WriteLine("Invalid Text");
            int errorVal = await Task.Run(() => usb_connection.SendWriteCommand((ushort)reg, (ushort)val));
            WriteButton.IsEnabled = true;
        }

        private void Compile_Click(object sender, RoutedEventArgs e)
        {
            OutputBox.Document.Blocks.Clear();
            RunButton.IsEnabled = false;
            CompileButton.IsEnabled = false;
            TextRange textRange = new TextRange(CodeBox.Document.ContentStart, CodeBox.Document.ContentEnd);
            string[] program = textRange.Text.Split(new string[] { "\r\n" }, StringSplitOptions.None);
            for (int i = 0; i < program.Length; i++)
            {
                string line = program[i];
                int idx = line.IndexOf("//");
                if (idx == -1) continue;
                program[i] = line.Substring(0, idx);
            }
            try
            {
                boardScriptingLanguage = new BoardScriptingLanguage(usb_connection, outputAdapter, program);
                RunButton.IsEnabled = true;
                outputAdapter.PrintOutput("Compile Success");
            }
            catch (BoardScriptingLanguage.ProgramException programException)
            {
                outputAdapter.PrintLineOutput("Compile Failed");
                outputAdapter.PrintLineOutput(programException.Message);
                outputAdapter.PrintLineOutput("Line #" + programException.LineError);
            }
            /*
            catch (Exception ex)
            {
                //This will snag if there is a a poorly coded part on my end
                outputAdapter.PrintLineOutput("Program Failed");
            }*/
            CompileButton.IsEnabled = true;


        }
        public void EnteredPassword(string entered_password)
		{
            if (password.Equals(entered_password))
            {
                if (passwordWindow != null)
                    passwordWindow.Close();
                superuserAccess = true;
                OnPropertyChanged("SuperuserAccess");
            }
            else
            {
                string messageBoxText = "Entered Invalid Password.";
                string caption = "Wrong Password!";
                MessageBoxButton button = MessageBoxButton.OK;
                MessageBoxImage icon = MessageBoxImage.Error;
                MessageBox.Show(messageBoxText, caption, button, icon);
            }
        }

        private void CodeBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (updating) return;
            updating = true;
            TextPointer textPointer = CodeBox.Document.ContentStart;
            TextPointer startComment = FindWordFromPosition(textPointer, commentSyntax);

            while (startComment != null)
            {
                TextRange text = new TextRange(startComment, CodeBox.Document.ContentEnd);
                if (text == null) break;
                string line = text.Text;
                string substring = line.Substring(0, line.IndexOf("\r\n"));
                TextPointer endComment = startComment.GetPositionAtOffset(substring.Length);
                TextRange comment = new TextRange(startComment, endComment);
                comment.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Green);
                TextRange colorReverse = new TextRange(endComment, CodeBox.Document.ContentEnd);
                colorReverse.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Black);
                startComment = FindWordFromPosition(endComment, commentSyntax);
            }
            updating = false;
        }

        TextPointer FindWordFromPosition(TextPointer position, string word)
        {
            while (position != null)
            {
                if (position.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text)
                {
                    string textRun = position.GetTextInRun(LogicalDirection.Forward);

                    // Find the starting index of substring that matches "word".
                    int indexInRun = textRun.IndexOf(word);
                    if (indexInRun >= 0)
                    {
                        TextPointer start = position.GetPositionAtOffset(indexInRun);
                        return start;
                    }
                }

                position = position.GetNextContextPosition(LogicalDirection.Forward);
            }

            // position will be null if "word" is not found.
            return null;
        }

        private void RunButton_Click(object sender, RoutedEventArgs e)
        {
            OutputBox.Document.Blocks.Clear();
            boardScriptingLanguage.Run();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //Not Implemented Yet
        }
        private void RefreshItemSources()
		{
            //Goes through each list of registers, refreshs the values
            var adc_regs = ADC_Registers.ItemsSource;
            if (adc_regs == null) return;
            List<Registers.ADCRegister> reformatedList = new List<Registers.ADCRegister>();
            foreach (var reg in adc_regs)
            {
                Registers.ADCRegister castRegister = (Registers.ADCRegister)reg;
                castRegister.Mode = BaseMode;
                reformatedList.Add(castRegister);
            }
            ADC_Registers.ItemsSource = reformatedList;
            var dac_regs = DAC_Registers.ItemsSource;
            if (dac_regs == null) return;
            List<Registers.DACRegister> reformatedList2 = new List<Registers.DACRegister>();
            foreach (var reg in dac_regs)
            {
                Registers.DACRegister castRegister = (Registers.DACRegister)reg;
                castRegister.Mode = BaseMode;
                reformatedList2.Add(castRegister);
            }
            DAC_Registers.ItemsSource = reformatedList2;
            var sel_regs = SEL_Registers.ItemsSource;
            if (sel_regs == null) return;
            List<Registers.SELRegister> reformatedList3 = new List<Registers.SELRegister>();
            foreach (var reg in sel_regs)
            {
                Registers.SELRegister castRegister = (Registers.SELRegister)reg;
                castRegister.Mode = BaseMode;
                reformatedList3.Add(castRegister);
            }
            SEL_Registers.ItemsSource = reformatedList3;
        }
		private void DecimalButton_Checked(object sender, RoutedEventArgs e)
		{
            BaseMode = DEC_MODE;
            RefreshItemSources();

        }
        private void BinaryButton_Checked(object sender, RoutedEventArgs e)
        {
            BaseMode = BIN_MODE;
            RefreshItemSources();

        }

        private void HexButton_Checked(object sender, RoutedEventArgs e)
        {
            BaseMode = HEX_MODE;
            RefreshItemSources();

        }

		private void PasswordCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
            if (!connectionStatus) return;
            if (superuserAccess)
            {
                superuserAccess = false;
                OnPropertyChanged("SuperuserAccess");
            }
            passwordWindow = new SuperuserMode(this);
            passwordWindow.ShowDialog();
        }

		private void LoadDefaultFirmware(object sender, RoutedEventArgs e)
		{
            #if DEBUG
            string curDir = Directory.GetCurrentDirectory();
            string baseDir = Directory.GetParent(Directory.GetParent(curDir).FullName).FullName;
            try
            {
                System.Diagnostics.Process.Start(curDir + "\\FirmwareInstall.bat", HID_Connection.DefaultVIDString() + " " + HID_Connection.DefaultUninstalledPIDString() + " \"" + curDir + curFirmwareName + "\"");
            }
			catch
			{

			}
            
            #else
            #endif
        }

        private void LoadCustomFirmware(object sender, RoutedEventArgs e)
        {
            #if DEBUG
            string curDir = Directory.GetCurrentDirectory();
            string baseDir = Directory.GetParent(Directory.GetParent(curDir).FullName).FullName;
            string filePath = string.Empty;

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = "c:\\";
            openFileDialog.Filter = "bin files (*.bin)|*.bin|All files (*.*)|*.*";

			bool? result = openFileDialog.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                // Open document
                string bin_filename = openFileDialog.FileName;
                System.Diagnostics.Process.Start(curDir+"\\FirmwareInstall.bat", HID_Connection.DefaultVIDString() + " " + HID_Connection.DefaultUninstalledPIDString() + " \"" + bin_filename + "\"");
            }
            #else
            #endif
        }

		private async void AutotuneMUXLeft(object sender, RoutedEventArgs e)
		{
            if (curAutotuneTest != 0) return;
            curAutotuneTest = 1;
            MUX_LeftGreen.Fill = grayBrush;
            MUX_LeftRed.Fill = grayBrush;
            usb_connection.SetProtectedMode(true);
            usb_connection.SendAutotuneCommand(0x01);
            MUX_LeftOrange.Fill = orangeBrush;
            int status = usb_connection.ReadAutotuneStatus();
            while(status < 5 && !abortFlag)
			{
                await Task.Delay(500);
                status = usb_connection.ReadAutotuneStatus();
			}
            MUX_LeftOrange.Fill = grayBrush;
			if (abortFlag || status < 7)
			{
                MUX_LeftRed.Fill = redBrush;
                abortFlag = false;
            }
			else
			{
                MUX_LeftGreen.Fill = greenBrush;
			}
            usb_connection.SetProtectedMode(false);
            curAutotuneTest = 0;
        }

        private async void AutotuneMUXRight(object sender, RoutedEventArgs e)
        {
            if (curAutotuneTest != 0) return;
            curAutotuneTest = 2;
            MUX_RightGreen.Fill = grayBrush;
            MUX_RightRed.Fill = grayBrush;
            usb_connection.SetProtectedMode(true);
            usb_connection.SendAutotuneCommand(0x02);
            MUX_RightOrange.Fill = orangeBrush;
            int status = usb_connection.ReadAutotuneStatus();
            while (status < 5 && !abortFlag)
            {
                await Task.Delay(500);
                status = usb_connection.ReadAutotuneStatus();
            }
            MUX_RightOrange.Fill = grayBrush;
            if (abortFlag || status < 7)
            {
                MUX_RightRed.Fill = redBrush;
                abortFlag = false;
            }
            else
            {
                MUX_RightGreen.Fill = greenBrush;
            }
            usb_connection.SetProtectedMode(false);
            curAutotuneTest = 0;
        }

        private async void AutotuneDMUXLeft(object sender, RoutedEventArgs e)
        {
            if (curAutotuneTest != 0) return;
            curAutotuneTest = 3;
            DMUX_LeftGreen.Fill = grayBrush;
            DMUX_LeftRed.Fill = grayBrush;
            usb_connection.SetProtectedMode(true);
            usb_connection.SendAutotuneCommand(0x03);
            DMUX_LeftOrange.Fill = orangeBrush;
            int status = usb_connection.ReadAutotuneStatus();
            while (status < 5 && !abortFlag)
            {
                await Task.Delay(500);
                status = usb_connection.ReadAutotuneStatus();
            }
            DMUX_LeftOrange.Fill = grayBrush;
            if (abortFlag || status < 7)
            {
                DMUX_LeftRed.Fill = redBrush;
                abortFlag = false;
            }
            else
            {
                DMUX_LeftGreen.Fill = greenBrush;
            }
            usb_connection.SetProtectedMode(false);
            curAutotuneTest = 0;
        }

        private async void AutotuneDMUXRight(object sender, RoutedEventArgs e)
        {
            if (curAutotuneTest != 0) return;
            curAutotuneTest = 4;
            DMUX_RightGreen.Fill = grayBrush;
            DMUX_RightRed.Fill = grayBrush;
            usb_connection.SetProtectedMode(true);
            usb_connection.SendAutotuneCommand(0x04);
            DMUX_RightOrange.Fill = orangeBrush;
            int status = usb_connection.ReadAutotuneStatus();
            while (status < 5 && !abortFlag)
            {
                await Task.Delay(500);
                status = usb_connection.ReadAutotuneStatus();
            }
            DMUX_RightOrange.Fill = grayBrush;
            if (abortFlag || status < 7)
            {
                DMUX_RightRed.Fill = redBrush;
                abortFlag = false;
            }
            else
            {
                DMUX_RightGreen.Fill = greenBrush;
            }
            usb_connection.SetProtectedMode(false);
            curAutotuneTest = 0;
        }

        private void Abort(object sender, RoutedEventArgs e)
		{
            abortFlag = true;
            usb_connection.SendAutotuneCommand(0xAA55);
		}
	}
	public abstract class BSLOutputBridge
	{
        public abstract void PrintOutput(string value);
        public abstract void PrintLineOutput(string value);

    }
    public class BSLRichTextBoxOutputBridge:BSLOutputBridge{
        private RichTextBox OutputBox;
        public BSLRichTextBoxOutputBridge(RichTextBox textBox)
		{
            OutputBox = textBox;
		}
        public override void PrintOutput(string value)
        {
            OutputBox.AppendText(value);
        }

        public override void PrintLineOutput(string value)
        {
            OutputBox.AppendText(value);
            OutputBox.AppendText(Environment.NewLine);
        }

    }

}
