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
    	public bool Connected { get; private set; }
        public CANSetting Setting;
        public Thread PeriodicMessageThread = null;
        public Thread ReceiveThread = null;
        private List<string[]> PeriodicCommands = null;
        private List<CAN_OBJ> listReceivedFrame = null;
        public bool EnablePeriodicMessage { get; set; }
        public bool EnableReceive { get; set; }
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

        public void ThreadReceive(object obj)
        {
            string str = obj as string;
            if (listReceivedFrame == null)
            {
                listReceivedFrame = new List<CAN_OBJ>();
            }

            while (EnableReceive)
            {
                try
                {
                    //ReceiveSingleMessage(out canObj, 5);
                    CAN_OBJ canObj = ReadFrame();
                    if (canObj.DataLen > 0)
                    {
                        listReceivedFrame.Add(canObj);
                        //debug purpose. To be deleted later
                        Console.WriteLine("ReceiveThread:,[{0:X8}],[{1}]", canObj.ID, BitConverter.ToString(canObj.data).Replace("-", string.Empty));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[Err][ThreadReceive]:{0}", ex.Message);
                }
            }
            if (false == EnableReceive)
            {
                ReceiveThread.Abort();
                if (ReceiveThread.ThreadState != ThreadState.Aborted)
                {
                    Thread.Sleep(100);
                }
            }
        }
            public void ThreadPeriodicMessagePara(object obj)
        {
            string str = obj as string;
            if (PeriodicCommands == null)
            {
                Assembly assm = Assembly.GetExecutingAssembly();
                string strAlllines = (string)Resource.ResourceManager.GetObject("PeriodicSequence");
                PeriodicCommands = LoadCommandList(strAlllines);
            }
            while (true == EnablePeriodicMessage)
            {
                foreach (string[] command in PeriodicCommands)
                {
                    SendMessage(command[0], command[1]);
                    Thread.Sleep(25);
                }

                if (false == EnablePeriodicMessage)
                {
                    PeriodicMessageThread.Abort();
                    if (PeriodicMessageThread.ThreadState != ThreadState.Aborted)
                    {
                        Thread.Sleep(100);
                    }
                }
            }
        }

        /// <summary>
        /// load simple commands file. Example of command line: 12b,1122334455667788 or: 12b,11 22 33 44 55 66 77 88
        /// </summary>
        /// <param name="commandFile">simple command list file</param>
        /// <returns></returns>
        private static List<string[]> LoadCommandList(string commandFile)
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
        public bool OpenDevice(UInt16 devID, UInt16 channel, out string messageOfFalse, bool startPeriodicMessage, bool enableReceive)
        {
            if (false == OpenDevice(devID, channel, out messageOfFalse))
            {
                return false;
            }
            //TODO:
            //Enable background thread: sending periodic message.

            Thread.Sleep(1000);
            if (true == startPeriodicMessage)
            {
                EnablePeriodicMessage = startPeriodicMessage;
                PeriodicMessageThread = new Thread(new ParameterizedThreadStart(ThreadPeriodicMessagePara));
                PeriodicMessageThread.IsBackground = true;
                PeriodicMessageThread.Start("hello");
           }
            if (true == enableReceive)
            {
                EnableReceive = enableReceive;

                ReceiveThread = new Thread(new ParameterizedThreadStart(ThreadReceive));
                ReceiveThread.IsBackground = false;
                ReceiveThread.Start("World");
            }
            return true;
        }

        public bool OpenDevice(UInt16 devID, UInt16 channel, out string messageOfFalse)
        {
            try
            {
            	if(true == Connected)
            	{
					ECANDLL.CloseDevice(Setting.DeviceType, devID);
					Connected = false;
				}
            	Setting.DeviceID = devID;

				//open device
				if(ECANDLL.OpenDevice(Setting.DeviceType, Setting.DeviceID, 0) != CAN.ECANStatus.STATUS_OK)
				{
					messageOfFalse = string.Format("Failed at open device.");
					return false;
				}
				//Init can channel with config
				Setting.Channel = channel;
				if(ECANDLL.InitCAN(Setting.DeviceType, Setting.DeviceID, Setting.Channel, ref Setting.InitCfg) != CAN.ECANStatus.STATUS_OK)
				{
					messageOfFalse = string.Format("Failed at initialize device.");
					return false;
				}
				//start can channel
				if(ECANDLL.StartCAN(Setting.DeviceType, Setting.DeviceID, Setting.Channel) != CAN.ECANStatus.STATUS_OK)
				{
					messageOfFalse = string.Format("Failed at initialize device.");
					return false;
				}
            }
            catch (Exception ex)
            {
				string strErrInfo = ReadError();
				throw new Exception(string.Format("Failed at open device method: {0}", strErrInfo));
            }

			Connected = true;
            messageOfFalse = string.Empty;
            return true;
        }

        public bool OpenDevice(out string messageOfFalse, bool startPeriodicMessage)
        {
            if (false == OpenDevice(out messageOfFalse))
            {
                return false;
            }
            //TODO:
            //Enable background thread: sending periodic message.

            return true;
        }
        public bool OpenDevice(out string messageOfFalse)
        {
            try
            {
            	if(true == Connected)
            	{
					ECANDLL.CloseDevice(Setting.DeviceType, Setting.DeviceID);
					Connected = false;
				}

				//open device
				if(ECANDLL.OpenDevice(Setting.DeviceType, Setting.DeviceID, 0) != CAN.ECANStatus.STATUS_OK)
				{
					messageOfFalse = string.Format("Failed at open device.");
					return false;
				}
				//Init can channel with config
				if(ECANDLL.InitCAN(Setting.DeviceType, Setting.DeviceID, Setting.Channel, ref Setting.InitCfg) != CAN.ECANStatus.STATUS_OK)
				{
					messageOfFalse = string.Format("Failed at initialize device.");
					return false;
				}
				//start can channel
				if(ECANDLL.StartCAN(Setting.DeviceType, Setting.DeviceID, Setting.Channel) != CAN.ECANStatus.STATUS_OK)
				{
					messageOfFalse = string.Format("Failed at initialize device.");
					return false;
				}
            }
            catch (Exception ex)
            {
				string strErrInfo = ReadError();
				throw new Exception(string.Format("Failed at open device method: {0}", strErrInfo));
            }

			Connected = true;
            messageOfFalse = string.Empty;
            return true;
        }
		public bool CloseDevice()
        {
			try
			{
				ECANDLL.CloseDevice(Setting.DeviceType, Setting.DeviceID);
				Connected = false;
			}
			catch(Exception ex)
			{
				Connected = false;
				string strErrInfo = ReadError();
				throw new Exception(string.Format("{0}. Failed at close can device @ID:{1}. {2}", strErrInfo, Setting.DeviceID, ex.Message));
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
        #endregion
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
