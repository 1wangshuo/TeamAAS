using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeamAAS.Robot.Enums;
using TeamAAS.Robot.Models;
using TeamAAS.Robot.Models.Robot;
using TeamAAS.Robot.Models;
using TouchSocket.Sockets;

namespace TeamAAS.Robot.Interfaces
{
    public interface IRobot : IDisposable
    {
        #region 基础信息
        /// <summary>
        /// 客户端
        /// </summary>
        TcpClient TcpClient { get; }
        /// <summary>
        /// 服务器
        /// </summary>
        TcpService TcpService { get; }
        /// <summary>
        /// 机器人ID
        /// </summary>
        Guid Id { get; set; }

        string Name { get; set; }

        /// <summary>
        /// 机器人编号
        /// </summary>
        int RobotNo { get; set; }

        /// <summary>
        /// 机器人端口
        /// </summary>
        int RobotPort { get; }
        /// <summary>
        /// 机器人IP地址
        /// </summary>
        string RobotIp { get; }
        /// <summary>
        /// 机器人通讯方式
        /// </summary>
        TCPConnectType ConnectType { get; }
        /// <summary>
        /// 结束符
        /// </summary>
        Terminator Terminator { get; }
        /// <summary>
        /// 通讯数据编码
        /// </summary>
        DataEncoding DataEncoding { get; }

        RobotBrand Brand { get; }

        event Action<Guid, object, ConnectedEventArgs> ConnectedEvent;
        event Action<Guid, object, ClosedEventArgs> DisconnectedEvent;
        event Action<Guid, object, ReceivedDataEventArgs> ReceivedEvent;
        event Action<Guid, object, string> SendEvent;

        bool CanExecute { get; }

        int SelectedTool { get; }

        /// <summary>
        /// 进入调试模式
        /// </summary>
        /// <returns></returns>
        bool EnterDebugMode { get; set; }

        #endregion

        #region 连接通讯
        /// <summary>
        /// 机器人连接状态
        /// </summary>
        bool IsConnected { get; }
        /// <summary>
        /// 超时时间
        /// </summary>
        int Timeout { get; set; }
        /// <summary>
        /// 连接机器人
        /// </summary>
        void Connect();
        /// <summary>
        /// 连接机器人
        /// </summary>
        /// <returns></returns>
        Task ConnectAsync();
        /// <summary>
        /// 断开连接
        /// </summary>
        void Disconnect();
        #endregion

        #region 控制
        /// <summary>
        /// 重置
        /// </summary>
        /// <returns></returns>
        bool Reset();
        /// <summary>
        /// 重置
        /// </summary>
        /// <returns></returns>
        Task<bool> ResetAsync();
        /// <summary>
        /// 电机
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        bool Motor(bool state);
        /// <summary>
        /// 电机
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        Task<bool> MotorAsync(bool state);
        /// <summary>
        /// PTP速度
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        bool Speed(int value);
        /// <summary>
        /// PTP速度
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        Task<bool> SpeedAsync(int value);
        /// <summary>
        /// PTP加减速度
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        bool Accel(int value);
        /// <summary>
        /// PTP加减速度
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        Task<bool> AccelAsync(int value);
        /// <summary>
        /// CP速度
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        bool Speeds(double value);
        /// <summary>
        /// CP速度
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        Task<bool> SpeedsAsync(double value);
        /// <summary>
        /// CP加减速度
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        bool Accels(double value);
        /// <summary>
        /// CP加减速度
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        Task<bool> AccelsAsync(double value);
        /// <summary>
        /// 整体运行速度比例
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        bool Speedfactor(int value);
        /// <summary>
        /// 整体运行速度比例
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        Task<bool> SpeedfactorAsync(int value);
        /// <summary>
        /// 功率
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        bool Power(bool state);
        /// <summary>
        /// 功率
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        Task<bool> PowerAsync(bool state);
        /// <summary>
        /// 用于在当前位置～指定位置之间以 PTP 动作移动机械臂
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        bool Go(RPoint position);
        /// <summary>
        /// 用于在当前位置～指定位置之间以 PTP 动作移动机械臂
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        Task<bool> GoAsync(RPoint position);
        /// <summary>
        /// 用于在当前位置～指定位置之间以直线动作移动机械臂。
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        bool Move(RPoint position);
        /// <summary>
        /// 用于在当前位置～指定位置之间以直线动作移动机械臂。
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        Task<bool> MoveAsync(RPoint position);
        /// <summary>
        /// 用于通过门控运动(首先垂直上升，然后水平移动，最后垂直下降的门型动作)使机械臂从当前位置向指定位置进行 PTP 动作。
        /// </summary>
        /// <param name="position"></param>
        /// <param name="LimZ"></param>
        /// <returns></returns>
        bool Jump(RPoint position,double? LimZ);
        /// <summary>
        /// 用于通过门控运动(首先垂直上升，然后水平移动，最后垂直下降的门型动作)使机械臂从当前位置向指定位置进行 PTP 动作。
        /// </summary>
        /// <param name="position"></param>
        /// <param name="LimZ"></param>
        /// <returns></returns>
        Task<bool> JumpAsync(RPoint position,double? LimZ);

        /// <summary>
        /// 坐标系相对移动
        /// </summary>
        /// <param name="axis"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        bool Jog(string axis, double distance);
        /// <summary>
        /// 坐标系相对移动
        /// </summary>
        /// <param name="axis"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        Task<bool> JogAsync(string axis, double distance);
        /// <summary>
        /// 关节相对转动
        /// </summary>
        /// <param name="joint"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        bool Joint(int joint, double distance);
        /// <summary>
        /// 关节相对转动
        /// </summary>
        /// <param name="joint"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        Task<bool> JointAsync(int joint, double distance);
        /// <summary>
        /// 获取机器人当前坐标
        /// </summary>
        /// <returns></returns>
        RPoint GetRobotPos();
        /// <summary>
        /// 获取机器人当前坐标
        /// </summary>
        /// <returns></returns>
        Task<RPoint> GetRobotPosAsync();
        /// <summary>
        /// 释放刹车
        /// </summary>
        /// <returns></returns>
        bool SFree();
        /// <summary>
        /// 释放刹车
        /// </summary>
        /// <returns></returns>
        Task<bool> SFreeAsync();
        /// <summary>
        /// 锁定刹车
        /// </summary>
        /// <returns></returns>
        bool SLock();
        /// <summary>
        /// 锁定刹车
        /// </summary>
        /// <returns></returns>
        Task<bool> SLockAsync();

        /// <summary>
        /// 发送校准运行参数
        /// </summary>
        /// <param name="Pick"></param>
        /// <param name="Speed"></param>
        /// <param name="Accel"></param>
        /// <param name="Power"></param>
        /// <param name="WaitSuction"></param>
        /// <param name="WaitBlow"></param>
        /// <returns></returns>
        bool CalibParame(PickInfo Pick, int Speed, int Accel, bool Power, int WaitSuction, int WaitBlow);

        /// <summary>
        /// 发送校准运行参数
        /// </summary>
        /// <param name="Pick"></param>
        /// <param name="Speed"></param>
        /// <param name="Accel"></param>
        /// <param name="Power"></param>
        /// <param name="WaitSuction"></param>
        /// <param name="WaitBlow"></param>
        /// <returns></returns>
        Task<bool> CalibParameAsync(PickInfo Pick, int Speed, int Accel, bool Power, int WaitSuction, int WaitBlow);


        List<object> GetNodesValue();


        /// <summary>
        /// 校准移动机器人
        /// </summary>
        /// <param name="position"></param>
        /// <param name="LimZ"></param>
        /// <returns></returns>
        bool CalibMotion(RPoint position, double? LimZ);

        /// <summary>
        /// 校准移动机器人
        /// </summary>
        /// <param name="position"></param>
        /// <param name="LimZ"></param>
        /// <returns></returns>
        Task<bool> CalibMotionAsync(RPoint position, double? LimZ);

        /// <summary>
        /// 校准取或者放校准块
        /// </summary>
        /// <param name="position"></param>
        /// <param name="LimZ"></param>
        /// <returns></returns>
        bool CalibOutIO(bool state);

        /// <summary>
        /// 校准取或者放校准块
        /// </summary>
        /// <param name="position"></param>
        /// <param name="LimZ"></param>
        /// <returns></returns>
        Task<bool> CalibOutIOAsync(bool state);

        /// <summary>
        /// 设置工具坐标
        /// </summary>
        /// <param name="index"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        bool SetTool(int index, double x,double y);

        /// <summary>
        /// 设置工具坐标
        /// </summary>
        /// <param name="index"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        Task<bool> SetToolAsync(int index, double x, double y);

        /// <summary>
        /// 选择工具坐标
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        bool SelectTool(int index);

        /// <summary>
        /// 选择工具坐标
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        Task<bool> SelectToolAsync(int index);
        #endregion

        #region 数据发送接收
        /// <summary>
        /// 发送并接收数据
        /// </summary>
        /// <param name="send"></param>
        /// <returns></returns>
        string SendAndReceive(string send);
        /// <summary>
        /// 发送并接收数据
        /// </summary>
        /// <param name="send"></param>
        /// <returns></returns>
        Task<string> SendAndReceiveAsync(string send);

        /// <summary>
        /// 发送并接收数据
        /// </summary>
        /// <param name="send"></param>
        /// <returns></returns>
        string SendAndReceive(ITcpSessionClient client,string send);
        /// <summary>
        /// 发送并接收数据
        /// </summary>
        /// <param name="send"></param>
        /// <returns></returns>
        Task<string> SendAndReceiveAsync(ITcpSessionClient client,string send);

        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="send"></param>
        void Send(string send);

        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="send"></param>
        /// <returns></returns>
        Task SendAsync(string send);

        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="send"></param>
        void Send(ITcpSessionClient client, string send);

        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="send"></param>
        /// <returns></returns>
        Task SendAsync(ITcpSessionClient client, string send);

        Encoding GetEncoding();
        #endregion
    }
}
