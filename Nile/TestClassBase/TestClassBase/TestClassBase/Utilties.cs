using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nile
{
    class Utilties
    {
        public static bool GetInput(string File, string Module, string Method, string InputName, ref int Input)
        {
            try
            {
                JToken jtInput = Read(File, Module, Method, InputName);
                if (jtInput.Type == JTokenType.Integer)
                {
                    Input = jtInput.Value<int>();
                    return true;
                }
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("[TestBaseClass][Utilties][GetInput][int]:Can't get value. {0}", ex.Message));
            }
            return false;
        }
        public static bool GetInput(string File, string Module, string Method, string InputName, ref double Input)
        {
            try
            {
                JToken jtInput = Read(File, Module, Method, InputName);
                if (jtInput.Type == JTokenType.Float)
                {
                    Input = jtInput.Value<double>();
                    return true;
                }
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("[TestBaseClass][Utilties][GetInput][int]:Can't get value. {0}", ex.Message));
            }
            return false;
        }
        public static bool GetInput(string File, string Module, string Method, string InputName, ref string Input)
        {
            try
            {
                JToken jtInput = Read(File, Module, Method, InputName);
                if (jtInput.Type == JTokenType.String)
                {
                    Input = jtInput.Value<string>();
                    return true;
                }
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("[TestBaseClass][Utilties][GetInput][int]:Can't get value. {0}", ex.Message));
            }
            return false;
        }
        public static bool GetInput(string File, string Module, string Method, string InputName, ref bool Input)
        {
            try
            {
                JToken jtInput = Read(File, Module, Method, InputName);
                if (jtInput.Type == JTokenType.Boolean)
                {
                    Input = jtInput.Value<bool>();
                    return true;
                }
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("[TestBaseClass][Utilties][GetInput][int]:Can't get value. {0}", ex.Message));
            }
            return false;
        }

        private static JToken Read(string File, string Module, string Method, string InputName)
        {
            JToken jtFile;
            JToken jtModule;
            JToken jtMethod;
            JToken jtInput;
            try
            {
                try
                {
                    StreamReader file = System.IO.File.OpenText(File);
                    JsonTextReader reader = new JsonTextReader(file);
                    jtFile = JToken.ReadFrom(reader);
                    reader.Close();
                    file.Close();
                }
                catch (Exception ex)
                {
                    throw new Exception(string.Format("[OpenLoad] {0}", ex.Message));
                }

                try
                {
                    if (jtFile.HasValues)
                    {
                        jtModule = jtFile[Module];
                    }
                    else
                    {
                        throw new Exception(string.Format("Can't find {0}", Module));
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception(string.Format("[Module] {0}", ex.Message));
                }

                try
                {
                    if (jtModule.HasValues)
                    {
                        jtMethod = jtModule[Method];
                    }
                    else
                    {
                        throw new Exception(string.Format("Can't find {0}", Method));
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception(string.Format("[Method] {0}", ex.Message));
                }

                try
                {
                    if (jtMethod.HasValues)
                    {
                        JObject joMethod = (JObject)jtMethod;
                        if (true == joMethod.ContainsKey(InputName))
                        {
                            jtInput = joMethod[InputName];
                            return jtInput;
                            //return jtMethod;//return the method to get value from []
                        }
                        else
                        {
                            throw new Exception(string.Format("Can't find {0}", InputName));
                        }
                    }
                    else
                    {
                        throw new Exception(string.Format("No sub nodes under {0}", Method));
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception(string.Format("[Input] {0}", ex.Message));
                }
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("[Read]{0}", ex.Message));
            }
        }
    }
}
