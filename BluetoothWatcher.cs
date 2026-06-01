using System;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.Advertisement;

namespace BlueLockScreen
{
    public class BluetoothWatcher
    {
        private BluetoothLEAdvertisementWatcher _watcher;
        private readonly string _targetAddress;
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
            _targetAddress = deviceAddress.ToLower().Replace(":", "").Replace("-", "");
        }

        public void Start()
        {
            if (_isRunning) return;

            if (!ulong.TryParse(_targetAddress, System.Globalization.NumberStyles.HexNumber,
                    null, out ulong addr))
                throw new ArgumentException("无效的蓝牙 MAC 地址");

            // 修正：使用 BluetoothLEAdvertisementFilter.BluetoothAddress 属性
            var filter = new BluetoothLEAdvertisementFilter
            {
                BluetoothAddress = addr
            };

            _watcher = new BluetoothLEAdvertisementWatcher
            {
                AdvertisementFilter = filter
            };

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
