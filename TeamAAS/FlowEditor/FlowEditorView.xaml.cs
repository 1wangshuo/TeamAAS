using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TeamAAS.FlowEditor.Plugins;

namespace TeamAAS.FlowEditor
{
    /// <summary>
    /// 流程编辑器视图
    /// </summary>
    public partial class FlowEditorView : UserControl
    {
        private FlowEditorViewModel _vm;
        private Point _dragStart;
        private bool _isDragging;
        private bool _initialized;

        public FlowEditorView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_initialized) return;
            _initialized = true;

            _vm = DataContext as FlowEditorViewModel;
            if (_vm == null)
            {
                _vm = new FlowEditorViewModel();
                DataContext = _vm;
            }

            // 画布初始化
            EditorCanvas.Graph = _vm.Graph;

            // 画布事件
            EditorCanvas.NodeDropRequested += (x, y, info) => _vm.CreateNode(x, y, info);
            EditorCanvas.NodeDoubleClicked += node => _vm.OpenPropertyDialog(node);
            EditorCanvas.NodeSelected += node =>
            {
                _vm.SelectedNode = node;
            };
            EditorCanvas.SelectionCleared += () => _vm.SelectedNode = null;
            EditorCanvas.ZoomChanged += zoom => TxtZoom.Text = $"{zoom * 100:F0}%";

            // 默认添加一个休眠节点作为示例
            if (_vm.Graph.Nodes.Count == 0)
            {
                var sleepPlugin = new SleepToolPlugin();
                var node = sleepPlugin.CreateNode(200, 150);
                _vm.Graph.AddNode(node);
            }
        }

        #region 工具箱拖拽
        private void ToolboxItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStart = e.GetPosition(null);
            _isDragging = false;
        }

        private void ToolboxItem_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;
            if (_isDragging) return;

            var pos = e.GetPosition(null);
            var diff = _dragStart - pos;
            if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                _isDragging = true;
                var pluginInfo = (sender as FrameworkElement)?.DataContext as NodePluginInfo;
                if (pluginInfo != null)
                {
                    DragDrop.DoDragDrop((DependencyObject)sender, pluginInfo, DragDropEffects.Copy);
                }
                _isDragging = false;
            }
        }
        #endregion

        #region 工具栏
        private void ZoomIn_Click(object sender, RoutedEventArgs e)
        {
            var screenCenter = new Point(EditorCanvas.ActualWidth / 2, EditorCanvas.ActualHeight / 2);
            EditorCanvas.ZoomAt(EditorCanvas.ToCanvasPoint(screenCenter), EditorCanvas.Zoom * 1.2);
        }

        private void ZoomOut_Click(object sender, RoutedEventArgs e)
        {
            var screenCenter = new Point(EditorCanvas.ActualWidth / 2, EditorCanvas.ActualHeight / 2);
            EditorCanvas.ZoomAt(EditorCanvas.ToCanvasPoint(screenCenter), EditorCanvas.Zoom / 1.2);
        }

        private void FitView_Click(object sender, RoutedEventArgs e)
        {
            EditorCanvas.FitToContent();
        }
        #endregion

        #region 流程执行
        private async void RunFlow_Click(object sender, RoutedEventArgs e)
        {
            if (_vm == null || _vm.IsRunning) return;

            BtnRun.IsEnabled = false;
            BtnStop.IsEnabled = true;

            await _vm.RunFlowAsync();

            BtnRun.IsEnabled = true;
            BtnStop.IsEnabled = false;
        }

        private void StopFlow_Click(object sender, RoutedEventArgs e)
        {
            _vm?.StopFlow();
        }
        #endregion

        #region 属性编辑
        private void EditProperties_Click(object sender, RoutedEventArgs e)
        {
            _vm?.OpenPropertyDialog(_vm?.SelectedNode);
        }
        #endregion
    }
}
