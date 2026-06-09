using NAudio.Wave;

namespace DailyVoice;

/// <summary>
/// NAudio 音频播放引擎 (⁎⁍̴̛ᴗ⁍̴̛⁎)
/// 支持链式播放：intro.mp3 → 正文 → onAllDone
/// </summary>
internal class AudioPlayer : IDisposable
{
    private WaveOutEvent? _waveOut;
    private AudioFileReader? _reader;
    private bool _disposed;

    public float Volume { get; set; } = 0.8f;
    public bool IsPlaying => _waveOut?.PlaybackState == PlaybackState.Playing;

    /// <summary>
    /// 按顺序播放文件列表（前一个是 intro 的话先播 intro），全部完成回调。
    /// </summary>
    public void PlayChain(string[] files, Action? onAllDone = null)
    {
        Stop();
        if (files.Length == 0)
        {
            onAllDone?.Invoke();
            return;
        }

        PlayNext(files, 0, onAllDone);
    }

    private void PlayNext(string[] files, int index, Action? onAllDone)
    {
        if (index >= files.Length)
        {
            onAllDone?.Invoke();
            return;
        }

        try
        {
            _reader = new AudioFileReader(files[index]);
            _waveOut = new WaveOutEvent();
            _waveOut.Volume = Volume;
            _waveOut.Init(_reader);

            _waveOut.PlaybackStopped += (_, _) =>
            {
                Cleanup();
                PlayNext(files, index + 1, onAllDone);
            };

            _waveOut.Play();
        }
        catch
        {
            // 文件损坏则跳过
            Cleanup();
            PlayNext(files, index + 1, onAllDone);
        }
    }

    /// <summary>
    /// 试听单个文件（不触发链式回调）
    /// </summary>
    public void Preview(string filePath, Action? onDone = null)
    {
        Stop();
        try
        {
            _reader = new AudioFileReader(filePath);
            _waveOut = new WaveOutEvent();
            _waveOut.Volume = Volume;
            _waveOut.Init(_reader);

            _waveOut.PlaybackStopped += (_, _) =>
            {
                Cleanup();
                onDone?.Invoke();
            };

            _waveOut.Play();
        }
        catch { Cleanup(); onDone?.Invoke(); }
    }

    public void Stop()
    {
        _waveOut?.Stop();
        Cleanup();
    }

    private void Cleanup()
    {
        _waveOut?.Dispose();
        _waveOut = null;
        _reader?.Dispose();
        _reader = null;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
    }
}
