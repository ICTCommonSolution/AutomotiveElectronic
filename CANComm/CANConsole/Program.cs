using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using CAN;
using System.IO;

namespace CAN
{
    class Program
    {
        static void Main(string[] args)
        {
			var arguments = CommandLineArgumentParser.Parse(args);

            //load vector log csv file
            if (arguments.Has("-c") && arguments.Has("-v"))
            {
                //check if config file exists
                if (false == File.Exists(arguments.Get("-c").Next))
                {
                    Console.WriteLine("The config file {0} dose not exist", arguments.Get("-c").Next);
                }
                else
                {
                    Console.WriteLine("Loading config {0} and init device", arguments.Get("-c").Next);
                }
                //check if command list file exists
                if (false == File.Exists(arguments.Get("-v").Next))
                {
                    Console.WriteLine("The command file {0} dose not exist", arguments.Get("-v").Next);
                }
                else
                {
                    Console.WriteLine("Loading command file {0}", arguments.Get("-v").Next);
                }
                RunVectorCommandList(arguments.Get("-c").Next, arguments.Get("-v").Next);
            }
            else if (arguments.Has("-c") && arguments.Has("-s"))
            {
                //check if config file exists
                if (false == File.Exists(arguments.Get("-c").Next))
                {
                    Console.WriteLine("The config file {0} dose not exist", arguments.Get("-c").Next);
                }
                else
                {
                    Console.WriteLine("Loading config {0} and init device", arguments.Get("-c").Next);
                }
                //check if command list file exists
                if (false == File.Exists(arguments.Get("-s").Next))
                {
                    Console.WriteLine("The command file {0} dose not exist", arguments.Get("-s").Next);
                }
                else
                {
                    Console.WriteLine("Loading command file {0}", arguments.Get("-s").Next);
                }
                RunSimpleCommandList(arguments.Get("-c").Next, arguments.Get("-s").Next);
            }
            else if (args.Count() == 0)
            {
                Console.WriteLine("Run without argu as default");
                DefaultNoArgu();
            }
            else
            { }

            Console.WriteLine("Sent over");
            Console.Write("Press any key to exit");
            Console.Read();

            return;
        }
        private static void RunSimpleCommandList(string configFile, string commandFile)
        {
            CANComm CanTalk = null;
            List<string[]> listCommand = null;
            string strtemp = string.Empty;

            try
            {
                //listCommand = LoadCommandFile(commandFile);
                listCommand = LoadSimpleCommandFileToCanList(commandFile);

                CanTalk = new CANComm(configFile);

                if (false == CanTalk.OpenDevice(out strtemp))
                {
                    Console.WriteLine(string.Format("Failed with message: {0}", strtemp));
                }
                else
                {
                    Console.WriteLine(string.Format("Device Opened!"));
                    Console.WriteLine(string.Format("Further features to be continued!"));
                }

                do
                {
                    for (int index = 0; index < listCommand.Count; index++)
                    //for (int index = 0; index < listCan.Count; index++)
                    {
                        string[] strPara = listCommand[index];
                        Console.WriteLine("Send: {0},{1}", strPara[0], strPara[1]);
                        SendCommand(CanTalk, strPara[0], strPara[1]);
                    }

                    Thread.Sleep(800);
                    CanTalk.ClearBuffer();
                //} while (((char)Console.Read()).Equals('q') == false);
                } while (true) ;
            //receive message.
            Receive(CanTalk);

                int iSleep = 20;
                Console.WriteLine(string.Format("Device will be auto closed within {0}s.", iSleep));
                Thread.Sleep(iSleep * 1000);

                if (false == CanTalk.CloseDevice())
                {
                    Console.WriteLine(string.Format("Failed at close"));
                }
                else
                {
                    Console.WriteLine(string.Format("Device closed!"));
                    //Console.WriteLine(string.Format("See you again"));
                }
            }
            catch (Exception ex)
            { }
            return;
        }
                private static void RunVectorCommandList(string configFile, string commandFile)
        {
            CANComm CanTalk = null;
            List<Dictionary<string, string>> listCommand = null;
            List<CAN_OBJ> listCan = null;
            string strtemp = string.Empty;

			try
			{
                //listCommand = LoadCommandFile(commandFile);
                listCan = LoadVectorCommandFileToCanList(commandFile);

                CanTalk = new CANComm(configFile);

				if (false == CanTalk.OpenDevice(out strtemp))
				{
					Console.WriteLine(string.Format("Failed with message: {0}", strtemp));
				}
				else
				{
					Console.WriteLine(string.Format("Device Opened!"));
					Console.WriteLine(string.Format("Further features to be continued!"));
				}

                //for (int index = 0; index < listCommand.Count; index++)
                //{
                //    Dictionary<string, string> dictCommand = listCommand[index];
                //    if (true == dictCommand["TxRx"].ToUpper().Equals("TX"))
                //    {
                //        //if (true == SendOrIgnore(dictCommand["ID"], dictCommand["Command"]))
                //        {
                //            SendCommand(CanTalk, dictCommand);
                //        }
                //    }
                //    else if (true == dictCommand["TxRx"].ToUpper().Equals("RX"))
                //    {
                //        ReceiveMessage(CanTalk, dictCommand);
                //    }
                //    else
                //    {
                //        Console.WriteLine(string.Format("Skip unkwon command. Command type: {0}", dictCommand["TxRx"]));
                //    }
                //}

                //for (int index = 0; index < 100; index++)
                    for (int index = 0; index < listCan.Count; index++)
                    {
                        Console.WriteLine("Send: {0}", CommandInfo(listCan[index]));
                    SendCommand(CanTalk, listCan[index]);
                }

			}
			catch(Exception ex)
			{
				Console.WriteLine(string.Format("Get error with message {0}", ex.Message));
			}
			return;
        }

        private static bool SendOrIgnore(string ID, string Command)
        {
            Console.Write(string.Format("Press Y to send command: {0} to ID: {1}. Or press N to skip: ", ID, Command));
            char c = (char)Console.Read();
            Console.WriteLine("");
            if (c == 'Y' || c == 'y')
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private static CAN_OBJ CommandToCanObj(Dictionary<string, string> command)
        {
            CAN_OBJ canObj = new CAN_OBJ();

            if (true == command["ID"].ToLower().EndsWith("x"))
            {
                byte[] data = StringToByteArray(command["ID"].Substring(0, command["ID"].Length-1));
                uint uiInput = 0; //convert byte[] to uint
                for (int i = 0; i < data.Length; i++)
                {
                    uint uiTemp = (uint)data[i] << (8 * (data.Length - i - 1));
                    uiInput = uiInput + uiTemp;
                }
                canObj.ID = uiInput;
                canObj.ExternFlag = 0x1;
            }
            else
            {
                byte[] data = StringToByteArray(command["ID"]);
                uint uiInput = 0; //convert byte[] to uint
                for (int i = 0; i < data.Length; i++)
                {
                    uiInput = uiInput + (uint)data[i] << (8 * (data.Length - i - 1));
                }
                canObj.ID = uiInput;
                canObj.ExternFlag = 0x0;
            }
            canObj.RemoteFlag = 0;
            canObj.Reserved = null;
            canObj.SendType = 0;
            canObj.TimeFlag = 0x0;
            canObj.TimeStamp = 0;
            byte[] dataTemp1 = StringToByteArray(command["DataLen"]);
            canObj.DataLen = dataTemp1[0];
            byte[] datatemp2 = StringToByteArray(command["Command"]);
            canObj.data = new byte[canObj.DataLen];
            datatemp2.CopyTo(canObj.data,0);

            return canObj;
        }
        private static void SendCommand(CANComm canTalk, CAN_OBJ canObj)
        {
            try
            {
                if (true == canTalk.SendMessage(canObj))
                {
                }
                else
                {
                    Console.WriteLine(string.Format("Failed Send command:{0}", CommandInfo(canObj)));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("Error in Sending command:{0}", CommandInfo(canObj)));
            }
        }
        private static void SendCommand(CANComm canTalk, string ID, string data)
        {
            try
            {
                if (true == canTalk.SendMessage(ID, data))
                {
                }
                else
                {
                    Console.WriteLine(string.Format("Failed Send command:{0}:{1}", ID, data));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("Error in Sending command:{0}:{1}", ID, data));
            }
        }

        private static void SendCommand(CANComm canTalk, Dictionary<string, string> command)
        {
            try
            {
				if(command["ID"].Length > 0 && command["Command"].Length >0 && false == command["Command"].Equals("#VALUE"))
				{
					//if(c == 'Y' || c == 'y')
					{
						string strCMD = command["Command"];
						List<byte[]> listData = new List<byte[]>();
                        listData.Add(StringToByteArray(strCMD));
						canTalk.SendMessages(listData, Convert.ToUInt32(command["ID"], 16));
					}
				}
				else
				{
					Console.WriteLine(string.Format("Skip command at Time: {0}", command["Time"]));
				}
			}
			catch(Exception ex)
			{
				Console.WriteLine(string.Format("Error in SendCommand"));
			}

			return;
        }
        private static void ReceiveMessage(CANComm canTalk, Dictionary<string, string> command)
        {
			CAN_OBJ canObj = new CAN_OBJ();

			try
			{
				if(true == canTalk.ReceiveSingleMessage(out canObj, 1000))
				{
					if(false == command["ID"].ToLower().Equals(canObj.ID.ToString().ToLower()))
					{
						Console.WriteLine(string.Format("ID Mismatch, expected ID: {0}", command["ID"]));
						Console.WriteLine(string.Format("Get frame, ID[{0}],DataLen[{1:X2}],Data[{2}]", canObj.ID,
										canObj.DataLen,
										BitConverter.ToString(canObj.data).Replace("-", string.Empty)));
					}
					else
					{
						Console.WriteLine(string.Format("Get frame, ID[{0}],DataLen[{1:X2}],Data[{2}]", canObj.ID,
										canObj.DataLen,
										BitConverter.ToString(canObj.data).Replace("-", string.Empty)));
					}
				}
				else
				{
					Console.WriteLine(string.Format("Error in ReceiveMessage from expected ID[{0}]", command["ID"]));
				}
			}
			catch(Exception ex)
			{
				Console.WriteLine(string.Format("General Error in ReceiveMessage from ID[{0}]", command["ID"]));
			}
            return;
        }

        private static string CommandInfo(Dictionary<string, string> command)
        {
            return string.Format("Time={0},Unknown1={1},ID={2},TxRx={3},FrameType={4},DataLen={5},Command={6}",
                                command["Time"],
                                command["Unknown1"],
                                command["ID"],
                                command["TxRx"],
                                command["FrameType"],
                                command["DataLen"],
                                command["CommandOrg"]);
        }

        private static string CommandInfo(CAN_OBJ canObj)
        {
            return string.Format("FrameInfo:,ID={0},Data={1}",canObj.ID, BitConverter.ToString(canObj.data).Replace("-", string.Empty));
        }
        private static List<Dictionary<string, string>> LoadCommandFile(string commandFile)
        {
            List<Dictionary<string, string>> listCommand = new List<Dictionary<string, string>>();
            string[] lines = null;

            try
            {
                lines = File.ReadAllLines(commandFile);
                foreach (string line in lines)
                {
                    Dictionary<string, string> ACommand = GetACommand(line);
                    if (ACommand.Count > 0)
                    {
                        listCommand.Add(ACommand);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Error in load vector command file with message: {0}", ex.Message));
            }
            return listCommand;
        }
        /// <summary>
        /// load simple commands file. Example of command line: 12b,1122334455667788
        /// </summary>
        /// <param name="commandFile">simple command list file</param>
        /// <returns></returns>
        private static List<string[]> LoadSimpleCommandFileToCanList(string commandFile)
        {
            string[] lines = null;
            List<string[]> listCommand = new List<string[]>();

            try
            {
                lines = File.ReadAllLines(commandFile);
                foreach (string line in lines)
                {
                    string[] strSplitted = line.Split(',');
                    string[] strPara = new string[2];
                    strPara[0] = strSplitted[0].Trim();
                    strPara[1] = strSplitted[1].Trim();
                    listCommand.Add(strPara);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Error in load simple command file with message: {0}", ex.Message));
            }
            return listCommand;
        }

        private static List<CAN_OBJ> LoadVectorCommandFileToCanList(string commandFile)
        {
            List<CAN_OBJ> listCommand = new List<CAN_OBJ>();
            string[] lines = null;

            try
            {
                lines = File.ReadAllLines(commandFile);
                foreach (string line in lines)
                {
                    Dictionary<string, string> command = GetACommand(line);
                    if (command["TxRx"].Equals("Tx") && false == command["Command"].Equals("#VALUE"))
                    {
                        listCommand.Add(CommandToCanObj(command));
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Error in load command file with message: {0}", ex.Message));
            }
            return listCommand;
        }

        private static Dictionary<string, string> GetACommand(string line)
        {
            Dictionary<string, string> command = new Dictionary<string, string>();
            string[] strElements = line.Split(',');
            for (int i = 0; i < strElements.Length; i++)
            {
                switch (i)
                {
                    case 0:
                        command.Add("Time", strElements[i].Trim());
                        break;
                    case 1:
                        command.Add("Unknown1", strElements[i].Trim());
                        break;
                    case 2:
                        command.Add("ID", strElements[i].Trim());
                        break;
                    case 3:
                        command.Add("TxRx", strElements[i].Trim());
                        break;
                    case 4:
                        command.Add("FrameType", strElements[i].Trim());
                        break;
                    case 5:
                        command.Add("DataLen", strElements[i].Trim());
                        break;
                    case 6:
                        command.Add("CommandOrg", strElements[i].Trim());
                        break;
                    case 7:
                        if (true == strElements[i].Trim().ToUpper().StartsWith("DATA"))
                        {
                            string strtemp1 = strElements[i].Substring(4);
                            command.Add("Command", strtemp1);
                        }
                        break;
                    default:
                        Console.WriteLine(string.Format("Unknown line: {0}", line));
                        break;
                }
            }
            return command;
        }

        private static void Receive(CANComm canTalk)
        {
            try
            {
                //			  Thread.Sleep(10000);
                List<CAN_OBJ> listRes = null;

                bool bReceive = false;
                bReceive = canTalk.ReceiveMessages(out listRes, 10000);
                if (true == bReceive)
                {
                    Console.WriteLine("Press D for detailed data including ID, or Press d for data part only, or Press N to quit.");
                    char cRequired = (char)Console.Read();

                    bool bAllData = false;
                    if (true == cRequired.Equals('D'))
                    {
                        bAllData = true;
                    }
                    else if (true == cRequired.Equals('d'))
                    {
                        bAllData = false;
                    }
                    else if (true == cRequired.Equals('n') || true == cRequired.Equals('N'))
                    {

                        Console.WriteLine("Unrecgized command. quit");
                        return;
                    }
                    Console.WriteLine(string.Format("Received total {0} can objects:", listRes.Count));
                    if (bAllData == false) // data part only
                    {
                        foreach (CAN_OBJ obj in listRes)
                        {
                            string line = string.Empty;
                            for (int i = 0; i < obj.DataLen; i++)
                            {
                                line += obj.data[i].ToString("X2");
                            }
                            Console.WriteLine(line);
                        }
                    }
                    else//detailed data
                    {
                        Console.WriteLine("Data format:[ID]:,[Data],[SendType],[TimeFlag],[TimeStamp],[Remoteflag]");
                        foreach (CAN_OBJ obj in listRes)
                        {
                            byte byteSendType = obj.SendType;
                            byte byteTimeFlag = obj.TimeFlag;
                            uint uiTimeStamp = obj.TimeStamp;
                            byte byteRemoteFlag = obj.RemoteFlag;
                            uint uiID = obj.ID;
                            string strData = string.Empty;
                            for (int i = 0; i < obj.DataLen; i++)
                            {
                                strData += obj.data[i].ToString("X2");
                            }
                            string line = string.Format("{0:X8}H:,{1}H,{2},{3},{4},{5}", uiID, strData, byteSendType, byteTimeFlag, uiTimeStamp, byteRemoteFlag);
                            Console.WriteLine(line);
                        }
                    }
                }
                else
                {
                    Console.WriteLine(string.Format("Error in Receive"));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error Info: {0}", ex.Message);
                Console.WriteLine("Press any key to quit");
                Console.Read();
            }
            return;
        }
        private static void DefaultNoArgu()
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
			
			//get command
			byte[] command = new byte[8];
			command[0] = 0x11;
			command[1] = 0x22;
			command[2] = 0x33;
			command[3] = 0x44;
			command[4] = 0x55;
			command[5] = 0x66;
			command[6] = 0x77;
			command[7] = 0x88;
			Console.WriteLine("Please input command to device. Directly press enter for demo command 0x1122334455667788: ");
			string strSend = (string)Console.ReadLine();
			if (strSend.Length > 0)
			{
				command = StringToByteArray(strSend);
			}
			
			//get receive info
			Console.WriteLine("Please input CAN ID you want to listen. Listening all devices by empty:");
			string strListen = (string)Console.ReadLine();
			uint uiCanID = 0;
			if (strListen.Length > 1)
			{
				uiCanID = Convert.ToUInt32(strListen, 16);
			}
			
			Console.Write(string.Format("Press Enter to send command 0x{0} :", BitConverter.ToString(command).Replace("-", string.Empty)));
			Console.ReadLine();
			
			List<byte[]> listData = new List<byte[]>();
			listData.Add(command);
			CanTalk.SendMessages(listData);
			
			try
			{
				//			  Thread.Sleep(10000);
				List<CAN_OBJ> listRes = null;
			
				bool bReceive = false;
				if (uiCanID != 0)
				{
					bReceive = CanTalk.ReceiveMessages(out listRes, uiCanID, 10000);
				}
				else
				{
					bReceive = CanTalk.ReceiveMessages(out listRes, 10000);
				}
				if (true == bReceive)
				{
					Console.WriteLine("Press D for detailed data including ID, or Press d for data part only");
					char cRequired = (char)Console.Read();
			
					bool bAllData = false;
					if (true == cRequired.Equals('D'))
					{
						bAllData = true;
					}
					else if (true == cRequired.Equals('d'))
					{
						bAllData = false;
					}
					else
					{
			
						Console.WriteLine("Unrecgized command. Show data part of frame(s) only");
					}
					Console.WriteLine(string.Format("Received total {0} can objects:", listRes.Count));
					if (bAllData == false) // data part only
					{
						foreach (CAN_OBJ obj in listRes)
						{
							string line = string.Empty;
							for (int i = 0; i < obj.DataLen; i++)
							{
								line += obj.data[i].ToString("X2");
							}
							Console.WriteLine(line);
						}
					}
					else//detailed data
					{
						Console.WriteLine("Data format:[ID]:,[Data],[SendType],[TimeFlag],[TimeStamp],[Remoteflag]");
						foreach (CAN_OBJ obj in listRes)
						{
							byte byteSendType = obj.SendType;
							byte byteTimeFlag = obj.TimeFlag;
							uint uiTimeStamp = obj.TimeStamp;
							byte byteRemoteFlag = obj.RemoteFlag;
							uint uiID = obj.ID;
							string strData = string.Empty;
							for (int i = 0; i < obj.DataLen; i++)
							{
								strData += obj.data[i].ToString("X2");
							}
							string line = string.Format("{0:X8}H:,{1}H,{2},{3},{4},{5}", uiID, strData, byteSendType, byteTimeFlag, uiTimeStamp, byteRemoteFlag);
							Console.WriteLine(line);
						}
					}
				}
				else
				{
					Console.WriteLine(string.Format("Error in Receive"));
				}
			
				int iSleep = 20;
				Console.WriteLine(string.Format("Device will be auto closed within {0}s.", iSleep));
				Thread.Sleep(iSleep*1000);
			
				if (false == CanTalk.CloseDevice())
				{
					Console.WriteLine(string.Format("Failed at close"));
				}
				else
				{
					Console.WriteLine(string.Format("Device closed!"));
					//Console.WriteLine(string.Format("See you again"));
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error Info: {0}", ex.Message);
				Console.WriteLine("Press any key to quit");
				Console.Read();
			}
			return;
		}
        public static byte[] StringToByteArray(string hex)
        {
            if (hex.ToLower().StartsWith("0x"))
            {
                hex = hex.Substring(2);
            }

            if ((hex.Length % 2) == 1)
            {
                hex = "0" + hex;
            }
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }
    }

	public class CommandLineArgument
	{
		List<CommandLineArgument> _arguments;

		int _index;

		string _argumentText;

		public CommandLineArgument Next
		{
			get
			{
				if (_index < _arguments.Count - 1)
				{
					return _arguments[_index + 1];
				}

			return null;
			}
		}
		public CommandLineArgument Previous
		{
			get
			{
				if (_index > 0)
				{
					return _arguments[_index - 1];
				}

				return null;
			}
		}
		internal CommandLineArgument(List<CommandLineArgument> args, int index, string argument)
		{
			_arguments = args;
			_index = index;
			_argumentText = argument;
		}

		public CommandLineArgument Take()
		{
			return Next;
		}

		public IEnumerable<CommandLineArgument> Take(int count)
		{
			var list = new List<CommandLineArgument>();
			var parent = this;
			for (int i = 0; i < count; i++)
			{
				var next = parent.Next;
				if (next == null)
					break;

				list.Add(next);

				parent = next;
			}

			return list;
		}

		public static implicit operator string(CommandLineArgument argument)
		{
			return argument._argumentText;
		}

		public override string ToString()
		{
			return _argumentText;
		}
	}

	public class CommandLineArgumentParser
	{
		List<CommandLineArgument> _arguments;
		public static CommandLineArgumentParser Parse(string[] args)
		{
			return new CommandLineArgumentParser(args);
		}

		public CommandLineArgumentParser(string[] args)
		{
			_arguments = new List<CommandLineArgument>();

			for (int i = 0; i < args.Length; i++)
			{
			_arguments.Add(new CommandLineArgument(_arguments,i,args[i]));
			}

		}

		public CommandLineArgument Get(string argumentName)
		{
			return _arguments.FirstOrDefault(p => p == argumentName);
		}

		public bool Has(string argumentName)
		{
			return _arguments.Count(p=>p==argumentName)>0;
		}
	}
}
