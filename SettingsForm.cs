using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Windows.Devices.Bluetooth.Advertisement;

namespace BlueLockScreen
{
    public partial class SettingsForm : Form
    {
        private TextBox _addressTextBox;
        private Button _scanButton;
        private ListBox _deviceListBox;
        private NumericUpDown _rssiNumeric;
        private NumericUpDown _timeoutNumeric;
        private CheckBox _logCheckBox;
        private Button _saveButton;
        private BluetoothLEAdvertisementWatcher _scanner;

        public SettingsForm()
        {
            InitializeComponent();
            LoadSettings();
        }

        private void InitializeComponent()
        {
            Text = "设置";
            Size = new Size(420, 430);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
                ColumnCount = 2,
                RowCount = 6
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70));
            Controls.Add(layout);

            layout.Controls.Add(new Label { Text = "设备地址:", TextAlign = ContentAlignment.MiddleRight }, 0, 0);
            _addressTextBox = new TextBox { Dock = DockStyle.Fill };
            layout.Controls.Add(_addressTextBox, 1, 0);

            _scanButton = new Button { Text = "扫描设备", AutoSize = true };
            layout.Controls.Add(_scanButton, 1, 1);

            _deviceListBox = new ListBox { Dock = DockStyle.Fill, Height = 80 };
            layout.Controls.Add(_deviceListBox, 0, 2);
            layout.SetColumnSpan(_deviceListBox, 2);

            layout.Controls.Add(new Label { Text = "RSSI 阈值 (dBm):", TextAlign = ContentAlignment.MiddleRight }, 0, 3);
            _rssiNumeric = new NumericUpDown { Minimum = -100, Maximum = -20, Value = -70, Dock = DockStyle.Fill };
            layout.Controls.Add(_rssiNumeric, 1, 3);

            layout.Controls.Add(new Label { Text = "断开超时 (秒):", TextAlign = ContentAlignment.MiddleRight }, 0, 4);
            _timeoutNumeric = new NumericUpDown { Minimum = 3, Maximum = 60, Value = 10, Dock = DockStyle.Fill };
            layout.Controls.Add(_timeoutNumeric, 1, 4);

            _logCheckBox = new CheckBox { Text = "关闭软件后生成日志", AutoSize = true };
            layout.Controls.Add(_logCheckBox, 0, 5);
            layout.SetColumnSpan(_logCheckBox, 2);

            _saveButton = new Button { Text = "保存", Anchor = AnchorStyles.None, Size = new Size(80, 30) };
            var savePanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(12) };
            savePanel.Controls.Add(_saveButton);
            Controls.Add(savePanel);

            _scanButton.Click += ScanButton_Click;
            _deviceListBox.DoubleClick += DeviceListBox_DoubleClick;
            _saveButton.Click += SaveButton_Click;
        }

        private void LoadSettings()
        {
            var s = SettingsManager.Settings;
            _addressTextBox.Text = s.DeviceAddress;
            _rssiNumeric.Value = s.RssiThreshold;
            _timeoutNumeric.Value = s.LostTimeoutSeconds;
            _logCheckBox.Checked = s.EnableLogOnExit;
        }

        private async void ScanButton_Click(object sender, EventArgs e)
        {
            _scanButton.Enabled = false;
            _deviceListBox.Items.Clear();
            _deviceListBox.Items.Add("正在扫描...");

            var devices = new Dictionary<string, string>(); // address -> name
            _scanner = new BluetoothLEAdvertisementWatcher();
            _scanner.Received += (s, args) =>
            {
                string addr = args.BluetoothAddress.ToString("X12");
                string name = args.Advertisement.LocalName;
                if (string.IsNullOrWhiteSpace(name)) name = "未知设备";
                if (!devices.ContainsKey(addr))
                {
                    devices[addr] = name;
                    UpdateList(devices);
                }
            };

            _scanner.Start();
            await Task.Delay(TimeSpan.FromSeconds(8));
            _scanner.Stop();
            _scanner = null;

            UpdateList(devices);
            _scanButton.Enabled = true;
        }

        private void UpdateList(Dictionary<string, string> devices)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateList(devices)));
                return;
            }

            _deviceListBox.Items.Clear();
            foreach (var kvp in devices)
            {
                string mac = string.Join(":", Enumerable.Range(0, 6).Select(i => kvp.Key.Substring(i * 2, 2)));
                _deviceListBox.Items.Add($"{kvp.Value}  ({mac})");
            }
        }

        private void DeviceListBox_DoubleClick(object sender, EventArgs e)
        {
            if (_deviceListBox.SelectedItem == null) return;
            string item = _deviceListBox.SelectedItem.ToString();
            int idx = item.LastIndexOf("(");
            if (idx > 0)
            {
                string mac = item.Substring(idx + 1, 17); // XX:XX:XX:XX:XX:XX
                _addressTextBox.Text = mac;
            }
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            var s = SettingsManager.Settings;
            s.DeviceAddress = _addressTextBox.Text.Trim();
            s.RssiThreshold = (int)_rssiNumeric.Value;
            s.LostTimeoutSeconds = (int)_timeoutNumeric.Value;
            s.EnableLogOnExit = _logCheckBox.Checked;
            SettingsManager.Save();
            MessageBox.Show("设置已保存。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
        }
    }
}
