using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeamAAS.Robot.Enums
{
    /// <summary>
    /// 坐标系模式
    /// </summary>
    public enum CoordinateModel
    {
        /// <summary>
        /// 直角正交
        /// </summary>
        [Description("直角正交(Orthogonality)")]
        Orthogonality = 0,

        /// <summary>
        /// 切变
        /// </summary>
        [Description("切变(Shear)")]
        Shear = 1,
    }
}
