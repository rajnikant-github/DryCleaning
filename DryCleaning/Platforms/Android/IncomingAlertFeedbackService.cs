using Android.Media;
using DryCleaning.Services;

namespace DryCleaning.Platforms.Android;

public sealed class IncomingAlertFeedbackService : IIncomingAlertFeedbackService
{
    private MediaPlayer? _player;

    public async Task PlayAsync(CancellationToken cancellationToken = default)
    {
        Stop();

        var soundPath = Path.Combine(FileSystem.CacheDirectory, "incoming-alert.mp3");
        await using (var source = await FileSystem.OpenAppPackageFileAsync("Alerts/alert.mp3"))
        await using (var destination = File.Create(soundPath))
            await source.CopyToAsync(destination, cancellationToken);

        _player = new MediaPlayer();
        _player.SetDataSource(soundPath);
        _player.Prepare();
        _player.Start();
        Vibrate();
    }

    public void Stop()
    {
        if (_player is null)
            return;

        if (_player.IsPlaying)
            _player.Stop();

        _player.Release();
        _player.Dispose();
        _player = null;
    }

    private static void Vibrate()
    {
        try
        {
            Microsoft.Maui.Devices.Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(350));
        }
        catch (FeatureNotSupportedException)
        {
            // Continue with the sound if a device doesn't expose vibration hardware.
        }
    }
}
