using Prism.Mvvm;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace TeamAAS.FlowEditor.Models
{
    /// <summary>
    /// 流程节点基类 - 所有节点类型的公共属性
    /// </summary>
    public class FlowNode : BindableBase
    {
        #region 标识属性
        public string NodeId { get; set; } = System.Guid.NewGuid().ToString("N");

        public string NodeName
        {
            get => _nodeName;
            set => SetProperty(ref _nodeName, value);
        }
        private string _nodeName = "新节点";

        public NodeCategory Category { get; set; } = NodeCategory.Normal;

        /// <summary>
        /// 关联的插件ID（普通节点和工具块节点使用）
        /// </summary>
        public string PluginId { get; set; }
        #endregion

        #region 布局属性
        public double X
        {
            get => _x;
            set => SetProperty(ref _x, value);
        }
        private double _x = 100;

        public double Y
        {
            get => _y;
            set => SetProperty(ref _y, value);
        }
        private double _y = 100;
        #endregion

        #region 运行属性
        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetProperty(ref _isEnabled, value);
        }
        private bool _isEnabled = true;

        public NodeRunStatus Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }
        private NodeRunStatus _status = NodeRunStatus.NotStarted;

        public int CostTime
        {
            get => _costTime;
            set => SetProperty(ref _costTime, value);
        }
        private int _costTime;
        #endregion

        #region 端口
        public List<NodePort> InputPorts { get; set; } = new List<NodePort>();
        public List<NodePort> OutputPorts { get; set; } = new List<NodePort>();
        #endregion

        #region 配置属性
        /// <summary>
        /// 节点配置属性（由插件定义，如休眠时间、循环次数等）
        /// </summary>
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
        #endregion

        #region UI辅助属性
        /// <summary>
        /// 节点图标（Material Design Geometry path 字符串）
        /// </summary>
        public string IconGeometry { get; set; } = "M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2Z";

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (SetProperty(ref _isSelected, value))
                    RaisePropertyChanged(nameof(BorderColor));
            }
        }
        private bool _isSelected;

        /// <summary>
        /// 边框颜色（选中时高亮）
        /// </summary>
        public string BorderColor => IsSelected ? "#007ACC" : "#3F3F46";

        /// <summary>
        /// 节点宽度（可被实际渲染尺寸覆盖）
        /// </summary>
        private double _nodeWidth = 0;
        public double NodeWidth
        {
            get
            {
                if (_nodeWidth > 0) return _nodeWidth;
                switch (Category)
                {
                    case NodeCategory.Decision: return 140;
                    case NodeCategory.ForLoop: return 180;
                    default: return 160;
                }
            }
            set => _nodeWidth = value;
        }

        /// <summary>
        /// 节点高度（可被实际渲染尺寸覆盖）
        /// </summary>
        private double _nodeHeight = 0;
        public double NodeHeight
        {
            get
            {
                if (_nodeHeight > 0) return _nodeHeight;
                switch (Category)
                {
                    case NodeCategory.Decision: return 92;
                    case NodeCategory.ForLoop: return 66;
                    default: return 52;
                }
            }
            set => _nodeHeight = value;
        }

        /// <summary>
        /// 是否有第二个输出端口（判断节点）
        /// </summary>
        public System.Windows.Visibility HasSecondOutput
        {
            get
            {
                return Category == NodeCategory.Decision
                    ? System.Windows.Visibility.Visible
                    : System.Windows.Visibility.Collapsed;
            }
        }

        /// <summary>
        /// 分类对应的标题色（十六进制）
        /// </summary>
        public string CategoryColor
        {
            get
            {
                switch (Category)
                {
                    case NodeCategory.Decision: return "#C2771A";
                    case NodeCategory.ToolBlock: return "#7B1FA2";
                    case NodeCategory.ForLoop: return "#00897B";
                    default: return "#007ACC";
                }
            }
        }

        /// <summary>
        /// 状态文本（属性面板用）
        /// </summary>
        public string StatusText
        {
            get
            {
                switch (Status)
                {
                    case NodeRunStatus.NotStarted: return "未运行";
                    case NodeRunStatus.Running: return "运行中...";
                    case NodeRunStatus.Success: return "成功";
                    case NodeRunStatus.Failed: return "失败";
                    case NodeRunStatus.Skipped: return "跳过";
                    default: return "";
                }
            }
        }

        /// <summary>
        /// 耗时显示文本（节点上显示）
        /// </summary>
        public string CostTimeText
        {
            get
            {
                if (CostTime == 0) return "0ms";
                if (CostTime < 1000) return CostTime + "ms";
                return (CostTime / 1000.0).ToString("F1") + "s";
            }
        }

        /// <summary>
        /// 状态指示色（十六进制）
        /// </summary>
        public string StatusColor
        {
            get
            {
                switch (Status)
                {
                    case NodeRunStatus.NotStarted: return "#888888";
                    case NodeRunStatus.Running: return "#FF9800";
                    case NodeRunStatus.Success: return "#4CAF50";
                    case NodeRunStatus.Failed: return "#F44336";
                    case NodeRunStatus.Skipped: return "#666666";
                    default: return "#888888";
                }
            }
        }

        /// <summary>
        /// 属性摘要
        /// </summary>
        public string PropertySummary
        {
            get
            {
                if (Properties == null || Properties.Count == 0) return "";
                var parts = new List<string>();
                foreach (var kv in Properties)
                    parts.Add(kv.Key + "=" + kv.Value);
                return string.Join(", ", parts);
            }
        }

        public void NotifyStatusChanged()
        {
            RaisePropertyChanged(nameof(StatusText));
            RaisePropertyChanged(nameof(StatusColor));
            RaisePropertyChanged(nameof(PropertySummary));
        }
        #endregion

        /// <summary>
        /// 根据边获取端口ID（统一查找 InputPorts + OutputPorts）
        /// </summary>
        public string GetPortIdBySide(PortSide side)
        {
            var port = InputPorts.FirstOrDefault(p => p.Side == side)
                    ?? OutputPorts.FirstOrDefault(p => p.Side == side);
            return port?.PortId;
        }

        /// <summary>
        /// 创建默认端口
        /// </summary>
        public virtual void EnsureDefaultPorts()
        {
            if (InputPorts.Count == 0)
            {
                InputPorts.Add(new NodePort
                {
                    PortName = "输入",
                    Direction = PortDirection.Input,
                    Side = PortSide.Left,
                    OwnerNodeId = NodeId
                });
                InputPorts.Add(new NodePort
                {
                    PortName = "输入",
                    Direction = PortDirection.Input,
                    Side = PortSide.Top,
                    OwnerNodeId = NodeId
                });
            }
            if (OutputPorts.Count == 0)
            {
                OutputPorts.Add(new NodePort
                {
                    PortName = "输出",
                    Direction = PortDirection.Output,
                    Side = PortSide.Right,
                    OwnerNodeId = NodeId
                });
                OutputPorts.Add(new NodePort
                {
                    PortName = "输出",
                    Direction = PortDirection.Output,
                    Side = PortSide.Bottom,
                    OwnerNodeId = NodeId
                });
            }
        }

        /// <summary>
        /// 克隆节点（深拷贝）
        /// </summary>
        public virtual FlowNode Clone()
        {
            return new FlowNode
            {
                NodeName = NodeName + "_副本",
                Category = Category,
                PluginId = PluginId,
                X = X + 30,
                Y = Y + 30,
                IsEnabled = IsEnabled,
                IconGeometry = IconGeometry,
                Properties = new Dictionary<string, object>(Properties)
            };
        }
    }
}
