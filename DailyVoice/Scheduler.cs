namespace DailyVoice;

/// <summary>
/// 定时检测 + 洗牌队列 (⁎⁍̴̛ᴗ⁍̴̛⁎)
/// 每秒检查是否到了设定的播放时间。
/// </summary>
internal class Scheduler : IDisposable
{
    private readonly System.Windows.Forms.Timer _timer;
    private readonly string _voiceDir;
    private readonly AudioPlayer _player;
    private readonly Func<Config> _configLoader;

    private DateTime _lastPlayedMinute = DateTime.MinValue;

    // 洗牌队列
    private List<string> _shuffledQueue = [];
    private int _queueIndex;

    // 事件：通知 UI 状态变化
    public event Action? OnPlayStarted;
    public event Action? OnPlayFinished;

    public Scheduler(string voiceDir, AudioPlayer player, Func<Config> configLoader)
    {
        _voiceDir = voiceDir;
        _player = player;
        _configLoader = configLoader;

        Directory.CreateDirectory(_voiceDir);

        BuildShuffleQueue();

        _timer = new System.Windows.Forms.Timer { Interval = 1000 }; // 1s 精度
        _timer.Tick += OnTick;
        _timer.Start();
    }

    /// <summary>
    /// 重建洗牌队列（文件列表变化时调用）
    /// </summary>
    public void BuildShuffleQueue()
    {
        var files = GetVoiceFiles();
        _shuffledQueue = [.. files];

        // Fisher-Yates 洗牌
        for (var i = _shuffledQueue.Count - 1; i > 0; i--)
        {
            var j = Random.Shared.Next(i + 1);
            (_shuffledQueue[i], _shuffledQueue[j]) = (_shuffledQueue[j], _shuffledQueue[i]);
        }
        _queueIndex = 0;
    }

    /// <summary>
    /// 获取语音文件列表（排除 intro.mp3）
    /// </summary>
    public string[] GetVoiceFiles()
    {
        var introPath = Path.Combine(_voiceDir, "intro.mp3");
        return Directory.GetFiles(_voiceDir, "*.mp3")
            .Concat(Directory.GetFiles(_voiceDir, "*.wav"))
            .Where(f => !string.Equals(f, introPath, StringComparison.OrdinalIgnoreCase))
            .OrderBy(f => f)
            .ToArray();
    }

    /// <summary>
    /// 立即播放一首（用户手动触发）
    /// </summary>
    public void PlayNow()
    {
        if (_player.IsPlaying) return;

        var file = DequeueNext();
        if (file == null) return;

        PlayWithIntro(file);
    }

    /// <summary>
    /// 试听指定文件
    /// </summary>
    public void Preview(string filePath)
    {
        _player.Preview(filePath);
    }

    /// <summary>
    /// 停止播放
    /// </summary>
    public void StopPlayback()
    {
        _player.Stop();
        OnPlayFinished?.Invoke();
    }

    private void OnTick(object? sender, EventArgs e)
    {
        var cfg = _configLoader();
        if (!TimeSpan.TryParse(cfg.PlayTime, out var playTime)) return;
        _player.Volume = cfg.Volume / 100f;

        var now = DateTime.Now;
        var currentMinute = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0);

        if (now.Hour == playTime.Hours && now.Minute == playTime.Minutes
            && _lastPlayedMinute != currentMinute)
        {
            _lastPlayedMinute = currentMinute;
            PlayNow();
        }
    }

    private string? DequeueNext()
    {
        var files = GetVoiceFiles();
        if (files.Length == 0) return null;

        // 文件列表变化检测
        if (_shuffledQueue.Count > 0 &&
            !files.SequenceEqual(_shuffledQueue.OrderBy(f => f)))
        {
            BuildShuffleQueue();
        }

        if (_queueIndex >= _shuffledQueue.Count)
            BuildShuffleQueue();

        var file = _shuffledQueue[_queueIndex];
        _queueIndex++;
        return file;
    }

    private void PlayWithIntro(string mainFile)
    {
        OnPlayStarted?.Invoke();

        var introPath = Path.Combine(_voiceDir, "intro.mp3");
        string[] chain;
        if (File.Exists(introPath))
            chain = [introPath, mainFile];
        else
            chain = [mainFile];

        _player.PlayChain(chain, () => OnPlayFinished?.Invoke());
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Dispose();
        _player.Dispose();
    }
}
