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
#region Receive Message
		public bool BufferEmpty()
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
				while((DateTime.Now - dtStart).TotalMilliseconds < timeOut)
				{
					if (false == BufferEmpty())
					{
						canObj = ReadFrame();
						if (canObj.DataLen > 0)
						{
							return true;
						}
					}
					Thread.Sleep(5);
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
				while ((DateTime.Now - dtStart).TotalMilliseconds < timeOut)
				{
					if (false == BufferEmpty())
					{
						Thread.Sleep(5);//wait for 5ms
						canObj = ReadFrame();
						if (canObj.data != null && canObj.ID == canID)
						{
							return true;
						}
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
		public bool ReceiveMessages(out List<CAN_OBJ> DataList,
								int timeOut)//total timeout. if frames are available, the actual time may exceed timeOut
		{
			DataList = new List<CAN_OBJ>();
			CAN_OBJ objFrame = new CAN_OBJ();
		
			try
			{
				DateTime dtStart = DateTime.Now;
				while (true)
				{
					if (false == BufferEmpty()) //unread frame in instrument
					{
						objFrame = ReadFrame();
						if (objFrame.DataLen > 0)
						{
							DataList.Add(objFrame);
						}
					}
					else if ((DateTime.Now - dtStart).TotalMilliseconds > timeOut)
					{
						break;
					}
					else
					{
						Thread.Sleep(Setting.MaxInterval);
						if (true == BufferEmpty()) //unread frame in instrument
						{
							break;//time exceeds the max interval, don't wait.
						}
					}
				} 
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
				while (true)
				{
					if (false == BufferEmpty()) //unread frame in instrument
					{
						objFrame = ReadFrame();
						if (objFrame.DataLen > 0 && objFrame.ID == canID)
						{
							DataList.Add(objFrame);
						}
					}
					else if ((DateTime.Now - dtStart).TotalMilliseconds > timeOut)
					{
						break;
					}
					else
					{
						Thread.Sleep(Setting.MaxInterval);
						if (true == BufferEmpty()) //unread frame in instrument
						{
							break;//time exceeds the max interval, don't wait.
						}
					}
				}
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
		
			if (false == BufferEmpty())
			{
				uint uiLen = 1;
				try
				{
					if (ECANDLL.Receive(Setting.DeviceType, Setting.DeviceID, Setting.Channel, out frame, uiLen, 1) != ECANStatus.STATUS_OK)
					{
						string strErrInfo = ReadError();
						throw new Exception(string.Format("Failed at CAN receive: {0}", strErrInfo));
					}
				}
				catch (Exception ex)
				{
					if (true == ex.Message.StartsWith("Failed at can receive:"))
					{
						throw new Exception(ex.Message);
					}
					throw new Exception(string.Format("Failure happeded at receive method: {0}", ex.Message));
				}
			}
		
			return frame;
		}
        #endregion
    }
}
