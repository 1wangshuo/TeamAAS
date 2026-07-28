using System.Windows;
using TeamAAS.FlowEditor.Models;

namespace TeamAAS.FlowEditor.Controls
{
    /// <summary>
    /// 连接点控件 - 节点上的输入/输出端口
    /// </summary>
    public class ConnectorControl : System.Windows.Controls.Control
    {
        #region 依赖属性
        public static readonly DependencyProperty DirectionProperty =
            DependencyProperty.Register("Direction", typeof(PortDirection),
                typeof(ConnectorControl), new PropertyMetadata(PortDirection.Input));

        /// <summary>
        /// 端口方向（输入/输出）
        /// </summary>
        public PortDirection Direction
        {
            get => (PortDirection)GetValue(DirectionProperty);
            set => SetValue(DirectionProperty, value);
        }

        public static readonly DependencyProperty PortIndexProperty =
            DependencyProperty.Register("PortIndex", typeof(int),
                typeof(ConnectorControl), new PropertyMetadata(0));

        /// <summary>
        /// 端口索引（用于判断节点的多个输出端口）
        /// </summary>
        public int PortIndex
        {
            get => (int)GetValue(PortIndexProperty);
            set => SetValue(PortIndexProperty, value);
        }

        public static readonly DependencyProperty SideProperty =
            DependencyProperty.Register("Side", typeof(PortSide),
                typeof(ConnectorControl), new PropertyMetadata(PortSide.Left));

        /// <summary>
        /// 端口所在边
        /// </summary>
        public PortSide Side
        {
            get => (PortSide)GetValue(SideProperty);
            set => SetValue(SideProperty, value);
        }

        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register("Label", typeof(string),
                typeof(ConnectorControl), new PropertyMetadata(null));

        /// <summary>
        /// 端口标签（如 True/False）
        /// </summary>
        public string Label
        {
            get => (string)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }
        #endregion

        static ConnectorControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ConnectorControl),
                new FrameworkPropertyMetadata(typeof(ConnectorControl)));
        }

        /// <summary>
        /// 获取所属的 FlowNode（通过 DataContext）
        /// </summary>
        public FlowNode GetNode()
        {
            return DataContext as FlowNode;
        }
    }
}
