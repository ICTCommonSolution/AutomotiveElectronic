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
    public class Key: TestClassBase
    {
        private CANComm canTalk = null;
        private string strTemp = string.Empty;
        private string settingFile = @"testinputsample.json";

        public Key()
        {//do nothing
        }

        public int Do()
        {
            int iWaitAfterOpen = 1000;
            int iTimeout = 0;

            //get input
            base.GetInput(settingFile, "SWS", "Key", "WaitAfterOpen", ref iWaitAfterOpen);
            base.GetInput(settingFile, "SWS", "Key", "ReadTimeOut", ref iTimeout);

            canTalk = new CANComm(@"d:\1_Code\AutomotiveElectronic\CANComm\Debug\settingsample.json");
            //ToDo:
            //Remove below dubugging info
            if (false == canTalk.OpenDevice(0, 0, out strTemp, iWaitAfterOpen, true, true))
            {
                Console.WriteLine(string.Format("Failed with message: {0}", strTemp));
            }
            else
            {
                Console.WriteLine(string.Format("Device Opened!"));
                Console.WriteLine(string.Format("Further features to be continued!"));
            }
            Thread.Sleep(iWaitAfterOpen);

            bool status = canTalk.ClearAndSeekMessages(0x0331, "80190002", 5000);
            Console.WriteLine("status={0}", status);
            List<string> listData = new List<string>();
            //canTalk.ClearAndFetchMessagesByID(out listData, 0x0331, 5000, true);
            foreach (string data in listData)
            {
                Console.WriteLine("{0}", data);
            }
            canTalk.CloseDevice();

            return 1;
        }
    }
}
