using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeamAAS.Robot.Core.Robots;
using TeamAAS.Robot.Interfaces;
using TeamAAS.Robot.Models;
using TouchSocket.Core;
using TouchSocket.Sockets;

namespace TeamAAS.Robot.Services
{
    public class RobotService : IRobotService, IDisposable
    {
        private readonly Dictionary<Guid, IRobot> _robotCollection;
        private readonly object _sync = new object();

        public RobotService()
        {
            _robotCollection = new Dictionary<Guid, IRobot>();
        }

        /// <summary>
        /// 创建机器人
        /// </summary>
        /// <param name="id"></param>
        /// <param name="robotInfo"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public bool CreateRobot(Guid id, RobotInfo robotInfo)
        {
            if (robotInfo == null) throw new ArgumentNullException(nameof(robotInfo));

            var robot = CreateRobotInstance(robotInfo);
            robot.Id = id;

            lock (_sync)
            {
                if (_robotCollection.ContainsKey(id))
                {
                    var currentRobot = _robotCollection[id];
                    try
                    {
                        currentRobot.Dispose();
                    }
                    catch
                    {
                        // 忽略单个机器人 Dispose 异常，继续替换
                    }
                }

                _robotCollection[id] = robot;
            }

            return true;
        }

        /// <summary>
        /// 创建机器人
        /// </summary>
        /// <param name="id"></param>
        /// <param name="robotInfo"></param>
        /// <returns></returns>
        public Task<bool> CreateRobotAsync(Guid id, RobotInfo robotInfo)
        {
            return Task.Run(() =>
            {
                return CreateRobot(id, robotInfo);
            });
        }

        public IRobot GetRobot(Guid id)
        {
            lock (_sync)
            {
                if (_robotCollection.TryGetValue(id, out var robot))
                {
                    return robot;
                }
            }
            return null;
        }

        public Task<IRobot> GetRobotAsync(Guid id)
        {
            return Task.Run(() =>
            {
                return GetRobot(id);
            });
        }

        public void UnRegisterRobot(Guid id)
        {
            IRobot robot = null;
            lock (_sync)
            {
                if (_robotCollection.TryGetValue(id, out robot))
                {
                    _robotCollection.Remove(id);
                }
            }
            if (robot != null)
            {
                try
                {
                    robot.Dispose();
                }
                catch
                {
                    // 忽略释放异常
                }
            }
        }

        public Task UnRegisterRobotAsync(Guid id)
        {
            
            return Task.Run(()=> UnRegisterRobot(id));
        }

        /// <summary>
        /// 获取所有机器人
        /// </summary>
        /// <returns></returns>
        public IReadOnlyCollection<IRobot> GetAllRobots()
        {
            lock (_sync)
            {
                return _robotCollection.Values.ToList().AsReadOnly();
            }
        }

        public Task<IReadOnlyCollection<IRobot>> GetAllRobotsAsync()
        {
            return Task.Run(() =>
            {
                return GetAllRobots();
            });
        }

        /// <summary>
        /// 尝试获取机器人
        /// </summary>
        /// <param name="id"></param>
        /// <param name="robot"></param>
        /// <returns></returns>
        public bool TryGetRobot(Guid id, out IRobot robot)
        {
            lock (_sync)
            {
                return _robotCollection.TryGetValue(id, out robot);
            }
        }

        public Task<(bool found, IRobot robot)> TryGetRobotAsync(Guid id)
        {
            
            return Task.Run(() =>
            {
                IRobot robot;
                bool found;
                lock (_sync)
                {
                    found = _robotCollection.TryGetValue(id, out robot);
                }
                return (found, robot);
            });
        }

        /// <summary>
        /// 检查是否存在
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool ContainsRobot(Guid id)
        {
            lock (_sync)
            {
                return _robotCollection.ContainsKey(id);
            }
        }

        public Task<bool> ContainsRobotAsync(Guid id)
        {
            return Task.Run(() =>
            {
                return ContainsRobot(id);
            });
        }

        /// <summary>
        /// 移除机器人并释放资源，返回是否成功移除
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool RemoveRobot(Guid id)
        {
            IRobot robot = null;
            lock (_sync)
            {
                if (_robotCollection.TryGetValue(id, out robot))
                {
                    _robotCollection.Remove(id);
                }
            }
            if (robot != null)
            {
                try
                {
                    robot.Dispose();
                }
                catch
                {
                    // 忽略释放异常
                }
                return true;
            }
            return false;
        }

        public Task<bool> RemoveRobotAsync(Guid id)
        {
            return Task.Run(() =>
            {
                return RemoveRobot(id);
            });
        }

        public IRobot GetRobotByNumber(int robotNo)
        {
            if (TryFindRobotByNumber(robotNo, out _, out var robot))
            {
                return robot;
            }
            return null;
        }

        public Task<IRobot> GetRobotByNumberAsync(int robotNo)
        {
           return Task.Run(() =>
           {
               return GetRobotByNumber(robotNo);
           });
        }

        public Task UnRegisterRobotByNumberAsync(int robotNo)
        {
            return Task.Run(() => UnRegisterRobotByNumber(robotNo));
        }

        public void UnRegisterRobotByNumber(int robotNo)
        {
            if (TryFindRobotByNumber(robotNo, out var id, out var robot))
            {
                // 复用已有的方法以保持行为一致（线程安全、异常处理）
                UnRegisterRobot(id);
            }
        }

        /// <summary>
        /// 移除所有机器人
        /// </summary>
        public void RemoveAllRobots()
        {
            //依次移除每个机器人，确保资源释放
            List<Guid> robotIds;
            lock (_sync)
            {
                robotIds = _robotCollection.Keys.ToList();
            }
            foreach (var id in robotIds)
            {
                RemoveRobot(id);
            }
        }

        /// <summary>
        /// 初始化所有机器人
        /// </summary>
        /// <param name="robots"></param>
        /// <returns></returns>
        public (bool IsSucceed, string Message) InitializeAllRobots(RobotInfo[] robots)
        {
            //清除现有机器人集合
            RemoveAllRobots();

            //根据传入的机器人信息数组，依次创建并注册机器人实例
            foreach (var robotInfo in robots)
            {
                var robot = CreateRobotInstance(robotInfo);
                lock (_sync)
                {
                    _robotCollection[robot.Id] = robot;
                }
            }

            bool allConnected = true;
            StringBuilder errorMessages = new StringBuilder();
            //获取所有机器人并连接
            var allRobots = GetAllRobots();
            foreach (var robot in allRobots)
            {
                try
                {
                    robot.Connect();
                }
                catch
                {
                    //忽略单个机器人连接异常，继续连接其他机器人
                }
                if (!robot.IsConnected)
                {
                    allConnected = false;
                    errorMessages.AppendLine($"机器人[{robot.Name}]连接失败。");
                }
            }
            if (allConnected)
            {
                return (true, "所有机器人初始化并连接成功。");
            }
            else
            {
                return (false, errorMessages.ToString());
            }
        }

        /// <summary>
        /// 初始化所有机器人 （异步）
        /// </summary>
        /// <param name="robots"></param>
        /// <returns></returns>
        public Task<(bool IsSucceed, string Message)> InitializeAllRobotsAsync(RobotInfo[] robots)
        {

            return Task.Run(() =>
            {
                return InitializeAllRobots(robots);
            });

        }

        // 私有工厂方法，避免重复代码
        private IRobot CreateRobotInstance(RobotInfo robotInfo)
        {
            if (robotInfo == null) throw new ArgumentNullException(nameof(robotInfo));

            IRobot robot;
            if (robotInfo.RobotBrand == Enums.RobotBrand.EPSON)
            {
                robot = new EpsonRobot(robotInfo);
            }
            else if (robotInfo.RobotBrand == Enums.RobotBrand.FANUC)
            {
                robot = new FanucRobot(robotInfo);
            }
            else if (robotInfo.RobotBrand == Enums.RobotBrand.Schneider)
            {
                robot = new SchneiderRobot(robotInfo);
            }
            else if (robotInfo.RobotBrand == Enums.RobotBrand.XYZ_Platform || robotInfo.RobotBrand == Enums.RobotBrand.XYZU_Platform)
            {
                //robot = new XYZ_Platform(robotInfo);
                robot = new EpsonRobot(robotInfo);
            }
            else
            {
                robot = new EpsonRobot(robotInfo);
            }

            return robot;
        }

        /// <summary>
        /// 更新机器人编号
        /// </summary>
        /// <param name="id"></param>
        /// <param name="newNumber"></param>
        public void UpdateRobotNumber(Guid id, int newNumber)
        {
            IRobot robot;
            lock (_sync)
            {
                if (_robotCollection.TryGetValue(id, out robot))
                {
                    robot.RobotNo = newNumber;
                }
            }
        }

        // 私有帮助方法：通过机器人编号查找字典中的条目（线程安全）
        private bool TryFindRobotByNumber(int robotNo, out Guid id, out IRobot robot)
        {
            lock (_sync)
            {
                foreach (var kvp in _robotCollection)
                {
                    var r = kvp.Value;
                    if (r == null) continue;

                    object rawValue = null;
                    var type = r.GetType();

                    // 1. 直接在机器人对象上查找常见编号属性
                    var prop = type.GetProperty("RobotNo") ??
                               type.GetProperty("RobotNumber") ??
                               type.GetProperty("Number") ??
                               type.GetProperty("No");
                    if (prop != null)
                    {
                        try
                        {
                            rawValue = prop.GetValue(r);
                        }
                        catch
                        {
                            rawValue = null;
                        }
                    }

                    // 2. 如果未找到，尝试从 RobotInfo / Info 属性中获取编号
                    if (rawValue == null)
                    {
                        var infoProp = type.GetProperty("RobotInfo") ?? type.GetProperty("Info");
                        if (infoProp != null)
                        {
                            try
                            {
                                var infoObj = infoProp.GetValue(r);
                                if (infoObj != null)
                                {
                                    var infoType = infoObj.GetType();
                                    var numberProp = infoType.GetProperty("RobotNo") ??
                                                     infoType.GetProperty("RobotNumber") ??
                                                     infoType.GetProperty("Number") ??
                                                     infoType.GetProperty("No");
                                    if (numberProp != null)
                                    {
                                        try
                                        {
                                            rawValue = numberProp.GetValue(infoObj);
                                        }
                                        catch
                                        {
                                            rawValue = null;
                                        }
                                    }
                                }
                            }
                            catch
                            {
                                // 忽略反射读取错误
                                rawValue = null;
                            }
                        }
                    }

                    if (rawValue != null)
                    {
                        int parsed;
                        try
                        {
                            if (rawValue is int) parsed = (int)rawValue;
                            else parsed = Convert.ToInt32(rawValue);
                        }
                        catch
                        {
                            // 不能转换则跳过
                            continue;
                        }

                        if (parsed == robotNo)
                        {
                            id = kvp.Key;
                            robot = r;
                            return true;
                        }
                    }
                }
            }

            id = Guid.Empty;
            robot = null;
            return false;
        }

        // 实现 IDisposable，释放所有机器人并清空集合
        public void Dispose()
        {
            List<IRobot> robots;
            lock (_sync)
            {
                robots = _robotCollection.Values.ToList();
                _robotCollection.Clear();
            }

            foreach (var r in robots)
            {
                try
                {
                    r.Dispose();
                }
                catch
                {
                    // 忽略单个释放异常
                }
            }

            GC.SuppressFinalize(this);
        }
    }
}
