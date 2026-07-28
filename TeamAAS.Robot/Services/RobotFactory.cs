using System;
using TeamAAS.Robot.Core.Robots;
using TeamAAS.Robot.Enums;
using TeamAAS.Robot.Interfaces;
using TeamAAS.Robot.Models;
using TeamAAS.Robot.Models.Robot;

namespace TeamAAS.Robot.Services
{
    /// <summary>
    /// 机器人工厂，根据品牌创建机器人实例
    /// </summary>
    public static class RobotFactory
    {
        public static IRobot CreateRobot(RobotInfo robotInfo)
        {
            if (robotInfo == null) throw new ArgumentNullException(nameof(robotInfo));

            return robotInfo.RobotBrand switch
            {
                RobotBrand.EPSON => new EpsonRobot(robotInfo),
                RobotBrand.FANUC => new FanucRobot(robotInfo),
                RobotBrand.Schneider => new SchneiderRobot(robotInfo),
                RobotBrand.XYZ_Platform or RobotBrand.XYZU_Platform => new EpsonRobot(robotInfo),
                _ => new EpsonRobot(robotInfo),
            };
        }
    }
}
