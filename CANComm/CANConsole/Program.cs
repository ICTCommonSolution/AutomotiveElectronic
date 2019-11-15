using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using CAN;

namespace CAN
{
    class Program
    {
        static void Main(string[] args)
        {
            string strtemp;
            UInt16 id = 0;
            UInt16 channel = 0;

            Console.Write("Please input a device id by ENTER: ");
            strtemp = (string)Console.ReadLine(); // get a char
            id = Convert.ToUInt16(strtemp);

            Console.Write("Please input a channel id by ENTER: ");
            strtemp = (string)Console.ReadLine(); // get a char
            channel = Convert.ToUInt16(strtemp);

            Console.Write("Press YES or NO to open device:");
            strtemp = (string)Console.ReadLine();

            if (true == strtemp.ToUpper().Equals("YES"))
            {

            }
            else if (true == strtemp.ToUpper().Equals("NO"))
            {
                Console.WriteLine("Cancelled!");
                return;
            }
            else
            {
                Console.WriteLine("Do nothing");
                return;
            }
            CANComm CanTalk = new CANComm(@"d:\1_Code\AutomotiveElectronic\CANComm\Debug\settingsample.json");

            if (false == CanTalk.OpenDevice(id, channel, out strtemp))
            {
                Console.WriteLine(string.Format("Failed with message: {0}", strtemp));
            }
            else
            {
                Console.WriteLine(string.Format("Device Opened!"));
                Console.WriteLine(string.Format("Further features to be continued!"));
            }

            Console.Write("Press Enter to send demo command: 0x1122334455667788");
            Console.ReadLine();

            byte[] command = new byte[8];
            command[0] = 0x11;
            command[1] = 0x22;
            command[2] = 0x33;
            command[3] = 0x44;
            command[4] = 0x55;
            command[5] = 0x66;
            command[6] = 0x77;
            command[7] = 0x88;

            List<byte[]> listData = new List<byte[]>();
            listData.Add(command);
            CanTalk.SendMessages(listData);

//            Thread.Sleep(10000);
			List<CAN_OBJ> listRes = null;
			if(true == CanTalk.ReceiveBytes(out listRes))
			{
				Console.WriteLine(string.Format("Received total {0} can objects:", listRes.Count));
				foreach(CAN_OBJ obj in listRes)
				{
					string line = string.Empty;
					for(int i = 0; i < obj.DataLen; i++)
					{
                        line += obj.data[i].ToString("X2");
					}
					Console.WriteLine(line);
				}
			}
			else
			{
				Console.WriteLine(string.Format("Error in Receive"));
			}

            Console.WriteLine(string.Format("Device will be auto closed within 5s."));
            Thread.Sleep(50000);

            if (false == CanTalk.CloseDevice())
            {
                Console.WriteLine(string.Format("Failed at close"));
            }
            else
            {
                Console.WriteLine(string.Format("Device closed!"));
                Console.WriteLine(string.Format("See you again"));
            }
            return;
        }
    }
}
