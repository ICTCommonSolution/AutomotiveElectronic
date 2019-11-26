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

namespace CAN
{
    public class CANComm
    {
    	public bool Connected { get; private set; }
        public CANSetting Setting;
        public CANComm(string settingFile)
    	{
            Setting = new CANSetting(settingFile);
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

		#region Receive Message
		private bool BufferEmpty()
		{
			try
			{
				ulong ulUnread = (ulong)ECANDLL.GetReceiveNum(Setting.DeviceType, Setting.DeviceID, Setting.Channel);
				if(ulUnread>0)
				{
					return false;//Not empty. unreceived frame exist(s)
				}
				else
				{
					return true;//empty
				}
			}
			catch(Exception ex)
			{
				string strErrInfo = ReadError();
				throw new Exception(string.Format("Failed at CAN check frames in buffer: ", strErrInfo));
			}
		}

		public bool ReceiveSingleMessage(out CAN_OBJ canObj, int timeOut)
		{
            canObj = new CAN_OBJ();

			try
			{
                DateTime dtStart = DateTime.Now;
                while(false == BufferEmpty() && (DateTime.Now - dtStart).TotalMilliseconds < timeOut)
                {
	                Thread.Sleep(50);

                    canObj = ReadFrame();
                } //unread frame in instrument
			}
			catch(Exception ex)
			{
				if(true == ex.Message.StartsWith("Failed at CAN receive: "))
				{
					throw new Exception(ex.Message);
				}
				else
				{
					throw new Exception(string.Format("Failed at receive single message method with message: {0}", ex.Message));
				}
			}
            return true;
		}

        /// <summary>
        /// Get a frame from specified CAN ID and the frame from other ID will be ignored. The listening will stopped if no frame come in within 20ms
        /// </summary>
        /// <param name="canObj">return the completed frame</param>
        /// <param name="canID">the desired CAN ID</param>
        /// <param name="timeOut">The time out of receiving frames</param>
        /// <returns>read frame successfully or not</returns>
        public bool ReceiveSingleMessage(out CAN_OBJ canObj, uint canID, int timeOut)
        {
            canObj = new CAN_OBJ();

            try
            {
                DateTime dtStart = DateTime.Now;
                while (false == BufferEmpty() && (DateTime.Now - dtStart).TotalMilliseconds < timeOut)
                {
					Thread.Sleep(50);//wait for 20ms
                    canObj = ReadFrame();
                    if (canObj.data != null && canObj.ID == canID)
                    {
                    	return true;
                    }
                } //check again if unread frame from bus
            }
            catch (Exception ex)
            {
                if (true == ex.Message.StartsWith("Failed at CAN receive: "))
                {
                    throw new Exception(ex.Message);
                }
                else
                {
                    throw new Exception(string.Format("Failed at receive message method with message: {0}", ex.Message));
                }
            }
            return false;
        }
		public bool ReceiveMessages(out List<CAN_OBJ> DataList, int timeOut)
		{
            DataList = new List<CAN_OBJ>();
			CAN_OBJ objFrame = new CAN_OBJ();

			try
			{
                DateTime dtStart = DateTime.Now;
                do
                {
                    do
                    {
                        objFrame = ReadFrame();
                        if (objFrame.data != null)
                        {
                            DataList.Add(objFrame);
                        }
                    } while (false == BufferEmpty());

                    Thread.Sleep(50);
                } while (false == BufferEmpty() && (DateTime.Now - dtStart).TotalMilliseconds < timeOut); //unread frame in instrument
			}
			catch(Exception ex)
			{
				if(true == ex.Message.StartsWith("Failed at CAN receive: "))
				{
					throw new Exception(ex.Message);
				}
				else
				{
					throw new Exception(string.Format("Failed at receive message method with message: {0}", ex.Message));
				}
			}
            return true;
		}

        /// <summary>
        /// Get data from specified CAN ID and the frames from other ID will be ignored. The listening will stopped if no frame come in within 20ms
        /// </summary>
        /// <param name="DataList">return the completed frames</param>
        /// <param name="canID">the desired CAN ID</param>
        /// <param name="timeOut">The time out of receiving frames</param>
        /// <returns>read frames successfully or not</returns>
        public bool ReceiveMessages(out List<CAN_OBJ> DataList, uint canID, int timeOut)
        {
            DataList = new List<CAN_OBJ>();
            CAN_OBJ objFrame = new CAN_OBJ();

            try
            {
                DateTime dtStart = DateTime.Now;
                do
                {
                    do
                    {
                        objFrame = ReadFrame();
                        if (objFrame.data != null && objFrame.ID == canID)
                        {
                            DataList.Add(objFrame);
                        }
                    } while (false == BufferEmpty());//untill no new frame come in.
                    Thread.Sleep(50);//wait for 20ms
                } while (false == BufferEmpty() && (DateTime.Now - dtStart).TotalMilliseconds < timeOut); //check again if unread frame from bus
            }
            catch (Exception ex)
            {
                if (true == ex.Message.StartsWith("Failed at CAN receive: "))
                {
                    throw new Exception(ex.Message);
                }
                else
                {
                    throw new Exception(string.Format("Failed at receive message method with message: {0}", ex.Message));
                }
            }
            return true;
        }

        /// <summary>
        /// Basicaly function to read a frame.
        /// </summary>
        /// <returns>return the completed frame from CAN bus </returns>
        private CAN_OBJ ReadFrame()
        {
            CAN_OBJ frame = new CAN_OBJ();

            uint uiLen = 1;
			try
			{
				if(ECANDLL.Receive(Setting.DeviceType, Setting.DeviceID, Setting.Channel, out frame, uiLen, 1) != ECANStatus.STATUS_OK)
				{
	                string strErrInfo = ReadError();
	                throw new Exception(string.Format("Failed at CAN receive: {0}", strErrInfo));
				}
            }
            catch (Exception ex)
            {
            	if(true == ex.Message.StartsWith("Failed at can receive:"))
            	{
					throw new Exception(ex.Message);
				}
                throw new Exception(string.Format("Failure happeded at receive method: {0}", ex.Message));
            }

            return frame;
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

        #endregion

        #region Send Message
        //Send data to BUS
        /// <summary>
        /// Send command (data) with specified ID
        /// </summary>
        /// <param name="ID">ID string in hex format without space</param>
        /// <param name="command">Hex string of command/data, space between bytes</param>
        /// <returns>sent successfully or not</returns>
        public bool SendMessage(string ID, string command)
        {
            int iLen = 0;
            string[] strBytes = null;
            byte[] data = null;
            CAN_OBJ canObj = new CAN_OBJ();

            //convert hex string to byte[]
            if (command.IndexOf(@" ") > 0)
            {
                iLen = (command.Length + 1) / 3; //space between bytes. e.g. "FE 00"
                strBytes = command.Split(' ');
                data = new byte[iLen];
            }
            else
            {
                iLen = command.Length / 2;//no space in hex string. e.g."FE00"
                strBytes = new string[iLen];
                for (int i = 0, index = 0; i + 1 < command.Length; i += 2, index++)
                {
                    strBytes[index] = command.Substring(i, 2);
                }
                data = new byte[iLen];
            }
            for (int index = 0; index < iLen; index++)
            {
                data[index] = Convert.ToByte(Int32.Parse(strBytes[index], System.Globalization.NumberStyles.HexNumber));//convert the HEX number string to a character and then to ASIC
            }
            UInt32 uiID = Convert.ToUInt32(ID, 16);

            if (uiID > 0x7FF)
            {
                canObj.ExternFlag = 0x1;
            }
            else
            {
                canObj.ExternFlag = 0x0;
            }
            canObj.ID = uiID;
            canObj.RemoteFlag = 0;
            canObj.Reserved = null;
            canObj.SendType = 0;
            canObj.TimeFlag = 0x0;
            canObj.TimeStamp = 0;
            canObj.data = new byte[data.Length];
            data.CopyTo(canObj.data, 0);
            canObj.DataLen = (byte)data.Length;

            return SendFrame(canObj, data);
        }

        public bool SendMessage(CAN_OBJ canObj)
        {
            byte[] byteData = new byte[canObj.DataLen];
            canObj.data.CopyTo(byteData, 0);

            return SendFrame(canObj, byteData);
        }
        public bool SendMessages(List<byte[]> DataList,
                            uint ID = 1,
        					uint TimeStamp = 0,
        					byte TimeFlag = 0x0,
        					byte SendType = 0x0,
        					byte RemoteFlag = 0x0,//not remote frame
        					byte ExternFlag = 0x0)//standard frame
        {
			byte[] byteData = new byte[8];
            CAN_OBJ canOBJ = new CAN_OBJ();
            bool bSendStatus = false;

			try
			{
				canOBJ.ExternFlag = ExternFlag;
				canOBJ.ID = ID;
				canOBJ.RemoteFlag = RemoteFlag;
				canOBJ.Reserved = null;
				canOBJ.SendType = SendType;
				canOBJ.TimeFlag = TimeFlag;
				canOBJ.TimeStamp = TimeStamp;

				foreach(byte[] byteCommand in DataList)
				{
					uint uLength = (uint)byteCommand.Length;

                    if (uLength > 8)
                    {
                        throw new Exception("The command length is large than 8.");
                    }
                    else{
                        //do nothing
                    }
					for (int i = 0; i < 8; i++)
					{
						if(i<uLength)
						{
							byteData[i] = byteCommand[i];
						}
						else
						{
							byteData[i] = 0x0;
						}
					}
                    canOBJ.DataLen = (byte)uLength;
                    bSendStatus = SendFrame(canOBJ, byteData);
                    if (bSendStatus == false)
                    {
                        return false;
                    }
				}
			}
			catch(Exception ex)
			{
				if(true == ex.Message.StartsWith("Failed at CAN transmit with data:"))
				{
					throw new Exception(string.Format("{0}", ex.Message));
				}
				else
				{
					throw new Exception(string.Format("Failed at SendMessage with message: {0}", ex.Message));
				}
			}
            return true;
        }

        //Send 8-byte to CAN BUS
        private bool SendFrame(CAN_OBJ canOBJ, byte[] message)
        {
        	CAN_OBJ[] objMessage = new CAN_OBJ[2];
            UInt16 uLen = 0;
            int iSizeOfObj = 0;
            try
            {
                byte[] byteData = new byte[8];
                for (int i = 0; i < byteData.Length; i++)
                {
                    if (i < message.Length)
                    {
                        byteData[i] = message[i];
                    }
                    else
                    {
                        byteData[i] = 0x0;
                    }
                }
                objMessage[0] = canOBJ;
                //objMessage[0].data = message;
                objMessage[0].data = byteData;
                objMessage[1] = objMessage[0];

                uLen = 1;
                iSizeOfObj = System.Runtime.InteropServices.Marshal.SizeOf(objMessage[0]);
                if (ECANDLL.Transmit(Setting.DeviceType, Setting.DeviceID, Setting.Channel, objMessage, (ushort)uLen) != ECANStatus.STATUS_OK)
                {
                    string strErrInfo = ReadError();
                    throw new Exception(string.Format("Failed at CAN transmit: {0}", strErrInfo));
                }
            }
            catch (Exception ex)
            {
            	if(true == ex.Message.StartsWith("Failed at CAN transmit:"))
            	{
					throw new Exception(ex.Message);
				}
                throw new Exception(string.Format("Failure happeded at send method: {0}", message));
            }

            return true;
        }
        #endregion

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
    public class CANSetting
    {
    //below settings for open device
    	public UInt16 DeviceID { get; set; }
        public UInt16 AccCode { get; set; }
		public UInt32 AccMask { get; set; }
		public byte Filter { get; set; }
		public int BaudRate { get; set; }
		public byte Timing0 { get; private set;}
		public byte Timing1 { get; private set;}
        public byte Mode { get; set; }
		public UInt16 DeviceType { get; set; }
		public UInt16 Channel { get; set;}
        public INIT_CONFIG InitCfg;

        public CANSetting(string file) 
        {
            if(false == File.Exists(file))
            {
                throw new Exception(string.Format("{0} does not exist.", file));
            }
			LoadSetting(file);
        }
		#region Load setting for CAN communication from json file
		private void LoadSetting(string fileName)
		{
            StreamReader file;
            JsonTextReader reader;
			string strBaudRate = string.Empty;

            try
			{
			//Load json file for CAN init settings
				file = File.OpenText(fileName);
				reader = new JsonTextReader(file);
				JObject joSetting = (JObject)JToken.ReadFrom(reader);

			//UInt16 DeviceID in measure system
                JObject joCAN = (JObject)joSetting["CAN"];
				if(true == joCAN.ContainsKey("DeviceID"))
				{
					DeviceID = (UInt16)joCAN["DeviceID"];
				}
				else
				{
					throw new Exception(string.Format("DeviceID is missing"));
				}
			//UInt16 AccCode
				if(true == joCAN.ContainsKey("AccCode"))
				{
					AccCode = (UInt16)joCAN["AccCode"];
				}
				else
				{
					throw new Exception(string.Format("AccCode is missing"));
				}
			//long AccMask
				if(true == joCAN.ContainsKey("AccMask"))
				{
					AccMask = (UInt32)joCAN["AccMask"];
				}
				else
				{
					throw new Exception(string.Format("AccMask is missing"));
				}
			//byte Filter
				if(true == joCAN.ContainsKey("Filter"))
				{
					Filter = (byte)joCAN["Filter"];
				}
				else
				{
					throw new Exception(string.Format("Filter is missing"));
				}
			//UInt16 Mode
				if(true == joCAN.ContainsKey("Mode"))
				{
					Mode = (byte)joCAN["Mode"];
				}
				else
				{
					throw new Exception(string.Format("Mode is missing"));
				}
			//int DeviceType
			   if(true == joCAN.ContainsKey("DeviceType"))
			   {
                    DeviceType = (UInt16)joCAN["DeviceType"];
                    /*string strDeviceType = (string)joCAN["DeviceType"];
					if(true == strDeviceType.ToUpper().Equals("USBCAN I"))
					{
						DeviceType = 3;
					}
				   else if(strDeviceType.ToUpper().Equals("USBCAN II"))
				   {
					   DeviceType = 4;
				   }
				   else
				   {
						throw new Exception(string.Format("Unsupported device: {0}", strDeviceType));
				   }*/
                }
                else
                {
                    throw new Exception(string.Format("DeviceType is missing"));
                }
			//UInt16 Channel
                if(true == joCAN.ContainsKey("Channel"))
                {
                    Channel = (UInt16)joCAN["Channel"];
                }
                else
                {
                    throw new Exception(string.Format("Channel is missing"));
                }
                //UInt16 BaudRate
                if (true == joCAN.ContainsKey("BaudRate"))
                {
                    strBaudRate = (string)joCAN["BaudRate"];
					string pattern = @"\d+";
					Regex reg = new Regex(pattern);
					bool match = reg.IsMatch(strBaudRate);
					int BaudRate = -1;
					if (true == match)
					{
						MatchCollection mc = reg.Matches(strBaudRate);
						if (int.TryParse(mc[0].Value, out BaudRate))
						{
						}
					}
                    switch (BaudRate)
                    {
                        case 1000:
                            Timing0 = 0;
                            Timing1 = 0x14;
                            break;
                        case 800:
                            Timing0 = 0;
                            Timing1 = 0x16;
                            break;
                        case 666:
                            Timing0 = 0x80;
                            Timing1 = 0xb6;
                            break;
                        case 500:
                            Timing0 = 0;
                            Timing1 = 0x1c;
                            break;
                        case 400:
                            Timing0 = 0x80;
                            Timing1 = 0xfa;
                            break;
                        case 250:
                            Timing0 = 0x01;
                            Timing1 = 0x1c;
                            break;
                        case 200:
                            Timing0 = 0x81;
                            Timing1 = 0xfa;
                            break;
                        case 125:
                            Timing0 = 0x03;
                            Timing1 = 0x1c;
                            break;
                        case 100:
                            Timing0 = 0x04;
                            Timing1 = 0x1c;
                            break;
                        case 80:
                            Timing0 = 0x83;
                            Timing1 = 0xff;
                            break;
                        case 50:
                            Timing0 = 0x09;
                            Timing1 = 0x1c;
                            break;
                        default:
                            throw new Exception(string.Format("Wrong baud rate value {0} from setting", strBaudRate));
                    }
                }
                else
                {
                    throw new Exception(string.Format("BaudRate is missing"));
                }
                /*
                 //int DeviceID
                    if(true == joCAN.ContainsKey("DeviceID"))
                    {
                        DeviceID = (int)joCAN["DeviceID"];
                    }
                    else
                    {
                        throw new Exception(string.Format("DeviceID is missing"));
                    }*/

                //init_config
                InitCfg = new INIT_CONFIG();
                InitCfg.AccCode = AccCode;
                InitCfg.AccMask = AccMask;
                InitCfg.Filter = Filter;
                InitCfg.Timing0 = Timing0;
                InitCfg.Timing1 = Timing1;
                InitCfg.Mode = Mode;
                InitCfg.Reserved = 0;
			}
			catch(Exception ex)
			{
				throw new Exception(string.Format("Failed at parse CAN Settings from {0}", ex.Message));
			}
            file.Close();
            reader.Close();
        }
		#endregion
	}
}
