﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Bson;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

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
                throw new Exception(string.Format("OpenDevice:{0}", ex.Message));
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
				throw new Exception(string.Format("Failed at close can device @ID:{0}. {1}", Setting.DeviceID, ex.Message));
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
				throw new Exception(string.Format("Failed at GetReceiveNum with message: {0}", ex.Message));
			}
		}
		
		public bool ReceiveBytes(out List<CAN_OBJ> DataList)
		{
			DataList = ReadMessages();
			return true;
		}

        private List<CAN_OBJ> ReadMessages()
        {
        	List<CAN_OBJ> listResp = new List<CAN_OBJ>();
            CAN_OBJ mMsg = new CAN_OBJ();

             do
            {
                uint mLen = 1;
                if (!((ECANDLL.Receive(1, 0, 0, out mMsg, mLen, 1) == ECANStatus.STATUS_OK) & (mLen > 0)))
                {
                    break;
                }
				else
				{
					listResp.Add(mMsg);
				}
			}
            while (false == BufferEmpty());

            return listResp;
        }
		#endregion

        #region Send Message
        //Send data to BUS
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
				if(true == ex.Message.StartsWith("failed at can transmit with data:"))
				{
					throw new Exception(string.Format("{0}", ex.Message));
				}
				else
				{
					throw new Exception(string.Format("Failed at SendBytes with message: {0}", ex.Message));
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
                objMessage[0] = canOBJ;
                objMessage[0].data = message;
                objMessage[1] = objMessage[0];

                uLen = 1;
                iSizeOfObj = System.Runtime.InteropServices.Marshal.SizeOf(objMessage[0]);
                if (ECANDLL.Transmit(Setting.DeviceType, Setting.DeviceID, Setting.Channel, objMessage, (ushort)uLen) != ECANStatus.STATUS_OK)
                {
                    //TODO:get last error message
                    throw new Exception(string.Format("failed at can transmit with data: {0}", message.ToString()));
                }
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Failed at send message: {0}", message));
            }

            return true;
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
