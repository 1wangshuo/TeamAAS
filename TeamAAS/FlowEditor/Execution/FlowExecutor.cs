using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using TeamAAS.FlowEditor.Models;
using TeamAAS.FlowEditor.Plugins;

namespace TeamAAS.FlowEditor.Execution
{
    /// <summary>
    /// 流程执行引擎 - DFS遍历 + 分支路由
    /// </summary>
    public class FlowExecutor
    {
        private readonly FlowGraph _graph;
        private readonly HashSet<string> _visited = new HashSet<string>();
        private CancellationToken _token;

        public bool IsRunning { get; private set; }

        /// <summary>
        /// 节点状态变化事件（在UI线程触发）
        /// </summary>
        public event Action<FlowNode> NodeStatusChanged;

        /// <summary>
        /// 执行完成事件（在UI线程触发）
        /// </summary>
        public event Action<bool> ExecutionCompleted;

        public FlowExecutor(FlowGraph graph)
        {
            _graph = graph;
        }

        /// <summary>
        /// 异步执行整个流程图
        /// </summary>
        public async Task ExecuteAsync(CancellationToken token = default)
        {
            _token = token;
            _visited.Clear();
            IsRunning = true;

            bool success = true;

            await Task.Run(() =>
            {
                try
                {
                    // 重置所有节点状态
                    ResetAllNodes();

                    // 找到根节点（没有输入连线的节点）
                    var rootNodes = GetRootNodes();

                    if (rootNodes.Count == 0 && _graph.Nodes.Count > 0)
                    {
                        // 没有根节点但有节点，说明可能有环（不应该发生，但防御性处理）
                        rootNodes = _graph.Nodes.ToList();
                    }

                    foreach (var root in rootNodes)
                    {
                        if (_token.IsCancellationRequested) break;
                        ExecuteFromNode(root);
                    }
                }
                catch (OperationCanceledException)
                {
                    success = false;
                }
                catch (Exception)
                {
                    success = false;
                }
            }, _token);

            IsRunning = false;

            // 在UI线程触发完成事件
            Application.Current?.Dispatcher.Invoke(() =>
            {
                ExecutionCompleted?.Invoke(success);
            });
        }

        /// <summary>
        /// DFS执行节点
        /// </summary>
        private void ExecuteFromNode(FlowNode node)
        {
            if (node == null || _visited.Contains(node.NodeId)) return;
            if (_token.IsCancellationRequested) return;

            _visited.Add(node.NodeId);

            // 禁用的节点跳过
            if (!node.IsEnabled)
            {
                UpdateNodeStatus(node, NodeRunStatus.Skipped, 0);
                ExecuteSuccessors(node, null);
                return;
            }

            // 标记运行中
            UpdateNodeStatus(node, NodeRunStatus.Running, 0);

            // 短暂延迟让UI能看到Running状态
            Thread.Sleep(50);

            var sw = Stopwatch.StartNew();
            NodeRunStatus result;
            bool? branchResult = null;

            try
            {
                var plugin = PluginManager.GetPlugin(node.PluginId);
                if (plugin != null)
                {
                    result = plugin.Execute(node.Properties);

                    // Decision节点获取分支结果
                    if (node.Category == NodeCategory.Decision &&
                        node.Properties != null &&
                        node.Properties.TryGetValue("__BranchResult", out var br) && br is bool b)
                    {
                        branchResult = b;
                    }
                }
                else
                {
                    // 无插件的节点（如ForLoop等尚未实现的）直接标记成功
                    result = NodeRunStatus.Success;
                }
            }
            catch (Exception)
            {
                result = NodeRunStatus.Failed;
            }

            sw.Stop();
            UpdateNodeStatus(node, result, (int)sw.ElapsedMilliseconds);

            // 执行失败则不继续后续节点
            if (result == NodeRunStatus.Failed) return;

            // 执行后续节点
            ExecuteSuccessors(node, branchResult);
        }

        /// <summary>
        /// 执行后续节点
        /// </summary>
        private void ExecuteSuccessors(FlowNode node, bool? branchResult)
        {
            var outgoing = _graph.Connections
                .Where(c => c.SourceNodeId == node.NodeId)
                .ToList();

            foreach (var conn in outgoing)
            {
                if (_token.IsCancellationRequested) break;

                // Decision节点：只走匹配的分支
                // Right = True分支, Bottom = False分支
                if (node.Category == NodeCategory.Decision && branchResult.HasValue)
                {
                    bool isTrueBranch = conn.SourceSide == PortSide.Right;
                    bool isFalseBranch = conn.SourceSide == PortSide.Bottom;

                    if (branchResult.Value && !isTrueBranch) continue;
                    if (!branchResult.Value && !isFalseBranch) continue;
                }

                var target = _graph.GetNode(conn.TargetNodeId);
                if (target != null)
                {
                    ExecuteFromNode(target);
                }
            }
        }

        /// <summary>
        /// 更新节点状态（确保在UI线程执行）
        /// </summary>
        private void UpdateNodeStatus(FlowNode node, NodeRunStatus status, int costTime)
        {
            node.Status = status;
            node.CostTime = costTime;

            Application.Current?.Dispatcher.Invoke(() =>
            {
                node.NotifyStatusChanged();
                NodeStatusChanged?.Invoke(node);
            });
        }

        /// <summary>
        /// 重置所有节点状态
        /// </summary>
        private void ResetAllNodes()
        {
            foreach (var node in _graph.Nodes)
            {
                node.Status = NodeRunStatus.NotStarted;
                node.CostTime = 0;
            }

            Application.Current?.Dispatcher.Invoke(() =>
            {
                foreach (var node in _graph.Nodes)
                {
                    node.NotifyStatusChanged();
                }
            });
        }

        /// <summary>
        /// 获取根节点（没有输入连线的节点）
        /// </summary>
        private List<FlowNode> GetRootNodes()
        {
            var targetIds = new HashSet<string>(
                _graph.Connections.Select(c => c.TargetNodeId)
            );
            return _graph.Nodes
                .Where(n => !targetIds.Contains(n.NodeId))
                .ToList();
        }
    }
}
