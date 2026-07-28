using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using TeamAAS.Camera;
using TeamAAS.Camera.Interfaces;
using TeamAAS.Camera.Models;
using TeamAAS.Views;
using TeamAAS.Robot.Enums;
using TeamAAS.Robot.Interfaces;
using TeamAAS.Robot.Models;
using Cognex.VisionPro;

namespace TeamAAS.ViewModels
{
    public class SettingViewModel : BindableBase
    {
        private readonly ICameraService _cameraService;
        private readonly IRobotService _robotService;

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        #region 子导航
        private string _selectedSection = "Camera";
        public string SelectedSection
        {
            get { return _selectedSection; }
            set { SetProperty(ref _selectedSection, value); }
        }
        #endregion

        #region 相机管理属性
        public ObservableCollection<CameraInfo> Cameras { get; }
        public ObservableCollection<SdkStatusItem> SdkStatus { get; }

        private CameraInfo _selectedCamera;
        public CameraInfo SelectedCamera
        {
            get { return _selectedCamera; }
            set
            {
                SetProperty(ref _selectedCamera, value);
                RaisePropertyChanged(nameof(HasSelectedCamera));
            }
        }

        public bool HasSelectedCamera => _selectedCamera != null;

        private BitmapSource _currentImage;
        public BitmapSource CurrentImage
        {
            get { return _currentImage; }
            set { SetProperty(ref _currentImage, value); }
        }

        private string _fpsText = "";
        public string FpsText
        {
            get { return _fpsText; }
            set { SetProperty(ref _fpsText, value); }
        }
        #endregion

        #region 机器人管理属性
        public ObservableCollection<RobotInfo> Robots { get; }

        private RobotInfo _selectedRobot;
        public RobotInfo SelectedRobot
        {
            get { return _selectedRobot; }
            set
            {
                SetProperty(ref _selectedRobot, value);
                RaisePropertyChanged(nameof(HasSelectedRobot));
                RaisePropertyChanged(nameof(IsRobotConnected));
                RaisePropertyChanged(nameof(RobotConnectionStatus));
            }
        }

        public bool HasSelectedRobot => _selectedRobot != null;

        public bool IsRobotConnected
        {
            get
            {
                if (_selectedRobot == null) return false;
                try
                {
                    var robot = _robotService.GetRobot(_selectedRobot.Id);
                    return robot?.IsConnected ?? false;
                }
                catch { return false; }
            }
        }

        public string RobotConnectionStatus => IsRobotConnected ? "已连接" : "未连接";

        private string _robotPosText = "X: ---  Y: ---  Z: ---\nU: ---  V: ---  W: ---";
        public string RobotPosText
        {
            get { return _robotPosText; }
            set { SetProperty(ref _robotPosText, value); }
        }

        private double _jogStepDistance = 1.0;
        public double JogStepDistance
        {
            get { return _jogStepDistance; }
            set { SetProperty(ref _jogStepDistance, value); }
        }

        private int _selectedToolIndex = 0;
        public int SelectedToolIndex
        {
            get { return _selectedToolIndex; }
            set { SetProperty(ref _selectedToolIndex, value); }
        }

        private string _robotMessage = "";
        public string RobotMessage
        {
            get { return _robotMessage; }
            set { SetProperty(ref _robotMessage, value); }
        }
        #endregion

        #region 相机命令
        public DelegateCommand SearchDevicesCommand { get; }
        public DelegateCommand DeleteCameraCommand { get; }
        public DelegateCommand ConnectCommand { get; }
        public DelegateCommand DisconnectCommand { get; }
        public DelegateCommand StartGrabbingCommand { get; }
        public DelegateCommand StopGrabbingCommand { get; }
        #endregion

        #region 机器人命令
        public DelegateCommand AddRobotCommand { get; }
        public DelegateCommand DeleteRobotCommand { get; }
        public DelegateCommand ConnectRobotCommand { get; }
        public DelegateCommand DisconnectRobotCommand { get; }
        public DelegateCommand MotorOnCommand { get; }
        public DelegateCommand MotorOffCommand { get; }
        public DelegateCommand ResetRobotCommand { get; }
        public DelegateCommand SFreeRobotCommand { get; }
        public DelegateCommand SLockRobotCommand { get; }
        public DelegateCommand<string> JogCommand { get; }
        public DelegateCommand GetRobotPosCommand { get; }
        public DelegateCommand SelectToolCommand { get; }
        #endregion

        public SettingViewModel(ICameraService cameraService, IRobotService robotService)
        {
            _cameraService = cameraService;
            _robotService = robotService;
            Cameras = new ObservableCollection<CameraInfo>();
            SdkStatus = new ObservableCollection<SdkStatusItem>();
            Robots = new ObservableCollection<RobotInfo>();

            // 相机命令
            SearchDevicesCommand = new DelegateCommand(OpenSearchDialog);
            DeleteCameraCommand = new DelegateCommand(DeleteCamera);
            ConnectCommand = new DelegateCommand(async () => await ConnectAsync());
            DisconnectCommand = new DelegateCommand(Disconnect);
            StartGrabbingCommand = new DelegateCommand(StartGrabbing);
            StopGrabbingCommand = new DelegateCommand(StopGrabbing);

            // 机器人命令
            AddRobotCommand = new DelegateCommand(AddRobot);
            DeleteRobotCommand = new DelegateCommand(DeleteRobot);
            ConnectRobotCommand = new DelegateCommand(async () => await ConnectRobotAsync());
            DisconnectRobotCommand = new DelegateCommand(DisconnectRobot);
            MotorOnCommand = new DelegateCommand(async () => await MotorAsync(true));
            MotorOffCommand = new DelegateCommand(async () => await MotorAsync(false));
            ResetRobotCommand = new DelegateCommand(async () => await ResetRobotAsync());
            SFreeRobotCommand = new DelegateCommand(async () => await SFreeRobotAsync());
            SLockRobotCommand = new DelegateCommand(async () => await SLockRobotAsync());
            JogCommand = new DelegateCommand<string>(async (p) => await JogAsync(p));
            GetRobotPosCommand = new DelegateCommand(async () => await GetRobotPosAsync());
            SelectToolCommand = new DelegateCommand(async () => await SelectToolAsync());

            LoadSdkStatus();
        }

        #region 相机功能
        private void LoadSdkStatus()
        {
            SdkStatus.Clear();
            try
            {
                var statusList = CameraFactory.GetSdkStatus();
                foreach (var (Name, Installed, Version) in statusList)
                {
                    SdkStatus.Add(new SdkStatusItem { Name = Name, Installed = Installed, Version = Version });
                }
            }
            catch { }
        }

        private void OpenSearchDialog()
        {
            var dialog = new SearchDeviceWindow
            {
                Owner = Application.Current.MainWindow
            };

            if (dialog.ShowDialog() == true)
            {
                foreach (var info in dialog.SelectedDevices)
                {
                    try
                    {
                        if (_cameraService.ContainsCamera(info.Id))
                        {
                            MessageBox.Show($"相机 {info.CameraName} 已存在，跳过。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                            continue;
                        }

                        _cameraService.CreateCamera(info.Id, info);
                        info.CameraNo = Cameras.Count + 1;
                        Cameras.Add(info);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"添加相机失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void DeleteCamera()
        {
            if (SelectedCamera == null) return;

            var result = MessageBox.Show(
                $"确定要删除相机 \"{SelectedCamera.CameraName}\" 吗？\n删除前将自动停止采集并释放设备资源。",
                "确认删除",
                MessageBoxButton.OKCancel,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.OK) return;

            var cameraToRemove = SelectedCamera;
            var id = cameraToRemove.Id;

            try
            {
                _cameraService.RemoveCamera(id);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"释放相机资源时出错: {ex.Message}");
            }

            Cameras.Remove(cameraToRemove);

            for (int i = 0; i < Cameras.Count; i++)
            {
                Cameras[i].CameraNo = i + 1;
            }

            SelectedCamera = null;
        }

        private async Task ConnectAsync()
        {
            if (SelectedCamera == null) return;

            try
            {
                var camera = _cameraService.GetCamera(SelectedCamera.Id);
                if (camera == null)
                {
                    MessageBox.Show("未找到相机实例，请重新添加。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                bool success = await camera.OpenDeviceAsync();
                SelectedCamera.IsConnected = success;

                if (!success)
                {
                    MessageBox.Show($"相机连接失败: {camera.ErrorMessage}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"连接失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Disconnect()
        {
            if (SelectedCamera == null) return;

            try
            {
                var camera = _cameraService.GetCamera(SelectedCamera.Id);
                if (camera == null) return;

                if (camera.IsGrabbing)
                {
                    camera.StopGrabbing();
                    camera.ImageCallbackEvent -= OnImageCallback;
                    SelectedCamera.IsGrabbing = false;
                }

                camera.CloseDevice();
                SelectedCamera.IsConnected = false;
                CurrentImage = null;
                FpsText = "";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"断开失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StartGrabbing()
        {
            if (SelectedCamera == null) return;

            try
            {
                var camera = _cameraService.GetCamera(SelectedCamera.Id);
                if (camera == null) return;

                if (!camera.IsConnected)
                {
                    camera.OpenDevice();
                    SelectedCamera.IsConnected = camera.IsConnected;
                }

                camera.ImageCallbackEvent += OnImageCallback;
                camera.StartGrabbing();
                SelectedCamera.IsGrabbing = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"开始采集失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StopGrabbing()
        {
            if (SelectedCamera == null) return;

            try
            {
                var camera = _cameraService.GetCamera(SelectedCamera.Id);
                if (camera == null) return;

                camera.StopGrabbing();
                camera.ImageCallbackEvent -= OnImageCallback;
                SelectedCamera.IsGrabbing = false;
                Application.Current.Dispatcher.Invoke(() => { CurrentImage = null; FpsText = ""; });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"停止采集失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnImageCallback(ICogImage image, TimeSpan elapsed, string info)
        {
            if (image == null) return;
            try
            {
                var bitmap = image.ToBitmap();
                var hBitmap = bitmap.GetHbitmap();
                var bmpSource = Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap, IntPtr.Zero, Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
                DeleteObject(hBitmap);
                bitmap.Dispose();
                bmpSource.Freeze();

                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    CurrentImage = bmpSource;
                    FpsText = elapsed.TotalMilliseconds > 0
                        ? $"{1000.0 / elapsed.TotalMilliseconds:F1} fps"
                        : "";
                }));
            }
            catch { }
        }
        #endregion

        #region 机器人功能
        private void AddRobot()
        {
            var robotInfo = new RobotInfo
            {
                Id = Guid.NewGuid(),
                RobotNo = Robots.Count + 1,
                RobotName = $"机器人 {Robots.Count + 1}",
                RobotBrand = RobotBrand.EPSON,
                ConnectType = TCPConnectType.Client,
                IP = "192.168.0.1",
                Port = 3600,
                StepDistance = 1.0
            };

            try
            {
                _robotService.CreateRobot(robotInfo.Id, robotInfo);
                Robots.Add(robotInfo);
                SelectedRobot = robotInfo;
                ShowRobotMessage($"已添加 {robotInfo.RobotName}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"添加机器人失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteRobot()
        {
            if (SelectedRobot == null) return;

            var result = MessageBox.Show(
                $"确定要删除 \"{SelectedRobot.RobotName}\" 吗？",
                "确认删除",
                MessageBoxButton.OKCancel,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.OK) return;

            var robotToRemove = SelectedRobot;
            try
            {
                _robotService.RemoveRobot(robotToRemove.Id);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"移除机器人时出错: {ex.Message}");
            }

            Robots.Remove(robotToRemove);

            for (int i = 0; i < Robots.Count; i++)
            {
                Robots[i].RobotNo = i + 1;
            }

            SelectedRobot = null;
        }

        private async Task ConnectRobotAsync()
        {
            if (SelectedRobot == null) return;

            try
            {
                var robot = _robotService.GetRobot(SelectedRobot.Id);
                if (robot == null)
                {
                    _robotService.CreateRobot(SelectedRobot.Id, SelectedRobot);
                    robot = _robotService.GetRobot(SelectedRobot.Id);
                }

                if (robot == null)
                {
                    ShowRobotMessage("无法获取机器人实例");
                    return;
                }

                await robot.ConnectAsync();
                RaisePropertyChanged(nameof(IsRobotConnected));
                RaisePropertyChanged(nameof(RobotConnectionStatus));
                ShowRobotMessage(robot.IsConnected ? "连接成功" : "连接失败");
            }
            catch (Exception ex)
            {
                ShowRobotMessage($"连接异常: {ex.Message}");
            }
        }

        private void DisconnectRobot()
        {
            if (SelectedRobot == null) return;

            try
            {
                var robot = _robotService.GetRobot(SelectedRobot.Id);
                if (robot == null) return;

                robot.Disconnect();
                RaisePropertyChanged(nameof(IsRobotConnected));
                RaisePropertyChanged(nameof(RobotConnectionStatus));
                ShowRobotMessage("已断开连接");
            }
            catch (Exception ex)
            {
                ShowRobotMessage($"断开异常: {ex.Message}");
            }
        }

        private async Task MotorAsync(bool state)
        {
            if (SelectedRobot == null) return;

            try
            {
                var robot = _robotService.GetRobot(SelectedRobot.Id);
                if (robot == null) return;

                await robot.MotorAsync(state);
                ShowRobotMessage(state ? "电机已开启" : "电机已关闭");
            }
            catch (Exception ex)
            {
                ShowRobotMessage($"电机操作异常: {ex.Message}");
            }
        }

        private async Task ResetRobotAsync()
        {
            if (SelectedRobot == null) return;

            try
            {
                var robot = _robotService.GetRobot(SelectedRobot.Id);
                if (robot == null) return;

                await robot.ResetAsync();
                ShowRobotMessage("已重置");
            }
            catch (Exception ex)
            {
                ShowRobotMessage($"重置异常: {ex.Message}");
            }
        }

        private async Task SFreeRobotAsync()
        {
            if (SelectedRobot == null) return;

            try
            {
                var robot = _robotService.GetRobot(SelectedRobot.Id);
                if (robot == null) return;

                await robot.SFreeAsync();
                ShowRobotMessage("已释放刹车");
            }
            catch (Exception ex)
            {
                ShowRobotMessage($"释放刹车异常: {ex.Message}");
            }
        }

        private async Task SLockRobotAsync()
        {
            if (SelectedRobot == null) return;

            try
            {
                var robot = _robotService.GetRobot(SelectedRobot.Id);
                if (robot == null) return;

                await robot.SLockAsync();
                ShowRobotMessage("已锁定刹车");
            }
            catch (Exception ex)
            {
                ShowRobotMessage($"锁定刹车异常: {ex.Message}");
            }
        }

        private async Task JogAsync(string parameter)
        {
            if (SelectedRobot == null || string.IsNullOrEmpty(parameter)) return;

            try
            {
                var robot = _robotService.GetRobot(SelectedRobot.Id);
                if (robot == null) return;

                // parameter 格式: "X+", "Y-", "Z+", "U-", "V+", "W-"
                string axis = parameter.Substring(0, 1);
                string dir = parameter.Substring(1, 1);
                double distance = JogStepDistance * (dir == "+" ? 1 : -1);

                await robot.JogAsync(axis, distance);
                ShowRobotMessage($"Jog {axis}{dir} {JogStepDistance}mm");

                // 自动刷新位置
                await GetRobotPosAsync();
            }
            catch (Exception ex)
            {
                ShowRobotMessage($"Jog异常: {ex.Message}");
            }
        }

        private async Task GetRobotPosAsync()
        {
            if (SelectedRobot == null) return;

            try
            {
                var robot = _robotService.GetRobot(SelectedRobot.Id);
                if (robot == null) return;

                var pos = await robot.GetRobotPosAsync();
                if (pos != null)
                {
                    RobotPosText = $"X: {pos.X:F3}  Y: {pos.Y:F3}  Z: {pos.Z:F3}\nU: {pos.U:F3}  V: {pos.V:F3}  W: {pos.W:F3}";
                }
            }
            catch (Exception ex)
            {
                ShowRobotMessage($"获取位置异常: {ex.Message}");
            }
        }

        private async Task SelectToolAsync()
        {
            if (SelectedRobot == null) return;

            try
            {
                var robot = _robotService.GetRobot(SelectedRobot.Id);
                if (robot == null) return;

                await robot.SelectToolAsync(SelectedToolIndex);
                ShowRobotMessage($"已选择 Tool {SelectedToolIndex}");
            }
            catch (Exception ex)
            {
                ShowRobotMessage($"选择Tool异常: {ex.Message}");
            }
        }

        private void ShowRobotMessage(string msg)
        {
            RobotMessage = msg;
        }
        #endregion
    }
}
