using NAudio.Wave;

namespace DailyVoice;

/// <summary>
/// ISampleProvider 静音生成器 (⁎⁍̴̛ᴗ⁍̴̛⁎)
/// 输出指定秒数的零采样，用于唤醒蓝牙/HDMI 休眠音响设备。
/// 耗尽时返回 0，表示流结束，ConcatenatingSampleProvider 自动切换到下一段。
/// </summary>
internal sealed class SilenceSampleProvider : ISampleProvider
{
    public WaveFormat WaveFormat { get; }

    private readonly long _totalSamples;
    private long _samplesRead;

    public SilenceSampleProvider(WaveFormat waveFormat, double seconds)
    {
        WaveFormat = waveFormat ?? throw new ArgumentNullException(nameof(waveFormat));
        _totalSamples = (long)(waveFormat.SampleRate * seconds * waveFormat.Channels);
    }

    public int Read(float[] buffer, int offset, int count)
    {
        var remaining = (int)Math.Min(count, _totalSamples - _samplesRead);
        if (remaining <= 0) return 0;

        Array.Clear(buffer, offset, remaining);
        _samplesRead += remaining;
        return remaining;
    }
}
