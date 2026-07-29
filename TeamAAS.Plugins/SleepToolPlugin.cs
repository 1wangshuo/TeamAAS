using System.Collections.Generic;
using System.Threading;
using TeamAAS.FlowEditor.Models;

namespace TeamAAS.FlowEditor.Plugins
{
    /// <summary>
    /// 休眠工具插件 - 第一个打样插件
    /// </summary>
    public class SleepToolPlugin : IFlowNodePlugin
    {
        public NodePluginInfo Info { get; } = new NodePluginInfo
        {
            PluginId = "sleep",
            DisplayName = "休眠",
            Category = NodeCategory.Normal,
            IconGeometry = "M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2M12,20A8,8 0 0,1 4,12A8,8 0 0,1 12,4A8,8 0 0,1 20,12A8,8 0 0,1 12,20M12.5,7V12.25L17,14.92L16.25,16.15L11,13V7H12.5Z",
            Description = "延时等待指定毫秒数",
            DefaultProperties = new Dictionary<string, object>
            {
                { "Duration", 1000 }
            }
        };

        public string PropertyEditorType => null; // 使用默认编辑器

        public FlowNode CreateNode(double x, double y)
        {
            var node = new FlowNode
            {
                NodeName = "休眠",
                Category = NodeCategory.Normal,
                PluginId = Info.PluginId,
                X = x,
                Y = y,
                IconGeometry = Info.IconGeometry,
                Properties = new Dictionary<string, object>(Info.DefaultProperties)
            };
            node.EnsureDefaultPorts();
            return node;
        }

        public NodeExecuteResult Execute(Dictionary<string, object> properties)
        {
            int duration = 1000;
            if (properties != null && properties.TryGetValue("Duration", out var val))
            {
                if (val is int intVal)
                    duration = intVal;
                else if (val != null && int.TryParse(val.ToString(), out var parsed))
                    duration = parsed;
            }

            if (duration > 0)
                Thread.Sleep(duration);

            return new NodeExecuteResult
            {
                Status = NodeRunStatus.Success,
                Results = new Dictionary<string, object>
                {
                    { "休眠时间(ms)", duration },
                    { "实际耗时(ms)", duration }
                }
            };
        }
    }
}
