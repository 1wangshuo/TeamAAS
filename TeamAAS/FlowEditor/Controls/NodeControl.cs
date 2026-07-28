using System.Windows;
using System.Windows.Controls;
using TeamAAS.FlowEditor.Models;

namespace TeamAAS.FlowEditor.Controls
{
    /// <summary>
    /// 节点控件 - VisionMaster风格的流程节点
    /// </summary>
    public class NodeControl : System.Windows.Controls.Control
    {
        public const double DefaultWidth = 160;
        public const double DefaultHeight = 38;

        static NodeControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(NodeControl),
                new FrameworkPropertyMetadata(typeof(NodeControl)));
        }

        /// <summary>
        /// 获取绑定的 FlowNode
        /// </summary>
        public FlowNode GetNode()
        {
            return DataContext as FlowNode;
        }
    }
}
