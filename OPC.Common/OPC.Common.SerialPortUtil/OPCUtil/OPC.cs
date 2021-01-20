using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using OPC.SerialPortUtil.Model;
using OPCAutomation;

namespace OPC.SerialPortUtil.OPCUtil
{
    public class OPC
    {
        private static OPCSerialInfoModel _opcSerialInfo;
        private static OPC _instance;
        private static string _kepServerName;

        public delegate void ReadDataEventHandler(int TransactionID, int NumItems, ref Array ClientHandles,
            ref Array ItemValues, ref Array Qualities, ref Array TimeStamps);

        private OPC(OPCSerialInfoModel opcSerialInfo)
        {
            _opcSerialInfo = opcSerialInfo;
        }

        public static OPC GetInstance(OPCSerialInfoModel opcSerialInfo)
        {
            if (_instance == null)
            {
                lock ("obj")
                {
                    if (_instance == null)
                    {
                        _instance = new OPC(opcSerialInfo);
                        return _instance;
                    }
                }
            }
            return _instance;
        }
        /// <summary>
        /// 获取KEPServer的服务器名字
        /// </summary>
        /// <returns></returns>
        public string GetOpcServers()
        {
            try
            {
                object opcServers = _opcSerialInfo.OpcServer.GetOPCServers(_opcSerialInfo.HostName);
                foreach (string turn in (Array)opcServers)
                {
                    _kepServerName = turn;
                }
                return _kepServerName;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        /// <summary>
        /// 连接KepServer服务
        /// </summary>
        /// <param name="kepServerName">KEPServer的服务器名字</param>
        /// <param name="hostIp">本机IP地址</param>
        /// <returns></returns>
        public bool ConnectServer(string kepServerName)
        {
            try
            {
                _opcSerialInfo.OpcServer.Connect(kepServerName, _opcSerialInfo.HostIp);
                return true;
            }
            catch (Exception e)
            {
                //Console.WriteLine(e);
                //throw;
                //为了表示此处有错误，特地在此写下该输出文件代码
                using (FileStream fileStream = new FileStream("d:\\data.txt", FileMode.Append))
                {
                    using (StreamWriter writer = new StreamWriter(fileStream))
                    {
                            Console.WriteLine("-----------------------------------------------------------------");
                            writer.WriteAsync($"{e}\n");
                    }
                }
                return false;
            }
        }
        /// <summary>
        /// 关闭KepServer服务
        /// </summary>
        public void CloseConnect()
        {
            if (_opcSerialInfo.OpcServer != null)
            {
                _opcSerialInfo.OpcServer.OPCGroups.RemoveAll();
                _opcSerialInfo.OpcServer.Disconnect();
            }
        }

        /// <summary>
        /// 获取对应的分支点
        /// </summary>
        /// <param name="filter">点名字</param>
        public List<OpcData> RecurBrowse(string[] filter)
        {
            OPCBrowser opcBrowser = _opcSerialInfo.OpcServer.CreateBrowser();
            List<OpcData> datas = new List<OpcData>();
            //展开分支
            opcBrowser.ShowBranches();
            //展开叶子
            opcBrowser.ShowLeafs(true);

            foreach (object turn in opcBrowser)
            {
                foreach (var r in filter)
                {
                    if (turn.ToString().ToUpper().Contains(r.ToUpper()))
                    {
                        OpcData data = new OpcData
                        {
                            OpcName = turn.ToString(),
                            OpcValue = "null",
                            OpcTime = DateTime.Now.ToString()
                        };
                        datas.Add(data);
                    }
                }
            }
            return datas;
        }
        /// <summary>
        /// 初始化OPCGroups和OPCItems
        /// </summary>
        /// <param name="opcGroups">OPCGroups对象</param>
        /// <param name="GroupName">自定义组名字</param>
        /// <param name="opcGroup">OPCGroup对象</param>
        /// <param name="opcItems">OPCItems对象</param>
        /// <param name="groupPropertiesModel">GroupPropertiesModel对象</param>
        public void SetGroupsAndItems(out OPCGroups opcGroups, string GroupName, out OPCGroup opcGroup, out OPCItems opcItems, GroupPropertiesModel groupPropertiesModel)
        {
            opcGroups = _opcSerialInfo.OpcServer.OPCGroups;
            opcGroup = opcGroups.Add(GroupName);
            SetGroupProperty(opcGroup, groupPropertiesModel);
            opcItems = opcGroup.OPCItems;
        }
        /// <summary>
        /// 设置组属性
        /// </summary>
        /// <param name="opcGroup">OPCGroup对象</param>
        /// <param name="groupPropertiesModel">GroupPropertiesModel对象</param>
        public void SetGroupProperty(OPCGroup opcGroup, GroupPropertiesModel groupPropertiesModel)
        {
            _opcSerialInfo.OpcServer.OPCGroups.DefaultGroupIsActive = groupPropertiesModel.DefaultGroupIsActive;
            _opcSerialInfo.OpcServer.OPCGroups.DefaultGroupDeadband = groupPropertiesModel.DefaultGroupDeadBand;
            opcGroup.IsActive = groupPropertiesModel.IsActive;
            opcGroup.IsSubscribed = groupPropertiesModel.IsSubscribed;
            opcGroup.UpdateRate = groupPropertiesModel.UpdateRate;
        }
        /// <summary>
        /// 设置服务器和客户端的句柄对应的点位名
        /// 将点位集合存入对应的服务器和客户端的句柄
        /// </summary>
        /// <param name="bindingData">点位集合</param>
        /// <param name="opcItems"></param>
        /// <param name="opcGroup"></param>
        /// <param name="dic">客户端句柄点位字典</param>
        /// <param name="serviceDic">服务器句柄点位字典</param>
        public void SetServerHandle(List<OpcData> bindingData, OPCItems opcItems,
            OPCGroup opcGroup,  Dictionary<int, string> dic, Dictionary<int, string> serviceDic)
        {
            for (int i = 0; i < bindingData.Count; i++)
            {
                try
                {
                    OPCItem item = opcItems.AddItem(bindingData[i].OpcName, i);
                    if (item != null)
                    {
                        dic.Add(item.ClientHandle, bindingData[i].OpcName);
                        serviceDic.Add(item.ServerHandle, bindingData[i].OpcName);
                    }
                }
                catch (Exception e)
                {
                    throw;
                }

                foreach (KeyValuePair<int, string> keyValuePair in serviceDic)
                {
                    try
                    {
                        ReadOpcValue(keyValuePair.Key, opcItems, opcGroup, bindingData, serviceDic);
                    }
                    catch (Exception e)
                    {
                        // ignored
                    }
                }

            }
        }
        /// <summary>
        /// 第一次加载分支根据句柄读取数据
        /// </summary>
        /// <param name="handle">分支句柄</param>
        /// <param name="opcItems"></param>
        /// <param name="opcGroup"></param>
        /// <param name="bindingData"></param>
        /// <param name="serviceDic"></param>
        private void ReadOpcValue(int handle, OPCItems opcItems, OPCGroup opcGroup, List<OpcData> bindingData, Dictionary<int, string> serviceDic)
        {
            OPCItem bItem = opcItems.GetOPCItem(handle);
            int[] temp = new int[2] { 0, bItem.ServerHandle };
            Array serverHandles = (Array)temp;
            Array Errors;
            int cancelID;
            opcGroup.AsyncRead(1, ref serverHandles, out Errors, 2009, out cancelID);
            OpcData data = bindingData.FirstOrDefault(x => x.OpcName == serviceDic[handle]);
            data.OpcValue = bItem.Value.ToString();
            data.OpcTime = DateTime.Now.ToString();
        }

        /// <summary>
        /// 填写数据改变事件
        /// </summary>
        /// <param name="opcGroup">OPCGroup添加DataChange事件</param>
        public void AddDataChangeEvent(OPCGroup opcGroup, ReadDataEventHandler eventHandler)
        {
            if (opcGroup != null)
            {
                opcGroup.DataChange += new DIOPCGroupEvent_DataChangeEventHandler(eventHandler);
            }
        }
        /// <summary>
        /// 卸载数据改变事件
        /// </summary>
        /// <param name="opcGroup">OPCGroup卸载DataChange事件</param>
        public void RemoveDataChangeEvent(OPCGroup opcGroup, ReadDataEventHandler eventHandler)
        {
            if (opcGroup != null)
            {
                opcGroup.DataChange -= new DIOPCGroupEvent_DataChangeEventHandler(eventHandler);
            }
        }

        

        //void opcGroup_DataChange(int TransactionID, int NumItems, ref Array ClientHandles, ref Array ItemValues, ref Array Qualities, ref Array TimeStamps)
        //{
        //    for (int i = 1; i <= NumItems; i++)
        //    {
        //        string opcName = _dic[(int)ClientHandles.GetValue(i)];
        //        OpcData data = _bindingData.FirstOrDefault(x => x.OpcName == opcName);
        //        if (data != null)
        //        {
        //            data.OpcValue = ItemValues.GetValue(i).ToString();
        //            data.OpcTime = DateTime.Now.ToString();
        //            OpcModel model = new OpcModel()
        //            {
        //                DateTime = DateTime.Now.ToString(),
        //                OpcValue = data.OpcValue
        //            };
        //            string serializeToString = JsonSerializer.SerializeToString(model);
        //            _client.Lists[opcName].Push(serializeToString);
        //            _client.Expire(opcName, 50);//50秒过期
        //        }
        //    }
        //}
    }
}