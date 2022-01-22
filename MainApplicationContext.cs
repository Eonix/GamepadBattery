using GamepadBattery.Properties;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Media;
using Windows.Devices.Power;
using Windows.Gaming.Input;
using Windows.System.Power;
using Timer = System.Windows.Forms.Timer;

namespace GamepadBattery;

internal class MainApplicationContext : ApplicationContext
{
    private const string UnkownText = "Unknown gamepad battery status.";
    private const int DangerPercentageLevel = 5;
    private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(5);

    private readonly Container _container;
    private readonly NotifyIcon _notifyIcon;
    private readonly Timer _timer;
    private readonly SoundPlayer _soundPlayer;

    private bool _audioPlayed;

    public MainApplicationContext()
    {
        _container = new Container();
        _notifyIcon = new NotifyIcon(_container)
        {
            Visible = true,
            ContextMenuStrip = new ContextMenuStrip
            {
                Items =
                {
                    new ToolStripMenuItem("Exit", null, HandleExitClick),
                }
            },
        };

        _timer = new Timer(_container) { Interval = (int)UpdateInterval.TotalMilliseconds };
        _timer.Tick += UpdateBatteryLevel;
        _timer.Start();

        _soundPlayer = new SoundPlayer(Resources.Alert);

        UpdateBatteryLevel(null, new());
    }

    private void UpdateBatteryLevel(object? sender, EventArgs e)
    {
        var gamepads = Gamepad.Gamepads;
        if (gamepads.Count < 1)
        {
            _notifyIcon.Icon = Resources.Disconnected;
            _notifyIcon.Text = "No gamepad found.";
            return;
        }

        foreach (var gamepad in gamepads)
        {
            var report = gamepad?.TryGetBatteryReport();
            if (report == null)
                continue;

            if (!TryGetChargePercentage(report, out var percentage))
            {
                _notifyIcon.Icon = Resources.Disconnected;
                _notifyIcon.Text = UnkownText;
                continue;
            }

            var (StatusText, Icon) = report.Status switch
            {
                BatteryStatus.NotPresent => ("No gamepad battery found.", Resources.Empty),
                BatteryStatus.Discharging or BatteryStatus.Idle => ($"Gamepad battery: {percentage}%", GetStatusIcon(percentage.Value)),
                BatteryStatus.Charging => ($"Gamepad charging: {percentage}%", Resources.Charging),
                _ => (UnkownText, Resources.Empty),
            };

            _notifyIcon.Icon = Icon;
            _notifyIcon.Text = StatusText;

            if (!_audioPlayed && percentage <= DangerPercentageLevel)
            {
                _soundPlayer.Play();
                _audioPlayed = true;
            }

            if (_audioPlayed && percentage > DangerPercentageLevel)
                _audioPlayed = false;
        }
    }

    private static Icon GetStatusIcon(int percentage) =>
        percentage > 66 ? Resources.Full :
        percentage > 33 ? Resources.Used :
        percentage > DangerPercentageLevel ? Resources.Low :
        Resources.Empty;

    private static bool TryGetChargePercentage(BatteryReport report, [NotNullWhen(true)] out int? percentage)
    {
        var full = (double?)report.FullChargeCapacityInMilliwattHours;
        var remaining = (double?)report.RemainingCapacityInMilliwattHours;

        if (full is null || remaining is null)
            percentage = null;
        else
            percentage = (int)Math.Round(remaining.Value / full.Value * 100);

        return percentage != null;
    }

    private void HandleExitClick(object? sender, EventArgs args) => ExitThread();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _container.Dispose();
            _soundPlayer.Dispose();
        }

        base.Dispose(disposing);
    }

    protected override void ExitThreadCore()
    {
        _notifyIcon.Visible = false;
        base.ExitThreadCore();
    }
}