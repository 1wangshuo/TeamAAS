using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using TeamAAS.FlowEditor.Models;
using TeamAAS.FlowEditor.Plugins;

namespace TeamAAS.FlowEditor.Controls
{
    /// <summary>
    /// 流程编辑器画布 - 支持缩放、平移、拖拽节点、绘制连线、框选
    /// </summary>
    public class FlowCanvas : Canvas
    {
        #region 常量
        private const double MinZoom = 0.2;
        private const double MaxZoom = 3.0;
        private const double ZoomFactor = 1.15;
        #endregion

        #region 字段
        private readonly ScaleTransform _scale;
        private readonly TranslateTransform _translate;

        private FlowGraph _graph;
        private readonly Dictionary<string, NodeControl> _nodeControls = new Dictionary<string, NodeControl>();
        private readonly Dictionary<string, Path> _connectionPaths = new Dictionary<string, Path>();

        // 平移状态
        private bool _isPanning;
        private Point _panStart;
        private Point _panOrigin;

        // 连线绘制状态
        private bool _isConnecting;
        private FlowNode _connectSourceNode;
        private PortSide _connectSourceSide;
        private Path _tempPath;

        // 框选状态
        private bool _isBoxSelecting;
        private Point _boxSelectStart;
        private Rectangle _selectionBox;

        // 选中项
        private FlowNode _selectedNode;
        private FlowConnection _selectedConnection;
        private readonly List<FlowNode> _selectedNodes = new List<FlowNode>();

        // 剪贴板（存储复制的节点数据）
        private List<FlowNode> _clipboard;
        #endregion

        #region 事件
        public event Action<FlowNode> NodeDoubleClicked;
        public event Action<FlowNode> NodeSelected;
        public event Action<FlowConnection> ConnectionSelected;
        public event Action SelectionCleared;
        public event Action<double, double, NodePluginInfo> NodeDropRequested;
        public event Action<double> ZoomChanged;
        #endregion

        #region 属性
        public double Zoom => _scale.ScaleX;

        public FlowGraph Graph
        {
            get => _graph;
            set
            {
                if (_graph != null) UnsubscribeGraph();
                ClearCanvas();
                _graph = value;
                if (_graph != null) SubscribeGraph();
            }
        }
        #endregion

        #region 构造函数
        public FlowCanvas()
        {
            _scale = new ScaleTransform(1, 1);
            _translate = new TranslateTransform(0, 0);
            var group = new TransformGroup();
            group.Children.Add(_scale);
            group.Children.Add(_translate);
            RenderTransform = group;

            ClipToBounds = true;
            Focusable = true;
            Background = new SolidColorBrush(Colors.White);
            AllowDrop = true;

            // 初始画布尺寸
            Width = 5000;
            Height = 3500;

            // 初始偏移，显示画布的一部分
            _translate.X = 0;
            _translate.Y = 0;
        }
        #endregion

        #region Graph 订阅
        private void SubscribeGraph()
        {
            _graph.Nodes.CollectionChanged += OnNodesChanged;
            _graph.Connections.CollectionChanged += OnConnectionsChanged;

            foreach (var node in _graph.Nodes)
                AddNodeVisual(node);
            foreach (var conn in _graph.Connections)
                AddConnectionVisual(conn);
        }

        private void UnsubscribeGraph()
        {
            _graph.Nodes.CollectionChanged -= OnNodesChanged;
            _graph.Connections.CollectionChanged -= OnConnectionsChanged;
        }

        private void ClearCanvas()
        {
            foreach (var node in _nodeControls.Values)
            {
                if (node.DataContext is FlowNode fn)
                    fn.PropertyChanged -= OnNodePropertyChanged;
            }
            _nodeControls.Clear();
            _connectionPaths.Clear();
            Children.Clear();
        }

        private void OnNodesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
                foreach (FlowNode node in e.NewItems)
                    AddNodeVisual(node);

            if (e.OldItems != null)
                foreach (FlowNode node in e.OldItems)
                    RemoveNodeVisual(node);
        }

        private void OnConnectionsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
                foreach (FlowConnection conn in e.NewItems)
                    AddConnectionVisual(conn);

            if (e.OldItems != null)
                foreach (FlowConnection conn in e.OldItems)
                    RemoveConnectionVisual(conn);
        }
        #endregion

        #region 节点可视化
        private void AddNodeVisual(FlowNode node)
        {
            var ctrl = new NodeControl { DataContext = node };
            SetLeft(ctrl, node.X);
            SetTop(ctrl, node.Y);
            Children.Add(ctrl);
            _nodeControls[node.NodeId] = ctrl;

            // 节点渲染后更新实际尺寸，用于端口位置和碰撞检测
            ctrl.SizeChanged += (s, e) =>
            {
                node.NodeWidth = ctrl.ActualWidth;
                node.NodeHeight = ctrl.ActualHeight;
                UpdateConnectionsForNode(node.NodeId);
                UpdateCanvasSize();
            };

            node.PropertyChanged += OnNodePropertyChanged;
        }

        private void RemoveNodeVisual(FlowNode node)
        {
            if (_nodeControls.TryGetValue(node.NodeId, out var ctrl))
            {
                Children.Remove(ctrl);
                _nodeControls.Remove(node.NodeId);
                node.PropertyChanged -= OnNodePropertyChanged;
            }

            // 移除相关连线可视化
            var toRemove = _connectionPaths
                .Where(kvp => kvp.Value.Tag is FlowConnection fc &&
                    (fc.SourceNodeId == node.NodeId || fc.TargetNodeId == node.NodeId))
                .ToList();
            foreach (var kvp in toRemove)
            {
                Children.Remove(kvp.Value);
                _connectionPaths.Remove(kvp.Key);
            }

            if (_selectedNode == node)
                _selectedNode = null;
            _selectedNodes.Remove(node);
        }

        private void OnNodePropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var node = (FlowNode)sender;
            if (e.PropertyName == nameof(FlowNode.X) || e.PropertyName == nameof(FlowNode.Y))
            {
                if (_nodeControls.TryGetValue(node.NodeId, out var ctrl))
                {
                    SetLeft(ctrl, node.X);
                    SetTop(ctrl, node.Y);
                }
                UpdateConnectionsForNode(node.NodeId);
                UpdateCanvasSize();
            }
            else if (e.PropertyName == nameof(FlowNode.Status))
            {
                node.NotifyStatusChanged();
            }
        }
        #endregion

        #region 连线可视化
        private void AddConnectionVisual(FlowConnection conn)
        {
            var path = new Path
            {
                Stroke = new SolidColorBrush(Color.FromRgb(232, 145, 73)),
                StrokeThickness = 2,
                Fill = new SolidColorBrush(Color.FromRgb(232, 145, 73)),
                Tag = conn,
                Cursor = Cursors.Hand
            };

            UpdateConnectionPath(path, conn);
            Children.Insert(0, path);
            _connectionPaths[conn.ConnectionId] = path;

            path.MouseLeftButtonDown += (s, e) =>
            {
                SelectConnection(conn);
                e.Handled = true;
            };
        }

        private void RemoveConnectionVisual(FlowConnection conn)
        {
            if (_connectionPaths.TryGetValue(conn.ConnectionId, out var path))
            {
                Children.Remove(path);
                _connectionPaths.Remove(conn.ConnectionId);
            }
            if (_selectedConnection == conn)
                _selectedConnection = null;
        }

        private void UpdateConnectionPath(Path path, FlowConnection conn)
        {
            var sourceNode = _graph?.GetNode(conn.SourceNodeId);
            var targetNode = _graph?.GetNode(conn.TargetNodeId);
            if (sourceNode == null || targetNode == null) return;

            Point start = GetPortPosition(sourceNode, conn.SourceSide);
            Point end = GetPortPosition(targetNode, conn.TargetSide);

            path.Data = CreateConnectionGeometry(start, end, conn.SourceSide, conn.TargetSide);

            if (conn.IsSelected)
            {
                path.Stroke = Brushes.White;
                path.Fill = Brushes.White;
                path.StrokeThickness = 3;
            }
            else
            {
                var color = Color.FromRgb(232, 145, 73); // 橙色
                path.Stroke = new SolidColorBrush(color);
                path.Fill = new SolidColorBrush(color);
                path.StrokeThickness = 2;
            }
        }

        private void UpdateConnectionsForNode(string nodeId)
        {
            foreach (var kvp in _connectionPaths)
            {
                if (kvp.Value.Tag is FlowConnection conn)
                {
                    if (conn.SourceNodeId == nodeId || conn.TargetNodeId == nodeId)
                        UpdateConnectionPath(kvp.Value, conn);
                }
            }
        }
        #endregion

        #region 端口位置 & 连线几何
        public static Point GetPortPosition(FlowNode node, PortDirection direction, int portIndex)
        {
            double w = node.NodeWidth;
            double h = node.NodeHeight;

            if (direction == PortDirection.Input)
            {
                // portIndex 0 = Left, 1 = Top
                if (portIndex == 0) return new Point(node.X, node.Y + h / 2);
                return new Point(node.X + w / 2, node.Y);
            }
            else // Output
            {
                // portIndex 0 = Right, 1 = Bottom
                if (portIndex == 0) return new Point(node.X + w, node.Y + h / 2);
                return new Point(node.X + w / 2, node.Y + h);
            }
        }

        /// <summary>
        /// 根据边获取端口位置（通用，不区分输入输出）
        /// </summary>
        public static Point GetPortPosition(FlowNode node, PortSide side)
        {
            double w = node.NodeWidth;
            double h = node.NodeHeight;
            switch (side)
            {
                case PortSide.Left: return new Point(node.X, node.Y + h / 2);
                case PortSide.Top: return new Point(node.X + w / 2, node.Y);
                case PortSide.Right: return new Point(node.X + w, node.Y + h / 2);
                case PortSide.Bottom: return new Point(node.X + w / 2, node.Y + h);
                default: return new Point(node.X, node.Y + h / 2);
            }
        }

        /// <summary>
        /// 根据端口方向和索引推断所在边
        /// </summary>
        public static PortSide GetPortSide(PortDirection direction, int portIndex)
        {
            if (direction == PortDirection.Input)
                return portIndex == 0 ? PortSide.Left : PortSide.Top;
            return portIndex == 0 ? PortSide.Right : PortSide.Bottom;
        }

        public static Geometry CreateConnectionGeometry(Point start, Point end, PortSide startSide = PortSide.Right, PortSide endSide = PortSide.Left)
        {
            // 偏移起止点，留出间距让箭头可见
            Point adjStart = OffsetPoint(start, startSide, 8);
            Point adjEnd = OffsetPoint(end, endSide, 12);

            var geo = new StreamGeometry();
            using (var ctx = geo.Open())
            {
                // 正交路由
                var points = OrthogonalRoute(adjStart, adjEnd, startSide, endSide);

                // 绘制连线
                ctx.BeginFigure(points[0], false, false);
                for (int i = 1; i < points.Count; i++)
                    ctx.LineTo(points[i], true, false);

                // 绘制箭头
                if (points.Count >= 2)
                {
                    var prev = points[points.Count - 2];
                    var tip = points[points.Count - 1];
                    double angle = Math.Atan2(tip.Y - prev.Y, tip.X - prev.X);
                    double arrowSize = 7;
                    var p1 = new Point(
                        tip.X - arrowSize * Math.Cos(angle - Math.PI / 6),
                        tip.Y - arrowSize * Math.Sin(angle - Math.PI / 6));
                    var p2 = new Point(
                        tip.X - arrowSize * Math.Cos(angle + Math.PI / 6),
                        tip.Y - arrowSize * Math.Sin(angle + Math.PI / 6));
                    ctx.BeginFigure(tip, true, true);
                    ctx.LineTo(p1, true, true);
                    ctx.LineTo(p2, true, true);
                }
            }
            geo.Freeze();
            return geo;
        }

        /// <summary>
        /// 正交路由 - 通用版，支持任意端口方向组合
        /// </summary>
        private static List<Point> OrthogonalRoute(Point start, Point end, PortSide startSide, PortSide endSide)
        {
            var points = new List<Point> { start };

            const double margin = 25; // 弯折余量

            // 出口点：从源端口方向延伸出去
            Point exit = start;
            switch (startSide)
            {
                case PortSide.Left: exit = new Point(start.X - margin, start.Y); break;
                case PortSide.Right: exit = new Point(start.X + margin, start.Y); break;
                case PortSide.Top: exit = new Point(start.X, start.Y - margin); break;
                case PortSide.Bottom: exit = new Point(start.X, start.Y + margin); break;
            }

            // 入口点：从目标端口方向延伸出去（连线最后一段从入口进入目标）
            Point entry = end;
            switch (endSide)
            {
                case PortSide.Left: entry = new Point(end.X - margin, end.Y); break;
                case PortSide.Right: entry = new Point(end.X + margin, end.Y); break;
                case PortSide.Top: entry = new Point(end.X, end.Y - margin); break;
                case PortSide.Bottom: entry = new Point(end.X, end.Y + margin); break;
            }

            points.Add(exit);

            bool startH = (startSide == PortSide.Left || startSide == PortSide.Right);
            bool endH = (endSide == PortSide.Left || endSide == PortSide.Right);

            if (startH != endH)
            {
                // 一水平一垂直 → L形/Z形，一个弯折点
                if (startH)
                    points.Add(new Point(entry.X, exit.Y)); // 先水平对齐，再垂直
                else
                    points.Add(new Point(exit.X, entry.Y)); // 先垂直对齐，再水平
            }
            else
            {
                // 同轴 → 需要中线，两个弯折点（Z形）
                if (startH)
                {
                    double midX = (exit.X + entry.X) / 2;
                    points.Add(new Point(midX, exit.Y));
                    points.Add(new Point(midX, entry.Y));
                }
                else
                {
                    double midY = (exit.Y + entry.Y) / 2;
                    points.Add(new Point(exit.X, midY));
                    points.Add(new Point(entry.X, midY));
                }
            }

            points.Add(entry);
            points.Add(end);

            return points;
        }

        /// <summary>
        /// 根据鼠标位置推断目标端口边
        /// </summary>
        private static PortSide DetermineEndSide(Point start, Point end, PortSide startSide)
        {
            double dx = end.X - start.X;
            double dy = end.Y - start.Y;

            switch (startSide)
            {
                case PortSide.Right:
                    return dx >= 0 ? PortSide.Left : PortSide.Right;
                case PortSide.Bottom:
                    return dy >= 0 ? PortSide.Top : PortSide.Bottom;
                case PortSide.Left:
                    return dx <= 0 ? PortSide.Right : PortSide.Left;
                case PortSide.Top:
                    return dy <= 0 ? PortSide.Bottom : PortSide.Top;
                default:
                    return PortSide.Left;
            }
        }

        /// <summary>
        /// 沿端口方向偏移点，留出间距
        /// </summary>
        private static Point OffsetPoint(Point p, PortSide side, double offset)
        {
            switch (side)
            {
                case PortSide.Left: return new Point(p.X - offset, p.Y);
                case PortSide.Right: return new Point(p.X + offset, p.Y);
                case PortSide.Top: return new Point(p.X, p.Y - offset);
                case PortSide.Bottom: return new Point(p.X, p.Y + offset);
                default: return p;
            }
        }
        #endregion

        #region 坐标转换
        public Point ToCanvasPoint(Point screenPoint)
        {
            return new Point(
                (screenPoint.X - _translate.X) / _scale.ScaleX,
                (screenPoint.Y - _translate.Y) / _scale.ScaleY);
        }
        #endregion

        #region 缩放 & 平移
        public void ZoomAt(Point center, double newScale)
        {
            newScale = Math.Max(MinZoom, Math.Min(MaxZoom, newScale));
            if (Math.Abs(newScale - _scale.ScaleX) < 0.001) return;

            // center 是 GetPosition(this) 返回的画布本地坐标
            // 保持 center 对应的屏幕点不变：screen = center * oldScale + oldTranslate = center * newScale + newTranslate
            _translate.X = _translate.X + center.X * (_scale.ScaleX - newScale);
            _translate.Y = _translate.Y + center.Y * (_scale.ScaleY - newScale);
            _scale.ScaleX = newScale;
            _scale.ScaleY = newScale;
            ClampTranslate();
            ZoomChanged?.Invoke(newScale);
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                // Ctrl + 滚轮 = 缩放
                var mousePos = e.GetPosition(this);
                double factor = e.Delta > 0 ? ZoomFactor : 1 / ZoomFactor;
                ZoomAt(mousePos, _scale.ScaleX * factor);
                e.Handled = true;
            }
            else
            {
                // 滚轮 = 上下平移
                _translate.Y -= e.Delta * 0.5;
                ClampTranslate();
                e.Handled = true;
            }
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.ChangedButton == MouseButton.Middle)
            {
                _isPanning = true;
                _panStart = e.GetPosition(this);
                _panOrigin = new Point(_translate.X, _translate.Y);
                CaptureMouse();
                e.Handled = true;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            var pos = e.GetPosition(this);

            if (_isPanning)
            {
                // GetPosition(this) 返回本地坐标，delta 需乘以缩放系数转换为屏幕增量
                _translate.X = _panOrigin.X + (pos.X - _panStart.X) * _scale.ScaleX;
                _translate.Y = _panOrigin.Y + (pos.Y - _panStart.Y) * _scale.ScaleY;
                ClampTranslate();
            }
            else if (_isConnecting && _tempPath != null)
            {
                Point start = GetPortPosition(_connectSourceNode, _connectSourceSide);
                PortSide endSide = DetermineEndSide(start, pos, _connectSourceSide);
                _tempPath.Data = CreateConnectionGeometry(start, pos, _connectSourceSide, endSide);
            }
            else if (_isBoxSelecting)
            {
                UpdateSelectionBox(pos);
            }
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.ChangedButton == MouseButton.Middle)
            {
                _isPanning = false;
                ReleaseMouseCapture();
                e.Handled = true;
            }
        }
        #endregion

        #region 左键交互

        // 双击检测 + 单击选中都用 Preview（隧道事件），在 MoveThumb 捕获鼠标之前触发
        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                var dblNode = FindAncestor<NodeControl>(e.OriginalSource as DependencyObject);
                if (dblNode != null)
                {
                    NodeDoubleClicked?.Invoke(dblNode.GetNode());
                    e.Handled = true;
                    return;
                }
            }
            else if (e.ClickCount == 1)
            {
                // 单击节点选中（在 MoveThumb 捕获鼠标之前处理）
                var nodeCtrl = FindAncestor<NodeControl>(e.OriginalSource as DependencyObject);
                if (nodeCtrl != null)
                {
                    bool isCtrl = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
                    SelectNode(nodeCtrl.GetNode(), isCtrl);
                    // 不设 Handled，让 MoveThumb 继续处理拖拽
                }
            }
            base.OnPreviewMouseLeftButtonDown(e);
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            Focus();

            var pos = e.GetPosition(this);
            var src = e.OriginalSource as DependencyObject;

            // 1. 检查连接点
            var connector = FindAncestor<ConnectorControl>(src);
            if (connector != null)
            {
                StartConnection(connector);
                e.Handled = true;
                return;
            }

            // 2. 检查连线（Path with FlowConnection Tag）
            if (src is Path path && path.Tag is FlowConnection conn)
            {
                SelectConnection(conn);
                e.Handled = true;
                return;
            }

            // 3. 检查节点
            var nodeCtrl = FindAncestor<NodeControl>(src);
            if (nodeCtrl != null)
            {
                bool isCtrl = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
                SelectNode(nodeCtrl.GetNode(), isCtrl);
                return; // 不设 Handled，让 MoveThumb 处理拖拽
            }

            // 4. 空白区域 - 框选
            ClearSelection();
            StartBoxSelect(pos);
            e.Handled = true;
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);

            if (_isConnecting)
                EndConnection(e.GetPosition(this));

            if (_isBoxSelecting)
                EndBoxSelect();
        }

        #endregion

        #region 连线绘制
        private void StartConnection(ConnectorControl connector)
        {
            var node = connector.GetNode();
            if (node == null) return;

            _isConnecting = true;
            _connectSourceNode = node;
            _connectSourceSide = connector.Side;

            _tempPath = new Path
            {
                Stroke = new SolidColorBrush(Color.FromRgb(232, 145, 73)),
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 4, 2 },
                IsHitTestVisible = false
            };
            Children.Add(_tempPath);
            CaptureMouse();
        }

        private void EndConnection(Point mousePos)
        {
            _isConnecting = false;
            ReleaseMouseCapture();

            if (_tempPath != null)
            {
                Children.Remove(_tempPath);
                _tempPath = null;
            }

            if (_connectSourceNode == null || _graph == null) return;

            // 手动命中检测：遍历所有节点的4个端口，找到最近的
            const double hitRadius = 22.0;
            double minDist = double.MaxValue;
            FlowNode targetNode = null;
            PortSide targetSide = PortSide.Left;

            var sides = new[] { PortSide.Left, PortSide.Top, PortSide.Right, PortSide.Bottom };

            foreach (var node in _graph.Nodes)
            {
                if (node.NodeId == _connectSourceNode.NodeId) continue;

                foreach (var side in sides)
                {
                    var pos = GetPortPosition(node, side);
                    double d = PointDist(pos, mousePos);
                    if (d < hitRadius && d < minDist)
                    {
                        minDist = d;
                        targetNode = node;
                        targetSide = side;
                    }
                }
            }

            if (targetNode == null) { _connectSourceNode = null; return; }

            // 源 = 拖出端，目标 = 放入端。不检查方向类型。
            var conn = new FlowConnection
            {
                SourceNodeId = _connectSourceNode.NodeId,
                SourcePortId = _connectSourceNode.GetPortIdBySide(_connectSourceSide),
                SourceSide = _connectSourceSide,
                TargetNodeId = targetNode.NodeId,
                TargetPortId = targetNode.GetPortIdBySide(targetSide),
                TargetSide = targetSide
            };
            _graph?.TryAddConnection(conn);

            _connectSourceNode = null;
        }

        private static double PointDist(Point a, Point b)
        {
            double dx = a.X - b.X;
            double dy = a.Y - b.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }
        #endregion

        #region 框选
        private void StartBoxSelect(Point pos)
        {
            _isBoxSelecting = true;
            _boxSelectStart = pos;
            _selectionBox = new Rectangle
            {
                Stroke = new SolidColorBrush(Color.FromArgb(100, 0, 122, 204)),
                Fill = new SolidColorBrush(Color.FromArgb(30, 0, 122, 204)),
                StrokeThickness = 1,
                IsHitTestVisible = false
            };
            Children.Add(_selectionBox);
            CaptureMouse();
        }

        private void UpdateSelectionBox(Point pos)
        {
            if (_selectionBox == null) return;
            double x = Math.Min(_boxSelectStart.X, pos.X);
            double y = Math.Min(_boxSelectStart.Y, pos.Y);
            double w = Math.Abs(pos.X - _boxSelectStart.X);
            double h = Math.Abs(pos.Y - _boxSelectStart.Y);
            SetLeft(_selectionBox, x);
            SetTop(_selectionBox, y);
            _selectionBox.Width = w;
            _selectionBox.Height = h;
        }

        private void EndBoxSelect()
        {
            _isBoxSelecting = false;
            ReleaseMouseCapture();

            if (_selectionBox != null)
            {
                double x = GetLeft(_selectionBox);
                double y = GetTop(_selectionBox);
                double w = _selectionBox.Width;
                double h = _selectionBox.Height;
                var rect = new Rect(x, y, w, h);

                // 清除旧选择
                foreach (var n in _selectedNodes)
                    n.IsSelected = false;
                _selectedNodes.Clear();

                // 多选模式：选中框选范围内的所有节点
                foreach (var node in _graph?.Nodes ?? Enumerable.Empty<FlowNode>())
                {
                    var nodeRect = new Rect(node.X, node.Y, node.NodeWidth, node.NodeHeight);
                    if (rect.IntersectsWith(nodeRect))
                    {
                        node.IsSelected = true;
                        _selectedNodes.Add(node);
                    }
                }

                if (_selectedNodes.Count > 0)
                {
                    _selectedNode = _selectedNodes[_selectedNodes.Count - 1];
                    NodeSelected?.Invoke(_selectedNode);
                }

                Children.Remove(_selectionBox);
                _selectionBox = null;
            }
        }
        #endregion

        #region 选择
        public void SelectNode(FlowNode node, bool isMultiSelect = false)
        {
            if (isMultiSelect && node != null)
            {
                // Ctrl+Click: 切换该节点的选中状态
                if (_selectedNodes.Contains(node))
                {
                    _selectedNodes.Remove(node);
                    node.IsSelected = false;
                }
                else
                {
                    _selectedNodes.Add(node);
                    node.IsSelected = true;
                }
                _selectedNode = node;
                if (_selectedConnection != null)
                {
                    _selectedConnection.IsSelected = false;
                    if (_connectionPaths.TryGetValue(_selectedConnection.ConnectionId, out var p))
                        UpdateConnectionPath(p, _selectedConnection);
                    _selectedConnection = null;
                }
                NodeSelected?.Invoke(node);
                return;
            }

            // 普通点击：如果点击的节点已在多选列表中，保持多选不变（用于拖拽）
            if (node != null && _selectedNodes.Contains(node))
            {
                _selectedNode = node;
                if (_selectedConnection != null)
                {
                    _selectedConnection.IsSelected = false;
                    if (_connectionPaths.TryGetValue(_selectedConnection.ConnectionId, out var p))
                        UpdateConnectionPath(p, _selectedConnection);
                    _selectedConnection = null;
                }
                NodeSelected?.Invoke(node);
                return;
            }

            // 否则清除多选，单选
            foreach (var n in _selectedNodes)
                n.IsSelected = false;
            _selectedNodes.Clear();

            if (_selectedNode == node && _selectedConnection == null) return;

            if (_selectedNode != null)
                _selectedNode.IsSelected = false;
            if (_selectedConnection != null)
            {
                _selectedConnection.IsSelected = false;
                if (_connectionPaths.TryGetValue(_selectedConnection.ConnectionId, out var p))
                    UpdateConnectionPath(p, _selectedConnection);
            }

            _selectedNode = node;
            _selectedConnection = null;

            if (node != null)
                node.IsSelected = true;

            NodeSelected?.Invoke(node);
        }

        private void SelectConnection(FlowConnection conn)
        {
            foreach (var n in _selectedNodes)
                n.IsSelected = false;
            _selectedNodes.Clear();

            if (_selectedNode != null)
                _selectedNode.IsSelected = false;
            if (_selectedConnection != null)
            {
                _selectedConnection.IsSelected = false;
                if (_connectionPaths.TryGetValue(_selectedConnection.ConnectionId, out var p))
                    UpdateConnectionPath(p, _selectedConnection);
            }

            _selectedNode = null;
            _selectedConnection = conn;

            if (conn != null)
            {
                conn.IsSelected = true;
                if (_connectionPaths.TryGetValue(conn.ConnectionId, out var p))
                    UpdateConnectionPath(p, conn);
            }

            ConnectionSelected?.Invoke(conn);
        }

        public void ClearSelection()
        {
            foreach (var n in _selectedNodes)
                n.IsSelected = false;
            _selectedNodes.Clear();

            if (_selectedNode != null)
                _selectedNode.IsSelected = false;
            if (_selectedConnection != null)
            {
                _selectedConnection.IsSelected = false;
                if (_connectionPaths.TryGetValue(_selectedConnection.ConnectionId, out var p))
                    UpdateConnectionPath(p, _selectedConnection);
            }

            _selectedNode = null;
            _selectedConnection = null;
            SelectionCleared?.Invoke();
        }
        #endregion

        #region 拖放
        protected override void OnDrop(DragEventArgs e)
        {
            base.OnDrop(e);

            NodePluginInfo pluginInfo = null;

            // 尝试从 DataObject 获取插件信息
            if (e.Data.GetDataPresent(typeof(NodePluginInfo)))
            {
                pluginInfo = e.Data.GetData(typeof(NodePluginInfo)) as NodePluginInfo;
            }
            else if (e.Data.GetDataPresent(DataFormats.StringFormat))
            {
                var str = e.Data.GetData(DataFormats.StringFormat) as string;
                if (!string.IsNullOrEmpty(str))
                    pluginInfo = PluginManager.GetPluginInfo(str);
            }

            if (pluginInfo != null)
            {
                // GetPosition(this) 在有 RenderTransform 时已返回画布本地坐标，无需再转换
                var pos = e.GetPosition(this);
                double dropX = pos.X - NodeControl.DefaultWidth / 2;
                double dropY = pos.Y - NodeControl.DefaultHeight / 2;

                // 避免与现有节点重叠
                var freePos = FindFreePosition(dropX, dropY);
                NodeDropRequested?.Invoke(freePos.X, freePos.Y, pluginInfo);
                UpdateCanvasSize();
                e.Handled = true;
            }
        }
        #endregion

        #region 键盘
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key == Key.Delete || e.Key == Key.Back)
            {
                // 批量删除多选节点
                if (_selectedNodes.Count > 0)
                {
                    foreach (var node in _selectedNodes.ToList())
                        _graph?.RemoveNode(node.NodeId);
                    _selectedNodes.Clear();
                    _selectedNode = null;
                    e.Handled = true;
                }
                else if (_selectedNode != null)
                {
                    _graph?.RemoveNode(_selectedNode.NodeId);
                    _selectedNode = null;
                    e.Handled = true;
                }
                else if (_selectedConnection != null)
                {
                    _graph?.RemoveConnection(_selectedConnection.ConnectionId);
                    _selectedConnection = null;
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.C && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                CopySelectedNodes();
                e.Handled = true;
            }
            else if (e.Key == Key.V && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                PasteNodes();
                e.Handled = true;
            }
        }
        #endregion

        #region 复制粘贴
        /// <summary>
        /// 获取当前选中的节点列表（供 MoveThumb 多选拖拽使用）
        /// </summary>
        public List<FlowNode> GetSelectedNodes() => _selectedNodes;

        private void CopySelectedNodes()
        {
            // 如果多选列表为空但单选不为空，将单选加入多选列表
            if (_selectedNodes.Count == 0 && _selectedNode != null)
            {
                _selectedNode.IsSelected = true;
                _selectedNodes.Add(_selectedNode);
            }

            if (_selectedNodes.Count == 0) return;

            _clipboard = new List<FlowNode>();
            foreach (var node in _selectedNodes)
            {
                _clipboard.Add(CloneNode(node));
            }
        }

        private void PasteNodes()
        {
            if (_clipboard == null || _clipboard.Count == 0 || _graph == null) return;

            // 清除当前选择
            ClearSelection();

            double offset = 30;
            var newNodes = new List<FlowNode>();

            foreach (var data in _clipboard)
            {
                var newNode = CloneNode(data);
                newNode.NodeId = System.Guid.NewGuid().ToString("N");
                newNode.X = data.X + offset;
                newNode.Y = data.Y + offset;
                newNode.IsSelected = true;

                // 重新生成端口ID和OwnerNodeId
                foreach (var port in newNode.InputPorts)
                {
                    port.PortId = System.Guid.NewGuid().ToString("N");
                    port.OwnerNodeId = newNode.NodeId;
                }
                foreach (var port in newNode.OutputPorts)
                {
                    port.PortId = System.Guid.NewGuid().ToString("N");
                    port.OwnerNodeId = newNode.NodeId;
                }

                _graph.AddNode(newNode);
                newNodes.Add(newNode);
            }

            _selectedNodes.AddRange(newNodes);
            if (newNodes.Count > 0)
                _selectedNode = newNodes[0];

            UpdateCanvasSize();
        }

        /// <summary>
        /// 深拷贝节点
        /// </summary>
        private static FlowNode CloneNode(FlowNode source)
        {
            var clone = new FlowNode
            {
                NodeName = source.NodeName,
                Category = source.Category,
                PluginId = source.PluginId,
                X = source.X,
                Y = source.Y,
                IconGeometry = source.IconGeometry,
                IsEnabled = source.IsEnabled,
                Properties = new Dictionary<string, object>(source.Properties)
            };

            // 深拷贝端口
            clone.InputPorts = new List<NodePort>();
            foreach (var port in source.InputPorts)
            {
                clone.InputPorts.Add(new NodePort
                {
                    PortName = port.PortName,
                    Direction = port.Direction,
                    DataType = port.DataType,
                    BranchLabel = port.BranchLabel,
                    Side = port.Side
                });
            }

            clone.OutputPorts = new List<NodePort>();
            foreach (var port in source.OutputPorts)
            {
                clone.OutputPorts.Add(new NodePort
                {
                    PortName = port.PortName,
                    Direction = port.Direction,
                    DataType = port.DataType,
                    BranchLabel = port.BranchLabel,
                    Side = port.Side
                });
            }

            return clone;
        }
        #endregion

        #region 辅助
        private static T FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            while (current != null && !(current is T))
                current = VisualTreeHelper.GetParent(current);
            return current as T;
        }
        #endregion

        #region 节点碰撞检测
        /// <summary>
        /// 检查指定位置是否会与其他节点碰撞
        /// </summary>
        public bool CheckNodeCollision(FlowNode draggingNode, double newX, double newY)
        {
            if (_graph?.Nodes == null) return false;
            var newRect = new Rect(newX, newY, draggingNode.NodeWidth, draggingNode.NodeHeight);
            foreach (var node in _graph.Nodes)
            {
                if (node.NodeId == draggingNode.NodeId) continue;
                var nodeRect = new Rect(node.X, node.Y, node.NodeWidth, node.NodeHeight);
                if (newRect.IntersectsWith(nodeRect))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 从指定位置开始查找不与任何节点重叠的空闲位置
        /// </summary>
        public Point FindFreePosition(double startX, double startY)
        {
            double x = startX, y = startY;
            if (_graph?.Nodes == null) return new Point(x, y);

            bool collision;
            do
            {
                collision = false;
                var dropRect = new Rect(x, y, NodeControl.DefaultWidth, NodeControl.DefaultHeight);
                foreach (var node in _graph.Nodes)
                {
                    var nodeRect = new Rect(node.X, node.Y, node.NodeWidth, node.NodeHeight);
                    if (nodeRect.IntersectsWith(dropRect))
                    {
                        x += 20;
                        y += 20;
                        collision = true;
                        break;
                    }
                }
            } while (collision);

            return new Point(x, y);
        }
        #endregion

        #region 鸟瞰图支持
        /// <summary>
        /// 动态更新画布大小，使内容超出时自动扩展
        /// </summary>
        public void UpdateCanvasSize()
        {
            if (_graph?.Nodes == null || _graph.Nodes.Count == 0) return;

            var bounds = GetContentBounds();
            double minW = ActualWidth > 0 ? ActualWidth : 800;
            double minH = ActualHeight > 0 ? ActualHeight : 600;
            double newW = Math.Max(bounds.Right + 300, minW);
            double newH = Math.Max(bounds.Bottom + 300, minH);

            if (Math.Abs(Width - newW) > 1 || Math.Abs(Height - newH) > 1)
            {
                Width = newW;
                Height = newH;
            }
        }

        /// <summary>
        /// 适应内容 - 重置到原点 100% 比例
        /// </summary>
        public void FitToContent()
        {
            _scale.ScaleX = 1.0;
            _scale.ScaleY = 1.0;
            _translate.X = 0;
            _translate.Y = 0;
            ZoomChanged?.Invoke(1.0);
        }

        /// <summary>
        /// 获取所有节点的内容边界
        /// </summary>
        public Rect GetContentBounds()
        {
            if (_graph?.Nodes == null || _graph.Nodes.Count == 0)
                return new Rect(0, 0, 0, 0);

            double minX = double.MaxValue, minY = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue;

            foreach (var node in _graph.Nodes)
            {
                minX = Math.Min(minX, node.X);
                minY = Math.Min(minY, node.Y);
                maxX = Math.Max(maxX, node.X + node.NodeWidth);
                maxY = Math.Max(maxY, node.Y + node.NodeHeight);
            }

            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }

        /// <summary>
        /// 获取当前视口（画布坐标空间中的可见区域）
        /// </summary>
        public Rect GetViewport()
        {
            var vp = GetViewportSize();
            double x = -_translate.X / _scale.ScaleX;
            double y = -_translate.Y / _scale.ScaleY;
            double w = vp.Width / _scale.ScaleX;
            double h = vp.Height / _scale.ScaleY;
            return new Rect(x, y, w, h);
        }

        /// <summary>
        /// 获取可视区域大小（屏幕像素）- 取父容器的实际尺寸
        /// </summary>
        private Size GetViewportSize()
        {
            var parent = System.Windows.Media.VisualTreeHelper.GetParent(this) as FrameworkElement;
            if (parent != null && parent.ActualWidth > 0 && parent.ActualHeight > 0)
                return new Size(parent.ActualWidth, parent.ActualHeight);
            return new Size(800, 600);
        }

        /// <summary>
        /// 将画布视口居中到指定画布坐标点
        /// </summary>
        public void CenterOn(Point canvasPoint)
        {
            var vp = GetViewportSize();
            _translate.X = vp.Width / 2 - canvasPoint.X * _scale.ScaleX;
            _translate.Y = vp.Height / 2 - canvasPoint.Y * _scale.ScaleY;
            ClampTranslate();
        }
        #endregion

        #region 平移限制
        /// <summary>
        /// 限制平移范围，确保不会看到内容区域之外
        /// </summary>
        private void ClampTranslate()
        {
            // 原点固定，不超出内容边界
            var vp = GetViewportSize();
            var bounds = GetContentBounds();
            if (bounds.Width <= 0 || bounds.Height <= 0) return;

            double margin = 50;
            double contentW = (bounds.Right + margin) * _scale.ScaleX;
            double contentH = (bounds.Bottom + margin) * _scale.ScaleY;

            // X 轴：原点不动，不超出右下边界
            if (contentW <= vp.Width)
                _translate.X = 0;
            else
            {
                double minX = vp.Width - contentW;
                if (_translate.X < minX) _translate.X = minX;
                if (_translate.X > 0) _translate.X = 0;
            }

            // Y 轴
            if (contentH <= vp.Height)
                _translate.Y = 0;
            else
            {
                double minY = vp.Height - contentH;
                if (_translate.Y < minY) _translate.Y = minY;
                if (_translate.Y > 0) _translate.Y = 0;
            }
        }
        #endregion

        #region 对齐引导线
        private readonly List<System.Windows.Documents.Adorner> _alignLines = new List<System.Windows.Documents.Adorner>();

        public void AddAlignLine(Point start, Point end)
        {
            var layer = System.Windows.Documents.AdornerLayer.GetAdornerLayer(this);
            if (layer == null) return;

            // Adorner 坐标空间与画布布局坐标空间一致，直接传画布坐标
            var line = new SelectionAlignLine(this, start, end);
            layer.Add(line);
            _alignLines.Add(line);
        }

        public void ClearAlignLines()
        {
            var layer = System.Windows.Documents.AdornerLayer.GetAdornerLayer(this);
            if (layer != null)
            {
                foreach (var line in _alignLines)
                    layer.Remove(line);
            }
            _alignLines.Clear();
        }

        /// <summary>
        /// 检查节点对齐并返回对齐偏移量
        /// </summary>
        public void CheckAlignment(FlowNode draggingNode, ref double x, ref double y)
        {
            ClearAlignLines();

            const double threshold = 5.0;
            double nodeLeft = x;
            double nodeTop = y;
            double nodeRight = x + draggingNode.NodeWidth;
            double nodeBottom = y + draggingNode.NodeHeight;
            double nodeCenterX = x + draggingNode.NodeWidth / 2;
            double nodeCenterY = y + draggingNode.NodeHeight / 2;

            double snapX = double.NaN;
            double snapY = double.NaN;

            if (_graph?.Nodes == null) return;

            foreach (var other in _graph.Nodes)
            {
                if (other.NodeId == draggingNode.NodeId) continue;

                double otherLeft = other.X;
                double otherTop = other.Y;
                double otherRight = other.X + other.NodeWidth;
                double otherBottom = other.Y + other.NodeHeight;
                double otherCenterX = other.X + other.NodeWidth / 2;
                double otherCenterY = other.Y + other.NodeHeight / 2;

                // 垂直对齐检测（显示垂直引导线，调整 X）
                if (!double.IsNaN(snapX))
                {
                    // 已有 X 对齐，只检查 Y
                }
                else if (Math.Abs(nodeLeft - otherLeft) < threshold)
                {
                    snapX = otherLeft;
                    AddAlignLine(new Point(otherLeft, Math.Min(nodeTop, otherTop)),
                                 new Point(otherLeft, Math.Max(nodeBottom, otherBottom)));
                }
                else if (Math.Abs(nodeRight - otherRight) < threshold)
                {
                    snapX = otherRight - draggingNode.NodeWidth;
                    AddAlignLine(new Point(otherRight, Math.Min(nodeTop, otherTop)),
                                 new Point(otherRight, Math.Max(nodeBottom, otherBottom)));
                }
                else if (Math.Abs(nodeCenterX - otherCenterX) < threshold)
                {
                    snapX = otherCenterX - draggingNode.NodeWidth / 2;
                    AddAlignLine(new Point(otherCenterX, Math.Min(nodeTop, otherTop)),
                                 new Point(otherCenterX, Math.Max(nodeBottom, otherBottom)));
                }

                // 水平对齐检测（显示水平引导线，调整 Y）
                if (!double.IsNaN(snapY))
                {
                    // 已有 Y 对齐
                }
                else if (Math.Abs(nodeTop - otherTop) < threshold)
                {
                    snapY = otherTop;
                    AddAlignLine(new Point(Math.Min(nodeLeft, otherLeft), otherTop),
                                 new Point(Math.Max(nodeRight, otherRight), otherTop));
                }
                else if (Math.Abs(nodeBottom - otherBottom) < threshold)
                {
                    snapY = otherBottom - draggingNode.NodeHeight;
                    AddAlignLine(new Point(Math.Min(nodeLeft, otherLeft), otherBottom),
                                 new Point(Math.Max(nodeRight, otherRight), otherBottom));
                }
                else if (Math.Abs(nodeCenterY - otherCenterY) < threshold)
                {
                    snapY = otherCenterY - draggingNode.NodeHeight / 2;
                    AddAlignLine(new Point(Math.Min(nodeLeft, otherLeft), otherCenterY),
                                 new Point(Math.Max(nodeRight, otherRight), otherCenterY));
                }
            }

            if (!double.IsNaN(snapX)) x = snapX;
            if (!double.IsNaN(snapY)) y = snapY;
        }
        #endregion
    }
}

