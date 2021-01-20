using System.Collections.Generic;
using System.IO;

namespace OPC.SerialPortUtil.Model
{
    public class SerialDataInfoModel<K,V>
    {
        public Dictionary<K,V> Data { get; set; }
    }
}