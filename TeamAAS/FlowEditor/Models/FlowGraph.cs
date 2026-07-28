using Prism.Mvvm;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TeamAAS.FlowEditor.Models
{
    /// <summary>
    /// 流程图模型 - 管理节点和连线的集合
    /// </summary>
    public class FlowGraph : BindableBase
    {
        public string GraphId { get; set; } = System.Guid.NewGuid().ToString("N");

        public string GraphName
        {
            get => _graphName;
            set => SetProperty(ref _graphName, value);
        }
        private string _graphName = "新流程";

        public ObservableCollection<FlowNode> Nodes { get; set; } = new ObservableCollection<FlowNode>();
        public ObservableCollection<FlowConnection> Connections { get; set; } = new ObservableCollection<FlowConnection>();

        /// <summary>
        /// 添加节点
        /// </summary>
        public void AddNode(FlowNode node)
        {
            node.EnsureDefaultPorts();
            Nodes.Add(node);
        }

        /// <summary>
        /// 删除节点及其关联连线
        /// </summary>
        public void RemoveNode(string nodeId)
        {
            // 删除关联连线
            var connectionsToRemove = new List<FlowConnection>();
            foreach (var conn in Connections)
            {
                if (conn.SourceNodeId == nodeId || conn.TargetNodeId == nodeId)
                    connectionsToRemove.Add(conn);
            }
            foreach (var conn in connectionsToRemove)
                Connections.Remove(conn);

            // 删除节点
            for (int i = Nodes.Count - 1; i >= 0; i--)
            {
                if (Nodes[i].NodeId == nodeId)
                {
                    Nodes.RemoveAt(i);
                    break;
                }
            }
        }

        /// <summary>
        /// 添加连线（自动检查重复和环）
        /// </summary>
        public bool TryAddConnection(FlowConnection connection)
        {
            // 检查重复 — 同一对节点之间不允许重复连线
            foreach (var conn in Connections)
            {
                if (conn.SourceNodeId == connection.SourceNodeId &&
                    conn.TargetNodeId == connection.TargetNodeId)
                    return false;
            }

            // 检查是否形成环
            if (WouldCreateCycle(connection.SourceNodeId, connection.TargetNodeId))
                return false;

            Connections.Add(connection);
            return true;
        }

        /// <summary>
        /// 删除连线
        /// </summary>
        public void RemoveConnection(string connectionId)
        {
            for (int i = Connections.Count - 1; i >= 0; i--)
            {
                if (Connections[i].ConnectionId == connectionId)
                {
                    Connections.RemoveAt(i);
                    break;
                }
            }
        }

        /// <summary>
        /// 检查添加连线后是否形成环
        /// </summary>
        private bool WouldCreateCycle(string sourceId, string targetId)
        {
            // 如果 target 能到达 source，则形成环
            return CanReach(targetId, sourceId, new HashSet<string>());
        }

        private bool CanReach(string fromId, string toId, HashSet<string> visited)
        {
            if (fromId == toId) return true;
            if (visited.Contains(fromId)) return false;
            visited.Add(fromId);

            foreach (var conn in Connections)
            {
                if (conn.SourceNodeId == fromId)
                {
                    if (CanReach(conn.TargetNodeId, toId, visited))
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 获取节点
        /// </summary>
        public FlowNode GetNode(string nodeId)
        {
            foreach (var node in Nodes)
            {
                if (node.NodeId == nodeId) return node;
            }
            return null;
        }
    }
}
