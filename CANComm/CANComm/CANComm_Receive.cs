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

        /// <summary>
        /// Get data from specified CAN ID and the frames from other ID will be ignored.
        /// </summary>
        /// <param name="DataList">return the received ID and data in one string, separated by ','</param>
        /// <param name="canID">the desired CAN ID</param>
        /// <param name="duration">read data within specified duration, unit in ms</param>
        /// <param name="timeOut">The time out of receiving frames</param>
        /// <param name="clearBufferBeforeRead">whether clear buffer of CAN receiver and received global variable</param>
        /// <returns>read frames successfully or not</returns>
        public bool ClearAndReceiveMessagesByID(out List<string> DataList, uint canID, int duration, int timeOut, bool clearBufferBeforeRead)
		{
			DataList = new List<string>();
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
        #endregion

		#region Fetch message(s) from received global variable
        /// <summary>
        /// Get data from received frames by specified CAN ID and the frames from other ID will be ignored.
        /// </summary>
        /// <param name="DataList">return the received ID and data in one string, separated by ','</param>
        /// <param name="canID">the desired CAN ID</param>
        /// <param name="duration">read data within specified duration, unit in ms</param>
        /// <param name="clearBufferBeforeRead">whether clear buffer of CAN receiver and received global variable</param>
        /// <returns>read frames successfully or not</returns>
        public bool ClearAndFetchMessagesByID(out List<string> DataList, uint canID, int duration, bool clearBufferBeforeRead)
		{
			DataList = new List<string>();
			CAN_OBJ objFrame = new CAN_OBJ();

            try
            {
                ReceiveThread.Suspend();
                while (ReceiveThread.ThreadState != ThreadState.Suspended)
                {
                    Thread.Sleep(5);
                }
                ClearBuffer(clearBufferBeforeRead);
                Console.WriteLine("Press key+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
                ReceiveThread.Resume();
                while (ReceiveThread.ThreadState != ThreadState.Running)
                {
                    Thread.Sleep(1);
                }

                //receiving
                Thread.Sleep(duration);

                //Stop receiving before fetch
                ReceiveThread.Suspend();
                while (ReceiveThread.ThreadState != ThreadState.Suspended)
                {
                    Thread.Sleep(5);
                }
                lock (listReceivedFrame)
                {
                    for (int i = 0; i < listReceivedFrame.Count; i++)
                    {
                        CAN_OBJ canObj = listReceivedFrame[i];
                        if (canObj.ID == canID)
                        {
                            DataList.Add(FrameToString(canObj));
                        }
                    }
                }
				//resume receiving thread
				ReceiveThread.Resume();
				while (ReceiveThread.ThreadState != ThreadState.Running)
				{
					Thread.Sleep(1);
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
        /// Get data from received frames by specified CAN ID and the frames from other ID will be ignored.
        /// </summary>
        /// <param name="DataList">return the received ID and data in one string, separated by ','</param>
        /// <param name="canID">the desired CAN ID</param>
        /// <param name="duration">read data within specified duration, unit in ms</param>
        /// <param name="clearBufferBeforeRead">whether clear buffer of CAN receiver and received global variable</param>
        /// <returns>read frames successfully or not</returns>
        public bool ClearAndSeekMessages(uint canID, string data, int timeOut, bool clearBufferBeforeRead = true)
		{
			CAN_OBJ objFrame = new CAN_OBJ();
            bool bFound = false;

			data = data.Replace("-", string.Empty);
            Console.WriteLine("[{0}] - [ClearAndSeekMessages] - Start", DateTime.Now.ToString("HH:mm:ss.ffff"));
            Console.WriteLine("[{0}] - [ClearAndSeekMessages] - canID={1};data={2};timeout={3}", DateTime.Now.ToString("HH:mm:ss.ffff"), canID, data, timeOut);

            try
            {
                Console.WriteLine("[{0}] - [ClearAndSeekMessages] - receivethread state = {1}", DateTime.Now.ToString("HH:mm:ss.ffff"), ReceiveThread.ThreadState.ToString());
                //if (ReceiveThread.ThreadState != ThreadState.Running)
                //{
                //    while (ReceiveThread.ThreadState == ThreadState.WaitSleepJoin)
                //    {
                //        Thread.Sleep(5);
                //    }

                //    //ReceiveThread.Start("ClearAndSeekMessages");
                //}
                ReceiveThread.Suspend();
                while (ReceiveThread.ThreadState != ThreadState.Suspended)
				{
					Thread.Sleep(5);
				}
                Console.WriteLine("[{0}] - [ClearAndSeekMessages] - 1st clear buffer", DateTime.Now.ToString("HH:mm:ss.ffff"));
                ClearBuffer(clearBufferBeforeRead);

                Console.WriteLine("Press key-------------------------------------------------------------");
                //start receiving
                Console.WriteLine("[{0}] - [ClearAndSeekMessages] - 1st Resume", DateTime.Now.ToString("HH:mm:ss.ffff"));
                ReceiveThread.Resume();
                Thread.Sleep(100);
                while (ReceiveThread.ThreadState != ThreadState.Running)
				{
					Thread.Sleep(1);
				}

                DateTime dtStart = DateTime.Now;
                int iDebugCount = 0;
                //Seek data
                while (bFound == false && (DateTime.Now - dtStart).TotalMilliseconds < timeOut)
                {
                    iDebugCount++;
                    Console.WriteLine("[{0}] - [ClearAndSeekMessages] - while loop:{1}", DateTime.Now.ToString("HH:mm:ss.ffff"), iDebugCount);
                    lock (listReceivedFrame)
                    {
                        if (listReceivedFrame.Count > 0)
                        {
                            foreach (CAN_OBJ canObj in listReceivedFrame)
                            {
                                iDebugCount++;
                                Console.WriteLine("[{0}] - [ClearAndSeekMessages] - while loop:{1}", DateTime.Now.ToString("HH:mm:ss.ffff"), iDebugCount);
                                string strData = BitConverter.ToString(canObj.data).Replace("-", string.Empty);
                                Console.WriteLine("foreach: {0:X} : {1:X}", canObj.ID, strData);
                                if (canObj.ID == canID)
                                {
                                    Console.WriteLine("if:{0:X}", strData);
                                    if (strData.IndexOf(data) >= 0)
                                    {
                                        Console.WriteLine("[{0}] - [ClearAndSeekMessages] - Found", DateTime.Now.ToString("HH:mm:ss.ffff"));
                                        bFound = true;
                                        break;
                                    }
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine("[{0}] - [ClearAndSeekMessages] - listReceivedFrame is empty", DateTime.Now.ToString("HH:mm:ss.ffff"));
                            Thread.Sleep(10);
                        }
                    }
                    Console.WriteLine("[{0}] - [ClearAndSeekMessages] - SpanTime: {1}ms", DateTime.Now.ToString("HH:mm:ss.ffff"), (DateTime.Now - dtStart).TotalMilliseconds);
                }
                if (bFound == false && (DateTime.Now - dtStart).TotalMilliseconds > timeOut)
                {
                    Console.WriteLine("[{0}] - [ClearAndSeekMessages] - timeout", DateTime.Now.ToString("HH:mm:ss.ffff"));
                }
                Console.WriteLine("[{0}] - [ClearAndSeekMessages] - 2nd clearbuffer", DateTime.Now.ToString("HH:mm:ss.ffff"));
                ClearBuffer(clearBufferBeforeRead);
				//while (ReceiveThread.ThreadState != ThreadState.Running)
				//{
                //    Console.WriteLine("[{0}] - [ClearAndSeekMessages] - receivethread is not running. to resume", DateTime.Now.ToString("HH:mm:ss.ffff"));
                    Console.WriteLine("[{0}] - [ClearAndSeekMessages] - receivethread state = {1}", DateTime.Now.ToString("HH:mm:ss.ffff"), ReceiveThread.ThreadState.ToString());
                //    ReceiveThread.Resume();
				//	Thread.Sleep(1);
				//}
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
            Console.WriteLine("[{0}] - [ClearAndSeekMessages] - End", DateTime.Now.ToString("HH:mm:ss.ffff"));
            return bFound;
		}
		#endregion

		private string FrameToString(CAN_OBJ canObj)
        {
            string str = BitConverter.ToString(canObj.data).Replace("-", string.Empty);

            return string.Format("{0:X},{1:X}", canObj.ID, BitConverter.ToString(canObj.data).Replace("-", string.Empty));
		}
	}
}
