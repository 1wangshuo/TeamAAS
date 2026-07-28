using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeamAAS.Robot.Models
{
    /// <summary>
    /// 机器人工具坐标
    /// </summary>
    public class RobotTool:BindableBase
    {
        private int _Number;
        /// <summary>
        /// 编号
        /// </summary>
        public int Number
        {
            get { return _Number; }
            set { SetProperty(ref _Number,value); }
        }

        private double _X;
        /// <summary>
        /// X坐标
        /// </summary>
        public double X
        {
            get { return _X; }
            set { SetProperty(ref _X, value); }
        }

        private double _Y;
        /// <summary>
        /// Y坐标
        /// </summary>
        public double Y
        {
            get { return _Y; }
            set { SetProperty(ref _Y, value); }
        }
        private double _Z;
        /// <summary>
        /// Z坐标
        /// </summary>
        public double Z
        {
            get { return _Z; }
            set { SetProperty(ref _Z, value); }
        }

        public RobotTool Copy()
        {
            RobotTool newTool = new RobotTool();
            newTool.Number = this.Number;
            newTool.X = this.X;
            newTool.Y = this.Y;
            newTool.Z = this.Z;
            return newTool;
        }
    }
}
