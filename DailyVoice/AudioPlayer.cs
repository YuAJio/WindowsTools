using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace DailyVoice;

/// <summary>
/// NAudio 音频播放引擎 (⁎⁍̴̛ᴗ⁍̴̛⁎)
/// 支持连续流播放：静音前导 + intro.mp3 + 正文 → 单次 WaveOutEvent 无间隙。
/// </summary>
internal class AudioPlayer : IDisposable
{
    private WaveOutEvent? _waveOut;
    private bool _disposed;

    public float Volume { get; set; } = 0.8f;
    public bool IsPlaying => _waveOut?.PlaybackState == PlaybackState.Playing;

    /// <summary>
    /// 连续流播放：前置静音 + 文件列表串成一条流，单次 WaveOutEvent。
    /// 静音段唤醒休眠音响设备，避免前 1-2 秒被吃掉。
    /// </summary>
    public void PlayContinuous(string[] files, double silenceSeconds, Action? onAllDone = null)
    {
        Stop();
        if (files.Length == 0)
        {
            onAllDone?.Invoke();
            return;
        }

        // 逐个打开文件，收集 ISampleProvider
        var providers = new List<ISampleProvider>();
        WaveFormat? format = null;

        foreach (var f in files)
        {
            try
            {
                var reader = new AudioFileReader(f);
                format ??= reader.WaveFormat;
                providers.Add(reader);
            }
            catch
            {
                // 文件损坏则跳过
            }
        }

        if (format == null || providers.Count == 0)
        {
            onAllDone?.Invoke();
            return;
        }

        // 前置静音段唤醒设备 (⁎⁍̴̛ᴗ⁍̴̛⁎)
        var silence = new SilenceSampleProvider(format, silenceSeconds);
        providers.Insert(0, silence);

        try
        {
            var concatenated = new ConcatenatingSampleProvider(providers);
            // WaveOutEvent.Init 需要 IWaveProvider，用 SampleToWaveProvider 包装
            var waveProvider = new SampleToWaveProvider(concatenated);

            _waveOut = new WaveOutEvent();
            _waveOut.Volume = Volume;
            _waveOut.Init(waveProvider);

            _waveOut.PlaybackStopped += (_, _) =>
            {
                Cleanup();
                onAllDone?.Invoke();
            };

            _waveOut.Play();
        }
        catch
        {
            Cleanup();
            onAllDone?.Invoke();
        }
    }

    /// <summary>
    /// 试听单个文件（也走连续流，享受静音前导）
    /// </summary>
    public void Preview(string filePath, Action? onDone = null)
    {
        PlayContinuous([filePath], silenceSeconds: 1.5, onDone);
    }

    /// <summary>
    /// 旧版链式播放（保留兼容，Scheduler 已迁移到 PlayContinuous）
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
            using var reader = new AudioFileReader(files[index]);
            _waveOut = new WaveOutEvent();
            _waveOut.Volume = Volume;
            _waveOut.Init(reader);

            _waveOut.PlaybackStopped += (_, _) =>
            {
                Cleanup();
                PlayNext(files, index + 1, onAllDone);
            };

            _waveOut.Play();
        }
        catch
        {
            Cleanup();
            PlayNext(files, index + 1, onAllDone);
        }
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
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
    }
}
