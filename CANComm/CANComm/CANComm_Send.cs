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

            Console.WriteLine();
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
                lock (this)
                {
                    string strData = BitConverter.ToString(canOBJ.data).Replace("-", string.Empty);
                    //Console.WriteLine("SendFrame: {0:X} : {1:X}", canOBJ.ID, strData);
                    if (ECANDLL.Transmit(Setting.DeviceType, Setting.DeviceID, Setting.Channel, objMessage, (ushort)uLen) != ECANStatus.STATUS_OK)
                    {
                        string strErrInfo = ReadError();
                        throw new Exception(string.Format("Failed at CAN transmit: {0}", strErrInfo));
                    }
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
    }
}
