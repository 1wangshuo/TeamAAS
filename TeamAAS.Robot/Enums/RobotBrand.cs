using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeamAAS.Robot.Enums
{
    /// <summary>
    /// 机器人品牌
    /// </summary>
    public enum RobotBrand
    {
        [Description("默认")]
        Default = 0,

        [Description("爱普生")]
        EPSON = 1,

        [Description("发那科")]
        FANUC =2,

        [Description("施耐德")]
        Schneider =3,

        [Description("XYZ模组平台")]
        XYZ_Platform = 11,

        [Description("XYZU模组平台")]
        XYZU_Platform = 12,
    }
}
