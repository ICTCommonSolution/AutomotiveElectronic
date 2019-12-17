using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Nile.Definitions;
using System.Collections;

namespace Nile
{
    public class TestClassBase : ITestClass
    {
        protected int refID;
        protected Type instanceType;
        protected object testInstance;
        public ArrayList Input  {get; protected set;}
        internal int CreateInstance()
        {
            try
            {
                this.testInstance = Activator.CreateInstance(this.instanceType);
            }
            catch (Exception exception)
            {
                //BridgeExceptionCatch.ReportException(new ResourceLoaderException(exception.InnerException.Message), this.instanceType.FullName);
                throw new Exception(exception.InnerException.Message + this.instanceType.FullName);
            }
            return this.refID;
        }
        public int Do()
        {
            int num;
            if (this.testInstance == null)
            {
                this.CreateInstance();
            }
            Type type = this.testInstance.GetType();
            try
            {
                num = (int)type.InvokeMember("Do", BindingFlags.InvokeMethod, null, this.testInstance, null);
            }
            catch (MissingMethodException)
            {
                throw new Exception("Measure method is missing. Explicit implementation of testmethod interface not allowed");
            }
            return num;
        }

        protected void GetInput(string SettingFile, string Module, string Method, string InputName, ref int Input)
        {
            if (false == System.IO.File.Exists(SettingFile))
            {
                throw new Exception(string.Format("[TestClassBase][LoadSetting]:{0} does not exist", SettingFile));
            }
            Utilties.GetInput(SettingFile, Module, Method, InputName, ref Input);
        }

        protected void GetInput(string SettingFile, string Module, string Method, string InputName, ref double Input)
        {
            if (false == System.IO.File.Exists(SettingFile))
            {
                throw new Exception(string.Format("[TestClassBase][LoadSetting]:{0} does not exist", SettingFile));
            }
            Utilties.GetInput(SettingFile, Module, Method, InputName, ref Input);
        }

        protected void GetInput(string SettingFile, string Module, string Method, string InputName, ref string Input)
        {
            if (false == System.IO.File.Exists(SettingFile))
            {
                throw new Exception(string.Format("[TestClassBase][LoadSetting]:{0} does not exist", SettingFile));
            }
            Utilties.GetInput(SettingFile, Module, Method, InputName, ref Input);
        }

        protected void GetInput(string SettingFile, string Module, string Method, string InputName, ref bool Input)
        {
            if (false == System.IO.File.Exists(SettingFile))
            {
                throw new Exception(string.Format("[TestClassBase][LoadSetting]:{0} does not exist", SettingFile));
            }
            Utilties.GetInput(SettingFile, Module, Method, InputName, ref Input);
        }
    }
}
