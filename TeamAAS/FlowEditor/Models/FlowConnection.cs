using Prism.Mvvm;
using System.Windows;

namespace TeamAAS.FlowEditor.Models
{
    /// <summary>
    /// 节点间的连线
    /// </summary>
    public class FlowConnection : BindableBase
    {
        public string ConnectionId { get; set; } = System.Guid.NewGuid().ToString("N");
        public string SourceNodeId { get; set; }
        public string SourcePortId { get; set; }
        public PortSide SourceSide { get; set; } = PortSide.Right;
        public string TargetNodeId { get; set; }
        public string TargetPortId { get; set; }
        public PortSide TargetSide { get; set; } = PortSide.Left;
        public ConnectionType Type { get; set; } = ConnectionType.DataFlow;

        /// <summary>
        /// 分支条件（仅 BranchNode 的输出连线使用，如 "True"/"False"）
        /// </summary>
        public string BranchCondition { get; set; }

        // ===== 以下为运行时属性（不序列化）=====

        public Point SourcePosition
        {
            get => _sourcePosition;
            set => SetProperty(ref _sourcePosition, value);
        }
        private Point _sourcePosition;

        public Point TargetPosition
        {
            get => _targetPosition;
            set => SetProperty(ref _targetPosition, value);
        }
        private Point _targetPosition;

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
        private bool _isSelected;
    }
}
