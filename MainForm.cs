using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace BlueLockScreen
{
    public partial class MainForm : Form
    {
        private BluetoothWatcher _watcher;
        private bool _monitoring;
        private bool _lockTriggered;
        private MenuStrip _menuStrip;
        private TableLayoutPanel _layout;
        private Label _statusLabel;
        private Label _rssiLabel;
        private Button _startButton;
        private ToolStripMenuItem _settingsMenu;
        private ToolStripMenuItem _testRssiMenu;
        private ToolStripMenuItem _exitMenu;

        public event Action<int> RssiChanged;

        public MainForm()
        {
            InitializeComponent();
            Load += MainForm_Load;
            FormClosing += MainForm_FormClosing;
        }

        private void InitializeComponent()
        {
            Text = $"蓝牙锁屏助手 v{Assembly.GetExecutingAssembly().GetName().Version}";
            Icon = new Icon("app.ico");
            Size = new Size(420, 300);
            MinimumSize = new Size(300, 200);
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.Font;

            _menuStrip = new MenuStrip();
            _settingsMenu = new ToolStripMenuItem("设置");
            _testRssiMenu = new ToolStripMenuItem("RSSI 测试");
            _exitMenu = new ToolStripMenuItem("退出");
            _menuStrip.Items.Add(_settingsMenu);
            _menuStrip.Items.Add(_testRssiMenu);
            _menuStrip.Items.Add(_exitMenu);
            Controls.Add(_menuStrip);
            MainMenuStrip = _menuStrip;

            _layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(12)
            };
            _layout.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
            _layout.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
            _layout.RowStyles.Add(new RowStyle(SizeType.Percent, 20));
            Controls.Add(_layout);

            _statusLabel = new Label
            {
                Text = "未监控",
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Font = new Font("Microsoft YaHei", 12F)
            };
            _layout.Controls.Add(_statusLabel, 0, 0);

            _rssiLabel = new Label
            {
                Text = "RSSI: -- dBm",
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Font = new Font("Microsoft YaHei", 10F)
            };
            _layout.Controls.Add(_rssiLabel, 0, 1);

            _startButton = new Button
            {
                Text = "开始监控",
                Anchor = AnchorStyles.None,
                Size = new Size(110, 35)
            };
            _layout.Controls.Add(_startButton, 0, 2);

            _settingsMenu.Click += (s, e) => new SettingsForm().ShowDialog(this);
            _testRssiMenu.Click += (s, e) => OpenTestRssi();
            _exitMenu.Click += (s, e) => Close();
            _startButton.Click += StartButton_Click;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            // 加载后的初始化操作
        }

        private void StartButton_Click(object sender, EventArgs e)
        {
            if (_monitoring)
            {
                StopMonitoring();
                return;
            }

            string addr = SettingsManager.Settings.DeviceAddress;
            if (string.IsNullOrWhiteSpace(addr))
            {
                MessageBox.Show("请先在设置中选择蓝牙设备。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                _watcher = new BluetoothWatcher(addr)
                {
                    LostTimeoutSeconds = SettingsManager.Settings.LostTimeoutSeconds
                };
                _watcher.RssiUpdated += Watcher_RssiUpdated;
                _watcher.DeviceLost += Watcher_DeviceLost;
                _watcher.DeviceFound += Watcher_DeviceFound;
                _watcher.Start();
                _monitoring = true;
                _lockTriggered = false;
                _startButton.Text = "停止监控";
                _statusLabel.Text = "监控中...";
                Logger.Log("监控已启动");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"启动失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void StopMonitoring()
        {
            _watcher?.Stop();
            _watcher = null;
            _monitoring = false;
            _startButton.Text = "开始监控";
            _statusLabel.Text = "未监控";
            _rssiLabel.Text = "RSSI: -- dBm";
            Logger.Log("监控已停止");
        }

        private void Watcher_RssiUpdated(object sender, int rssi)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => Watcher_RssiUpdated(sender, rssi)));
                return;
            }

            _rssiLabel.Text = $"RSSI: {rssi} dBm";
            RssiChanged?.Invoke(rssi);

            if (!_lockTriggered && rssi < SettingsManager.Settings.RssiThreshold)
            {
                TriggerLock($"RSSI 低于阈值 ({SettingsManager.Settings.RssiThreshold} dBm)");
            }
        }

        private void Watcher_DeviceLost(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => Watcher_DeviceLost(sender, e)));
                return;
            }

            if (!_lockTriggered)
            {
                TriggerLock("蓝牙设备信号丢失");
            }
        }

        private void Watcher_DeviceFound(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => Watcher_DeviceFound(sender, e)));
                return;
            }

            _statusLabel.Text = "设备已连接";
        }

        private void TriggerLock(string reason)
        {
            _lockTriggered = true;
            Logger.Log($"锁屏触发: {reason}");
            LockHelper.LockWorkStation();
            StopMonitoring();
            _statusLabel.Text = "已锁屏";
        }

        private void OpenTestRssi()
        {
            if (!_monitoring)
            {
                MessageBox.Show("请先开始监控以测试 RSSI。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var testForm = new TestRssiForm(this);
            testForm.ShowDialog(this);
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_monitoring)
            {
                StopMonitoring();
            }

            if (SettingsManager.Settings.EnableLogOnExit)
            {
                Logger.Enabled = true;
                Logger.Log("程序关闭");
                Logger.SaveToFile();
            }
        }
    }
}
