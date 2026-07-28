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
    /// 1. 无输入连线的节点是任务起点，每个作为独立线程启动
    /// 2. 每个节点等待所有前驱节点完成后再运行（IF判断除外）
    /// 3. 节点完成后，启动后继节点为独立线程（fire-and-forget），自身立即返回
    /// 4. Decision 节点只启动匹配分支的后继
    /// 5. 主线程通过计数器等待所有节点完成
    /// </summary>
    public class FlowExecutor
    {
        private readonly FlowGraph _graph;
        private CancellationToken _token;

        /// <summary>已启动的节点ID集合（防止多前驱重复启动同一后继）</summary>
        private readonly HashSet<string> _startedNodes = new HashSet<string>();
        private readonly object _startLock = new object();

        /// <summary>正在运行的节点计数（主线程等待此计数归零）</summary>
        private int _pendingCount = 0;
        private readonly object _pendingLock = new object();

        public bool IsRunning { get; private set; }

        public event Action<FlowNode> NodeStatusChanged;
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
            _pendingCount = 0;
            IsRunning = true;

            bool success = true;

            await Task.Run(() =>
            {
                try
                {
                    ResetAllNodes();

                    var rootNodes = GetRootNodes();
                    if (rootNodes.Count == 0 && _graph.Nodes.Count > 0)
                        rootNodes = _graph.Nodes.ToList();

                    // 每个根节点作为独立线程启动
                    foreach (var root in rootNodes)
                    {
                        StartNode(root);
                    }

                    // 等待所有节点完成（计数器归零）
                    lock (_pendingLock)
                    {
                        while (_pendingCount > 0)
                        {
                            if (_token.IsCancellationRequested) break;
                            Monitor.Wait(_pendingLock, 100);
                        }
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
            });

            IsRunning = false;

            Application.Current?.Dispatcher.Invoke(() =>
            {
                ExecutionCompleted?.Invoke(success);
            });
        }

        /// <summary>
        /// 启动一个节点为独立线程（fire-and-forget）
        /// </summary>
        private void StartNode(FlowNode node)
        {
            // 去重：多前驱可能同时尝试启动同一后继
            lock (_startLock)
            {
                if (_startedNodes.Contains(node.NodeId)) return;
                _startedNodes.Add(node.NodeId);
            }

            // 计数+1
            lock (_pendingLock) { _pendingCount++; }

            // 每个节点独立线程运行，不等待
            Task.Run(async () =>
            {
                try
                {
                    await RunNodeAsync(node);
                }
                catch { }
                finally
                {
                    // 计数-1，通知主线程
                    lock (_pendingLock)
                    {
                        _pendingCount--;
                        Monitor.Pulse(_pendingLock);
                    }
                }
            });
        }

        /// <summary>
        /// 运行单个节点：等待前驱 → 执行 → 启动后继
        /// </summary>
        private async Task RunNodeAsync(FlowNode node)
        {
            if (_token.IsCancellationRequested) return;

            var predecessors = GetPredecessors(node);

            // 等待所有前驱节点完成
            if (predecessors.Count > 0)
            {
                while (true)
                {
                    if (_token.IsCancellationRequested) return;

                    // 前驱有失败 → 跳过本节点，但仍启动后继
                    if (predecessors.Any(p => p.Status == NodeRunStatus.Failed))
                    {
                        UpdateNodeStatus(node, NodeRunStatus.Skipped, 0);
                        StartSuccessors(node, null);
                        return;
                    }

                    // 所有前驱完成（Success或Skipped）→ 继续运行
                    if (predecessors.All(p => p.Status == NodeRunStatus.Success || p.Status == NodeRunStatus.Skipped))
                        break;

                    try { await Task.Delay(50, _token); }
                    catch (OperationCanceledException) { return; }
                }
            }

            // 禁用的节点直接跳过，但仍启动后继
            if (!node.IsEnabled)
            {
                UpdateNodeStatus(node, NodeRunStatus.Skipped, 0);
                StartSuccessors(node, null);
                return;
            }

            // 标记运行中
            UpdateNodeStatus(node, NodeRunStatus.Running, 0);
            Thread.Sleep(50); // 短暂延迟让UI能看到Running状态

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

            // 启动后继节点（fire-and-forget，不等待）
            StartSuccessors(node, branchResult);
        }

        /// <summary>
        /// 启动后继节点（每个后继作为独立线程，不等待）
        /// </summary>
        private void StartSuccessors(FlowNode node, bool? branchResult)
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

            foreach (var conn in outgoing)
            {
                if (_token.IsCancellationRequested) break;
                var target = _graph.GetNode(conn.TargetNodeId);
                if (target != null)
                {
                    StartNode(target);
                }
            }
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
