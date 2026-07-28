using System;
using System.Windows;
using System.Windows.Controls;
using TeamAAS.Robot.Enums;
using TeamAAS.Robot.Models;

namespace TeamAAS.Views
{
    public partial class AddRobotDialog 
    {
        public RobotInfo Result { get; private set; }

        public AddRobotDialog()
        {
            InitializeComponent();
        }

        public AddRobotDialog(int robotCount) : this()
        {
            txtName.Text = $"机器人 {robotCount + 1}";
        }

        private void BtnOK_Click(object sender, RoutedEventArgs e)
        {
            var brandItem = cbBrand.SelectedItem as ComboBoxItem;
            var connItem = cbConnectType.SelectedItem as ComboBoxItem;
            var termItem = cbTerminator.SelectedItem as ComboBoxItem;
            var encItem = cbEncoding.SelectedItem as ComboBoxItem;

            Result = new RobotInfo
            {
                Id = Guid.NewGuid(),
                RobotName = string.IsNullOrWhiteSpace(txtName.Text) ? "未命名机器人" : txtName.Text.Trim(),
                RobotBrand = brandItem != null ? (RobotBrand)int.Parse(brandItem.Tag.ToString()) : RobotBrand.Default,
                ConnectType = connItem != null ? (TCPConnectType)int.Parse(connItem.Tag.ToString()) : TCPConnectType.Client,
                IP = string.IsNullOrWhiteSpace(txtIP.Text) ? "192.168.0.1" : txtIP.Text.Trim(),
                Port = (int)nudPort.Value,
                Terminator = termItem != null ? (Terminator)int.Parse(termItem.Tag.ToString()) : Terminator.CRLF,
                DataEncoding = encItem != null ? (DataEncoding)int.Parse(encItem.Tag.ToString()) : DataEncoding.Default,
                StepDistance = 1.0
            };

            DialogResult = true;
        }
    }
}
