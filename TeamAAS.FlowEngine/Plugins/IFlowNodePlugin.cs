using System.Collections.Generic;
using TeamAAS.FlowEditor.Models;

namespace TeamAAS.FlowEditor.Plugins
{
    /// <summary>
    /// 节点插件信息（工具箱展示用）
    /// </summary>
    public class NodePluginInfo
    {
        public string PluginId { get; set; }
        public string DisplayName { get; set; }
        public NodeCategory Category { get; set; }
        public string IconGeometry { get; set; }
        public string Description { get; set; }

        /// <summary>
        /// 默认属性键值对
        /// </summary>
        public Dictionary<string, object> DefaultProperties { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// 节点执行结果
    /// </summary>
    public class NodeExecuteResult
    {
        public NodeRunStatus Status { get; set; } = NodeRunStatus.Success;
        public Dictionary<string, object> Results { get; set; } = new Dictionary<string, object>();

        public static implicit operator NodeExecuteResult(NodeRunStatus status)
            => new NodeExecuteResult { Status = status };
    }

    /// <summary>
    /// 节点插件接口 - 每个工具节点对应一个插件实现
    /// </summary>
    public interface IFlowNodePlugin
    {
        /// <summary>
        /// 插件信息
        /// </summary>
        NodePluginInfo Info { get; }

        /// <summary>
        /// 创建默认节点
        /// </summary>
        FlowNode CreateNode(double x, double y);

        /// <summary>
        /// 执行节点逻辑，返回执行状态和结果数据
        /// </summary>
        NodeExecuteResult Execute(Dictionary<string, object> properties);

        /// <summary>
        /// 获取属性编辑器类型（null表示使用默认编辑器）
        /// </summary>
        string PropertyEditorType { get; }
    }
}
