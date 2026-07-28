using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeamAAS.Robot.Enums
{
    /// <summary>
    /// TCP连接方式
    /// </summary>
    public enum TCPConnectType
    {
        [Description("软件作为服务器")]
        Server =0,

        [Description("软件作为客户端")]
        Client =1,
    }
}
