using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeamAAS.Robot.Models.Robot
{
    /// <summary>
    /// 多机器人点位表
    /// </summary>
    public class MultipleRobotPoint: RPoint
    {
        private Guid _RobotId;
        /// <summary>
        /// 机器人
        /// </summary>
        public Guid RobotId
        {
            get { return _RobotId; }
            set { SetProperty(ref _RobotId, value); }
        }
    }
}
