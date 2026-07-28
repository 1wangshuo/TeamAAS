using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeamAAS.Robot.Enums
{
    /// <summary>
    /// 结束符
    /// </summary>
    public enum Terminator
    {
        [Description("回车")]
        CR =0,

        [Description("换行")]
        LF =1,

        [Description("回车+换行")]
        CRLF =2,

        [Description("无")]
        None =3
    }
}
