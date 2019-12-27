using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nile;
using Nile.Instruments.CAN;
using Nile.Instruments;
using System.Threading;
using System.Reflection;

namespace TestClass.SWS
{
    public class PressAll : TestClassBase
    {
        private CANComm canTalk = null;
        private string settingFile = @"testinputsample.json";
        int iWaitAfterOpen = 1000;
        int iWaitBeforeFetch = 5000;
        int iTimeout = 2500;

        public PressAll()
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

            //SDS
            string[] expectedValue = new string[] { "19 00 01", "19 00 04", "19 00 05", "19 00 06", "19 00 07", "19 00 08", "19 00 09" };
            Key_ByteCheck("SDS", 0x5BF, expectedValue, iWaitBeforeFetch, iTimeout);
            Console.WriteLine("");
            Thread.Sleep(2000);

            //Volume-
            expectedValue = new string[] { "11 00 01", "11 00 04", "11 00 05", "11 00 06", "11 00 07", "11 00 08", "11 00 09" };
            Key_ByteCheck("Volume-", 0x5BF, expectedValue, iWaitBeforeFetch, iTimeout);

            //Up
            expectedValue = new string[] { "04 00 01", "04 00 04", "04 00 05", "04 00 06", "04 00 07", "04 00 08", "04 00 09" };
            Key_ByteCheck("Up", 0x5BF, expectedValue, iWaitBeforeFetch, iTimeout);

            //OK
            expectedValue = new string[] { "07 00 01", "07 00 04", "07 00 05", "07 00 06", "07 00 07", "07 00 08", "07 00 09" };
            Key_ByteCheck("OK", 0x5BF, expectedValue, iWaitBeforeFetch, iTimeout);

            //Down
            expectedValue = new string[] { "05 00 01", "05 00 04", "05 00 05", "05 00 06", "05 00 07", "05 00 08", "05 00 09" };
            Key_ByteCheck("Down", 0x5BF, expectedValue, iWaitBeforeFetch, iTimeout);

            //Volume+
            expectedValue = new string[] { "10 00 01", "10 00 04", "10 00 05", "10 00 06", "10 00 07", "100008", "10 00 09" };
            Key_ByteCheck("Volume+", 0x5BF, expectedValue, iWaitBeforeFetch, iTimeout);

            //GRA_Hauptschalter, 0/1?
            Key_BitsCheck("GRA_Hauptschalter(0/1)", 0x12B, 12, 1, 1, iWaitBeforeFetch, iTimeout);

            //GRA_Tip_Wiederaufnahme (RES)
            Key_BitsCheck("RES", 0x12B, 19, 1, 1, iWaitBeforeFetch, iTimeout);

            //FAS_Menu_Thumbwheel
            expectedValue = new string[] { "74 00 01", "74 00 04", "74 00 05", "74 00 06", "74 00 07", "74 00 08", "74 00 09" };
            Key_ByteCheck("Thumbwheel", 0x5BF, expectedValue, iWaitBeforeFetch, iTimeout);

            // "+"
            Key_BitsCheck("+", 0x12B, 17, 1, 1, iWaitBeforeFetch, iTimeout);

            //GRA_Verstellung_Zeitluecke (In Lane?)
            Key_BitsCheck("ACC", 0x12B, 20, 2, 3, iWaitBeforeFetch, iTimeout);

            // "-"
            Key_BitsCheck("-", 0x12B, 18, 1, 1, iWaitBeforeFetch, iTimeout);

            //GRA_Tip_Setzen (SET)?
            Key_BitsCheck("SET", 0x12B, 16, 1, 1, iWaitBeforeFetch, iTimeout);

            canTalk.EnablePeriodicMessageThread = false;
            CloseDevice();

            //foreach (byte[] data in listData)
            //{
            //    Console.WriteLine(string.Format("{0:X}:{1:X}", 0x12B, BitConverter.ToString(data).Replace("-", " ")));
            //    Console.WriteLine(string.Format("[GRA_Hauptschalter] - value = {0}", CANComm.GetBitsFromFrame(data, 12, 1))) ;
            //    Console.WriteLine(string.Format("[GRA_Tip_Setzen] - value = {0}", CANComm.GetBitsFromFrame(data, 16, 1)));
            //    Console.WriteLine(string.Format("[GRA_Tip_Hoch] - value = {0}", CANComm.GetBitsFromFrame(data, 17, 1)));
            //    Console.WriteLine(string.Format("[GRA_Tip_Runter] - value = {0}", CANComm.GetBitsFromFrame(data, 18, 1)));
            //    Console.WriteLine(string.Format("[GRA_Tip_Wiederaufnahme] - value = {0}", CANComm.GetBitsFromFrame(data, 19, 1)));
            //    Console.WriteLine(string.Format("[GRA_Verstellung_Zeitluecke] - value = {0}", CANComm.GetBitsFromFrame(data, 20, 2)));
            //}

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
                bStatus = false;
            }
            else
            {
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
                    if (bStatus)
                        break;
                }
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
            if (canTalk.ReceiveThread != null)
            {
                Console.WriteLine("[{0}] - [{2}.Do] - ReceivedThread state = {1}", DateTime.Now.ToString("HH:mm:ss.ffff"), canTalk.ReceiveThread.ThreadState, this.ToString());
            }
            if (canTalk.PeriodicMessageThread != null)
            {
                Console.WriteLine("[{0}] - [{2}.Do] - PeriodicThread state = {1}", DateTime.Now.ToString("HH:mm:ss.ffff"), canTalk.PeriodicMessageThread.ThreadState, this.ToString());
            }
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
