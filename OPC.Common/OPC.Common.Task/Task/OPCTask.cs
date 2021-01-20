using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using OPC.SerialPortUtil.Common;
using OPC.SerialPortUtil.Model;
using OPCAutomation;
using Quartz;

namespace OPC.Common.Task.Task
{
    public class OPCTask : IJob
    {
        private static OPCGroups _opcGroups;
        private static string _groupName = "newGroupName";
        private static OPCGroup _opcGroup;
        private static OPCItems _opcItems;
        public static Configuration Configuration =
            ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        private static Dictionary<int, string> dic = new Dictionary<int, string>();
        private static Dictionary<int, string> serivceDic = new Dictionary<int, string>();
        private static List<OpcData> opcDatas;

        public async System.Threading.Tasks.Task Execute(IJobExecutionContext context)
        {
            
            string hostIp = string.Empty;
            string hostName = string.Empty;
            IPHostEntry ipHost = Dns.GetHostEntry(Environment.MachineName);
            if (ipHost.AddressList.Length > 0)
            {
                hostIp = ipHost.AddressList[0].ToString();
                IPHostEntry ipHostName = Dns.GetHostEntry(hostIp);
                hostName = ipHostName.HostName;
            }
            OPC.SerialPortUtil.OPCUtil.OPC instance = OPC.SerialPortUtil.OPCUtil.OPC.GetInstance(new OPCSerialInfoModel()
            {
                DefaultGroupDeadBand = 0,
                DefaultGroupIsActive = true,
                IsActive = true,
                IsSubscribed = true,
                UpdateRate = 500,
                OpcServer = new OPCServer(),
                HostIp = hostIp,
                HostName = hostName,
            });
            string opcServers = instance.GetOpcServers();
            bool flag = instance.ConnectServer(opcServers);
            if (flag)
            {
                opcDatas = instance.RecurBrowse(ReadConfig.GetStrArray(Configuration, "filter"));
                instance.SetGroupsAndItems(out _opcGroups, _groupName, out _opcGroup, out _opcItems,
                    new GroupPropertiesModel()
                    {
                        DefaultGroupDeadBand = 0,
                        DefaultGroupIsActive = true,
                        IsActive = true,
                        IsSubscribed = true,
                        UpdateRate = 500,
                    });
                instance.SetServerHandle(opcDatas, _opcItems, _opcGroup, dic, serivceDic);
                instance.AddDataChangeEvent(_opcGroup,
                    new OPC.SerialPortUtil.OPCUtil.OPC.ReadDataEventHandler(opcGroup_DataChange));
                Thread.Sleep(10000);
                instance.SetGroupProperty(_opcGroup, new GroupPropertiesModel()
                {
                    DefaultGroupDeadBand = 0,
                    DefaultGroupIsActive = true,
                    IsActive = true,
                    IsSubscribed = true,
                    UpdateRate = 1500,
                });
                Thread.Sleep(10000);
                instance.RemoveDataChangeEvent(_opcGroup,
                    new OPC.SerialPortUtil.OPCUtil.OPC.ReadDataEventHandler(opcGroup_DataChange));
                instance.CloseConnect();
                _opcGroups = null;
                _opcGroup = null;
                _opcItems = null;
                dic.Clear();
                serivceDic.Clear();
                opcDatas = null;
            }
            else
            {
                Console.WriteLine("连接失败");
            }
            
            
        }

        /// <summary>
        /// 每次改变的数据都会被写入opcDatas集合，将当前句柄的值索引集合，然后操作数据curd
        /// </summary>
        /// <param name="TransactionID"></param>
        /// <param name="NumItems"></param>
        /// <param name="ClientHandles">客户端</param>
        /// <param name="ItemValues">数据值</param>
        /// <param name="Qualities">品质</param>
        /// <param name="TimeStamps">数据产生时的时间</param>
        private static void opcGroup_DataChange(int TransactionID, int NumItems, ref Array ClientHandles,
            ref Array ItemValues, ref Array Qualities, ref Array TimeStamps)
        {
            for (int i = 1; i <= NumItems; i++)
            {
                string opcName = dic[(int) ClientHandles.GetValue(i)];
                OpcData data = opcDatas.FirstOrDefault(x => x.OpcName == opcName);
                //更新集合的数据
                if (data != null)
                {
                    data.OpcValue = ItemValues.GetValue(i).ToString();
                    data.OpcTime = TimeStamps.GetValue(i).ToString();
                    //data.OpcTime = DateTime.Now.ToString();
                    //Console.WriteLine($"点位名：{opcName}，读取值：{data.OpcValue}，读取时间：{DateTime.Now.ToString()}");
                    //OpcModel model = new OpcModel()
                    //{
                    //    DateTime = DateTime.Now.ToString(),
                    //    OpcValue = data.OpcValue
                    //};
                    //string serializeToString = JsonSerializer.SerializeToString(model);
                    //_client.Lists[opcName].Push(serializeToString);
                    //_client.Expire(opcName, 50);//50秒过期
                }
            }
            //以下代码用于观察读取数据情况，数据会输出再d:\\OPCData.txt
            foreach (OpcData opcData in opcDatas)
            {
                Console.WriteLine("-----------------------------------------------------------------");
                Console.WriteLine($"{opcData.OpcName}--{opcData.OpcValue}--{opcData.OpcTime}");
            }
            using (FileStream fileStream = new FileStream("d:\\OPCData.txt",FileMode.Append))
            {
                using (StreamWriter writer = new StreamWriter(fileStream))
                {
                    foreach (OpcData opcData in opcDatas)
                    {
                        Console.WriteLine("-----------------------------------------------------------------");
                        writer.WriteAsync($"{opcData.OpcName}--{opcData.OpcValue}--{opcData.OpcTime}\n");
                    }
                }
            }
        }
    }
}