using System.Windows;
using TeamAAS.FlowEditor.Models;

namespace TeamAAS.FlowEditor.Views
{
    /// <summary>
    /// 节点属性对话框 - 双击节点时弹出
    /// </summary>
    public partial class NodePropertyDialog
    {
        private readonly FlowNode _node;

        public NodePropertyDialog(FlowNode node)
        {
            InitializeComponent();
            _node = node;

            // 填充基本信息
            TxtNodeName.Text = node.NodeName;
            TxtNodeType.Text = node.Category.ToString();
            TxtPluginId.Text = node.PluginId ?? "(无)";

            // 根据插件类型显示对应属性编辑器
            ShowPropertyEditor(node);
        }

        private void ShowPropertyEditor(FlowNode node)
        {
            SleepProperties.Visibility = Visibility.Collapsed;
            LoopProperties.Visibility = Visibility.Collapsed;
            GenericProperties.Visibility = Visibility.Collapsed;

            if (node.PluginId == "sleep")
            {
                SleepProperties.Visibility = Visibility.Visible;
                int duration = 1000;
                if (node.Properties != null && node.Properties.TryGetValue("Duration", out var val))
                {
                    if (val is int intVal) duration = intVal;
                    else if (val != null && int.TryParse(val.ToString(), out var parsed)) duration = parsed;
                }
                NumDuration.Value = duration;
            }
            else if (node.PluginId == "forloop")
            {
                LoopProperties.Visibility = Visibility.Visible;
                int count = 10;
                if (node.Properties != null && node.Properties.TryGetValue("Count", out var val))
                {
                    if (val is int intVal) count = intVal;
                    else if (val != null && int.TryParse(val.ToString(), out var parsed)) count = parsed;
                }
                NumLoopCount.Value = count;
            }
            else
            {
                GenericProperties.Visibility = Visibility.Visible;
            }
        }

        private void BtnOK_Click(object sender, RoutedEventArgs e)
        {
            // 保存节点名称
            _node.NodeName = TxtNodeName.Text;

            // 保存属性
            if (_node.PluginId == "sleep")
            {
                _node.Properties["Duration"] = (int)NumDuration.Value;
            }
            else if (_node.PluginId == "forloop")
            {
                _node.Properties["Count"] = (int)NumLoopCount.Value;
            }

            _node.NotifyStatusChanged();
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
