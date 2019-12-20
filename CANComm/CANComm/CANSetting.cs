using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Nile.Instruments.CAN
{
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
        public UInt16 MaxInterval { get; set; }

        public bool SwapBitOrder { get; set; }
        public bool SwapByteOrder { get; set; }
        public CANSetting(UInt16 deviceType, UInt16 deviceID, UInt16 channel, UInt16 accCode, UInt32 accMask, byte filter, byte mode, string baudRate, bool swapBitOrder, bool swapByteOrder)
        {
            DeviceType = deviceType;
            DeviceID = deviceID;
            Channel = channel;
            AccCode = accCode;
            AccMask = accMask;
            Filter = filter;
            Mode = mode;
            SwapBitOrder = swapBitOrder;
            SwapByteOrder = swapByteOrder;
            string pattern = @"\d+";
            Regex reg = new Regex(pattern);
            bool match = reg.IsMatch(baudRate);
            int BaudRate = -1;
            if (true == match)
            {
                MatchCollection mc = reg.Matches(baudRate);
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
                    throw new Exception(string.Format("Wrong baud rate value {0} from setting", baudRate));
            }
        }
        /// <summary>
        /// Initial an instance from preloaded settings
        /// </summary>
        /// <param name="SettingList">Preloaded settings</param>
        public CANSetting(Dictionary<string, List<object>> SettingDictionary)
        {
            if (true == SettingDictionary.ContainsKey("DeviceID"))
            {
                DeviceID = (UInt16)SettingDictionary["DeviceID"][0];
            }
            else
            {
                throw new Exception(string.Format("DeviceID is missing"));
            }
            //UInt16 AccCode
            if (true == SettingDictionary.ContainsKey("AccCode"))
            {
                AccCode = (UInt16)SettingDictionary["AccCode"][0];
            }
            else
            {
                throw new Exception(string.Format("AccCode is missing"));
            }
            //long AccMask
            if (true == SettingDictionary.ContainsKey("AccMask"))
            {
                AccMask = (UInt32)SettingDictionary["AccMask"][0];
            }
            else
            {
                throw new Exception(string.Format("AccMask is missing"));
            }
            //byte Filter
            if (true == SettingDictionary.ContainsKey("Filter"))
            {
                Filter = (byte)SettingDictionary["Filter"][0];
            }
            else
            {
                throw new Exception(string.Format("Filter is missing"));
            }
            //UInt16 Mode
            if (true == SettingDictionary.ContainsKey("Mode"))
            {
                Mode = (byte)SettingDictionary["Mode"][0];
            }
            else
            {
                throw new Exception(string.Format("Mode is missing"));
            }
            //int DeviceType
            if (true == SettingDictionary.ContainsKey("DeviceType"))
            {
                DeviceType = (UInt16)SettingDictionary["DeviceType"][0];
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
            if (true == SettingDictionary.ContainsKey("Channel"))
            {
                Channel = (UInt16)SettingDictionary["Channel"][0];
            }
            else
            {
                throw new Exception(string.Format("Channel is missing"));
            }
            //UInt16 MaxInterval
            if (true == SettingDictionary.ContainsKey("MaxInterval"))
            {
                MaxInterval = (UInt16)SettingDictionary["MaxInterval"][0];
            }
            else
            {
                MaxInterval = 100;//unit in ms. default value
            }
            //bool SwapBitOrder
            if (true == SettingDictionary.ContainsKey("SwapBitOrder"))
            {
                SwapBitOrder = (bool)SettingDictionary["SwapBitOrder"][0];
            }
            else
            {
                SwapBitOrder = false;//default value
            }
            //bool SwapByteOrder
            if (true == SettingDictionary.ContainsKey("SwapByteOrder"))
            {
                SwapByteOrder = (bool)SettingDictionary["SwapByteOrder"][0];
            }
            else
            {
                SwapByteOrder = false;//default value
            }
            //UInt16 BaudRate
            if (true == SettingDictionary.ContainsKey("BaudRate"))
            {
                string strBaudRate = (string)SettingDictionary["BaudRate"][0];
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
        }
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
                //UInt16 MaxInterval
                if (true == joCAN.ContainsKey("MaxInterval"))
                {
                    MaxInterval = (UInt16)joCAN["MaxInterval"];
                }
                else
                {
                    MaxInterval = 100;//unit in ms. default value
                }
                //bool SwapBitOrder
                if (true == joCAN.ContainsKey("SwapBitOrder"))
                {
                    SwapBitOrder = (bool)joCAN["SwapBitOrder"];
                }
                else
                {
                    SwapBitOrder = false ;//default value
                }
                //bool SwapByteOrder
                if (true == joCAN.ContainsKey("SwapByteOrder"))
                {
                    SwapByteOrder = (bool)joCAN["SwapByteOrder"];
                }
                else
                {
                    SwapByteOrder = false;//default value
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
