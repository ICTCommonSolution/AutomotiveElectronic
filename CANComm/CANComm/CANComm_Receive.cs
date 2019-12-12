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

		/// <summary>
		/// Basicaly function to read a frame.
		/// </summary>
		/// <returns>return the completed frame from CAN bus </returns>
		private CAN_OBJ ReadFrame()
		{
			CAN_OBJ frame = new CAN_OBJ();
		
			if (false == BufferEmpty())
			{
                lock (this)
                {
                    //Console.WriteLine("[{0}] - [ReadFrame] - start");
                    uint uiLen = 1;
                    try
                    {
                        lock (this)
                        {
                            if (ECANDLL.Receive(Setting.DeviceType, Setting.DeviceID, Setting.Channel, out frame, uiLen, 1) != ECANStatus.STATUS_OK)
                            {
                                string strErrInfo = ReadError();
                                throw new Exception(string.Format("Failed at CAN receive: {0}", strErrInfo));
                            }
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
			}
		
			return frame;
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
                        {
                            canObj = ReadFrame();
                            if (canObj.DataLen > 0)
                            {
                                return true;
                            }
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
		/// Get a frame from specified CAN ID and the frame from other ID will be ignored.
		/// </summary>
		/// <param name="canObj">return the completed frame</param>
		/// <param name="canID">the desired CAN ID</param>
		/// <param name="timeOut">The time out of receiving frames</param>
		/// <returns>read frame successfully or not</returns>
		public bool ReceiveSingleMessageByID(out CAN_OBJ canObj, uint canID, int timeOut)
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
        /// Get data from specified CAN ID and the frames from other ID will be ignored.
        /// </summary>
        /// <param name="DataList">return the completed frames</param>
        /// <param name="canID">the desired CAN ID</param>
        /// <param name="duration">read data within specified duration, unit in ms</param>
        /// <param name="timeOut">The time out of receiving frames</param>
        /// <returns>read frames successfully or not</returns>
        public bool ReceiveMessagesByID(out List<CAN_OBJ> DataList, uint canID, int timeOut)
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
        /// Get data from specified CAN ID and the frames from other ID will be ignored.
        /// </summary>
        /// <param name="DataList">return the completed frames</param>
        /// <param name="canID">the desired CAN ID</param>
        /// <param name="duration">read data within specified duration, unit in ms</param>
        /// <param name="timeOut">The time out of receiving frames</param>
        /// <param name="clearBufferBeforeRead">whether clear buffer of CAN receiver and received global variable</param>
        /// <returns>read frames successfully or not</returns>
        public bool ReceiveMessagesByID(out List<CAN_OBJ> DataList, uint canID, int timeOut, bool clearBufferBeforeRead)
        {
            DataList = new List<CAN_OBJ>();
            CAN_OBJ objFrame = new CAN_OBJ();

            try
            {
                ClearBuffer(clearBufferBeforeRead);
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
        /// Get data from specified CAN ID and the frames from other ID will be ignored.
        /// </summary>
        /// <param name="DataList">return the completed frames</param>
        /// <param name="canID">the desired CAN ID</param>
        /// <param name="duration">read data within specified duration, unit in ms</param>
        /// <param name="timeOut">The time out of receiving frames</param>
        /// <returns>read frames successfully or not</returns>
        public bool ReceiveMessagesByID(out List<string> DataList, uint canID, int duration, int timeOut)
		{
			DataList = new List<string>();
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
							DataList.Add(FrameToString(objFrame));
						}
					}
					else if ((DateTime.Now - dtStart).TotalMilliseconds > timeOut && (DateTime.Now - dtStart).TotalMilliseconds > duration)
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

		public bool FetchFrames(out List<CAN_OBJ> CanObjList, int TimeOut)
        {
        	try
        	{
	        	//if null, exception
				if(ReceiveThread == null)
				{
					throw new Exception(string.Format("Receiving thread is not initialized."));
				}
				//if not running, start
				if(ReceiveThread.ThreadState != ThreadState.Running && ReceiveThread.ThreadState != ThreadState.WaitSleepJoin)
				{
					StartReceiveThread("World");
				}
				Thread.Sleep(TimeOut);

				EnableReceiveThread = false;
                CanObjList = new List<CAN_OBJ>();
                CanObjList =  listReceivedFrame.ToList();
				//listReceivedFrame.ForEach(i => CanObjList.Add(i));

				//debugging info
				Console.WriteLine("[{0}]-[FetchFrames] - Can received: {1} frames", DateTime.Now.ToString("HH:mm:ss.ffff"), listReceivedFrame.Count);
				Console.WriteLine("[{0}]-[FetchFrames] - Method received: {1} frames", DateTime.Now.ToString("HH:mm:ss.ffff"), CanObjList.Count);
				Console.WriteLine("[{0}]-[FetchFrames] - BufferEmpty = {1}", DateTime.Now.ToString("HH:mm:ss.ffff"), BufferEmpty());
				Console.WriteLine("[{0}]-[FetchFrames] - ReceivedThread state = {1}", DateTime.Now.ToString("HH:mm:ss.ffff"), ReceiveThread.ThreadState);
			}
			catch(Exception ex)
			{
				throw new Exception(string.Format("[FetchFrames]:{0}", ex.Message));
			}
			return true;
		}

		public bool FetchDataByID(out List<byte[]> DataList, uint ID, int TimeOut)
		{
			Dictionary<uint, List<byte[]>> dictData = null;
			try
			{
				if(false == FetchCategorizedData(out dictData, TimeOut))
				{
                    DataList = new List<byte[]>();
					return false;
				}

				if(false == dictData.ContainsKey(ID))
				{
                    DataList = new List<byte[]>();
                    return false;
				}

				DataList = dictData[ID].ToList();

				//debugging info
				Console.WriteLine("[{0}]-[FetchDataByID] - Method received: {1} frames", DateTime.Now.ToString("HH:mm:ss.ffff"), DataList.Count);
			}
			catch(Exception ex)
			{
				throw new Exception(string.Format("[FetchDataByID]:{0}", ex.Message));
			}
			return true;
		}
		public bool FetchCategorizedData(out Dictionary<uint, List<byte[]>> DataList, int TimeOut)
        {
            DataList = new Dictionary<uint, List<byte[]>>();
            List<CAN_OBJ> canObjList = null;
            try
        	{
        		FetchFrames(out canObjList, TimeOut);
				if(canObjList.Count <= 0)
				{
					return false;
				}

                foreach (CAN_OBJ canObj in canObjList)
                {
                    //blank space between bytes
                    //string strData = string.Format("{0:X}", BitConverter.ToString(canObj.data).Replace("-", " "));
                    if (DataList.ContainsKey(canObj.ID))
                    {
                        DataList[canObj.ID].Add(canObj.data);
                    }
                    else
                    {
                        List<byte[]> listNew = new List<byte[]>();
                        listNew.Add(canObj.data);
                        DataList.Add(canObj.ID, listNew);
                    }
				}
			}
			catch(Exception ex)
			{
				throw new Exception(string.Format("[FetchCategorizedData:Dict]:{0}", ex.Message));
			}
			//debugging info
			Console.WriteLine("[{0}]-[FetchCategorizedData] - Frames from {1} IDs", DateTime.Now.ToString("HH:mm:ss.ffff"), DataList.Count);
            //Console.WriteLine("[{0}]-[FetchCategorizedData] - Clearbuffer and receivebuffer", DateTime.Now.ToString("HH:mm:ss.ffff"));
            //ClearBuffer(true);
			return true;
    	}

		public bool FindExpectedData(uint ID, string Data, int TimeOut)
		{
			Dictionary<uint, List<byte[]>> dictData = null;

			try
			{
				if(false == FetchCategorizedData(out dictData, TimeOut))
				{
					return false;
				}

				if(dictData.ContainsKey(ID))
				{
					List<byte[]> listValues = dictData[ID];
					foreach(byte[] frameData in listValues)
					{
						//blank space between bytes
						string strFrameData = string.Format("{0:X}", BitConverter.ToString(frameData).Replace("-", " "));

						if(strFrameData.IndexOf(Data) != -1)
						{
							return true;//found
						}
						else
						{
							return false;// not found
						}
					}
				}
				else
				{
					return false;// not found
				}
			}
			catch(Exception ex)
			{
				throw new Exception(string.Format("[FindExpectedData:Dict]:{0}", ex.Message));
			}
			return false;
		}

		public static string FrameToString(CAN_OBJ canObj)
        {
            return string.Format("{0:X},{1:X}", canObj.ID, BitConverter.ToString(canObj.data).Replace("-", string.Empty));
		}
	}
    #endregion
}
