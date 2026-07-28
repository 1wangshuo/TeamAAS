using Prism.Mvvm;
using Prism.Commands;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using TeamAAS.FlowEditor.Models;
using TeamAAS.FlowEditor.Plugins;
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
        #endregion

        #region 命令
        public DelegateCommand ClearSelectionCommand { get; }
        public DelegateCommand DeleteSelectedCommand { get; }
        #endregion

        #region 插件注册
        private static readonly Dictionary<string, IFlowNodePlugin> _plugins = new Dictionary<string, IFlowNodePlugin>();

        static FlowEditorViewModel()
        {
            RegisterPlugin(new SleepToolPlugin());
        }

        public static void RegisterPlugin(IFlowNodePlugin plugin)
        {
            _plugins[plugin.Info.PluginId] = plugin;
        }

        public static NodePluginInfo FindPluginInfo(string pluginId)
        {
            if (_plugins.TryGetValue(pluginId, out var plugin))
                return plugin.Info;
            return null;
        }

        public static FlowNode CreateNodeFromPlugin(NodePluginInfo info, double x, double y)
        {
            FlowNode node;

            if (info.Category == NodeCategory.Normal && _plugins.TryGetValue(info.PluginId, out var plugin))
            {
                node = plugin.CreateNode(x, y);
            }
            else
            {
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

                if (info.Category == NodeCategory.Decision)
                {
                    node.InputPorts.Add(new NodePort { PortName = "输入", Direction = PortDirection.Input, OwnerNodeId = node.NodeId });
                    node.OutputPorts.Add(new NodePort { PortName = "True", Direction = PortDirection.Output, BranchLabel = "True", OwnerNodeId = node.NodeId });
                    node.OutputPorts.Add(new NodePort { PortName = "False", Direction = PortDirection.Output, BranchLabel = "False", OwnerNodeId = node.NodeId });
                }
                else
                {
                    node.EnsureDefaultPorts();
                }
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
                        _plugins["sleep"].Info
                    }
                },
                new ToolboxGroup
                {
                    GroupName = "判断类型",
                    Category = NodeCategory.Decision,
                    Items = new List<NodePluginInfo>
                    {
                        new NodePluginInfo
                        {
                            PluginId = "branch",
                            DisplayName = "条件分支",
                            Category = NodeCategory.Decision,
                            IconGeometry = "M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2M12,20A8,8 0 0,1 4,12A8,8 0 0,1 12,4A8,8 0 0,1 20,12A8,8 0 0,1 12,20M12,6A6,6 0 0,0 6,12A6,6 0 0,0 12,18A6,6 0 0,0 18,12A6,6 0 0,0 12,6M12,10A2,2 0 0,1 14,12A2,2 0 0,1 12,14A2,2 0 0,1 10,12A2,2 0 0,1 12,10Z",
                            Description = "条件判断分支节点"
                        }
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
