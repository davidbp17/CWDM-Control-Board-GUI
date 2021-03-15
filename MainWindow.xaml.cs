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
        private (string, ushort)[] adc_registers = {
            ("ADC_MUX_A-1",0x1000),
            ("ADC_MUX_A-2",0x1001),
            ("ADC_MUX_A-3",0x1002),
            ("ADC_MUX_B-1",0x1003),
            ("ADC_MUX_B-2",0x1004),
            ("ADC_MUX_C-1",0x1005),
            ("ADC_MUX_C-2",0x1006),
            ("ADC_DMUX_A-1", 0x1020),
            ("ADC_DMUX_A-2", 0x1021),
            ("ADC_DMUX_A-3", 0x1022),
            ("ADC_DMUX_A1-1",0x1023),
            ("ADC_DMUX_A1-2",0x1024),
            ("ADC_DMUX_A1-3",0x1025),
            ("ADC_DMUX_A2-1",0x1026),
            ("ADC_DMUX_A2-2",0x1027),
            ("ADC_DMUX_A2-3",0x1028),
            ("ADC_DMUX_B-1", 0x1029),
            ("ADC_DMUX_B-2", 0x102a),
            ("ADC_DMUX_B1-1",0x102b),
            ("ADC_DMUX_B1-2",0x102c),
            ("ADC_DMUX_B2-1",0x102d),
            ("ADC_DMUX_B2-2",0x102e),
            ("ADC_DMUX_C-1", 0x102f),
            ("ADC_DMUX_C-2", 0x1030),
            ("ADC_DMUX_C1-1",0x1031),
            ("ADC_DMUX_C1-2",0x1032),
            ("ADC_DMUX_C2-1",0x1033),
            ("ADC_DMUX_C2-2",0x1034),
            ("ADC_MUX_TDIODE_1_2", 0x1040),
            ("ADC_MUX_TDIODE_3_4", 0x1041),
            ("ADC_DMUX_TDIODE_1_2",0x1042),
            ("ADC_DMUX_TDIODE_3_4",0x1043),
            ("ADC_RING_1",0x1050),
            ("ADC_RING_2",0x1051),
            ("ADC_RING_3",0x1052),
            ("ADC_RING_4",0x1053),
            ("ADC_RING_5",0x1054),
            ("ADC_RING_6",0x1055),
            ("ADC_RING_7",0x1056),
            ("ADC_RING_8",0x1057),
            ("ADC_PDIODE_1",0x1060),
            ("ADC_PDIODE_2",0x1061),
            ("ADC_PDIODE_3",0x1062),
            ("ADC_PDIODE_4",0x1063),
            ("ADC_PDIODE_5",0x1064),
            ("ADC_PDIODE_6",0x1065),
            ("ADC_PDIODE_7",0x1066),
            ("ADC_PDIODE_8",0x1067),
            ("ADC_TEST1",0x10f0),
            ("ADC_TEST2",0x10f1)};

        private (string, ushort, ushort, ushort)[] dac_registers =
        {
            ("DAC_MUX_A-1",0x1100,0x1200,0x1300),
            ("DAC_MUX_A-2",0x1101,0x1201,0x1301),
            ("DAC_MUX_A-3",0x1102,0x1202,0x1302),
            ("DAC_MUX_B-1",0x1103,0x1203,0x1303),
            ("DAC_MUX_B-2",0x1104,0x1204,0x1304),
            ("DAC_MUX_C-1",0x1105,0x1205,0x1305),
            ("DAC_MUX_C-2",0x1106,0x1206,0x1306),
            ("DAC_DMUX_A-1" ,0x1120,0x1220,0x1320),
            ("DAC_DMUX_A-2" ,0x1121,0x1221,0x1321),
            ("DAC_DMUX_A-3" ,0x1122,0x1222,0x1322),
            ("DAC_DMUX_A1-1",0x1123,0x1223,0x1323),
            ("DAC_DMUX_A1-2",0x1124,0x1224,0x1324),
            ("DAC_DMUX_A1-3",0x1125,0x1225,0x1325),
            ("DAC_DMUX_A2-1",0x1126,0x1226,0x1326),
            ("DAC_DMUX_A2-2",0x1127,0x1227,0x1327),
            ("DAC_DMUX_A2-3",0x1128,0x1228,0x1328),
            ("DAC_DMUX_B-1" ,0x1129,0x1229,0x1329),
            ("DAC_DMUX_B-2" ,0x112a,0x122a,0x132a),
            ("DAC_DMUX_B1-1",0x112b,0x122b,0x132b),
            ("DAC_DMUX_B1-2",0x112c,0x122c,0x132c),
            ("DAC_DMUX_B2-1",0x112d,0x122d,0x132d),
            ("DAC_DMUX_B2-2",0x112e,0x122e,0x132e),
            ("DAC_DMUX_C-1" ,0x112f,0x122f,0x132f),
            ("DAC_DMUX_C-2" ,0x1130,0x1230,0x1330),
            ("DAC_DMUX_C1-1",0x1131,0x1231,0x1331),
            ("DAC_DMUX_C1-2",0x1132,0x1232,0x1332),
            ("DAC_DMUX_C2-1",0x1133,0x1233,0x1333),
            ("DAC_DMUX_C2-2",0x1134,0x1234,0x1334),
            ("DAC_MUX_TDIODE_1_2" ,0x1140,0x1240,0x1340),
            ("DAC_MUX_TDIODE_3_4" ,0x1141,0x1241,0x1341),
            ("DAC_DMUX_TDIODE_1_2",0x1142,0x1242,0x1342),
            ("DAC_DMUX_TDIODE_3_4",0x1143,0x1243,0x1343),
            ("DAC_RING_1",0x1150,0x1250,0x1350),
            ("DAC_RING_2",0x1151,0x1251,0x1351),
            ("DAC_RING_3",0x1152,0x1252,0x1352),
            ("DAC_RING_4",0x1153,0x1253,0x1353),
            ("DAC_RING_5",0x1154,0x1254,0x1354),
            ("DAC_RING_6",0x1155,0x1255,0x1355),
            ("DAC_RING_7",0x1156,0x1256,0x1356),
            ("DAC_RING_8",0x1157,0x1257,0x1357),
            ("DAC_RING_1",0x1150,0x1250,0x1350),
            ("DAC_RING_2",0x1151,0x1251,0x1351),
            ("DAC_RING_3",0x1152,0x1252,0x1352),
            ("DAC_RING_4",0x1153,0x1253,0x1353),
            ("DAC_RING_5",0x1154,0x1254,0x1354),
            ("DAC_RING_6",0x1155,0x1255,0x1355),
            ("DAC_RING_7",0x1156,0x1256,0x1356),
            ("DAC_RING_8",0x1157,0x1257,0x1357),
            ("DAC_TEST1", 0x11f0,0x12f0,0x13f0),
            ("DAC_TEST2", 0x11f1,0x12f1,0x13f1)
        };

        private (string, ushort)[] sel_registers = {
            ("MUX_A-1_SEL",0x1400),
            ("MUX_A-2_SEL",0x1401),
            ("MUX_A-3_SEL",0x1402),
            ("MUX_B-1_SEL",0x1403),
            ("MUX_B-2_SEL",0x1404),
            ("MUX_C-1_SEL",0x1405),
            ("MUX_C-2_SEL",0x1406),
            ("DMUX_A-1_SEL",    0x1420),
            ("DMUX_A-2_SEL",    0x1421),
            ("DMUX_A-3_SEL",    0x1422),
            ("DMUX_A1-1_SEL",   0x1423),
            ("DMUX_A1-2_SEL",   0x1424),
            ("DMUX_A1-3_SEL",   0x1425),
            ("DMUX_A2-1_SEL",   0x1426),
            ("DMUX_A2-2_SEL",   0x1427),
            ("DMUX_A2-3_SEL",   0x1428),
            ("DMUX_B-1_SEL",    0x1429),
            ("DMUX_B-2_SEL",    0x142a),
            ("DMUX_B1-1_SEL",   0x142b),
            ("DMUX_B1-2_SEL",   0x142c),
            ("DMUX_B2-1_SEL",   0x142d),
            ("DMUX_B2-2_SEL",   0x142e),
            ("DMUX_C-1_SEL",    0x142f),
            ("DMUX_C-2_SEL",    0x1430),
            ("DMUX_C1-1_SEL",   0x1431),
            ("DMUX_C1-2_SEL",   0x1432),
            ("DMUX_C2-1_SEL",   0x1433),
            ("DMUX_C2-2_SEL",   0x1434),
            ("MUX_TDIODE_1_2_SEL",  0x1440),
            ("MUX_TDIODE_3_4_SEL",  0x1441),
            ("DMUX_TDIODE_1_2_SEL", 0x1442),
            ("DMUX_TDIODE_3_4_SEL", 0x1443),
            ("TEST_SEL",    0x14f0)
        };


        public class ADCRegister
        {

            //Mode 1 is Binary
            //Mode 2 is Hex
            //Mode 3+ is Decimal
            
            public int Mode { get; set; }
            public string Name { get; set; }
            
            public string RegisterString
			{
				get {
                    if (Mode == 1)
                    {
                        string bin = Convert.ToString(RegisterValue, 2);
                        string leadingZeros = "";
                        for (int i = bin.Length; i <= 15; i++)
                            leadingZeros += "0";
                        return "0b" + leadingZeros+bin;
                    }
                    else if (Mode == 2)
                        return "0x" + RegisterValue.ToString("X4");
                    else
                        return RegisterValue.ToString();
                }
			}
            public int RegisterValue { get; set; }
            public string ErrorText
            {
                get
                {
                    if (RegisterValue == -1)
                    {
                        return "WRITE_COMM_ERROR";
                    }
                    else if (RegisterValue == -2)
                    {
                        return "READ_COMM_ERROR";
                    }
                    else if (RegisterValue == -3)
                    {
                        return "READ_MISMATCH";
                    }
                    else if (RegisterValue == -4)
                    {
                        return "CONNECTION_ERROR";
                    }
                    else return "";
                }
            }
        }
        public class SELRegister
        {

            public int Mode { get; set; }
            public string Name { get; set; }

            public string RegisterString
            {
                get
                {
                    if (Mode == 1)
                    {
                        string bin = Convert.ToString(RegisterValue, 2);
                        string leadingZeros = "";
                        for (int i = bin.Length; i <= 15; i++)
                            leadingZeros += "0";
                        return "0b" + leadingZeros + bin;
                    }
                    else if (Mode == 2)
                        return "0x" + RegisterValue.ToString("X4");
                    else
                        return RegisterValue.ToString();
                }
            }
            public int RegisterValue { get; set; }
            public string ErrorText
            {
                get
                {
                    if (RegisterValue == -1)
                    {
                        return "WRITE_COMM_ERROR";
                    }
                    else if (RegisterValue == -2)
                    {
                        return "READ_COMM_ERROR";
                    }
                    else if (RegisterValue == -3)
                    {
                        return "READ_MISMATCH";
                    }
                    else if (RegisterValue == -4)
                    {
                        return "CONNECTION_ERROR";
                    }
                    else return "";
                }
            }
        }
        public class DACRegister
        {
            public int Mode { get; set; }
            public string Name { get; set; }
            public string OutputString
            {
                get
                {
                    if (Mode == 1)
                    {
                        string bin = Convert.ToString(OutputValue, 2);
                        string leadingZeros = "";
                        for (int i = bin.Length; i <= 15; i++)
                            leadingZeros += "0";
                        return "0b" + leadingZeros + bin;
                    }
                    else if (Mode == 2)
                        return "0x" + OutputValue.ToString("X4");
                    else
                        return OutputValue.ToString();
                }
            }

            public string OffsetString
            {
                get
                {
                    if (Mode == 1)
                    {
                        string bin = Convert.ToString(OffsetValue, 2);
                        string leadingZeros = "";
                        for (int i = bin.Length; i <= 15; i++)
                            leadingZeros += "0";
                        return "0b" + leadingZeros + bin;
                    }
                    else if (Mode == 2)
                        return "0x" + OffsetValue.ToString("X4");
                    else
                        return OffsetValue.ToString();
                }
            }
            public string GainString
            {
                get
                {
                    if (Mode == 1)
                    {
                        string bin = Convert.ToString(GainValue, 2);
                        string leadingZeros = "";
                        for (int i = bin.Length; i <= 15; i++)
                            leadingZeros += "0";
                        return "0b" + leadingZeros + bin;
                    }
                    else if (Mode == 2)
                        return "0x" + GainValue.ToString("X4");
                    else
                        return GainValue.ToString();
                }
            }
            public int OutputValue { get; set; }
            public int OffsetValue { get; set; }
            public int GainValue { get; set; }

            public string OutputErrorText
            {
                get
                {
                    if (OutputValue == -1)
                    {
                        return "WRITE_COMM_ERROR";
                    }
                    else if (OutputValue == -2)
                    {
                        return "READ_COMM_ERROR";
                    }
                    else if (OutputValue == -3)
                    {
                        return "READ_MISMATCH";
                    }
                    else if (OutputValue == -4)
                    {
                        return "CONNECTION_ERROR";
                    }
                    else return "";
                }
            }
            public string OffsetErrorText
            {
                get
                {
                    if (OffsetValue == -1)
                    {
                        return "WRITE_COMM_ERROR";
                    }
                    else if (OffsetValue == -2)
                    {
                        return "READ_COMM_ERROR";
                    }
                    else if (OffsetValue == -3)
                    {
                        return "READ_MISMATCH";
                    }
                    else if (OffsetValue == -4)
                    {
                        return "CONNECTION_ERROR";
                    }
                    else return "";
                }
            }
            public string GainErrorText
            {
                get
                {
                    if (GainValue == -1)
                    {
                        return "WRITE_COMM_ERROR";
                    }
                    else if (GainValue == -2)
                    {
                        return "READ_COMM_ERROR";
                    }
                    else if (GainValue == -3)
                    {
                        return "READ_MISMATCH";
                    }
                    else if (GainValue == -4)
                    {
                        return "CONNECTION_ERROR";
                    }
                    else return "";
                }
            }
        }
        public MainWindow()
        {
            InitializeComponent();
            usb_connection = new HID_Connection();
            DataContext = this;
            superuserAccess = true;
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
            ReadProgress.Maximum = adc_registers.Length + dac_registers.Length + sel_registers.Length;
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

        private List<ADCRegister> refreshADCRegisters()
        {
            List<ADCRegister> registers = new List<ADCRegister>();
            for (int i = 0; i < adc_registers.Length;)
            {
                int len = adc_registers.Length - i >= 10 ? 10 : adc_registers.Length - i;
                string[] names = new string[len];
                ushort[] regAddr = new ushort[len];
                for (int j = 0; j < len && i + j < adc_registers.Length; j++)
                {
                    (names[j], regAddr[j]) = adc_registers[i + j];
                }
                int[] values = usb_connection.SendReadCommands(regAddr);
                for (int j = 0; j < len; j++)
                {
                    if (values.Length == 1 && values[0] < 0)
                    {
                        registers.Add(new ADCRegister { Name = names[j], RegisterValue = values[0], Mode = BaseMode }); 
                    }
                    else
                    {
                        registers.Add(new ADCRegister { Name = names[j], RegisterValue = values[j], Mode = BaseMode });
                    }
                    ReadProgress.Dispatcher.Invoke(new Action(() => ReadProgress.Value += 1));
                }
                i += len;
            }
            return registers;
        }


        private List<DACRegister> refreshDACRegisters()
        {
            List<DACRegister> registers = new List<DACRegister>();
            for (int i = 0; i < dac_registers.Length; i++)
            {
                (string name, ushort output_addr, ushort offset_addr, ushort gain_addr) = dac_registers[i];
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
                registers.Add(new DACRegister { Name = name, OutputValue = output_val, OffsetValue = offset_val, GainValue = gain_val, Mode = BaseMode });
                ReadProgress.Dispatcher.Invoke(new Action(() => ReadProgress.Value += 1));
            }
            return registers;
        }


        private List<SELRegister> refreshSELRegisters()
        {
            List<SELRegister> registers = new List<SELRegister>();
            for (int i = 0; i < sel_registers.Length;)
            {
                int len = sel_registers.Length - i >= 10 ? 10 : sel_registers.Length - i;
                string[] names = new string[len];
                ushort[] regAddr = new ushort[len];
                for (int j = 0; j < len && i + j < sel_registers.Length; j++)
                {
                    (names[j], regAddr[j]) = sel_registers[i + j];
                }
                int[] values = usb_connection.SendReadCommands(regAddr);
                for (int j = 0; j < len; j++)
                {
                    if (values.Length == 1 && values[0] < 0)
                    {
                        registers.Add(new SELRegister { Name = names[j], RegisterValue = values[0], Mode = BaseMode });
                    }
                    else
                    {
                        registers.Add(new SELRegister { Name = names[j], RegisterValue = values[j], Mode = BaseMode });
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
            var adc_regs = ADC_Registers.ItemsSource;
            if (adc_regs == null) return;
            List<ADCRegister> reformatedList = new List<ADCRegister>();
            foreach (var reg in adc_regs)
            {
                ADCRegister castRegister = (ADCRegister)reg;
                castRegister.Mode = BaseMode;
                reformatedList.Add(castRegister);
            }
            ADC_Registers.ItemsSource = reformatedList;
            var dac_regs = DAC_Registers.ItemsSource;
            if (dac_regs == null) return;
            List<DACRegister> reformatedList2 = new List<DACRegister>();
            foreach (var reg in dac_regs)
            {
                DACRegister castRegister = (DACRegister)reg;
                castRegister.Mode = BaseMode;
                reformatedList2.Add(castRegister);
            }
            DAC_Registers.ItemsSource = reformatedList2;
            var sel_regs = SEL_Registers.ItemsSource;
            if (sel_regs == null) return;
            List<SELRegister> reformatedList3 = new List<SELRegister>();
            foreach (var reg in sel_regs)
            {
                SELRegister castRegister = (SELRegister)reg;
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
            System.Diagnostics.Process.Start(baseDir+"\\FirmwareInstall.bat", HID_Connection.DefaultVIDString()+" "+HID_Connection.DefaultUninstalledPIDString() + " \"" + baseDir+"\\cwdm_fw-20210115.bin\"");
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
                System.Diagnostics.Process.Start(baseDir + "\\FirmwareInstall.bat", HID_Connection.DefaultVIDString() + " " + HID_Connection.DefaultUninstalledPIDString() + " \"" + bin_filename + "\"");
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
            usb_connection.SendAutotuneCommand(0x01);
            MUX_LeftOrange.Fill = orangeBrush;
            int status = usb_connection.ReadAutotuneStatus();
            int count = 20;
            while(status < 5 && !abortFlag)
			{
                await Task.Delay(500);
                status = usb_connection.ReadAutotuneStatus();
                if (count-- == 0) break;
			}
            MUX_LeftOrange.Fill = grayBrush;
			if (abortFlag)
			{
                MUX_LeftRed.Fill = redBrush;
                abortFlag = false;
            }
			else
			{
                MUX_LeftGreen.Fill = greenBrush;
			}
            curAutotuneTest = 0;
        }

        private async void AutotuneMUXRight(object sender, RoutedEventArgs e)
        {
            if (curAutotuneTest != 0) return;
            curAutotuneTest = 2;
            MUX_RightGreen.Fill = grayBrush;
            MUX_RightRed.Fill = grayBrush;
            usb_connection.SendAutotuneCommand(0x02);
            MUX_RightOrange.Fill = orangeBrush;
            int status = usb_connection.ReadAutotuneStatus();
            int count = 20;
            while (status < 5 && !abortFlag)
            {
                await Task.Delay(500);
                status = usb_connection.ReadAutotuneStatus();
                if (count-- == 0) break;
            }
            MUX_RightOrange.Fill = grayBrush;
            if (abortFlag)
            {
                MUX_RightRed.Fill = redBrush;
                abortFlag = false;
            }
            else
            {
                MUX_RightGreen.Fill = greenBrush;
            }
            curAutotuneTest = 0;
        }

        private async void AutotuneDMUXLeft(object sender, RoutedEventArgs e)
        {
            if (curAutotuneTest != 0) return;
            curAutotuneTest = 3;
            DMUX_LeftGreen.Fill = grayBrush;
            DMUX_LeftRed.Fill = grayBrush;
            usb_connection.SendAutotuneCommand(0x03);
            DMUX_LeftOrange.Fill = orangeBrush;
            int status = usb_connection.ReadAutotuneStatus();
            int count = 20;
            while (status < 5 && !abortFlag)
            {
                await Task.Delay(500);
                status = usb_connection.ReadAutotuneStatus();
                if (count-- == 0) break;
            }
            DMUX_LeftOrange.Fill = grayBrush;
            if (abortFlag)
            {
                DMUX_LeftRed.Fill = redBrush;
                abortFlag = false;
            }
            else
            {
                DMUX_LeftGreen.Fill = greenBrush;
            }
            curAutotuneTest = 0;
        }

        private async void AutotuneDMUXRight(object sender, RoutedEventArgs e)
        {
            if (curAutotuneTest != 0) return;
            curAutotuneTest = 4;
            DMUX_RightGreen.Fill = grayBrush;
            DMUX_RightRed.Fill = grayBrush;
            usb_connection.SendAutotuneCommand(0x04);
            DMUX_RightOrange.Fill = orangeBrush;
            int status = usb_connection.ReadAutotuneStatus();
            int count = 20;
            while (status < 5 && !abortFlag)
            {
                await Task.Delay(500);
                status = usb_connection.ReadAutotuneStatus();
                if (count-- == 0) break;
            }
            DMUX_RightOrange.Fill = grayBrush;
            if (abortFlag)
            {
                DMUX_RightRed.Fill = redBrush;
                abortFlag = false;
            }
            else
            {
                DMUX_RightGreen.Fill = greenBrush;
            }
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
