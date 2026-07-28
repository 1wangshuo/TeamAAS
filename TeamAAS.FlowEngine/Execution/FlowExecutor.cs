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
    /// 流程执行引擎 - 并行任务流模型
    /// 参考 VisionKit ToolManagement.cs 的执行模式：
    /// 1. 无输入连线的节点是任务起点，并行启动
    /// 2. 每个节点等待所有前驱节点完成后再运行
    /// 3. 节点完成后，所有后继节点作为独立任务并行启动
    /// 4. Decision 节点只启动匹配分支的后继节点
    /// </summary>
    public class FlowExecutor
    {
        private readonly FlowGraph _graph;
        private CancellationToken _token;

        /// <summary>已启动的节点ID集合（防止多前驱重复启动同一后继）</summary>
        private readonly HashSet<string> _startedNodes = new HashSet<string>();
        private readonly object _startLock = new object();

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
            _startedNodes.Clear();
            IsRunning = true;

            bool success = true;

            await Task.Run(async () =>
            {
                try
                {
                    // 重置所有节点状态
                    ResetAllNodes();

                    // 找到根节点（没有输入连线的节点）
                    var rootNodes = GetRootNodes();

                    if (rootNodes.Count == 0 && _graph.Nodes.Count > 0)
                    {
                        // 没有根节点但有节点（可能有环），取全部节点
                        rootNodes = _graph.Nodes.ToList();
                    }

                    // 每个根节点作为独立任务并行启动
                    var tasks = rootNodes.Select(n => RunNodeAsync(n)).ToList();
                    await Task.WhenAll(tasks);
                }
                catch (OperationCanceledException)
                {
                    success = false;
                }
                catch (Exception)
                {
                    success = false;
                }
            });

            IsRunning = false;

            Application.Current?.Dispatcher.Invoke(() =>
            {
                ExecutionCompleted?.Invoke(success);
            });
        }

        /// <summary>
        /// 运行单个节点（含等待前驱、执行、启动后继）
        /// </summary>
        private async Task RunNodeAsync(FlowNode node)
        {
            // 防止多前驱重复启动同一后继
            lock (_startLock)
            {
                if (_startedNodes.Contains(node.NodeId)) return;
                _startedNodes.Add(node.NodeId);
            }

            if (_token.IsCancellationRequested) return;

            // 获取前驱节点
            var predecessors = GetPredecessors(node);

            // 等待所有前驱节点完成
            if (predecessors.Count > 0)
            {
                while (true)
                {
                    if (_token.IsCancellationRequested) return;

                    // 如果有前驱节点失败，跳过本节点
                    if (predecessors.Any(p => p.Status == NodeRunStatus.Failed))
                    {
                        UpdateNodeStatus(node, NodeRunStatus.Skipped, 0);
                        await StartSuccessorsAsync(node, null);
                        return;
                    }

                    // 所有前驱节点都完成（Success 或 Skipped）则继续
                    if (predecessors.All(p => p.Status == NodeRunStatus.Success || p.Status == NodeRunStatus.Skipped))
                        break;

                    try
                    {
                        await Task.Delay(50, _token);
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                }
            }

            // 禁用的节点直接跳过，但仍启动后继
            if (!node.IsEnabled)
            {
                UpdateNodeStatus(node, NodeRunStatus.Skipped, 0);
                await StartSuccessorsAsync(node, null);
                return;
            }

            // 标记运行中
            UpdateNodeStatus(node, NodeRunStatus.Running, 0);

            // 短暂延迟让UI能看到Running状态
            Thread.Sleep(50);

            // 执行节点
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
                    // 无插件的节点直接标记成功
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

            // 启动后继节点
            await StartSuccessorsAsync(node, branchResult);
        }

        /// <summary>
        /// 启动后继节点（每个后继作为独立并行任务）
        /// </summary>
        private async Task StartSuccessorsAsync(FlowNode node, bool? branchResult)
        {
            var outgoing = _graph.Connections
                .Where(c => c.SourceNodeId == node.NodeId)
                .ToList();

            // Decision节点：只走匹配的分支
            // Right = True分支, Bottom = False分支
            if (node.Category == NodeCategory.Decision && branchResult.HasValue)
            {
                outgoing = outgoing.Where(c =>
                    branchResult.Value ? c.SourceSide == PortSide.Right : c.SourceSide == PortSide.Bottom
                ).ToList();
            }

            // 每个后继作为独立任务并行启动
            var tasks = new List<Task>();
            foreach (var conn in outgoing)
            {
                if (_token.IsCancellationRequested) break;

                var target = _graph.GetNode(conn.TargetNodeId);
                if (target != null)
                {
                    tasks.Add(RunNodeAsync(target));
                }
            }

            if (tasks.Count > 0)
                await Task.WhenAll(tasks);
        }

        /// <summary>
        /// 获取前驱节点
        /// </summary>
        private List<FlowNode> GetPredecessors(FlowNode node)
        {
            return _graph.Connections
                .Where(c => c.TargetNodeId == node.NodeId)
                .Select(c => _graph.GetNode(c.SourceNodeId))
                .Where(n => n != null)
                .ToList();
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
    }
}
