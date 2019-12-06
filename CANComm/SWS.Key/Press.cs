using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nile;
using CAN;
using System.Threading;

namespace TestClass.SWS
{
    public class Press : TestClassBase
    {
        private CANComm canTalk = null;
        private string settingFile = @"testinputsample.json";

        public Press()
        {//do nothing
        }

        public int Do()
        {
            int iWaitAfterOpen = 1000;

            Console.WriteLine("[{0}] - [Press.Do] - Start", DateTime.Now.ToString("HH:mm:ss.ffff"));
            //get input
            base.GetInput(settingFile, "SWS", "Vector", "WaitAfterOpen", ref iWaitAfterOpen);

            canTalk = new CANComm(@"d:\1_Code\AutomotiveElectronic\CANComm\Debug\settingsample.json");
            Console.WriteLine("[{0}] - [Press.Do] - close device", DateTime.Now.ToString("HH:mm:ss.ffff"));

            string strTemp = string.Empty;
            canTalk.OpenDevice(0,0, out strTemp);
            Thread.Sleep(iWaitAfterOpen);

            //Init threads
            canTalk.InitPeriodicFrameThread(false);
            canTalk.InitReceiveThread(false);

            //start thread
            Console.WriteLine("[{0}] - [Press.Do] - send start", DateTime.Now.ToString("HH:mm:ss.ffff"));
            canTalk.StartPeroidicFrameThread("Hello");
            canTalk.StartReceiveThread("World");

            //test in this period
            Thread.Sleep(5000);
            canTalk.EnablePeriodicMessageThread = false;
            Console.WriteLine("[{0}] - [Press.Do] - PeriodicThread state = {1}", DateTime.Now.ToString("HH:mm:ss.ffff"), canTalk.PeriodicMessageThread.ThreadState);

            Dictionary<uint, List<string>> dictData = null;
            canTalk.FetchCategorizedData(out dictData, 2500);
            Console.WriteLine("Send {0} frames", canTalk.icount);
            Thread.Sleep(1000);
            Console.WriteLine("[{0}] - [Press.Do] - ReceivedThread state = {1}", DateTime.Now.ToString("HH:mm:ss.ffff"), canTalk.ReceiveThread.ThreadState);
            Console.WriteLine("[{0}] - [Press.Do] - PeriodicThread state = {1}", DateTime.Now.ToString("HH:mm:ss.ffff"), canTalk.PeriodicMessageThread.ThreadState);


            //new test
            canTalk.icount = 0;
            canTalk.StartPeroidicFrameThread("Hello");
            canTalk.StartReceiveThread("World");

            Thread.Sleep(10000);
            canTalk.EnablePeriodicMessageThread = false;
            Console.WriteLine("[{0}] - [Press.Do] - PeriodicThread state = {1}", DateTime.Now.ToString("HH:mm:ss.ffff"), canTalk.PeriodicMessageThread.ThreadState);
            List<string> listData = null;
            canTalk.FetchDataByID(out listData, 0x5BF, 2500);
            foreach (string data in listData)
            {
                Console.WriteLine(string.Format("{0}:{1}", 0x5BF, data));
            }
            Console.WriteLine("Send {0} frames", canTalk.icount);
            Console.WriteLine("[{0}] - [Press.Do] - ReceivedThread state = {1}", DateTime.Now.ToString("HH:mm:ss.ffff"), canTalk.ReceiveThread.ThreadState);
            Console.WriteLine("[{0}] - [Press.Do] - PeriodicThread state = {1}", DateTime.Now.ToString("HH:mm:ss.ffff"), canTalk.PeriodicMessageThread.ThreadState);

            canTalk.ClearBuffer(true);
            canTalk.CloseDevice();
            Thread.Sleep(1000);

            return 1;
        }
    }
}
