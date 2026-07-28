using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using TeamAAS.FlowEditor.Models;

namespace TeamAAS.FlowEditor.Controls
{
    /// <summary>
    /// 节点拖拽手柄 - 处理节点选择和拖动
    /// </summary>
    public class MoveThumb : Thumb
    {
        private FlowCanvas _canvas;

        public MoveThumb()
        {
            DragDelta += OnDragDeltaHandler;
            DragCompleted += OnDragCompletedHandler;
        }

        private FlowCanvas FindCanvas()
        {
            if (_canvas != null) return _canvas;
            DependencyObject parent = VisualTreeHelper.GetParent(this);
            while (parent != null && !(parent is FlowCanvas))
                parent = VisualTreeHelper.GetParent(parent);
            _canvas = parent as FlowCanvas;
            return _canvas;
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            // 选择由 FlowCanvas.OnMouseLeftButtonDown 统一处理（含 Ctrl 多选）
            base.OnMouseLeftButtonDown(e);
        }

        private void OnDragDeltaHandler(object sender, DragDeltaEventArgs e)
        {
            var node = DataContext as FlowNode;
            if (node == null) return;

            var canvas = FindCanvas();

            // 多选拖拽：如果当前节点在多选列表中且有多选，一起移动
            var selectedNodes = canvas?.GetSelectedNodes();
            if (selectedNodes != null && selectedNodes.Count > 1 && selectedNodes.Contains(node))
            {
                double dx = e.HorizontalChange;
                double dy = e.VerticalChange;

                foreach (var selNode in selectedNodes)
                {
                    selNode.X = Math.Max(0, selNode.X + dx);
                    selNode.Y = Math.Max(0, selNode.Y + dy);
                }
                canvas?.UpdateCanvasSize();
                return;
            }

            // 单选拖拽（原有逻辑）
            // DragDeltaEventArgs 已包含 RenderTransform 变换，无需再除以 zoom
            double newX = node.X + e.HorizontalChange;
            double newY = node.Y + e.VerticalChange;

            // 画布边界限制 — 不能移动到0点之前
            newX = Math.Max(0, newX);
            newY = Math.Max(0, newY);

            // 节点碰撞检测
            if (canvas != null)
            {
                if (canvas.CheckNodeCollision(node, newX, newY))
                {
                    // 尝试只移动X
                    if (!canvas.CheckNodeCollision(node, newX, node.Y))
                        node.X = newX;
                    // 尝试只移动Y
                    else if (!canvas.CheckNodeCollision(node, node.X, newY))
                        node.Y = newY;
                    return;
                }

                // 对齐引导线检测
                canvas.CheckAlignment(node, ref newX, ref newY);
            }

            node.X = newX;
            node.Y = newY;

            // 通知画布更新大小
            canvas?.UpdateCanvasSize();
        }

        private void OnDragCompletedHandler(object sender, DragCompletedEventArgs e)
        {
            var canvas = FindCanvas();
            canvas?.ClearAlignLines();
        }
    }
}
