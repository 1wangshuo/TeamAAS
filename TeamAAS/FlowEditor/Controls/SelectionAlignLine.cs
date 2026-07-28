using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace TeamAAS.FlowEditor.Controls
{
    /// <summary>
    /// 对齐引导线装饰器 - 拖拽节点时显示对齐参考线
    /// </summary>
    public class SelectionAlignLine : Adorner
    {
        private readonly Point _start;
        private readonly Point _end;
        private static readonly Pen AlignPen;

        static SelectionAlignLine()
        {
            AlignPen = new Pen(new SolidColorBrush(Color.FromRgb(0xE6, 0xA7, 0x00)), 1.5)
            {
                DashStyle = DashStyles.Dash
            };
            AlignPen.Freeze();
        }

        public SelectionAlignLine(UIElement adornedElement, Point start, Point end)
            : base(adornedElement)
        {
            _start = start;
            _end = end;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if (double.IsNaN(_start.X) || double.IsNaN(_start.Y) ||
                double.IsNaN(_end.X) || double.IsNaN(_end.Y))
                return;

            drawingContext.DrawLine(AlignPen, _start, _end);
        }
    }
}
