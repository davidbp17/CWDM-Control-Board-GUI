using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using HidLibrary;
namespace CWDM_Control_Board_GUI
{
    class HID_Connection
    {
        private int vid;
        private int pid;
        const byte WRITE_MSG_ID = 0x04;
        const byte READ_MSG_ID = 0x01;
        const byte READ_INPUT_MSG_ID = 0x03;
        const byte WRITE_MSG_LEN = 0x06;
        const byte READ_MSG_LEN = 0x04;
        const int WRITE_COMM_ERROR = -1;
        const int READ_COMM_ERROR = -2;
        const int READ_MISMATCH = -3;
        const int CONNECTION_ERROR = -4;
        const int THREAD_CLOSED_ERROR = -5;
        private static HidDevice _device;
        public bool Connected = false;
        private static Mutex mutex;

        public HID_Connection()
        {
            //Default Constructor with VID and PID values
            vid = 0x1FC9;
            pid = 0x8248;
        }

        public HID_Connection(int vid,int pid)
        {
            //If Device VID or PID changes
            this.vid = vid;
            this.pid = pid;
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
            mutex = new Mutex();
            Connected = true;
        }

        public bool ConnectionStatus()
		{
            if (!Connected) return false;
            mutex.WaitOne();
			bool tmpStatus = _device.IsConnected;
            mutex.ReleaseMutex();
            return tmpStatus;
		}

        public void DisconnectDevice()
        {
            mutex.Close();
            _device.CloseDevice();

            Connected = false;
        }
        public int SendWriteCommand(ushort register, ushort val,int delay = 100)
        {
            try
            {
                mutex.WaitOne();
            }
            catch (ObjectDisposedException)
            {
                return THREAD_CLOSED_ERROR;
            }
            if (!Connected)
            {
                mutex.ReleaseMutex();
                return CONNECTION_ERROR;
            }
            byte[] writeData = WriteDataArray(register, val);
            bool writeSuccess = _device.Write(writeData, delay);
            if (!writeSuccess)
            {
                mutex.ReleaseMutex();
                return WRITE_COMM_ERROR;

            }
            byte[] readArray = _device.Read(delay).Data;
            try
            {
                mutex.ReleaseMutex();
            }
            catch (ObjectDisposedException)
            {

            }
            return 0;
        }

        private bool RawWrite(byte[] data,int delay = 0)
		{
            return _device.Write(data, delay);
        }
        private byte[] RawRead(int delay = 0)
		{
            return  _device.Read(delay).Data;
        }

        public int SendReadCommand(ushort register, int delay = 100)
        {
            try
            {
                mutex.WaitOne();
            }
            catch (ObjectDisposedException)
            {
                return THREAD_CLOSED_ERROR;
            }
            if (!Connected)
            {
                mutex.ReleaseMutex();
                return CONNECTION_ERROR;
            }
            byte[] readCommandData = ReadDataArray(register);
            bool writeSuccess = _device.Write(readCommandData, delay);
            if (!writeSuccess)
            {
                mutex.ReleaseMutex();
                return WRITE_COMM_ERROR;
            }
            byte[] readArray = _device.Read(delay).Data;
            (bool readStatus, ushort regRead, ushort value) = ReadValue(readArray);
            if (!readStatus)
			{
                mutex.ReleaseMutex();
                return READ_COMM_ERROR;
			}
            if (regRead != register)
            {
                mutex.ReleaseMutex();
                return READ_MISMATCH;

            }
            mutex.ReleaseMutex();
            return value;
        }

        public int[] SendReadCommands(ushort[] registers, int delay = 100)
        {
            int len = registers.Length<=10 ? registers.Length : 10;
            int[] values = new int[len];
            for(int i = 0; i < values.Length; i++)
			{
                values[i] = READ_COMM_ERROR;
			}
            try
            {
                mutex.WaitOne();
            }
            catch (ObjectDisposedException)
            {
                return new int[] { THREAD_CLOSED_ERROR };
            }
            if (!Connected)
            {
                mutex.ReleaseMutex();
                return new int[] { CONNECTION_ERROR };
            }
            byte[] readCommandData = ReadDataArray(registers);
            bool writeSuccess = _device.Write(readCommandData, delay);
            if (!writeSuccess)
            {
                mutex.ReleaseMutex();
                return new int[] { WRITE_COMM_ERROR };
            }
            byte[] readArray = _device.Read(delay).Data;
            (bool,ushort,ushort)[] dataValues = ReadValues(readArray,len);
            for (int i = 0; i < dataValues.Length;i++)
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
            mutex.ReleaseMutex();
            return values;
        }

        public byte[] WriteDataArray(ushort register, ushort val)
        {
            byte[] writeData = new byte[64];
            writeData[1] = WRITE_MSG_ID;//Should be 0x04
            writeData[2] = WRITE_MSG_LEN; //Write Message Length
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
                writeData[1 + (i * 6)] = WRITE_MSG_ID;//Should be 0x04
                writeData[2 + (i * 6)] =  WRITE_MSG_LEN; //Write Message Length
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
            newData[1] = READ_MSG_ID;
            newData[2] = READ_MSG_LEN; //Read Message Length
            newData[3] = (byte)(register);
            newData[4] = (byte)(register >> 8);
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
                readData[1 + (i * 4)] = READ_MSG_ID;//Should be 0x04
                readData[2 + (i * 4)] = READ_MSG_LEN; //Read Message Length
                readData[3 + (i * 4)] = (byte)(register[i]);//last 8 bits
                readData[4 + (i * 4)] = (byte)(register[i] >> 8);//first 8 bits
            }
            return readData;
        }

        public (bool, ushort, ushort) ReadValue(byte[] readData)
        {
            if (readData[1] == READ_INPUT_MSG_ID && readData[2] == READ_MSG_LEN)
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
                if ((readData[1 + (i * 6)] == READ_INPUT_MSG_ID && readData[2 + (i * 6)] == WRITE_MSG_LEN)||( readData[2 + (i * 6)] == WRITE_MSG_LEN && i == 1))
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
}
