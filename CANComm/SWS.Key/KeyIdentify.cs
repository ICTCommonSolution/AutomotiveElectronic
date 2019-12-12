using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nile;
using CAN;
using System.Threading;
using System.Reflection;

namespace TestClass.SWS
{
    public class KeyIdentify : TestClassBase
    {
        private CANComm canTalk = null;
        private string settingFile = @"testinputsample.json";
        int iWaitAfterOpen = 1000;
        int iWaitBeforeFetch = 5000;
        int iTimeout = 2500;

        public KeyIdentify()
        {//do nothing
        }

        public int Do()
        {
            OpenDevice();
            StartPeriodicFrameThread();

            Console.WriteLine("[{0}] - [{1}.Do] - Start", DateTime.Now.ToString("HH:mm:ss.ffff"), this.GetType().Name);
            //get input
            base.GetInput(settingFile, Assembly.GetExecutingAssembly().GetName().Name, this.GetType().Name, "WaitAfterOpen", ref iWaitAfterOpen);
            base.GetInput(settingFile, Assembly.GetExecutingAssembly().GetName().Name, this.GetType().Name, "WaitBeforeFetch", ref iWaitBeforeFetch);
            base.GetInput(settingFile, Assembly.GetExecutingAssembly().GetName().Name, this.GetType().Name, "Timeout", ref iTimeout);
            this.GetType().ToString();

            StartReceiveThread();

            Thread.Sleep(5000);

            List<byte[]> listResponse = new List<byte[]>();
            canTalk.FetchDataByID(out listResponse, 0x012B, 2500);
            if (listResponse.Count > 0)
            {
                foreach (byte[] data in listResponse)
                {
                    Console.WriteLine(BitConverter.ToString(data).Replace("-", " "));
                }
            }


            canTalk.EnablePeriodicMessageThread = false;

            CloseDevice();
            return 1;
        }

        private List<string> FetchData(uint ID, int waitBeforeFetch, int timeOut)
        {
            //test in this period
            Thread.Sleep(waitBeforeFetch);
            //canTalk.EnablePeriodicMessageThread = false;

            List<byte[]> listByteData = new List<byte[]>();
            if (false == canTalk.FetchDataByID(out listByteData, ID, timeOut))
            {
                return null;
            }
            else
            {
                List<string> listStrData = new List<string>();
                foreach (byte[] data in listByteData)
                {
                    listStrData.Add(BitConverter.ToString(data).Replace("-", " "));
                }
                return listStrData;
            }
        }
        private bool Key_ByteCheck(string keyName, uint ID, string[] expectedData,int waitBeforeFetch, int timeOut)
        {
            bool bStatus = false;

            //Console.WriteLine("[{0}]-[{1}.Key_ByteCheck] - Clearbuffer and receivebuffer", DateTime.Now.ToString("HH:mm:ss.ffff"), this.GetType().Name);
            //canTalk.ClearBuffer(true);
            StartReceiveThread();

            Console.WriteLine("");
            Console.WriteLine(string.Format("Please {0} key after lights on", keyName));
            Console.WriteLine("");

            List<string> listResponse = FetchData(ID, waitBeforeFetch, timeOut);

            if (listResponse == null || listResponse.Count <= 0)
            {
                return false;                
            }
            foreach (string strExpected in expectedData)
            {
                foreach (string strResponse in listResponse)
                {
                    if (strResponse.IndexOf(strExpected) >= 0)
                    {
                        bStatus = true;
                        break;
                    }
                }
                if(bStatus)
                    break;
            }

            if (true == bStatus)
            {
                Console.WriteLine("");
                Console.WriteLine(string.Format("{0} key pressed!!!!!!!!!!!!!!!!!!!", keyName));
                Console.WriteLine("");
            }
            else
            {
                Console.WriteLine("");
                Console.WriteLine("Not pressed-----------");
                Console.WriteLine("");
            }

            return bStatus;
        }
        //FAS_Menu_Thumbwheel

        private bool Key_BitsCheck(string keyName, uint ID, uint startBit, uint bitLength, ulong expectedValue, int waitBeforeFetch, int timeOut)
        {
            bool status = false;
            List<byte[]> listData = null;

            //Console.WriteLine("[{0}]-[{1}.Key_BitsCheck] - Clearbuffer and receivebuffer", DateTime.Now.ToString("HH:mm:ss.ffff"), this.GetType().Name);
            //canTalk.ClearBuffer(true);
            StartReceiveThread();

            Console.WriteLine("");
            Console.WriteLine("Please {0} key after lights on", keyName);
            Console.WriteLine("");

            Thread.Sleep(waitBeforeFetch);
            if (false == canTalk.FetchDataByID(out listData, ID, timeOut))
            {
                status = false;
            }
            else
            {
                foreach (byte[] data in listData)
                {
                    if (expectedValue == canTalk.GetBitsFromFrame(data, startBit, bitLength))
                    {
                        status = true;
                        break;
                    }
                }
            }

            if(true == status)
            {
                Console.WriteLine("");
                Console.WriteLine("{0} key pressed!!!!!!!!!!!!!!!!!!!", keyName);
                Console.WriteLine("");
            }
            else
            {
                Console.WriteLine("");
                Console.WriteLine("Not pressed-----------");
                Console.WriteLine("");
            }
            return status;
        }

        private void OpenDevice()
        {
            int iWaitAfterOpen = 1000;

            Console.WriteLine("[{0}] - [{1}.Do] - Start", DateTime.Now.ToString("HH:mm:ss.ffff"), this.GetType().Name);
            //get input

            canTalk = new CANComm(@"d:\1_Code\AutomotiveElectronic\CANComm\Debug\settingsample.json");
            Console.WriteLine("[{0}] - [{1}.Do] - close device", DateTime.Now.ToString("HH:mm:ss.ffff"), this.GetType().Name);

            string strTemp = string.Empty;
            canTalk.OpenDevice(0, 0, out strTemp);
            Thread.Sleep(iWaitAfterOpen);
        }

        private void CloseDevice()
        {
            Console.WriteLine("[{0}] - [{2}.Do] - ReceivedThread state = {1}", DateTime.Now.ToString("HH:mm:ss.ffff"), canTalk.ReceiveThread.ThreadState, this.ToString());
            Console.WriteLine("[{0}] - [{2}.Do] - PeriodicThread state = {1}", DateTime.Now.ToString("HH:mm:ss.ffff"), canTalk.PeriodicMessageThread.ThreadState, this.ToString());
            canTalk.ClearBuffer(true);
        }

        private void StartThreads()
        {
            //Init threads
            canTalk.InitPeriodicFrameThread(false);
            canTalk.InitReceiveThread(false);

            //start thread
            Console.WriteLine("[{0}] - [{1}.StartThreads] - send start", DateTime.Now.ToString("HH:mm:ss.ffff"), this.ToString());
            canTalk.StartPeroidicFrameThread("Hello");
            canTalk.StartReceiveThread("World");
        }

        private void StartPeriodicFrameThread()
        {
            //Init threads
            canTalk.InitPeriodicFrameThread(false);

            //start thread
            Console.WriteLine("[{0}] - [{1}.StartPeriodicFrameThread] - send start", DateTime.Now.ToString("HH:mm:ss.ffff"), this.ToString());
            canTalk.StartPeroidicFrameThread("Hello");
        }
        private void StartReceiveThread()
        {
            //Init threads
            canTalk.InitReceiveThread(false);

            Console.WriteLine("[{0}]-[{1}.StartReceiveThread] - Clearbuffer and receivebuffer", DateTime.Now.ToString("HH:mm:ss.ffff"), this.GetType().Name);
            canTalk.ClearBuffer(true);

            //start thread
            Console.WriteLine("[{0}] - [{1}.StartReceiveThread] - send start", DateTime.Now.ToString("HH:mm:ss.ffff"), this.ToString());
            canTalk.StartReceiveThread("World");
        }
    }

}
