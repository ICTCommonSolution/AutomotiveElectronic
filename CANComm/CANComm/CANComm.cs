using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Newtonsoft.Json.Bson;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using System.Collections;
using System.Reflection;

namespace CAN
{
    public partial class CANComm
    {
        public int icount = 0;
        public bool Connected { get; private set; }
        public CANSetting Setting;
        public Thread PeriodicMessageThread = null;
        public Thread ReceiveThread = null;
        private List<string[]> PeriodicCommands = null;
        public List<CAN_OBJ> listReceivedFrame = null;
        //private List<CAN_OBJ> listReceivedFrame = null;
        public bool EnablePeriodicMessageThread { get; set; }
        public bool EnableReceiveThread { get; set; }
		public bool EnableClearBuffer{private get; set; }
        /// <summary>
        /// Initial instance with settings from file.
        /// </summary>
        /// <param name="settingFile">setting file name</param>
        public CANComm(string settingFile)
    	{
            Setting = new CANSetting(settingFile);
        }
        public CANComm(UInt16 deviceType, UInt16 deviceID, UInt16 channel, UInt16 accCode, UInt32 accMask, byte filter, byte mode, string baudRate)
        {
            Setting = new CANSetting(deviceType, deviceID, channel, accCode, accMask, filter, mode, baudRate);
        }

        public void InitReceiveThread(bool StartThread, string Para = "")
        {
            EnableReceiveThread = true;
            ReceiveThread = new Thread(new ParameterizedThreadStart(ThreadFunc_Receive));
            ReceiveThread.IsBackground = false;
            if (true == StartThread)
            {
                ReceiveThread.Start(Para);
            }
        }

        public void StartReceiveThread(string Para)
        {
            if (ReceiveThread == null)
            {
                InitReceiveThread(true, Para);
                return;
            }
            //reset enabler if thread is stopped
            EnableReceiveThread = true;
            if (ReceiveThread.ThreadState == ThreadState.Unstarted)
            {
                ReceiveThread.Start(Para);
                return;
            }

            if (ReceiveThread.ThreadState == ThreadState.WaitSleepJoin)
            {
                Console.WriteLine(string.Format("[StartReceiveThread]:Unsupported state:{0}", ReceiveThread.ThreadState));
                while (ReceiveThread.ThreadState == ThreadState.WaitSleepJoin)
                {
                    Thread.Sleep(1);
                }
                return;
            }

            InitReceiveThread(true, Para);
        }
        public void StopReceiveThread()
        {
            if (ReceiveThread == null)
            {
                throw new Exception(string.Format("Thread is not initialized."));
            }
            EnableReceiveThread = false;
        }

        public void InitPeriodicFrameThread(bool StartThread, string Para="")
        {
            EnablePeriodicMessageThread = true;
            PeriodicMessageThread = new Thread(new ParameterizedThreadStart(ThreadFunc_PeriodicFrame));
            PeriodicMessageThread.IsBackground = true;
            if (true == StartThread)
            {
                PeriodicMessageThread.Start(Para);
            }
        }

        public void StartPeroidicFrameThread(string Para)
        {
            if (PeriodicMessageThread == null)
            {
                InitPeriodicFrameThread(true, Para);
                return;
            }

			//reset enabler if thread is stopped
            EnablePeriodicMessageThread = true;
            if (PeriodicMessageThread.ThreadState == (ThreadState.Unstarted|ThreadState.Background))
            {
                PeriodicMessageThread.Start(Para);
                return;
            }


            if (PeriodicMessageThread.ThreadState == ThreadState.WaitSleepJoin)
            {
                Console.WriteLine(string.Format("[StartPeroidicFrameThread]:Unsupported state:{0}", PeriodicMessageThread.ThreadState));
                while (PeriodicMessageThread.ThreadState == ThreadState.WaitSleepJoin)
                {
                    Thread.Sleep(1);
                }
                return;
            }

            InitPeriodicFrameThread(true, Para);
        }

        public void StopPeroidicFrameThread()
        {
            if (PeriodicMessageThread == null)
            {
                throw new Exception(string.Format("Thread is not initialized."));
            }
            EnablePeriodicMessageThread = false;
        }

        public void ThreadFunc_Receive(object obj)
        {
            string str = obj as string;
            Console.WriteLine("[{0}] - [ThreadFunc_Receive] - Start", DateTime.Now.ToString("HH:mm:ss.ffff"));
            
            if (listReceivedFrame == null)
            {
                Console.WriteLine("[{0}] - [ThreadFunc_Receive] - init listReceivedFrame", DateTime.Now.ToString("HH:mm:ss.ffff"));
                listReceivedFrame = new List<CAN_OBJ>();
            }
            int iThreadFunc_ReceiveCount = 0;
            while (EnableReceiveThread)
            {
                try
                {
                    lock (listReceivedFrame)
                    {
                        if(false == BufferEmpty())
                        {
                            CAN_OBJ canObj = ReadFrame();
                            if (canObj.DataLen > 0)
                            {
                                listReceivedFrame.Add(canObj);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[Err][ThreadFunc_Receive]:{0}", ex.Message);
                }
                iThreadFunc_ReceiveCount++;
            }
        }
        public void ThreadFunc_PeriodicFrame(object obj)
        {
            string str = obj as string;
            Console.WriteLine("[{0}] - [ThreadFunc_PeriodicFrame] - start", DateTime.Now.ToString("HH:mm:ss.ffff"));
            if (PeriodicCommands == null)
            {
                Console.WriteLine("[{0}] - [ThreadFunc_PeriodicFrame] - command list is empty", DateTime.Now.ToString("HH:mm:ss.ffff"));
                Assembly assm = Assembly.GetExecutingAssembly();
                string strAlllines = (string)Resource.ResourceManager.GetObject("PeriodicSequence");
                PeriodicCommands = LoadCommandList(strAlllines);
            }

			try
			{
                int count = PeriodicCommands.Count;
                for (int i = 0; i < count && true == EnablePeriodicMessageThread; i++)
                {
                    string[] command = PeriodicCommands[i];
                    //Console.WriteLine("[{0}] - [SendStamp] - {1}", DateTime.Now.ToString("HH:mm:ss.ffff"), icount);
                    SendMessage(command[0], command[1]);
                    if (icount < 100)
                        Thread.Sleep(10);
                    else
                        Thread.Sleep(25);
                    icount++;
                    if (i == count-1)
                        i = -1;
                }
			}
			catch (Exception ex)
			{
				//do nothing
			}
        }

        /// <summary>
        /// load simple commands file. Example of command line: 12b,1122334455667788 or: 12b,11 22 33 44 55 66 77 88
        /// </summary>
        /// <param name="commandFile">simple command list file</param>
        /// <returns></returns>
        public static List<string[]> LoadCommandList(string commandFile)
        {
            List<string[]> listCommand = new List<string[]>();

            try
            {
                string[] lines = null;
                if (false == File.Exists(commandFile))
                {
                    char[] separator = { '\n', '\r' };
                    lines = commandFile.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                }
                else
                {
                    lines = File.ReadAllLines(commandFile);
                }
                foreach (string line in lines)
                {
                    string[] strSplitted = line.Split(',');
                    string[] strPara = new string[2];
                    strPara[0] = strSplitted[0].Trim();
                    strPara[1] = strSplitted[1].Trim();
                    listCommand.Add(strPara);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Error in load simple command file with message: {0}", ex.Message));
            }
            return listCommand;
        }

        public bool OpenDevice(UInt16 devID, UInt16 channel, out string messageOfFalse, int waitAfterOpen, bool startPeriodicMessage=true, bool enableReceive=true)
        {
            if (false == OpenDevice(devID, channel, out messageOfFalse))
            {
                return false;
            }

            //periodic background thread to enable DUT
            Thread.Sleep(waitAfterOpen);
            EnablePeriodicMessageThread = startPeriodicMessage;
            PeriodicMessageThread = new Thread(new ParameterizedThreadStart(ThreadFunc_PeriodicFrame));
            PeriodicMessageThread.IsBackground = true;
            if (true == startPeriodicMessage)
            {
                PeriodicMessageThread.Start("hello");
            }

            //Receiving thread
            EnableReceiveThread = enableReceive;
            ReceiveThread = new Thread(new ParameterizedThreadStart(ThreadFunc_Receive));
            ReceiveThread.IsBackground = false;
            if (true == enableReceive)
            {
                ReceiveThread.Start("World");
            }
            return true;
        }

        public bool OpenDevice(UInt16 devID, UInt16 channel, out string messageOfFalse)
        {
            try
            {
            	Setting.DeviceID = devID;
				Setting.Channel = channel;

				return OpenDevice(out messageOfFalse);
            }
            catch (Exception ex)
            {
				string strErrInfo = ReadError();
				throw new Exception(string.Format("Failed at open device with parameters: {0}", strErrInfo));
            }

			Connected = false;
            messageOfFalse = string.Empty;
            return false;
        }

        public bool OpenDevice(out string messageOfFalse,   int waitAfterOpen, bool startPeriodicMessage, bool enableReceive)
        {
            if (false == OpenDevice(out messageOfFalse))
            {
                return false;
            }
			Thread.Sleep(waitAfterOpen);
			if (true == startPeriodicMessage)
			{
                EnablePeriodicMessageThread = startPeriodicMessage;
				PeriodicMessageThread = new Thread(new ParameterizedThreadStart(ThreadFunc_PeriodicFrame));
				PeriodicMessageThread.IsBackground = true;
				PeriodicMessageThread.Start("hello");
			}
			if (true == enableReceive)
			{
                EnableReceiveThread = enableReceive;

				ReceiveThread = new Thread(new ParameterizedThreadStart(ThreadFunc_Receive));
				ReceiveThread.IsBackground = false;
				ReceiveThread.Start("World");
			}
			return true;
        }

        public bool OpenDevice(out string messageOfFalse)
        {
            try
            {
                Console.WriteLine("[{0}] - [OpenDevice] - Start", DateTime.Now.ToString("HH:mm:ss.ffff"));
                if (true == Connected)
            	{
                    Console.WriteLine("[{0}] - [OpenDevice] - device already connected. to close before test", DateTime.Now.ToString("HH:mm:ss.ffff"));
                    ECANDLL.CloseDevice(Setting.DeviceType, Setting.DeviceID);
					Connected = false;
				}

				//open device
				if(ECANDLL.OpenDevice(Setting.DeviceType, Setting.DeviceID, 0) != CAN.ECANStatus.STATUS_OK)
				{
                    Console.WriteLine("[{0}] - [OpenDevice] - failed to open device", DateTime.Now.ToString("HH:mm:ss.ffff"));
                    messageOfFalse = string.Format("Failed at open device.");
					return false;
				}
				//Init can channel with config
				if(ECANDLL.InitCAN(Setting.DeviceType, Setting.DeviceID, Setting.Channel, ref Setting.InitCfg) != CAN.ECANStatus.STATUS_OK)
				{
                    Console.WriteLine("[{0}] - [OpenDevice] - fail to init CAN", DateTime.Now.ToString("HH:mm:ss.ffff"));
                    messageOfFalse = string.Format("Failed at initialize device.");
					return false;
				}
				//start can channel
				if(ECANDLL.StartCAN(Setting.DeviceType, Setting.DeviceID, Setting.Channel) != CAN.ECANStatus.STATUS_OK)
				{
                    Console.WriteLine("[{0}] - [OpenDevice] - fail to start CAN", DateTime.Now.ToString("HH:mm:ss.ffff"));
                    messageOfFalse = string.Format("Failed at initialize device.");
					return false;
				}
            }
            catch (Exception ex)
            {
				string strErrInfo = ReadError();
				throw new Exception(string.Format("Failed at open device method: {0}", strErrInfo));
            }

            Console.WriteLine("[{0}] - [OpenDevice] - connected", DateTime.Now.ToString("HH:mm:ss.ffff"));
            Connected = true;
            messageOfFalse = string.Empty;
            if (ReceiveThread != null)
            {
                Console.WriteLine("[{0}] - [ThreadFunc_Receive-State] - {1}", DateTime.Now.ToString("HH:mm:ss.ffff"), ReceiveThread.ThreadState);
            }
            return true;
        }

		public bool CloseDevice()
        {
            if (ReceiveThread != null)
            {
                Console.WriteLine("[{0}] - [ThreadFunc_Receive-State] - {1}", DateTime.Now.ToString("HH:mm:ss.ffff"), ReceiveThread.ThreadState);
            }
            if (true == Connected)
			{
                Console.WriteLine("[{0}] - [CloseDevice] - Start", DateTime.Now.ToString("HH:mm:ss.ffff"));
                //Abort period message thread before close
                if (true == EnablePeriodicMessageThread)
				{
                    Console.WriteLine("[{0}] - [CloseDevice] - to abort PeriodicMessageThread", DateTime.Now.ToString("HH:mm:ss.ffff"));
                    PeriodicMessageThread.Abort();
					while(PeriodicMessageThread.ThreadState != ThreadState.Aborted)
					{
						Thread.Sleep(10);
					}
				}
				//Abort receiving thread before close
				if(true == EnableReceiveThread)
				{
                    Console.WriteLine("[{0}] - [CloseDevice] - to abort ReceiveThread", DateTime.Now.ToString("HH:mm:ss.ffff"));
                    ReceiveThread.Abort();
	                while(ReceiveThread.ThreadState != ThreadState.Aborted)
	                {
                        Console.WriteLine("[{0}] - [CloseDevice] - ReceiveThread is not aborted", DateTime.Now.ToString("HH:mm:ss.ffff"));
                        Thread.Sleep(10);
	                }
				}

				//Close device
				try
				{
                    Console.WriteLine("[{0}] - [CloseDevice] - to close device", DateTime.Now.ToString("HH:mm:ss.ffff"));
                    ECANDLL.CloseDevice(Setting.DeviceType, Setting.DeviceID);
					Connected = false;
				}
				catch(Exception ex)
				{
					Connected = false;
					string strErrInfo = ReadError();
					throw new Exception(string.Format("{0}. Failed at close can device @ID:{1}. {2}", strErrInfo, Setting.DeviceID, ex.Message));
				}
				Connected = false;
			}
			return true;
		}

        public ulong GetBitsFromFrames(CAN_OBJ[] arrOBJ, uint startBit, uint length)
        {
            int iLengthOfAll = 0;//unit in Bytes
            int index = 0;
            //get lenght of all data
            foreach (CAN_OBJ obj in arrOBJ)
            {
                iLengthOfAll += obj.DataLen;
            }
            byte[] byteAll = new byte[iLengthOfAll];

            //get all data in one array
            foreach (CAN_OBJ obj in arrOBJ)
            {

                obj.data.CopyTo(byteAll, index);
                index += obj.DataLen;
            }

            //get value
            uint uiMask = (uint)(Math.Pow(2.0, (double)length) - 1);
            int iMoveLen = sizeof(byte) * 8 * byteAll.Length - (int)(startBit + length - 1);//the length of left shift
            ulong ulInput = 0; //convert byte[] to ulong
            for(int i = 0; i < byteAll.Length; i++)
            {
                ulInput = ulInput + (uint)byteAll[i] << (8 * (byteAll.Length - i - 1));
            }
            ulong ulValue = (ulInput >> iMoveLen) & uiMask;

            return 0xFFFF;
        }

        public ulong GetBitsFromFrame(CAN_OBJ canOBJ, uint startBit, uint length)
        {
            byte[] byteData = new byte[canOBJ.DataLen];

            //get all data from frame
            canOBJ.data.CopyTo(byteData, 0);

            //get value
            uint uiMask = (uint)(Math.Pow(2.0, (double)length) - 1);
            int iMoveLen = sizeof(byte) * 8 * byteData.Length - (int)(startBit + length - 1);//the length of left shift
            ulong ulInput = 0; //convert byte[] to ulong
            for (int i = 0; i < byteData.Length; i++)
            {
                ulInput = ulInput + (uint)byteData[i] << (8 * (byteData.Length - i - 1));
            }
            ulong ulValue = (ulInput >> iMoveLen) & uiMask;

            return 0xFFFF;
        }

		#region Error handling
        private string ReadError()
        {

            CAN_ERR_INFO errInfo = new CAN_ERR_INFO();

            if (ECANDLL.ReadErrInfo(Setting.DeviceType, Setting.DeviceID, Setting.Channel, out errInfo) == ECANStatus.STATUS_OK)
            {
            	string strErrMessage = string.Empty;
				strErrMessage = string.Format("Error Code[0x{0:X4}]. Error Text: {0:X4} and  {0:X4}", errInfo.ErrCode, errInfo.Passive_ErrData[0], errInfo.Passive_ErrData[1]);
				return strErrMessage;
            }
            else
            {
				throw new Exception("Failed at get error message? Is can device connected?");
            }
        }
        #endregion

        #region ClearBuffer
        public bool ClearBuffer()
        {
            try
            {
                if (ECANDLL.ClearBuffer(Setting.DeviceType, Setting.DeviceID, Setting.Channel) == ECANStatus.STATUS_OK)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

		public bool ClearBuffer(bool ClearOnce)
		{
			if(true == ClearOnce)
			{
                try
                {
                    lock (listReceivedFrame)
                    {
                        if (listReceivedFrame.Count > 0)
                        //clear the list at the same time.
                        {
                            Console.WriteLine("[{0}] - [ClearBuffer] - to clear listReceivedFrame", DateTime.Now.ToString("HH:mm:ss.ffff"));
                            listReceivedFrame.Clear();
                        }
                    }
                    if (false == BufferEmpty())
                    {
                        Console.WriteLine("[{0}] - [ClearBuffer] - to clear CAN device buffer", DateTime.Now.ToString("HH:mm:ss.ffff"));
                        if (ECANDLL.ClearBuffer(Setting.DeviceType, Setting.DeviceID, Setting.Channel) == ECANStatus.STATUS_OK)
                        {
                            EnableClearBuffer = false;//reset global variable.
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    return false;
                }
			}
            return false;
        }
        #endregion

		public int GetBitValue(byte[] Data, int StartBit, int length)
        {
			uint uiMask = (uint)Math.Pow(2, length) - 1;

			byte[] value = new byte[8];
			Array.Reverse(Data); // otherwise you will have a1612240fff result
			Array.Copy(Data, value, 6);
			ulong ulData = BitConverter.ToUInt64(value, 0);
			Console.WriteLine("{0:x}", result);

			
		}
    }

    public enum CANBaudRate:UInt16
    {
        BaudRate50 = 0,//  = 50;
        BaudRate80 = 1,//  = 80;
        BaudRate100 = 2,// = 100;
        BaudRate125 = 3,// = 125;
        BaudRate200 = 4,// = 200;
        BaudRate250 = 5,// = 250;
        BaudRate400 = 6,// = 400;
        BaudRate500 = 7,// = 500;
        BaudRate666 = 8,// = 666;
        BaudRate800 = 9,// = 800;
        BaudRate1000 = 10, // 1000;
	}

/*	    public struct INIT_CONFIG
    {

        public uint AccCode;
        public uint AccMask;
        public uint Reserved;
        public byte Filter;
        public byte Timing0;
        public byte Timing1;
        public byte Mode;

  

    }*/
}
