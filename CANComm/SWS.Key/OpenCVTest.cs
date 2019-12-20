using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nile;
using CAN;
using System.Threading;
using System.Reflection;
using OpenCvSharp;

namespace TestClass.SWS
{
    public class OpenCVTest : TestClassBase
    {
        public OpenCVTest()
        {//do nothing
        }

        public int Do()
        {
            Mat xxx = new Mat(@"d:\Desktop\1.PNG", ImreadModes.AnyColor);
            return 1;
        }
    }

}
