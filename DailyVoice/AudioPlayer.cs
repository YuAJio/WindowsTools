using NAudio.Wave;

namespace DailyVoice;

/// <summary>
/// NAudio 音频播放引擎 (⁎⁍̴̛ᴗ⁍̴̛⁎)
/// 支持连续流播放：静音前导 + intro.mp3 + 正文 → 单次 WaveOutEvent 无间隙。
/// 使用自研 ConcatenatedWaveProvider 替代 NAudio 的 SampleProvider 管线，
/// 避免 ConcatenatingSampleProvider 兼容性问题。
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
            // 自研串联 WaveProvider — 手动拼接 + PCM 转换，绕开 ConcatenatingSampleProvider
            var concatenated = new ConcatenatedWaveProvider(format, providers);

            _waveOut = new WaveOutEvent();
            _waveOut.Volume = Volume;
            _waveOut.Init(concatenated);

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

/// <summary>
/// 自研 ISampleProvider 串联 + float→PCM 转换 IWaveProvider (⁎⁍̴̛ᴗ⁍̴̛⁎)
/// 手动按序读取多个 ISampleProvider，实时转换为 16-bit PCM，
/// 绕开 NAudio 的 ConcatenatingSampleProvider + SampleToWaveProvider 兼容性问题。
/// </summary>
internal sealed class ConcatenatedWaveProvider : IWaveProvider
{
    public WaveFormat WaveFormat { get; }

    private readonly List<ISampleProvider> _providers;
    private int _currentProvider;

    public ConcatenatedWaveProvider(WaveFormat format, List<ISampleProvider> providers)
    {
        WaveFormat = format ?? throw new ArgumentNullException(nameof(format));
        _providers = providers ?? throw new ArgumentNullException(nameof(providers));
    }

    public int Read(byte[] buffer, int offset, int count)
    {
        int bytesWritten = 0;

        while (_currentProvider < _providers.Count && bytesWritten < count)
        {
            // 每次读一小块 float 采样，即时转换为 PCM
            int maxSamples = (count - bytesWritten) / 2; // 16-bit = 2 bytes/sample
            int samplesToRead = Math.Min(4096, maxSamples);
            var floatBuffer = new float[samplesToRead];

            int read = _providers[_currentProvider].Read(floatBuffer, 0, samplesToRead);

            if (read == 0)
            {
                // 当前 provider 耗尽，切换到下一个
                _currentProvider++;
                continue;
            }

            // float → 16-bit PCM 即时转换 (⁎⁍̴̛ᴗ⁍̴̛⁎)
            for (int i = 0; i < read; i++)
            {
                var clamped = Math.Clamp(floatBuffer[i], -1f, 1f);
                var pcm = (short)(clamped * short.MaxValue);
                buffer[offset + bytesWritten] = (byte)(pcm & 0xFF);
                buffer[offset + bytesWritten + 1] = (byte)((pcm >> 8) & 0xFF);
                bytesWritten += 2;
            }
        }

        return bytesWritten;
    }
}
