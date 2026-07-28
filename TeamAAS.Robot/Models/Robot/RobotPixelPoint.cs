using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeamAAS.Robot.Models.Robot
{
    /// <summary>
    /// 机器人与像素坐标
    /// </summary>
    public class RobotPixelPoint : BindableBase
    {
        private int _Number;

        public int Number
        {
            get { return _Number; }
            set { SetProperty(ref _Number,value); }
        }

        private string _Description;

        public string Description
        {
            get { return _Description; }
            set { SetProperty(ref _Description, value); }
        }


        private PointF _Robot;

		/// <summary>
		/// 机器人坐标
		/// </summary>
		public PointF Robot
		{
			get { return _Robot; }
			set { SetProperty(ref _Robot,value); }
		}

        private PointF _Pixel;

        /// <summary>
        /// 像素坐标
        /// </summary>
        public PointF Pixel
        {
            get { return _Pixel; }
            set { SetProperty(ref _Pixel, value); }
        }

        public RobotPixelPoint Clone()
        {
            return new RobotPixelPoint()
            {
                Number = this.Number,
                Description = this.Description,
                Robot = new PointF(this.Robot.X, this.Robot.Y),
                Pixel = new PointF(this.Pixel.X, this.Pixel.Y)
            };
        }
    }
}
