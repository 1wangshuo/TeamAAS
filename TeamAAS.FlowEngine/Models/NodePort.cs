using Prism.Mvvm;
using System.Windows;

namespace TeamAAS.FlowEditor.Models
{
    /// <summary>
    /// 节点端口（连接点）
    /// </summary>
    public class NodePort : BindableBase
    {
        public string PortId { get; set; } = System.Guid.NewGuid().ToString("N");
        public string PortName { get; set; }
        public PortDirection Direction { get; set; }
        public PortDataType DataType { get; set; } = PortDataType.Any;
        public string OwnerNodeId { get; set; }

        /// <summary>
        /// 端口所在边
        /// </summary>
        public PortSide Side { get; set; } = PortSide.Left;

        /// <summary>
        /// 分支标签（仅 BranchNode 输出端口使用，如 "True"/"False"）
        /// </summary>
        public string BranchLabel { get; set; }

        /// <summary>
        /// 端口在画布上的绝对位置（运行时计算，不序列化）
        /// </summary>
        public Point Position
        {
            get => _position;
            set => SetProperty(ref _position, value);
        }
        private Point _position;
    }
}
