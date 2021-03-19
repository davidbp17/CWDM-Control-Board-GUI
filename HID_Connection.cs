using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HidLibrary;
namespace CWDM_Control_Board_GUI
{
    class HID_Connection
    {
        protected int vid;
        protected int pid;
        protected const int default_vid = 0x1FC9;
        protected const int default_pid = 0x8248;
        protected const int default_uninstalled_pid = 0x0130;
        protected bool protected_mode;
        protected enum MessageID: byte {
            AUTOTUNE_ID = 0x04,
            AUTOTUNE_STATUS_ID = 0x05,
            WRITE_MSG_ID = 0x04,
            READ_MSG_ID = 0x01,
            READ_INPUT_MSG_ID = 0x03,
            WRITE_MSG_LEN = 0x06,
            READ_MSG_LEN = 0x04
        }
        public enum ErrorMessage: int
        {
            WRITE_COMM_ERROR = -1,
            READ_COMM_ERROR = -2,
            READ_MISMATCH = -3,
            CONNECTION_ERROR = -4,
            THREAD_CLOSED_ERROR = -5,
            PROTECTED_MODE_ERROR = -6,
            NO_DATA_ERROR = -7,
            WAIT_ERROR = -8
        }
        protected static HidDevice _device;
        public bool Connected = false;
        protected static Mutex mutex;

        private int indexOf(ushort[] arr, ushort val)
		{
            for(int i = 0; i < arr.Length;++i)
			{
                if (arr[i] == val)
                    return i;
			}
            return -1;
		}

        public HID_Connection()
        {
            //Default Constructor with VID and PID values
            vid = default_vid;
            pid = default_pid;
            protected_mode = false;
        }

        public HID_Connection(int vid,int pid)
        {
            //If Device VID or PID changes
            this.vid = vid;
            this.pid = pid;
            protected_mode = false;
        }
        public static int DefaultVID()
		{
            return default_vid;
        }
        public static int DefaultPID()
        {
            return default_pid;
        }

        public static string DefaultVIDString()
        {
            return "0x" + default_vid.ToString("X4");
        }
        public static string DefaultPIDString()
        {
            return "0x" + default_pid.ToString("X4"); 
        }
        public static string DefaultUninstalledPIDString()
        {
            return "0x" + default_uninstalled_pid.ToString("X4");//Default of NXP Devices
        }
        public void ConnectDevice()
        {
            if (Connected)
                return;
            _device = HidDevices.Enumerate(vid, pid).FirstOrDefault();
            if (_device == null)
            {
                Console.WriteLine("No Device!");
                Connected = false;
                return;
            }
            _device.OpenDevice();
            bool opened = _device.IsConnected;
            mutex = new Mutex();
            Connected = true;
        }

        public void SetProtectedMode(bool mode)
		{
            protected_mode = mode;
		}

        public bool ConnectionStatus()
		{
            if (_device == null)
                return false;
            return _device.IsConnected;
 		}

        public void DisconnectDevice()
        {
            mutex.Close();
            _device.CloseDevice();

            Connected = false;
        }

        public byte[] AutotuneDataArray(ushort test)
        {
            byte[] newData = new byte[64];
            newData[1] = (byte)MessageID.AUTOTUNE_ID;
            newData[2] = (byte)MessageID.READ_MSG_LEN; 
            newData[3] = (byte)(test);
            newData[4] = (byte)(test >> 8);
            return newData;
        }

        public byte[] AutotuneStatusArray()
        {
            byte[] newData = new byte[64];
            newData[1] = (byte)MessageID.AUTOTUNE_STATUS_ID;
            newData[2] = (byte)MessageID.READ_MSG_LEN; //Leave it as Read Message Length
            return newData;
        }
        public int SendAutotuneCommand(ushort test, int timeout = 100)
        {
            try
            {
                mutex.WaitOne();
            }
            catch (ObjectDisposedException)
            {
                return (int) ErrorMessage.THREAD_CLOSED_ERROR;
            }
            if (protected_mode)
            {
                mutex.ReleaseMutex();
                return (int) ErrorMessage.PROTECTED_MODE_ERROR;
            }
            byte[] writeData = AutotuneDataArray(test);
            bool writeSuccess = _device.Write(writeData, timeout);
            mutex.ReleaseMutex();
            if (!writeSuccess) return (int) ErrorMessage.WRITE_COMM_ERROR;
            return 0;


        }
        public int ReadAutotuneStatus(int timeout = 100)
		{
            try
            {
                mutex.WaitOne();
            }
            catch (ObjectDisposedException)
            {
                return (int)ErrorMessage.THREAD_CLOSED_ERROR;
            }
            byte[] writeData = AutotuneStatusArray();
            bool writeSuccess = _device.Write(writeData);
            if (!writeSuccess) return (int)ErrorMessage.WRITE_COMM_ERROR;
            byte[] readArray = _device.Read(timeout).Data;
            int status = 0;
            if (readArray[0] == 0)
                status = readArray[1];
            else
                status = readArray[0];
            mutex.ReleaseMutex();
            
            return status;
        }
        public int SendWriteCommand(ushort register, ushort val,int timeout = 100)
        {
            if (protected_mode) return (int)ErrorMessage.PROTECTED_MODE_ERROR;
            try
            {
                mutex.WaitOne();
            }
            catch (ObjectDisposedException)
            {
                return (int) ErrorMessage.THREAD_CLOSED_ERROR;
            }
            if (!Connected)
            {
                mutex.ReleaseMutex();
                return (int)ErrorMessage.CONNECTION_ERROR;
            }
            byte[] writeData = WriteDataArray(register, val);
            bool writeSuccess = _device.Write(writeData, timeout);

            mutex.ReleaseMutex();
            if (!writeSuccess)
            {
                return (int)ErrorMessage.WRITE_COMM_ERROR;

            }
            
            return 0;
        }



        public int SendReadCommand(ushort register, int timeout = 100)
        {
            (bool readStatus, ushort regRead, ushort value) = (false, 0, 0);
            bool writeError = false;
            if (protected_mode) 
                return (int)ErrorMessage.PROTECTED_MODE_ERROR;
            if (!Connected)
            {
                return (int)ErrorMessage.CONNECTION_ERROR;
            }
            try
            {
                mutex.WaitOne();
                byte[] readCommandData = ReadDataArray(register);
                bool writeSuccess = _device.Write(readCommandData, timeout);
                if (writeSuccess)
                {
                    byte[] readArray = _device.Read(timeout).Data;
                    (readStatus, regRead, value) = ReadValue(readArray);
                }
                else
                    writeError = true;
                
            }
            catch (ObjectDisposedException)
            {
                return (int)ErrorMessage.THREAD_CLOSED_ERROR;
            }
			finally
			{
                try
                {
                    mutex.ReleaseMutex();
                }
				catch (ObjectDisposedException)
                {
					unchecked
                    {
                        value = (ushort)ErrorMessage.THREAD_CLOSED_ERROR;
                    }
                }
            }
			if (writeError)
			{
                return (int)ErrorMessage.WRITE_COMM_ERROR;
            }
            if (!readStatus)
			{
                return (int)ErrorMessage.READ_COMM_ERROR;
			}
            if (regRead != register)
            {
                return (int)ErrorMessage.READ_MISMATCH;
            }
            return value;
        }
        //Method for sending multiple reads at once
        public int[] SendReadCommands(ushort[] registers, int timeout = 100)
        {
            if(protected_mode) 
                return new int[] { (int)ErrorMessage.PROTECTED_MODE_ERROR };
            if (!Connected)
                return new int[] { (int)ErrorMessage.CONNECTION_ERROR };
            bool writeError = false;
            int len = registers.Length<=10 ? registers.Length : 10;
            int[] values = new int[len];
            try
            {
                mutex.WaitOne();
                byte[] readCommandData = ReadDataArray(registers);
                bool writeSuccess = _device.Write(readCommandData, timeout);
                if (writeSuccess)
                {
                    byte[] readArray = _device.Read(timeout).Data;
                    (bool, ushort, ushort)[] dataValues = ReadValues(readArray, len);
                    for (int i = 0; i < dataValues.Length; i++)
                    {
                        (bool readStatus, ushort regRead, ushort value) = dataValues[i];
                        if (readStatus)
                        {
                            int idx = indexOf(registers, regRead);
                            if (idx == -1)
                            {
                                values[i] = (int)ErrorMessage.READ_MISMATCH;
                                continue;
                            }

                            values[idx] = value;
                        }

                    }
                }
                else
                    writeError = true;

            }
            catch (ObjectDisposedException)
            {
                return new int[] { (int)ErrorMessage.THREAD_CLOSED_ERROR };
            }
            finally
            {
                try
                {
                    mutex.ReleaseMutex();
                }
                catch (ObjectDisposedException)
                {
                    values = new int[] {(int) ErrorMessage.THREAD_CLOSED_ERROR };
                }
            }
            if (writeError)
            {
                return new int[] { (int)ErrorMessage.WRITE_COMM_ERROR };
            }
            return values;
        }

        public byte[] WriteDataArray(ushort register, ushort val)
        {
            byte[] writeData = new byte[64];
            writeData[1] = (byte)MessageID.WRITE_MSG_ID;//Should be 0x04
            writeData[2] = (byte)MessageID.WRITE_MSG_LEN; //Write Message Length
            writeData[3] = (byte)(register);//last 8 bits
            writeData[4] = (byte)(register >> 8);//first 8 bits
            writeData[5] = (byte)(val);//last 8 bits
            writeData[6] = (byte)(val >> 8);//first 8 bits
            return writeData;
        }

        public byte[] WriteDataArray(ushort[] register, ushort[] val)
        {
            if(register.Length != val.Length || register.Length > 0x0F)
			{
                throw new Exception("Invalid Input Arrays");
			}
            byte[] writeData = new byte[64];

            for (int i = 0; i < register.Length && 6*(i+1) < 64; i++)
            {
                writeData[1 + (i * 6)] = (byte)MessageID.WRITE_MSG_ID;//Should be 0x04
                writeData[2 + (i * 6)] = (byte)MessageID.WRITE_MSG_LEN; //Write Message Length
                writeData[3 + (i * 6)] = (byte)(register[i]);//last 8 bits
                writeData[4 + (i * 6)] = (byte)(register[i] >> 8);//first 8 bits
                writeData[5 + (i * 6)] = (byte)(val[i]);//last 8 bits
                writeData[6 + (i * 6)] = (byte)(val[i] >> 8);//first 8 bits
            }
            return writeData;
        }
        public byte[] ReadDataArray(ushort register)
        {
            byte[] newData = new byte[64];
            newData[0] = (byte)MessageID.READ_MSG_ID;
            newData[1] = (byte)MessageID.READ_MSG_LEN; //Read Message Length
            newData[2] = (byte)(register);
            newData[3] = (byte)(register >> 8);
            return newData;
        }

        public byte[] ReadDataArray(ushort[] register)
        {
            if (register.Length > 0x10)
            {
                throw new Exception("Invalid Input Arrays");
            }
            byte[] readData = new byte[64];
            for (int i = 0; i < register.Length && 4*(i+1) < 64; i++)
            {
                readData[1 + (i * 4)] = (byte)MessageID.READ_MSG_ID;//Should be 0x04
                readData[2 + (i * 4)] = (byte)MessageID.READ_MSG_LEN; //Read Message Length
                readData[3 + (i * 4)] = (byte)(register[i]);//last 8 bits
                readData[4 + (i * 4)] = (byte)(register[i] >> 8);//first 8 bits
            }
            return readData;
        }

        public (bool, ushort, ushort) ReadValue(byte[] readData)
        {
            if (readData[1] == (byte)MessageID.READ_INPUT_MSG_ID && readData[2] == (byte)MessageID.READ_MSG_LEN)
            {
                ushort startReg = readData[3];
                ushort endReg = readData[4];
                ushort reg = (ushort)(startReg + (ushort)(endReg << 8));
                ushort startVal = readData[5];
                ushort endVal = readData[6];
                ushort val = (ushort)(startVal + (ushort)(endVal << 8));
                return (true, reg, val);
            }
            return (false, 0, 0);
        }
        public (bool, ushort, ushort)[] ReadValues(byte[] readData,int valsExpected = 10)
        {
            (bool, ushort, ushort)[] readValues = new (bool, ushort, ushort)[valsExpected];
            for(int i = 0; i < valsExpected && 6*(i+1) < 64; i++)
			{
                if ((readData[1 + (i * 6)] == (byte)MessageID.READ_INPUT_MSG_ID && readData[2 + (i * 6)] == (byte)MessageID.WRITE_MSG_LEN))//||( readData[2 + (i * 6)] == WRITE_MSG_LEN && i == 1)
                {
                    ushort startReg = readData[3 + (i * 6)];
                    ushort endReg = readData[4 + (i * 6)];
                    ushort reg = (ushort)(startReg + (ushort)(endReg << 8));
                    ushort startVal = readData[5 + (i * 6)];
                    ushort endVal = readData[6 + (i * 6)];
                    ushort val = (ushort)(startVal + (ushort)(endVal << 8));
                    readValues[i] = (true, reg, val);
                }
                else
                {
                    readValues[i] = (false, 0, 0);
                }
			}
            return readValues;
        }
    }

    class HID_CommandStatus
	{
		public HID_CommandStatus(int error)
		{
            Error = error;
		}
        public HID_CommandStatus(byte[] data,int error)
        {
            Data = data;
            Error = error;
        }
        public HID_CommandStatus(HidDeviceData readData)
		{
            Data = readData.Data;
			switch (readData.Status)
			{
                case HidDeviceData.ReadStatus.NoDataRead:
                    Error = (int)HID_Connection.ErrorMessage.NO_DATA_ERROR;
                    break;
                case HidDeviceData.ReadStatus.NotConnected:
                    Error = (int)HID_Connection.ErrorMessage.CONNECTION_ERROR;
                    break;
                case HidDeviceData.ReadStatus.ReadError:
                    Error = (int)HID_Connection.ErrorMessage.READ_COMM_ERROR;
                    break;
                case HidDeviceData.ReadStatus.WaitFail:
                    Error = (int)HID_Connection.ErrorMessage.WAIT_ERROR;
                    break;
                case HidDeviceData.ReadStatus.WaitTimedOut:
                    Error = (int)HID_Connection.ErrorMessage.WAIT_ERROR;
                    break;
                default:
                    Error = 0;
                    break;

            }
		}
        public byte[] Data { get; }
        public int Error { get; }
    }


    class Async_HID_Connection : HID_Connection
    {
        public Async_HID_Connection():base()
		{
            
		}
        public Async_HID_Connection(int vid, int pid) : base(vid, pid)
		{

		}
        private new async Task<int> SendWriteCommand(ushort register, ushort value, int delay = 100)
		{
            byte[] writeData = WriteDataArray(register, value);
            Task<bool> writeTask = _device.WriteAsync(writeData);
            bool success = await writeTask;
            if (success) return 0;
            return (int)ErrorMessage.WRITE_COMM_ERROR;
        }
        private async Task<int> SendWriteCommands(ushort[] register, ushort[] value, int delay = 100)
        {
            byte[] writeData = WriteDataArray(register, value);
            Task<bool> writeTask = _device.WriteAsync(writeData);
            bool success = await writeTask;
            if (success) return 0;
            return (int)ErrorMessage.WRITE_COMM_ERROR;
        }
        private async Task<HID_CommandStatus> AsyncReadCommand(ushort register, int delay = 100)
        {
            byte[] writeData = ReadDataArray(register);
            Task<bool> writeTask = _device.WriteAsync(writeData);
            bool success = await writeTask;
            if (!success) return new HID_CommandStatus((int)ErrorMessage.WRITE_COMM_ERROR);
            HidDeviceData readData = await _device.ReadAsync();
            return new HID_CommandStatus(readData);
        }

        private async Task<HID_CommandStatus> AsyncReadCommand(ushort[] register, int delay = 100)
        {
            byte[] writeData = ReadDataArray(register);
            Task<bool> writeTask = _device.WriteAsync(writeData);
            bool success = await writeTask;
            if (!success) return new HID_CommandStatus((int)ErrorMessage.WRITE_COMM_ERROR);
            HidDeviceData readData = await _device.ReadAsync();
            return new HID_CommandStatus(readData);
        }

        public new async Task<int> SendReadCommand(ushort register, int delay = 100)
        {
            HID_CommandStatus commandStatus = await AsyncReadCommand(register, delay);
            if (commandStatus.Error != 0) return commandStatus.Error;
            (bool, ushort, ushort) convertedValue = ReadValue(commandStatus.Data);
            return convertedValue.Item2;

        }
        public new async Task<int[]> SendReadCommands(ushort[] registers, int delay = 100)
        {
            if (protected_mode) return new int[] { (int)ErrorMessage.PROTECTED_MODE_ERROR };
            int len = registers.Length <= 10 ? registers.Length : 10;
            int[] values = new int[] { (int)ErrorMessage.READ_COMM_ERROR };
            HID_CommandStatus commandStatus = await AsyncReadCommand(registers, delay);
            if (commandStatus.Error != 0) return new int[] { commandStatus.Error };
            (bool, ushort, ushort)[] dataValues = ReadValues(commandStatus.Data);
            for (int i = 0; i < dataValues.Length; i++)
            {
                (bool readStatus, ushort regRead, ushort value) = dataValues[i];
                if (readStatus)
                {
                    int idx = Array.IndexOf(registers, regRead);
                    if (idx == -1)
                        continue;
                    values[idx] = value;
                }

            }
            return values;
        }

    }
}
