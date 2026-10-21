using AVFoundation;
using DryCleaning.Services;
using Foundation;

namespace DryCleaning.Platforms.iOS;

public sealed class IncomingAlertFeedbackService : IIncomingAlertFeedbackService
{
    private AVAudioPlayer? _player;

    public async Task PlayAsync(CancellationToken cancellationToken = default)
    {
        Stop();

        var soundPath = Path.Combine(FileSystem.CacheDirectory, "incoming-alert.mp3");
        await using (var source = await FileSystem.OpenAppPackageFileAsync("Alerts/alert.mp3"))
        await using (var destination = File.Create(soundPath))
            await source.CopyToAsync(destination, cancellationToken);

        _player = AVAudioPlayer.FromUrl(NSUrl.FromFilename(soundPath))
            ?? throw new InvalidOperationException("The incoming alert sound could not be loaded.");
        _player.PrepareToPlay();
        _player.Play();
        Vibrate();
    }

    public void Stop()
    {
        _player?.Stop();
        _player?.Dispose();
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
            // Some devices and the iOS simulator don't expose vibration hardware.
        }
    }
}
