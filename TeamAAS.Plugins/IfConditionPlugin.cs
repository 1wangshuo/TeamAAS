using System.Collections.Generic;
using TeamAAS.FlowEditor.Models;

namespace TeamAAS.FlowEditor.Plugins
{
    /// <summary>
    /// 条件判断插件 - IF节点
    /// 比较 LeftValue [Operator] RightValue，结果决定走True分支(Right端口)还是False分支(Bottom端口)
    /// </summary>
    public class IfConditionPlugin : IFlowNodePlugin
    {
        public NodePluginInfo Info { get; } = new NodePluginInfo
        {
            PluginId = "if",
            DisplayName = "条件判断",
            Category = NodeCategory.Decision,
            IconGeometry = "M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2M12,20A8,8 0 0,1 4,12A8,8 0 0,1 12,4A8,8 0 0,1 20,12A8,8 0 0,1 12,20M12,6A6,6 0 0,0 6,12A6,6 0 0,0 12,18A6,6 0 0,0 18,12A6,6 0 0,0 12,6M12,10A2,2 0 0,1 14,12A2,2 0 0,1 12,14A2,2 0 0,1 10,12A2,2 0 0,1 12,10Z",
            Description = "条件判断，True走右分支，False走下分支",
            DefaultProperties = new Dictionary<string, object>
            {
                { "LeftValue", 0.0 },
                { "Operator", ">" },
                { "RightValue", 0.0 }
            }
        };

        public string PropertyEditorType => null;

        public FlowNode CreateNode(double x, double y)
        {
            var node = new FlowNode
            {
                NodeName = "条件判断",
                Category = NodeCategory.Decision,
                PluginId = Info.PluginId,
                X = x,
                Y = y,
                IconGeometry = Info.IconGeometry,
                Properties = new Dictionary<string, object>(Info.DefaultProperties)
            };
            // Decision 节点：1输入 + True/False 2输出
            node.InputPorts.Add(new NodePort
            {
                PortName = "输入",
                Direction = PortDirection.Input,
                OwnerNodeId = node.NodeId
            });
            node.OutputPorts.Add(new NodePort
            {
                PortName = "True",
                Direction = PortDirection.Output,
                BranchLabel = "True",
                OwnerNodeId = node.NodeId
            });
            node.OutputPorts.Add(new NodePort
            {
                PortName = "False",
                Direction = PortDirection.Output,
                BranchLabel = "False",
                OwnerNodeId = node.NodeId
            });
            return node;
        }

        public NodeExecuteResult Execute(Dictionary<string, object> properties)
        {
            double left = 0, right = 0;
            string op = ">";

            if (properties != null)
            {
                if (properties.TryGetValue("LeftValue", out var lv))
                    double.TryParse(lv?.ToString(), out left);
                if (properties.TryGetValue("RightValue", out var rv))
                    double.TryParse(rv?.ToString(), out right);
                if (properties.TryGetValue("Operator", out var ov))
                    op = ov?.ToString() ?? ">";
            }

            bool result;
            switch (op)
            {
                case ">":  result = left > right; break;
                case "<":  result = left < right; break;
                case ">=": result = left >= right; break;
                case "<=": result = left <= right; break;
                case "==": result = left == right; break;
                case "!=": result = left != right; break;
                default:   result = false; break;
            }

            // 存储分支结果供执行引擎使用
            properties["__BranchResult"] = result;

            return new NodeExecuteResult
            {
                Status = NodeRunStatus.Success,
                Results = new Dictionary<string, object>
                {
                    { "左值", left },
                    { "运算符", op },
                    { "右值", right },
                    { "判断结果", result }
                }
            };
        }
    }
}
