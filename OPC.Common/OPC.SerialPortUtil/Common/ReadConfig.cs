using System.Configuration;

namespace OPC.SerialPortUtil.Common
{
    public class ReadConfig
    {
        public static string[] GetStrArray(Configuration configuration, string settingName)//读取配置
        {
            string[] split = configuration.AppSettings.Settings[settingName].Value.Split(',');
            for (int i = 0; i < split.Length; i++)
            {
                split[i] = split[i].Trim();
            }
            return split;
        }

        public static string GetStr(Configuration configuration, string settingName)
        {
            string value = configuration.AppSettings.Settings[settingName].Value;
            return value;
        }
    }
}