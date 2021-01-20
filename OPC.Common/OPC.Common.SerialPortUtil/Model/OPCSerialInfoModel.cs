using OPCAutomation;

namespace OPC.SerialPortUtil.Model
{
    public class OPCSerialInfoModel
    {
        public OPCServer OpcServer { get; set; }
        public string HostIp { get; set; }
        public string HostName { get; set; }
        public bool DefaultGroupIsActive { get; set; } = true;
        public int DefaultGroupDeadBand { get; set; } = 0;
        public bool IsActive { get; set; } = true;
        public bool IsSubscribed { get; set; } = true;
        public int UpdateRate { get; set; } = 1000;
    }
}