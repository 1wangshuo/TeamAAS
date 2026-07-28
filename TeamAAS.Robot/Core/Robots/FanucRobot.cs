using MathNet.Numerics;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TeamAAS.Robot.Enums;
using TeamAAS.Robot.Interfaces;
using TeamAAS.Robot.Models;
using TouchSocket.Core;
using TouchSocket.Sockets;
using static TeamAAS.Robot.Core.CoordinateTransformer;
using TeamAAS.Robot.Models;

namespace TeamAAS.Robot.Core.Robots
{
    public class FanucRobot : IRobot, INotifyPropertyChanged
    {
        private IWaitingClient<ITcpClient, IReceiverResult> WaitClient;
        private IWaitingClient<ITcpSessionClient, IReceiverResult> WaitClientServer;
        //private bool isFirstConnect = true;
        private Task bgConnectTask;
        public event Action<Guid, object, ConnectedEventArgs> ConnectedEvent;
        public event Action<Guid, object, ClosedEventArgs> DisconnectedEvent;
        public event Action<Guid, object, ReceivedDataEventArgs> ReceivedEvent;
        public event Action<Guid, object, string> SendEvent;

        public FanucRobot(RobotInfo robot)
        {
            Id = robot.Id;
            Name = robot.RobotName;
            RobotNo = robot.RobotNo;
            RobotIp = robot.IP;
            RobotPort = robot.Port;
            ConnectType = robot.ConnectType;
            Terminator = robot.Terminator;
            DataEncoding = robot.DataEncoding;
            Brand = robot.RobotBrand;
            if (ConnectType == TCPConnectType.Client)
            {
                TcpClient = new TouchSocket.Sockets.TcpClient();
                var config = new TouchSocketConfig();
                config.SetRemoteIPHost(new IPHost(IPAddress.Parse(RobotIp), RobotPort));
                config.ConfigurePlugins(a => {
                    ////断线重连配置
                    a.UseReconnection<TcpClient>(options =>
                    {
                        options.PollingInterval = TimeSpan.FromSeconds(1);
                    });
                });
                //设置结束符
                if (Terminator == Terminator.CR)
                {
                    config.SetTcpDataHandlingAdapter(() => { return new TerminatorPackageAdapter("\r"); });       //命令行中使用\r结尾 
                }
                else if (Terminator == Terminator.LF)
                {
                    config.SetTcpDataHandlingAdapter(() => { return new TerminatorPackageAdapter("\n"); });       //命令行中使用\n结尾 
                }
                else if (Terminator == Terminator.CRLF)
                {
                    config.SetTcpDataHandlingAdapter(() => { return new TerminatorPackageAdapter("\r\n"); });       //命令行中使用\r\n结尾 
                }
                //载入配置
                TcpClient.SetupAsync(config).GetAwaiter().GetResult();
                ////调用CreateWaitingClient获取到IWaitingClient的对象。
                WaitClient = TcpClient.CreateWaitingClient(new WaitingOptions()
                {
                    FilterFunc = response => //设置用于筛选的fun委托，当返回为true时，才会响应返回
                    {
                        return true;

                        //if (response.Data.Length == 1)
                        //{
                        //    return true;
                        //}
                        //return false;
                    }
                });

                TcpClient.Connected = ConnectedVoid;//成功连接到服务器
                //有客户端断开连接
                TcpClient.Closed = DisconnectedVoid;
                //从客户端收到信息
                TcpClient.Received = ReceivedVoid;
            }
            else
            {
                TcpService = new TcpService();

                var config = new TouchSocketConfig();
                config.SetListenIPHosts(new IPHost[] { new IPHost($"{RobotIp}:{RobotPort}"), new IPHost(RobotPort + 1) }); //同时监听两个地址
                //设置结束符
                if (Terminator == Terminator.CR)
                {
                    config.SetTcpDataHandlingAdapter(() => { return new TerminatorPackageAdapter("\r"); });       //命令行中使用\r结尾 
                }
                else if (Terminator == Terminator.LF)
                {
                    config.SetTcpDataHandlingAdapter(() => { return new TerminatorPackageAdapter("\n"); });       //命令行中使用\n结尾 
                }
                else if (Terminator == Terminator.CRLF)
                {
                    config.SetTcpDataHandlingAdapter(() => { return new TerminatorPackageAdapter("\r\n"); });       //命令行中使用\r\n结尾 
                }
                //载入配置
                TcpService.SetupAsync(config).GetAwaiter().GetResult();

                /////有客户端成功连接
                TcpService.Connected = ConnectedVoid;
                //有客户端断开连接
                TcpService.Closed = DisconnectedVoid;
                //从客户端收到信息
                TcpService.Received = ReceivedVoid;

            }
        }

        #region 属性
        public TcpClient TcpClient { get; private set; }

        public TcpService TcpService { get; private set; }

        public Guid Id { get; set; }

        public string Name { get; set; }    

        /// <summary>
        /// 机器人编号
        /// </summary>
        public int RobotNo { get; set; }

        public int RobotPort { get; private set; }

        public string RobotIp { get; private set; }

        public TCPConnectType ConnectType { get; private set; }

        public Terminator Terminator { get; private set; }

        public DataEncoding DataEncoding { get; private set; }

        public bool IsConnected
        {
            get
            {
                if (ConnectType == TCPConnectType.Client)
                {
                    return TcpClient.Online;
                }
                else
                {
                    if (TcpService.Count > 0)
                        return true;
                    else return false;
                }
            }
        }

        public int Timeout { get; set; } = 5000;

        private bool _CanExecute = false;

        public bool CanExecute
        {
            get { return _CanExecute; }
            set { SetProperty(ref _CanExecute, value); }
        }

        public int SelectedTool { get; private set; } = 0;

        public RobotBrand Brand { get; private set; }

        /// <summary>
        /// 进入调试模式
        /// </summary>
        /// <returns></returns>
        public bool EnterDebugMode { get; set; }
        #endregion

        #region 事件接收
        //-----------------------------------------------服务器---------------------------------------------
        /// <summary>
        /// 有客户端成功连接
        /// </summary>
        /// <param name="client"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private Task ConnectedVoid(ITcpSessionClient client, ConnectedEventArgs e)
        {
            CanExecute = true;
            //调用CreateWaitingClient获取到IWaitingClient的对象。
            WaitClientServer = client.CreateWaitingClient(new WaitingOptions()
            {
                FilterFunc = response => //设置用于筛选的fun委托，当返回为true时，才会响应返回
                {
                    return true;

                    //if (response.Data.Length == 1)
                    //{
                    //    return true;
                    //}
                    //return false;
                }
            });
            ConnectedEvent?.Invoke(this.Id, client, e);
            return EasyTask.CompletedTask;
        }
        /// <summary>
        /// 有客户端断开连接
        /// </summary>
        /// <param name="client"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private Task DisconnectedVoid(ITcpSessionClient client, ClosedEventArgs e)
        {
            if (TcpService.Count < 1)
            {
                CanExecute = false;
            }
            DisconnectedEvent?.Invoke(this.Id, client, e);
            if (WaitClientServer != null)
            {
                if (WaitClientServer.Client.Id == client.Id)
                {
                    if (TcpService.Count > 0)
                    {
                        WaitClientServer = ((ITcpSessionClient)(TcpService.Clients.First())).CreateWaitingClient(new WaitingOptions()
                        {
                            FilterFunc = response => //设置用于筛选的fun委托，当返回为true时，才会响应返回
                            {
                                return true;

                                //if (response.Data.Length == 1)
                                //{
                                //    return true;
                                //}
                                //return false;
                            }
                        });
                    }
                }
            }
            return EasyTask.CompletedTask;
        }
        /// <summary>
        /// 从客户端收到信息
        /// </summary>
        /// <param name="client"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private Task ReceivedVoid(ITcpSessionClient client, ReceivedDataEventArgs e)
        {
            ReceivedEvent?.Invoke(this.Id, client, e);
            //调用CreateWaitingClient获取到IWaitingClient的对象。
            WaitClientServer = client.CreateWaitingClient(new WaitingOptions()
            {
                FilterFunc = response => //设置用于筛选的fun委托，当返回为true时，才会响应返回
                {
                    return true;

                    //if (response.Data.Length == 1)
                    //{
                    //    return true;
                    //}
                    //return false;
                }
            });
            return EasyTask.CompletedTask;
        }

        //---------------------------------------------客户端--------------------------------------------
        /// <summary>
        /// 连接到服务器时
        /// </summary>
        /// <param name="client"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private Task ConnectedVoid(ITcpClient client, ConnectedEventArgs e)
        {
            CanExecute = true;
            ConnectedEvent?.Invoke(this.Id, client, e);
            return EasyTask.CompletedTask;
        }

        /// <summary>
        /// 有客户端断开连接
        /// </summary>
        /// <param name="client"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private Task DisconnectedVoid(ITcpClient client, ClosedEventArgs e)
        {
            CanExecute = false;
            DisconnectedEvent?.Invoke(this.Id, client, e);
            return EasyTask.CompletedTask;
        }
        /// <summary>
        /// 从客户端收到信息
        /// </summary>
        /// <param name="client"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private Task ReceivedVoid(ITcpClient client, ReceivedDataEventArgs e)
        {
            ReceivedEvent?.Invoke(this.Id, client, e);
            return EasyTask.CompletedTask;
        }
        #endregion

        #region 连接
        public void Connect()
        {
            if (ConnectType == TCPConnectType.Client)
            {
                try
                {
                    TcpClient.ConnectAsync().GetAwaiter().GetResult();
                    //isFirstConnect = false;
                }
                catch (Exception)
                {
                    if (bgConnectTask == null)
                    {
                        bgConnectTask = Task.Run(async () =>
                        {
                            while (true)
                            {
                                try
                                {
                                    TcpClient.ConnectAsync().GetAwaiter().GetResult();
                                    //isFirstConnect = false;
                                    return;
                                }
                                catch (Exception)
                                {
                                    await Task.Delay(1000);
                                }
                            }
                        });
                    }
                    throw;
                }
            }
            else
            {
                TcpService.StartAsync().GetAwaiter().GetResult();
            }
        }

        public async Task ConnectAsync()
        {
            if (ConnectType == TCPConnectType.Client)
            {
                try
                {
                    await TcpClient.ConnectAsync();
                    //isFirstConnect = false;
                }
                catch (Exception)
                {
                    if (bgConnectTask == null)
                    {
                        bgConnectTask = Task.Run(async () =>
                        {
                            while (true)
                            {
                                try
                                {
                                    TcpClient.ConnectAsync().GetAwaiter().GetResult();
                                    //isFirstConnect = false;
                                    return;
                                }
                                catch (Exception)
                                {
                                    await Task.Delay(1000);
                                }
                            }
                        });
                    }
                    throw;
                }
            }
            else
            {
                await TcpService.StartAsync();
            }
        }

        public void Disconnect()
        {
            if (TcpClient != null)
            {
                TcpClient?.CloseAsync();
            }
            if (TcpService != null)
            {
                TcpService?.StopAsync();
            }
        }

        public void Dispose()
        {
            if (TcpClient != null)
            {
                TcpClient?.Dispose();
            }
            if (TcpService != null)
            {
                TcpService?.Dispose();
            }
        }
        #endregion

        #region 控制
        public bool Reset()
        {
            try
            {
                CanExecute = false;
                string rev = SendAndReceive($"Reset");
                if (rev.Contains("Reset"))
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                // LogHelper removed
                CanExecute = true;
                throw ex;
            }
            finally
            {
                CanExecute = true;
            }
        }

        public async Task<bool> ResetAsync()
        {
            try
            {
                CanExecute = false;
                string rev = await SendAndReceiveAsync($"Reset");
                if (rev.Contains("Reset"))
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                // LogHelper removed
                CanExecute = true;
                throw ex;
            }
            finally
            {
                CanExecute = true;
            }
        }

        public bool Motor(bool state)
        {
            try
            {
                CanExecute = false;
                int v = state ? 1 : 0;
                string rev = SendAndReceive($"Motor,{v}");
                if (rev.Contains("Motor"))
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                // LogHelper removed
                CanExecute = true;
                throw ex;
            }
            finally
            {
                CanExecute = true;
            }
        }
        public async Task<bool> MotorAsync(bool state)
        {
            try
            {
                CanExecute = false;
                int v = state ? 1 : 0;
                string rev = await SendAndReceiveAsync($"Motor,{v}");
                if (rev.Contains("Motor"))
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                // LogHelper removed
                CanExecute = true;
                throw ex;
            }
            finally
            {
                CanExecute = true;
            }
        }

        public bool Power(bool state)
        {
            try
            {
                CanExecute = false;
                int v = state ? 1 : 0;
                string rev = SendAndReceive($"Power,{v}");
                if (rev.Contains("Power"))
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                // LogHelper removed
                CanExecute = true;
                throw ex;
            }
            finally
            {
                CanExecute = true;
            }
        }

        public async Task<bool> PowerAsync(bool state)
        {
            try
            {
                CanExecute = false;
                int v = state ? 1 : 0;
                string rev = await SendAndReceiveAsync($"Power,{v}");
                if (rev.Contains("Power"))
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                // LogHelper removed
                CanExecute = true;
                throw ex;
            }
            finally
            {
                CanExecute = true;
            }
        }

        public bool Speed(int value)
        {
            try
            {
                CanExecute = false;
                string rev = SendAndReceive($"Speed,{value}");
                if (rev.Contains("Speed"))
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                // LogHelper removed
                CanExecute = true;
                throw ex;
            }
            finally
            {
                CanExecute = true;
            }
        }

        public async Task<bool> SpeedAsync(int value)
        {
            try
            {
                CanExecute = false;
                string rev = await SendAndReceiveAsync($"Speed,{value}");
                if (rev.Contains("Speed"))
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                // LogHelper removed
                CanExecute = true;
                throw ex;
            }
            finally
            {
                CanExecute = true;
            }
        }

        public bool Speedfactor(int value)
        {
            try
            {
                CanExecute = false;
                string rev = SendAndReceive($"Speedfactor,{value}");
                if (rev.Contains("Speedfactor"))
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                // LogHelper removed
                CanExecute = true;
                throw ex;
            }
            finally
            {
                CanExecute = true;
            }
        }

        public async Task<bool> SpeedfactorAsync(int value)
        {
            try
            {
                CanExecute = false;
                string rev = await SendAndReceiveAsync($"Speedfactor,{value}");
                if (rev.Contains("Speedfactor"))
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                // LogHelper removed
                CanExecute = true;
                throw ex;
            }
            finally
            {
                CanExecute = true;
            }
        }

        public bool Speeds(double value)
        {
            try
            {
                CanExecute = false;
                string rev = SendAndReceive($"Speeds,{value}");
                if (rev.Contains("Speeds"))
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                // LogHelper removed
                CanExecute = true;
                throw ex;
            }
            finally
            {
                CanExecute = true;
            }
        }

        public async Task<bool> SpeedsAsync(double value)
        {
            try
            {
                CanExecute = false;
                string rev = await SendAndReceiveAsync($"Speeds,{value}");
                if (rev.Contains("Speeds"))
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                // LogHelper removed
                CanExecute = true;
                throw ex;
            }
            finally
            {
                CanExecute = true;
            }
        }

        public bool Accel(int value)
        {
            try
            {
                CanExecute = false;
                string rev = SendAndReceive($"Accel,{value},{value}");
                if (rev.Contains("Accel"))
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                // LogHelper removed
                CanExecute = true;
                throw ex;
            }
            finally
            {
                CanExecute = true;
            }
        }

        public async Task<bool> AccelAsync(int value)
        {
            try
            {
                CanExecute = false;
                string rev = await SendAndReceiveAsync($"Accel,{value},{value}");
                if (rev.Contains("Accel"))
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                // LogHelper removed
                CanExecute = true;
                throw ex;
            }
            finally
            {
                CanExecute = true;
            }
        }

        public bool Accels(double value)
        {
            try
            {
                CanExecute = false;
                string rev = SendAndReceive($"Accels,{value},{value}");
                if (rev.Contains("Accels"))
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                // LogHelper removed
                CanExecute = true;
                throw ex;
            }
            finally
            {
                CanExecute = true;
            }
        }

        public async Task<bool> AccelsAsync(double value)
        {
            try
            {
                CanExecute = false;
                string rev = await SendAndReceiveAsync($"Accels,{value},{value}");
                if (rev.Contains("Accels"))
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                // LogHelper removed
                CanExecute = true;
                throw ex;
            }
            finally
            {
                CanExecute = true;
            }
        }

        public RPoint GetRobotPos()
        {
            try
            {
                
                CanExecute = false;
                RPoint point = new RPoint();
                string rev = SendAndReceive($"GetRobotPos,1");
                if (rev.Contains("GetRobotPos"))
                {
                    
                    var items = rev.Split(',');
                    point.X = Convert.ToSingle(items[1]);
                    point.Y = Convert.ToSingle(items[2]);
                    point.Z = Convert.ToSingle(items[3]);
                    point.U = Convert.ToSingle(items[4]);
                    //point.V = Convert.ToInt32(items[5]);
                    //point.W = Convert.ToInt32(items[6]);
                    //point.Hand = (RobotHand)Convert.ToInt32(items[7]);
                    //point.Local = Convert.ToInt32(items[8]);
                    //point.Tool = Convert.ToInt32(items[9]);
                }
                else
                    return null;
                rev = SendAndReceive($"GetRobotPos,2");
                if (rev.Contains("GetRobotPos"))
                {

                    var items = rev.Split(',');
                    //point.X = Convert.ToInt32(items[1]);
                    //point.Y = Convert.ToInt32(items[2]);
                    //point.Z = Convert.ToInt32(items[3]);
                    //point.U = Convert.ToInt32(items[4]);
                    point.V = Convert.ToSingle(items[1]);
                    point.W = Convert.ToSingle(items[2]);
                    point.Hand = (RobotHand)Convert.ToInt32(items[3]);
                    point.Local = Convert.ToInt32(items[4]);
                    point.Tool = Convert.ToInt32(items[5]);
                    return point;
                }
                else
                    return null;
            }
            catch (Exception ex)
            {
                // LogHelper removed
                CanExecute = true;
                throw ex;
            }
            finally
            {
                CanExecute = true;
            }
        }

        public async Task<RPoint> GetRobotPosAsync()
        {
            try
            {
                CanExecute = false;
                RPoint point = new RPoint();
                string rev = await SendAndReceiveAsync($"GetRobotPos,1");
                if (rev.Contains("GetRobotPos"))
                {

                    var items = rev.Split(',');
                    point.X = Convert.ToSingle(items[1]);
                    point.Y = Convert.ToSingle(items[2]);
                    point.Z = Convert.ToSingle(items[3]);
                    point.U = Convert.ToSingle(items[4]);
                    //point.V = Convert.ToInt32(items[5]);
                    //point.W = Convert.ToInt32(items[6]);
                    //point.Hand = (RobotHand)Convert.ToInt32(items[7]);
                    //point.Local = Convert.ToInt32(items[8]);
                    //point.Tool = Convert.ToInt32(items[9]);
                }
                else
                    return null;
                await Task.Delay(100);
                rev = await SendAndReceiveAsync($"GetRobotPos,2");
                if (rev.Contains("GetRobotPos"))
                {

                    var items = rev.Split(',');
                    //point.X = Convert.ToInt32(items[1]);
                    //point.Y = Convert.ToInt32(items[2]);
                    //point.Z = Convert.ToInt32(items[3]);
                    //point.U = Convert.ToInt32(items[4]);
                    point.V = Convert.ToSingle(items[1]);
                    point.W = Convert.ToSingle(items[2]);
                    point.Hand = (RobotHand)Convert.ToInt32(items[3]);
                    point.Local = Convert.ToInt32(items[4]);
                    point.Tool = Convert.ToInt32(items[5]);
                    return point;
                }
                else
                    return null;
            }
            catch (Exception ex)
            {
                // LogHelper removed
                CanExecute = true;
                throw ex;
            }
            finally
            {
                CanExecute = true;
            }
        }

        public bool Jog(string axis, double distance)
        {
            try
            {
                CanExecute = false;
                string rev = SendAndReceive($"Jog,{axis},{distance}");
                if (rev.Contains("Jog"))
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                // LogHelper removed
                CanExecute = true;
                throw ex;
            }
            finally
            {
                CanExecute = true;
            }
        }
        public async Task<bool> JogAsync(string axis, double distance)
        {
            try
            {
                CanExecute = false;
                string rev = await SendAndReceiveAsync($"Jog,{axis},{distance}");
                if (rev.Contains("Jog"))
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                // LogHelper removed
                CanExecute = true;
                throw ex;
            }
            finally
            {
                CanExecute = true;
            }
        }
        public bool Joint(int joint, double distance)
        {
            try
            {
                CanExecute = false;
                string rev = SendAndReceive($"Joint,{joint},{distance}");
                if (rev.Contains("Joint"))
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                // LogHelper removed
                CanExecute = true;
                throw ex;
            }
            finally
            {
                CanExecute = true;
            }
        }
        public async Task<bool> JointAsync(int joint, double distance)
        {
            try
            {
                CanExecute = false;
                string rev = await SendAndReceiveAsync($"Joint,{joint},{distance}");
                if (rev.Contains("Joint"))
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                // LogHelper removed
                CanExecute = true;
                throw ex;
            }
            finally
            {
                CanExecute = true;
            }
        }

        public bool Go(RPoint position)
        {
            try
            {
                CanExecute = false;
                string rev = SendAndReceive($"Go,{position.X},{position.Y},{position.Z},{position.U},{position.V},{position.W},{(int)position.Hand},{position.Local},{position.Tool}");
                if (rev.Contains("Go"))
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                // LogHelper removed
                CanExecute = true;
                throw ex;
            }
            finally
            {
                CanExecute = true;
            }
        }

        public async Task<bool> GoAsync(RPoint position)
        {
            try
            {
                CanExecute = false;
                string rev = await SendAndReceiveAsync($"Go,{position.X},{position.Y},{position.Z},{position.U},{position.V},{position.W},{(int)position.Hand},{position.Local},{position.Tool}");
                if (rev.Contains("Go"))
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                // LogHelper removed
                CanExecute = true;
                throw ex;
            }
            finally
            {
                CanExecute = true;
            }
        }
        public bool Jump(RPoint position, double? LimZ)
        {
            try
            {
                CanExecute = false;
                double limz = LimZ == null ? 0 : (double)LimZ;
                string rev = SendAndReceive($"Jump,{position.X},{position.Y},{position.Z},{position.U},{position.V},{position.W},{(int)position.Hand},{position.Local},{position.Tool},{limz}");
                if (rev.Contains("Jump"))
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                // LogHelper removed
                CanExecute = true;
                throw ex;
            }
            finally
            {
                CanExecute = true;
            }
        }
        public async Task<bool> JumpAsync(RPoint position, double? LimZ)
        {
            try
            {
                CanExecute = false;
                double limz = LimZ == null ? 0 : (double)LimZ;
                string rev = await SendAndReceiveAsync($"Jump,{position.X},{position.Y},{position.Z},{position.U},{position.V},{position.W},{(int)position.Hand},{position.Local},{position.Tool},{limz}");
                if (rev.Contains("Jump"))
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                // LogHelper removed
                CanExecute = true;
                throw ex;
            }
            finally
            {
                CanExecute = true;
            }
        }

        public bool Move(RPoint position)
        {
            try
            {
                CanExecute = false;
                string rev = SendAndReceive($"Move,{position.X},{position.Y},{position.Z},{position.U},{position.V},{position.W},{(int)position.Hand},{position.Local},{position.Tool}");
                if (rev.Contains("Move"))
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                // LogHelper removed
                CanExecute = true;
                throw ex;
            }
            finally
            {
                CanExecute = true;
            }
        }

        public async Task<bool> MoveAsync(RPoint position)
        {
            try
            {
                CanExecute = false;
                string rev = await SendAndReceiveAsync($"Move,{position.X},{position.Y},{position.Z},{position.U},{position.V},{position.W},{(int)position.Hand},{position.Local},{position.Tool}");
                if (rev.Contains("Move"))
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                // LogHelper removed
                CanExecute = true;
                throw ex;
            }
            finally
            {
                CanExecute = true;
            }
        }

        public bool SFree()
        {
            try
            {
                CanExecute = false;
                string rev = SendAndReceive($"SFree");
                if (rev.Contains("SFree"))
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                // LogHelper removed
                CanExecute = true;
                throw ex;
            }
            finally
            {
                CanExecute = true;
            }
        }

        public async Task<bool> SFreeAsync()
        {
            try
            {
                CanExecute = false;
                string rev = await SendAndReceiveAsync($"SFree");
                if (rev.Contains("SFree"))
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                // LogHelper removed
                CanExecute = true;
                throw ex;
            }
            finally
            {
                CanExecute = true;
            }
        }

        public bool SLock()
        {
            try
            {
                CanExecute = false;
                string rev = SendAndReceive($"SLock");
                if (rev.Contains("SLock"))
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                // LogHelper removed
                CanExecute = true;
                throw ex;
            }
            finally
            {
                CanExecute = true;
            }
        }

        public async Task<bool> SLockAsync()
        {
            try
            {
                CanExecute = false;
                string rev = await SendAndReceiveAsync($"SLock");
                if (rev.Contains("SLock"))
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                // LogHelper removed
                CanExecute = true;
                throw ex;
            }
            finally
            {
                CanExecute = true;
            }
        }

        public bool CalibMotion(RPoint position, double? LimZ)
        {
            try
            {
                CanExecute = false;
                double limz = LimZ == null ? 0 : (double)LimZ;
                string rev = SendAndReceive($"CalibMotion,{position.X},{position.Y},{position.Z},{position.U},{position.V},{position.W},{(int)position.Hand},{position.Local},{position.Tool},{limz}");
                if (rev.Contains("CalibMotion"))
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                // LogHelper removed
                CanExecute = true;
                throw ex;
            }
            finally
            {
                CanExecute = true;
            }
        }

        public async Task<bool> CalibMotionAsync(RPoint position, double? LimZ)
        {
            try
            {
                CanExecute = false;
                double limz = LimZ == null ? 0 : (double)LimZ;
                string rev = await SendAndReceiveAsync($"CalibMotion,{position.X},{position.Y},{position.Z},{position.U},{position.V},{position.W},{(int)position.Hand},{position.Local},{position.Tool},{limz}");
                if (rev.Contains("CalibMotion"))
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                // LogHelper removed
                CanExecute = true;
                throw ex;
            }
            finally
            {
                CanExecute = true;
            }
        }

        public bool CalibOutIO(bool state)
        {
            try
            {
                CanExecute = false;
                int va = state ? 1 : 0;
                string rev = SendAndReceive($"CalibOutIO,{va}");
                if (rev.Contains("CalibOutIO"))
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                // LogHelper removed
                CanExecute = true;
                throw ex;
            }
            finally
            {
                CanExecute = true;
            }
        }

        public async Task<bool> CalibOutIOAsync(bool state)
        {
            try
            {
                CanExecute = false;
                int va = state ? 1 : 0;
                string rev = await SendAndReceiveAsync($"CalibOutIO,{va}");
                if (rev.Contains("CalibOutIO"))
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                // LogHelper removed
                CanExecute = true;
                throw ex;
            }
            finally
            {
                CanExecute = true;
            }
        }

        public bool CalibParame(PickInfo Pick, int Speed, int Accel, bool Power, int WaitSuction, int WaitBlow)
        {
            try
            {
                CanExecute = false;
                int va = Power ? 1 : 0;
                string rev = SendAndReceive($"CalibParame,{(int)Pick.PickPlaceModel},{Pick.Inhal},{Pick.Blow},{Speed},{Accel},{va},{WaitSuction},{WaitBlow}");
                if (rev.Contains("CalibParame"))
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                // LogHelper removed
                CanExecute = true;
                throw ex;
            }
            finally
            {
                CanExecute = true;
            }
        }

        public async Task<bool> CalibParameAsync(PickInfo Pick, int Speed, int Accel, bool Power, int WaitSuction, int WaitBlow)
        {
            try
            {
                CanExecute = false;
                int va = Power ? 1 : 0;
                string rev = await SendAndReceiveAsync($"CalibParame,{(int)Pick.PickPlaceModel},{Pick.Inhal},{Pick.Blow},{Speed},{Accel},{va},{WaitSuction},{WaitBlow}");
                if (rev.Contains("CalibParame"))
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                // LogHelper removed
                CanExecute = true;
                throw ex;
            }
            finally
            {
                CanExecute = true;
            }
        }

        /// <summary>
        /// 设置工具坐标
        /// </summary>
        /// <param name="index"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public bool SetTool(int index, double x, double y)
        {
            try
            {
                CanExecute = false;
                string rev = SendAndReceive($"SetTool,{index},{x},{y}");
                if (rev.Contains("SetTool"))
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                // LogHelper removed
                CanExecute = true;
                throw ex;
            }
            finally
            {
                CanExecute = true;
            }
        }

        /// <summary>
        /// 设置工具坐标
        /// </summary>
        /// <param name="index"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public async Task<bool> SetToolAsync(int index, double x, double y)
        {
            try
            {
                CanExecute = false;
                string rev = await SendAndReceiveAsync($"SetTool,{index},{x},{y}");
                if (rev.Contains("SetTool"))
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                // LogHelper removed
                CanExecute = true;
                throw ex;
            }
            finally
            {
                CanExecute = true;
            }
        }

        /// <summary>
        /// 选择工具坐标
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public bool SelectTool(int index)
        {
            try
            {
                SelectedTool = index;
                CanExecute = false;
                string rev = SendAndReceive($"SelectTool,{index}");
                if (rev.Contains("SetTool"))
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                // LogHelper removed
                CanExecute = true;
                throw ex;
            }
            finally
            {
                CanExecute = true;
            }
        }

        /// <summary>
        /// 选择工具坐标
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public async Task<bool> SelectToolAsync(int index)
        {
            {
                try
                {
                    SelectedTool = index;
                    CanExecute = false;
                    string rev = await SendAndReceiveAsync($"SelectTool,{index}");
                    if (rev.Contains("SetTool"))
                        return true;
                    else
                        return false;
                }
                catch (Exception ex)
                {
                    // LogHelper removed
                    CanExecute = true;
                    throw ex;
                }
                finally
                {
                    CanExecute = true;
                }
            }
        }
        #endregion

        #region 收发数据
        /// <summary>
        /// 发送并接收数据
        /// </summary>
        /// <param name="send"></param>
        /// <returns></returns>
        public string SendAndReceive(string send)
        {
            if (ConnectType == TCPConnectType.Client)
            {
                Encoding encoding = GetEncoding();
                SendEvent?.Invoke(this.Id, WaitClient.Client, send);
                var returnData = WaitClient.SendThenResponseAsync(encoding.GetBytes(send), Timeout).GetAwaiter().GetResult();
                string receivedString = returnData.Memory.Span.ToString(encoding);
                ReceivedDataEventArgs receivedDataEvent = new ReceivedDataEventArgs(returnData.Memory.Span.ToArray(), null);
                ReceivedEvent?.Invoke(this.Id, WaitClient, receivedDataEvent);
                return receivedString;
            }
            else
            {
                Encoding encoding = GetEncoding();
                SendEvent?.Invoke(this.Id, WaitClientServer.Client, send);
                var returnData = WaitClientServer.SendThenResponseAsync(encoding.GetBytes(send), Timeout).GetAwaiter().GetResult();
                string receivedString = returnData.Memory.Span.ToString(encoding);
                ReceivedDataEventArgs receivedDataEvent = new ReceivedDataEventArgs(returnData.Memory.Span.ToArray(), null);
                ReceivedEvent?.Invoke(this.Id, WaitClientServer.Client, receivedDataEvent);
                return receivedString;
            }
        }

        /// <summary>
        /// 发送并接收数据
        /// </summary>
        /// <param name="send"></param>
        /// <returns></returns>
        public async Task<string> SendAndReceiveAsync(string send)
        {
            if (ConnectType == TCPConnectType.Client)
            {
                Encoding encoding = GetEncoding();
                SendEvent?.Invoke(this.Id, WaitClient.Client, send);
                var returnData = await WaitClient.SendThenResponseAsync(encoding.GetBytes(send), Timeout);
                string receivedString = returnData.Memory.Span.ToString(encoding);
                ReceivedDataEventArgs receivedDataEvent = new ReceivedDataEventArgs(returnData.Memory.Span.ToArray(), null);
                ReceivedEvent?.Invoke(this.Id, WaitClient, receivedDataEvent);
                return receivedString;
            }
            else
            {
                Encoding encoding = GetEncoding();
                SendEvent?.Invoke(this.Id, WaitClientServer.Client, send);
                var returnData = await WaitClientServer.SendThenResponseAsync(encoding.GetBytes(send), Timeout);
                string receivedString = returnData.Memory.Span.ToString(encoding);
                ReceivedDataEventArgs receivedDataEvent = new ReceivedDataEventArgs(returnData.Memory.Span.ToArray(), null);
                ReceivedEvent?.Invoke(this.Id, WaitClientServer.Client, receivedDataEvent);
                return receivedString;
            }
        }

        /// <summary>
        /// 发送并接收数据
        /// </summary>
        /// <param name="send"></param>
        /// <returns></returns>
        public string SendAndReceive(ITcpSessionClient client, string send)
        {
            if (ConnectType == TCPConnectType.Client)
            {
                Encoding encoding = GetEncoding();
                SendEvent?.Invoke(this.Id, WaitClient.Client, send);
                var returnData = WaitClient.SendThenResponseAsync(encoding.GetBytes(send), Timeout).GetAwaiter().GetResult();
                string receivedString = returnData.Memory.Span.ToString(encoding);
                ReceivedDataEventArgs receivedDataEvent = new ReceivedDataEventArgs(returnData.Memory.Span.ToArray(), null);
                ReceivedEvent?.Invoke(this.Id, WaitClient, receivedDataEvent);
                return receivedString;
            }
            else
            {
                //调用CreateWaitingClient获取到IWaitingClient的对象。
                var waitClientServer = client.CreateWaitingClient(new WaitingOptions()
                {
                    FilterFunc = response => //设置用于筛选的fun委托，当返回为true时，才会响应返回
                    {
                        return true;

                        //if (response.Data.Length == 1)
                        //{
                        //    return true;
                        //}
                        //return false;
                    }
                });
                Encoding encoding = GetEncoding();
                SendEvent?.Invoke(this.Id, WaitClientServer.Client, send);
                var returnData = waitClientServer.SendThenResponseAsync(encoding.GetBytes(send), Timeout).GetAwaiter().GetResult();
                string receivedString = returnData.Memory.Span.ToString(encoding);
                ReceivedDataEventArgs receivedDataEvent = new ReceivedDataEventArgs(returnData.Memory.Span.ToArray(), null);
                ReceivedEvent?.Invoke(this.Id, WaitClientServer.Client, receivedDataEvent);
                return receivedString;
            }
        }

        /// <summary>
        /// 发送并接收数据
        /// </summary>
        /// <param name="send"></param>
        /// <returns></returns>
        public async Task<string> SendAndReceiveAsync(ITcpSessionClient client, string send)
        {
            if (ConnectType == TCPConnectType.Client)
            {
                Encoding encoding = GetEncoding();
                SendEvent?.Invoke(this.Id, WaitClient.Client, send);
                var returnData = await WaitClient.SendThenResponseAsync(encoding.GetBytes(send), Timeout);
                string receivedString = returnData.Memory.Span.ToString(encoding);
                ReceivedDataEventArgs receivedDataEvent = new ReceivedDataEventArgs(returnData.Memory.Span.ToArray(), null);
                ReceivedEvent?.Invoke(this.Id, WaitClientServer.Client, receivedDataEvent);
                return receivedString;
            }
            else
            {
                //调用CreateWaitingClient获取到IWaitingClient的对象。
                var waitClientServer = client.CreateWaitingClient(new WaitingOptions()
                {
                    FilterFunc = response => //设置用于筛选的fun委托，当返回为true时，才会响应返回
                    {
                        return true;

                        //if (response.Data.Length == 1)
                        //{
                        //    return true;
                        //}
                        //return false;
                    }
                });
                Encoding encoding = GetEncoding();
                SendEvent?.Invoke(this.Id, WaitClientServer.Client, send);
                var returnData = await waitClientServer.SendThenResponseAsync(encoding.GetBytes(send), Timeout);
                string receivedString = returnData.Memory.Span.ToString(encoding);
                ReceivedDataEventArgs receivedDataEvent = new ReceivedDataEventArgs(returnData.Memory.Span.ToArray(), null);
                ReceivedEvent?.Invoke(this.Id, WaitClientServer.Client, receivedDataEvent);
                return receivedString;
            }
        }

        public void Send(string send)
        {
            if (ConnectType == TCPConnectType.Client)
            {
                Encoding encoding = GetEncoding();
                SendEvent?.Invoke(this.Id, TcpClient, send);
                TcpClient.SendAsync(encoding.GetBytes(send)).GetAwaiter().GetResult();
            }
            else
            {
                Encoding encoding = GetEncoding();
                SendEvent?.Invoke(this.Id, WaitClientServer.Client, send);
                TcpService.SendAsync(WaitClientServer.Client.Id, encoding.GetBytes(send)).GetAwaiter().GetResult();
            }
        }

        public async Task SendAsync(string send)
        {
            if (ConnectType == TCPConnectType.Client)
            {
                Encoding encoding = GetEncoding();
                SendEvent?.Invoke(this.Id, TcpClient, send);
                await TcpClient.SendAsync(encoding.GetBytes(send));
            }
            else
            {
                Encoding encoding = GetEncoding();
                SendEvent?.Invoke(this.Id, WaitClientServer.Client, send);
                await TcpService.SendAsync(WaitClientServer.Client.Id, encoding.GetBytes(send));
            }
        }

        public void Send(ITcpSessionClient client, string send)
        {
            try
            {
                if (ConnectType == TCPConnectType.Client)
                {
                    Encoding encoding = GetEncoding();
                    SendEvent?.Invoke(this.Id, TcpClient, send);
                    TcpClient.SendAsync(encoding.GetBytes(send)).GetAwaiter().GetResult();
                }
                else
                {
                    Encoding encoding = GetEncoding();
                    SendEvent?.Invoke(this.Id, client, send);
                    TcpService.SendAsync(client.Id, encoding.GetBytes(send)).GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                // LogHelper removed
            }

        }

        public async Task SendAsync(ITcpSessionClient client, string send)
        {
            try
            {
                if (ConnectType == TCPConnectType.Client)
                {
                    Encoding encoding = GetEncoding();
                    SendEvent?.Invoke(this.Id, TcpClient, send);
                    await TcpClient.SendAsync(encoding.GetBytes(send));
                }
                else
                {
                    Encoding encoding = GetEncoding();
                    SendEvent?.Invoke(this.Id, client, send);
                    await TcpService.SendAsync(client.Id, encoding.GetBytes(send));
                }
            }
            catch (Exception ex)
            {
                // LogHelper removed
            }

        }
        /// <summary>
        /// 获取编码
        /// </summary>
        /// <param name="dataEncoding"></param>
        /// <returns></returns>
        public Encoding GetEncoding()
        {
            DataEncoding dataEncoding = this.DataEncoding;
            Encoding encoding;
            if (dataEncoding == DataEncoding.Default)
            {
                encoding = Encoding.Default;
            }
            else if (dataEncoding == DataEncoding.ASCII)
            {
                encoding = Encoding.ASCII;
            }
            else if (dataEncoding == DataEncoding.UTF7)
            {
                encoding = Encoding.UTF7;
            }
            else if (dataEncoding == DataEncoding.UTF8)
            {
                encoding = Encoding.UTF8;
            }
            else if (dataEncoding == DataEncoding.UTF32)
            {
                encoding = Encoding.UTF32;
            }
            else if (dataEncoding == DataEncoding.Unicode)
            {
                encoding = Encoding.Unicode;
            }
            else if (dataEncoding == DataEncoding.BigEndianUnicode)
            {
                encoding = Encoding.BigEndianUnicode;
            }
            else if (dataEncoding == DataEncoding.GB2312)
            {

                encoding = Encoding.GetEncoding("gb2312");
            }
            else
            {
                encoding = Encoding.Default;
            }
            return encoding;
        }
        #endregion

        #region 属性通知
        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Checks if a property already matches a desired value. Sets the property and
        /// notifies listeners only when necessary.
        /// </summary>
        /// <typeparam name="T">Type of the property.</typeparam>
        /// <param name="storage">Reference to a property with both getter and setter.</param>
        /// <param name="value">Desired value for the property.</param>
        /// <param name="propertyName">Name of the property used to notify listeners. This
        /// value is optional and can be provided automatically when invoked from compilers that
        /// support CallerMemberName.</param>
        /// <returns>True if the value was changed, false if the existing value matched the
        /// desired value.</returns>
        protected virtual bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value)) return false;

            storage = value;
            RaisePropertyChanged(propertyName);

            return true;
        }

        /// <summary>
        /// Checks if a property already matches a desired value. Sets the property and
        /// notifies listeners only when necessary.
        /// </summary>
        /// <typeparam name="T">Type of the property.</typeparam>
        /// <param name="storage">Reference to a property with both getter and setter.</param>
        /// <param name="value">Desired value for the property.</param>
        /// <param name="propertyName">Name of the property used to notify listeners. This
        /// value is optional and can be provided automatically when invoked from compilers that
        /// support CallerMemberName.</param>
        /// <param name="onChanged">Action that is called after the property value has been changed.</param>
        /// <returns>True if the value was changed, false if the existing value matched the
        /// desired value.</returns>
        protected virtual bool SetProperty<T>(ref T storage, T value, Action onChanged, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value)) return false;

            storage = value;
            onChanged?.Invoke();
            RaisePropertyChanged(propertyName);

            return true;
        }

        /// <summary>
        /// Raises this object's PropertyChanged event.
        /// </summary>
        /// <param name="propertyName">Name of the property used to notify listeners. This
        /// value is optional and can be provided automatically when invoked from compilers
        /// that support <see cref="CallerMemberNameAttribute"/>.</param>
        protected void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Raises this object's PropertyChanged event.
        /// </summary>
        /// <param name="args">The PropertyChangedEventArgs</param>
        protected virtual void OnPropertyChanged(PropertyChangedEventArgs args)
        {
            PropertyChanged?.Invoke(this, args);
        }

        public List<object> GetNodesValue()
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
