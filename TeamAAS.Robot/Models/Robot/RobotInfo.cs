using Newtonsoft.Json;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeamAAS.Robot.Enums;
using TeamAAS.Robot.Models.PLC;

namespace TeamAAS.Robot.Models
{
    /// <summary>
    /// 机器人参数
    /// </summary>
    public class RobotInfo : BindableBase
    {
        private Guid _Id;

        public Guid Id
        {
            get { return _Id; }
            set { SetProperty(ref _Id, value); }
        }

        private int _RobotNo;
        /// <summary>
        /// 机器人编号
        /// </summary>
        public int RobotNo
        {
            get { return _RobotNo; }
            set { SetProperty(ref _RobotNo, value); }
        }

        private string _RobotName;
        /// <summary>
        /// 机器人名称
        /// </summary>
        public string RobotName
        {
            get { return _RobotName; }
            set { SetProperty(ref _RobotName, value); }
        }

        private RobotBrand _RobotBrand = RobotBrand.Default;
        /// <summary>
        /// 机器人品牌
        /// </summary>
        public RobotBrand RobotBrand
        {
            get { return _RobotBrand; }
            set { SetProperty(ref _RobotBrand, value); }
        }

        private TCPConnectType _ConnectType = TCPConnectType.Client;
        /// <summary>
        /// 通讯方式
        /// </summary>
        public TCPConnectType ConnectType
        {
            get { return _ConnectType; }
            set { SetProperty(ref _ConnectType, value); }
        }

        private string _IP = "192.168.0.1";
        /// <summary>
        /// IP地址
        /// </summary>
        public string IP
        {
            get { return _IP; }
            set { SetProperty(ref _IP, value); }
        }

        private int _Port = 3600;
        /// <summary>
        /// 端口号
        /// </summary>
        public int Port
        {
            get { return _Port; }
            set { SetProperty(ref _Port, value); }
        }

        private Terminator _Terminator = Terminator.CRLF;
        /// <summary>
        /// 结束符
        /// </summary>
        public Terminator Terminator
        {
            get { return _Terminator; }
            set { SetProperty(ref _Terminator, value); }
        }

        private DataEncoding _DataEncoding=DataEncoding.Default;

        /// <summary>
        /// 数据编码格式
        /// </summary>
        public DataEncoding DataEncoding
        {
            get { return _DataEncoding; }
            set { SetProperty(ref _DataEncoding, value); }
        }

        private double _StepDistance=1.0;
        /// <summary>
        /// 步进距离
        /// </summary>
        public double StepDistance
        {
            get { return _StepDistance; }
            set { SetProperty(ref _StepDistance, value); }
        }

        private Guid _PLC;
        /// <summary>
        /// 对应的PLC
        /// </summary>
        public Guid PLC
        {
            get { return _PLC; }
            set { SetProperty(ref _PLC, value); }
        }

        private PlcRobotParameter _PlcRobotParameter=new PlcRobotParameter(); 
        public PlcRobotParameter PlcRobotParameter
        {
            get { return _PlcRobotParameter; }
            set { SetProperty(ref _PlcRobotParameter, value); }
        }

        public RobotInfo Clone()
        {
            return JsonConvert.DeserializeObject<RobotInfo>(JsonConvert.SerializeObject(this)); ;
        }
    }
}
