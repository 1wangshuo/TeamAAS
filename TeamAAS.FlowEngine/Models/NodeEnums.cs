using System.ComponentModel;

namespace TeamAAS.FlowEditor.Models
{
    /// <summary>
    /// 节点类型分类
    /// </summary>
    public enum NodeCategory
    {
        [Description("普通")]
        Normal = 0,
        [Description("判断")]
        Decision = 1,
        [Description("工具块")]
        ToolBlock = 2,
        [Description("循环")]
        ForLoop = 3
    }

    /// <summary>
    /// 节点运行状态
    /// </summary>
    public enum NodeRunStatus
    {
        [Description("未运行")]
        NotStarted = 0,
        [Description("运行中")]
        Running = 1,
        [Description("成功")]
        Success = 2,
        [Description("失败")]
        Failed = 3,
        [Description("跳过")]
        Skipped = 4
    }

    /// <summary>
    /// 端口方向
    /// </summary>
    public enum PortDirection
    {
        Input = 0,
        Output = 1
    }

    /// <summary>
    /// 端口所在边
    /// </summary>
    public enum PortSide
    {
        Left = 0,
        Right = 1,
        Top = 2,
        Bottom = 3
    }

    /// <summary>
    /// 端口数据类型
    /// </summary>
    public enum PortDataType
    {
        Any = 0,
        Bool = 1,
        Int = 2,
        Double = 3,
        String = 4,
        Image = 5,
        Object = 6
    }

    /// <summary>
    /// 连线类型
    /// </summary>
    public enum ConnectionType
    {
        DataFlow = 0,
        Branch = 1
    }
}
