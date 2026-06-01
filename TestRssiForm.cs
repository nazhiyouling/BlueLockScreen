using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace BlueLockScreen
{
    public partial class TestRssiForm : Form
    {
        private readonly MainForm _main;
        private int _currentRssi;
        private readonly List<int> _recordedValues = new List<int>();
        private Label _currentRssiLabel;
        private Label _recordLabel;
        private ListBox _recordListBox;
        private Button _recordButton;
        private Button _exportButton;

        public TestRssiForm(MainForm main)
        {
            _main = main;
            InitializeComponent();
            _main.RssiChanged += OnRssiChanged;
            FormClosing += (s, e) => _main.RssiChanged -= OnRssiChanged;
        }

        private void InitializeComponent()
        {
            Text = "RSSI 阈值测试";
            Size = new Size(360, 380);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
                ColumnCount = 1,
                RowCount = 5
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            Controls.Add(layout);

            _currentRssiLabel = new Label
            {
                Text = "当前 RSSI: -- dBm",
                Font = new Font("Microsoft YaHei", 10F, FontStyle.Bold),
                Dock = DockStyle.Fill
            };
            layout.Controls.Add(_currentRssiLabel, 0, 0);

            _recordLabel = new Label
            {
                Text = "记录值:",
                Dock = DockStyle.Fill
            };
            layout.Controls.Add(_recordLabel, 0, 1);

            _recordListBox = new ListBox { Dock = DockStyle.Fill };
            layout.Controls.Add(_recordListBox, 0, 2);

            _recordButton = new Button
            {
                Text = "记录当前 RSSI",
                Dock = DockStyle.Fill
            };
            layout.Controls.Add(_recordButton, 0, 3);

            _exportButton = new Button
            {
                Text = "导出记录值的日志",
                Dock = DockStyle.Fill
            };
            layout.Controls.Add(_exportButton, 0, 4);

            _recordButton.Click += (s, e) =>
            {
                _recordedValues.Add(_currentRssi);
                RefreshRecordList();
            };

            _exportButton.Click += (s, e) => ExportRecords();
        }

        private void OnRssiChanged(int rssi)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnRssiChanged(rssi)));
                return;
            }
            _currentRssi = rssi;
            _currentRssiLabel.Text = $"当前 RSSI: {rssi} dBm";
        }

        private void RefreshRecordList()
        {
            _recordListBox.Items.Clear();
            for (int i = 0; i < _recordedValues.Count; i++)
            {
                _recordListBox.Items.Add($"[{i + 1}] {_recordedValues[i]} dBm");
            }
        }

        private void ExportRecords()
        {
            if (_recordedValues.Count == 0)
            {
                MessageBox.Show("没有记录值可导出。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string fileName = $"rssi_records_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
            using (var writer = new StreamWriter(path))
            {
                writer.WriteLine("RSSI 记录值日志");
                writer.WriteLine("时间: " + DateTime.Now);
                writer.WriteLine("阈值设置: " + SettingsManager.Settings.RssiThreshold + " dBm");
                writer.WriteLine("--------------------");
                for (int i = 0; i < _recordedValues.Count; i++)
                {
                    writer.WriteLine($"{i + 1}: {_recordedValues[i]} dBm");
                }
            }
            MessageBox.Show($"已导出至: {path}", "导出成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
