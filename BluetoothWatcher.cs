using System;
using System.Globalization;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.Advertisement;

namespace BlueLockScreen
{
    public class BluetoothWatcher
    {
        private BluetoothLEAdvertisementWatcher _watcher;
        private readonly ulong _targetAddressLong;
        private DateTime _lastReceived = DateTime.MinValue;
        private int _lastRssi;
        private bool _isRunning;

        public event EventHandler<int> RssiUpdated;
        public event EventHandler DeviceLost;
        public event EventHandler DeviceFound;

        public bool IsRunning => _isRunning;
        public int LostTimeoutSeconds { get; set; } = 10;

        public BluetoothWatcher(string deviceAddress)
        {
            string hex = deviceAddress.ToLower().Replace(":", "").Replace("-", "");
            if (!ulong.TryParse(hex, NumberStyles.HexNumber, null, out ulong addr))
                throw new ArgumentException("无效的蓝牙 MAC 地址");
            _targetAddressLong = addr;
        }

        public void Start()
        {
            if (_isRunning) return;

            // 不设置过滤器，在接收事件中通过地址筛选
            _watcher = new BluetoothLEAdvertisementWatcher();

            _watcher.Received += OnReceived;
            _watcher.Stopped += OnStopped;
            _isRunning = true;
            _lastReceived = DateTime.MinValue;
            _watcher.Start();

            Task.Run(CheckLostLoop);
        }

        public void Stop()
        {
            if (_watcher == null || !_isRunning) return;
            _watcher.Stop();
            _watcher.Received -= OnReceived;
            _watcher.Stopped -= OnStopped;
            _watcher = null;
            _isRunning = false;
        }

        private void OnReceived(BluetoothLEAdvertisementWatcher sender,
            BluetoothLEAdvertisementReceivedEventArgs args)
        {
            // 仅处理匹配的目标蓝牙地址
            if (args.BluetoothAddress != _targetAddressLong)
                return;

            _lastReceived = DateTime.Now;
            _lastRssi = args.RawSignalStrengthInDBm;
            RssiUpdated?.Invoke(this, _lastRssi);
            DeviceFound?.Invoke(this, EventArgs.Empty);
        }

        private void OnStopped(BluetoothLEAdvertisementWatcher sender,
            BluetoothLEAdvertisementWatcherStoppedEventArgs args)
        {
            _isRunning = false;
        }

        private async void CheckLostLoop()
        {
            while (_isRunning)
            {
                await Task.Delay(1000);
                if (!_isRunning) break;
                if (_lastReceived != DateTime.MinValue &&
                    (DateTime.Now - _lastReceived).TotalSeconds > LostTimeoutSeconds)
                {
                    DeviceLost?.Invoke(this, EventArgs.Empty);
                    break;
                }
            }
        }
    }
}
