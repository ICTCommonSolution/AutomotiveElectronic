using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using Nile.CommonInstrument;
using Nile.Instruments.CAN;
using TestClass;
using TestClass.SWS;
using Nile.Log;
using Nile.Definitions;
using System.Reflection;

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
                //if (false == File.Exists(arguments.Get("-c").Next))
                //{
                //    Console.WriteLine("The config file {0} dose not exist", arguments.Get("-c").Next);
                //}
                //else
                //{
                //    Console.WriteLine("Loading config {0} and init device", arguments.Get("-c").Next);
                //}
            }
            else if (arguments.Has("-c") && arguments.Has("-s"))
            {
            }
            else if (arguments.Has("-t") && arguments.Has("-k"))
            {
                //check if config file exists
                //Press key = new Press();
                //key.Do();

                //Thread.Sleep(10000);

                FileInfo fi = new FileInfo( Assembly.GetExecutingAssembly().Location);
                string strPath = fi.DirectoryName + "\\";
                NileLogger logger = new NileLogger(string.Format("{1}{0}.txt", DateTime.Now.ToString("yyyy-MM-dd_HHmmss"), strPath));

                PressAll ki = new PressAll();
                //classAA.AA_Myevent += new ClassAA.A_DelegateEventHander(classBB.change);
                ki.eventSent2Log += new PressAll.Send2LogEventHanler(logger.OnReceiveLogInfo);

                ki.Do();

                Thread.Sleep(1000);
            }
            else if (args.Count() == 0)
            {
                Console.WriteLine("Run without argu as default");
            }
            else
            { }

            Console.WriteLine("Sent over");
            Console.Write("Press any key to exit");
            Console.Read();

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
