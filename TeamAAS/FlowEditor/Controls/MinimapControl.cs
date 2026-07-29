using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using TeamAAS.FlowEditor.Models;

namespace TeamAAS.FlowEditor.Controls
{
    /// <summary>
    /// 鸟瞰图控件 - 显示流程图缩略概览，支持点击导航
    /// </summary>
    public class MinimapControl : System.Windows.Controls.Control
    {
        private FlowCanvas _canvas;
        private DispatcherTimer _refreshTimer;
        private Point _lastMousePos;
        private bool _isDragging;

        static MinimapControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(MinimapControl),
                new FrameworkPropertyMetadata(typeof(MinimapControl)));
        }

        #region Canvas 依赖属性
        public FlowCanvas Canvas
        {
            get => (FlowCanvas)GetValue(CanvasProperty);
            set => SetValue(CanvasProperty, value);
        }

        public static readonly DependencyProperty CanvasProperty =
            DependencyProperty.Register("Canvas", typeof(FlowCanvas), typeof(MinimapControl),
                new PropertyMetadata(null, OnCanvasChanged));

        private static void OnCanvasChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is MinimapControl ctrl)
            {
                ctrl._canvas = e.NewValue as FlowCanvas;
                ctrl.StartRefresh();
            }
        }
        #endregion

        #region 刷新
        private void StartRefresh()
        {
            _refreshTimer?.Stop();
            _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(60) };
            _refreshTimer.Tick += (s, e) => InvalidateVisual();
            _refreshTimer.Start();
        }
        #endregion

        #region 渲染
        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            // 裁剪到控件边界，防止内容溢出
            dc.PushClip(new RectangleGeometry(new Rect(0, 0, ActualWidth, ActualHeight)));

            // 背景
            var bgBrush = new SolidColorBrush(Color.FromArgb(235, 37, 37, 38));
            var borderPen = new Pen(new SolidColorBrush(Color.FromRgb(63, 63, 70)), 1);
            dc.DrawRoundedRectangle(bgBrush, borderPen, new Rect(0, 0, ActualWidth, ActualHeight), 4, 4);

            if (_canvas?.Graph == null || _canvas.Graph.Nodes.Count == 0)
            {
                var hintBrush = new SolidColorBrush(Color.FromRgb(90, 90, 90));
                var text = new FormattedText("暂无节点",
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface("Microsoft YaHei"), 11, hintBrush, 1.25);
                dc.DrawText(text, new Point((ActualWidth - text.Width) / 2, (ActualHeight - text.Height) / 2));
                return;
            }

            // 计算内容边界
            var bounds = CalculateContentBounds();
            if (bounds.Width <= 0 || bounds.Height <= 0) return;

            // 计算缩放
            double padding = 8;
            double availW = ActualWidth - padding * 2;
            double availH = ActualHeight - padding * 2;
            double scale = Math.Min(availW / bounds.Width, availH / bounds.Height);

            double offsetX = padding + (availW - bounds.Width * scale) / 2 - bounds.X * scale;
            double offsetY = padding + (availH - bounds.Height * scale) / 2 - bounds.Y * scale;

            // 绘制连线
            var linePen = new Pen(new SolidColorBrush(Color.FromArgb(180, 85, 153, 255)), 0.8);
            linePen.Freeze();
            foreach (var conn in _canvas.Graph.Connections)
            {
                var srcNode = _canvas.Graph.GetNode(conn.SourceNodeId);
                var dstNode = _canvas.Graph.GetNode(conn.TargetNodeId);
                if (srcNode == null || dstNode == null) continue;

                var srcPos = FlowCanvas.GetPortPosition(srcNode, conn.SourceSide);
                var dstPos = FlowCanvas.GetPortPosition(dstNode, conn.TargetSide);
                var src = ToMinimap(srcPos.X, srcPos.Y, scale, offsetX, offsetY);
                var dst = ToMinimap(dstPos.X, dstPos.Y, scale, offsetX, offsetY);
                dc.DrawLine(linePen, src, dst);
            }

            // 绘制节点
            foreach (var node in _canvas.Graph.Nodes)
            {
                var color = GetCategoryColor(node.Category);
                var brush = new SolidColorBrush(color);
                brush.Freeze();
                var rect = new Rect(
                    node.X * scale + offsetX,
                    node.Y * scale + offsetY,
                    Math.Max(4, node.NodeWidth * scale),
                    Math.Max(3, node.NodeHeight * scale));
                dc.DrawRoundedRectangle(brush, null, rect, 1.5, 1.5);

                // 选中节点高亮
                if (node.IsSelected)
                {
                    var selPen = new Pen(Brushes.White, 1.2);
                    selPen.Freeze();
                    dc.DrawRoundedRectangle(null, selPen, rect, 1.5, 1.5);
                }
            }

            // 绘制视口框
            var viewport = _canvas.GetViewport();
            var vpRect = new Rect(
                viewport.X * scale + offsetX,
                viewport.Y * scale + offsetY,
                viewport.Width * scale,
                viewport.Height * scale);
            var vpPen = new Pen(new SolidColorBrush(Color.FromArgb(200, 0, 122, 204)), 1.5);
            vpPen.Freeze();
            dc.DrawRectangle(null, vpPen, vpRect);

            // 结束裁剪
            dc.Pop();
        }

        private Point ToMinimap(double x, double y, double scale, double offsetX, double offsetY)
        {
            return new Point(x * scale + offsetX, y * scale + offsetY);
        }

        private Color GetCategoryColor(NodeCategory category)
        {
            switch (category)
            {
                case NodeCategory.Decision: return Color.FromRgb(0xC2, 0x77, 0x1A);
                case NodeCategory.ToolBlock: return Color.FromRgb(0x7B, 0x1F, 0xA2);
                default: return Color.FromRgb(0x00, 0x7A, 0xCC);
            }
        }

        private Rect CalculateContentBounds()
        {
            if (_canvas == null) return new Rect(0, 0, 0, 0);
            var bounds = _canvas.GetContentBounds();
            if (bounds.Width <= 0 || bounds.Height <= 0)
                return new Rect(0, 0, 0, 0);
            // 只用节点实际范围 + 边距，不用画布尺寸
            double margin = 50;
            return new Rect(0, 0, bounds.Right + margin, bounds.Bottom + margin);
        }
        #endregion

        #region 鼠标交互
        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
            _isDragging = true;
            _lastMousePos = e.GetPosition(this);
            NavigateTo(_lastMousePos);
            CaptureMouse();
            e.Handled = true;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_isDragging)
            {
                _lastMousePos = e.GetPosition(this);
                NavigateTo(_lastMousePos);
            }
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);
            _isDragging = false;
            ReleaseMouseCapture();
        }

        private void NavigateTo(Point minimapPoint)
        {
            if (_canvas?.Graph == null || _canvas.Graph.Nodes.Count == 0) return;

            var bounds = CalculateContentBounds();
            if (bounds.Width <= 0 || bounds.Height <= 0) return;

            double padding = 8;
            double availW = ActualWidth - padding * 2;
            double availH = ActualHeight - padding * 2;
            double scale = Math.Min(availW / bounds.Width, availH / bounds.Height);

            double offsetX = padding + (availW - bounds.Width * scale) / 2 - bounds.X * scale;
            double offsetY = padding + (availH - bounds.Height * scale) / 2 - bounds.Y * scale;

            double canvasX = (minimapPoint.X - offsetX) / scale;
            double canvasY = (minimapPoint.Y - offsetY) / scale;

            _canvas.CenterOn(new Point(canvasX, canvasY));
        }
        #endregion
    }
}
