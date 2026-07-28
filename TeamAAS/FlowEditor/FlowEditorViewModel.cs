using Prism.Mvvm;
using Prism.Commands;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using TeamAAS.FlowEditor.Models;
using TeamAAS.FlowEditor.Plugins;
using TeamAAS.FlowEditor.Execution;
using TeamAAS.FlowEditor.Views;

namespace TeamAAS.FlowEditor
{
    /// <summary>
    /// 工具箱分组
    /// </summary>
    public class ToolboxGroup
    {
        public string GroupName { get; set; }
        public NodeCategory Category { get; set; }
        public List<NodePluginInfo> Items { get; set; } = new List<NodePluginInfo>();
    }

    /// <summary>
    /// 流程编辑器 ViewModel
    /// </summary>
    public class FlowEditorViewModel : BindableBase
    {
        #region 属性
        public FlowGraph Graph { get; }

        public ObservableCollection<ToolboxGroup> ToolboxGroups { get; }

        private FlowNode _selectedNode;
        public FlowNode SelectedNode
        {
            get => _selectedNode;
            set => SetProperty(ref _selectedNode, value);
        }

        private double _zoom = 1.0;
        public double Zoom
        {
            get => _zoom;
            set => SetProperty(ref _zoom, value);
        }

        private bool _isRunning;
        public bool IsRunning
        {
            get => _isRunning;
            set => SetProperty(ref _isRunning, value);
        }
        #endregion

        #region 命令
        public DelegateCommand ClearSelectionCommand { get; }
        public DelegateCommand DeleteSelectedCommand { get; }
        public DelegateCommand RunFlowCommand { get; }
        public DelegateCommand StopFlowCommand { get; }
        #endregion

        #region 插件注册
        static FlowEditorViewModel()
        {
            PluginManager.Register(new SleepToolPlugin());
            PluginManager.Register(new IfConditionPlugin());
        }

        public static FlowNode CreateNodeFromPlugin(NodePluginInfo info, double x, double y)
        {
            FlowNode node;

            // 有插件的节点（包括 Normal 和 Decision）使用插件创建
            var plugin = PluginManager.GetPlugin(info.PluginId);
            if (plugin != null)
            {
                node = plugin.CreateNode(x, y);
            }
            else
            {
                // 无插件的节点（ForLoop 等）手动创建
                node = new FlowNode
                {
                    NodeName = info.DisplayName,
                    Category = info.Category,
                    PluginId = info.PluginId,
                    X = x,
                    Y = y,
                    IconGeometry = info.IconGeometry,
                    Properties = new Dictionary<string, object>(info.DefaultProperties)
                };
                node.EnsureDefaultPorts();
            }

            return node;
        }
        #endregion

        #region 构造函数
        public FlowEditorViewModel()
        {
            Graph = new FlowGraph { GraphName = "主流程" };

            ToolboxGroups = new ObservableCollection<ToolboxGroup>
            {
                new ToolboxGroup
                {
                    GroupName = "普通类型",
                    Category = NodeCategory.Normal,
                    Items = new List<NodePluginInfo>
                    {
                        PluginManager.GetPlugin("sleep").Info
                    }
                },
                new ToolboxGroup
                {
                    GroupName = "判断类型",
                    Category = NodeCategory.Decision,
                    Items = new List<NodePluginInfo>
                    {
                        PluginManager.GetPlugin("if").Info
                    }
                },
                new ToolboxGroup
                {
                    GroupName = "工具块",
                    Category = NodeCategory.ToolBlock,
                    Items = new List<NodePluginInfo>
                    {
                        new NodePluginInfo
                        {
                            PluginId = "forloop",
                            DisplayName = "For循环",
                            Category = NodeCategory.ToolBlock,
                            IconGeometry = "M4,4H10V10H4V4M20,4V10H14V4H20M14,14H20V20H14V14M4,20V14H10V20H4Z",
                            Description = "For循环工具块",
                            DefaultProperties = { { "Count", 10 } }
                        }
                    }
                }
            };

            ClearSelectionCommand = new DelegateCommand(() => SelectedNode = null);
            DeleteSelectedCommand = new DelegateCommand(() =>
            {
                if (SelectedNode != null)
                {
                    Graph.RemoveNode(SelectedNode.NodeId);
                    SelectedNode = null;
                }
            });

            RunFlowCommand = new DelegateCommand(async () => await RunFlowAsync(), () => !IsRunning);
            StopFlowCommand = new DelegateCommand(() => StopFlow(), () => IsRunning);
        }
        #endregion

        #region 执行
        private CancellationTokenSource _cts;
        private FlowExecutor _executor;

        public async Task RunFlowAsync()
        {
            if (IsRunning) return;

            _cts = new CancellationTokenSource();
            _executor = new FlowExecutor(Graph);

            IsRunning = true;
            RunFlowCommand.RaiseCanExecuteChanged();
            StopFlowCommand.RaiseCanExecuteChanged();

            _executor.ExecutionCompleted += (success) =>
            {
                IsRunning = false;
                RunFlowCommand.RaiseCanExecuteChanged();
                StopFlowCommand.RaiseCanExecuteChanged();
            };

            await _executor.ExecuteAsync(_cts.Token);
        }

        public void StopFlow()
        {
            _cts?.Cancel();
        }
        #endregion

        #region 方法
        public void CreateNode(double x, double y, NodePluginInfo info)
        {
            var node = CreateNodeFromPlugin(info, x, y);
            Graph.AddNode(node);
        }

        public void OpenPropertyDialog(FlowNode node)
        {
            if (node == null) return;
            var dialog = new NodePropertyDialog(node);
            dialog.ShowDialog();
        }
        #endregion
    }
}
