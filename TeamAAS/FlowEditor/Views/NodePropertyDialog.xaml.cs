using System.Windows;
using System.Windows.Controls;
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
            IfProperties.Visibility = Visibility.Collapsed;
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
            else if (node.PluginId == "if")
            {
                IfProperties.Visibility = Visibility.Visible;

                double left = 0, right = 0;
                string op = ">";

                if (node.Properties != null)
                {
                    if (node.Properties.TryGetValue("LeftValue", out var lv) && lv != null)
                        double.TryParse(lv.ToString(), out left);
                    if (node.Properties.TryGetValue("RightValue", out var rv) && rv != null)
                        double.TryParse(rv.ToString(), out right);
                    if (node.Properties.TryGetValue("Operator", out var ov) && ov != null)
                        op = ov.ToString();
                }

                NumLeftValue.Value = left;
                NumRightValue.Value = right;

                // 选择对应的运算符
                for (int i = 0; i < CmbOperator.Items.Count; i++)
                {
                    if (CmbOperator.Items[i] is ComboBoxItem item && (string)item.Tag == op)
                    {
                        CmbOperator.SelectedIndex = i;
                        break;
                    }
                }
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
            else if (_node.PluginId == "if")
            {
                _node.Properties["LeftValue"] = NumLeftValue.Value;
                _node.Properties["RightValue"] = NumRightValue.Value;
                if (CmbOperator.SelectedItem is ComboBoxItem item)
                    _node.Properties["Operator"] = item.Tag?.ToString() ?? ">";
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
