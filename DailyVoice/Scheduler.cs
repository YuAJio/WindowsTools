using System.Diagnostics;

namespace DailyVoice;

/// <summary>
/// 定时检测 + 持久化洗牌队列 + 视频独立定时 (⁎⁍̴̛ᴗ⁍̴̛⁎)
/// 每秒检查是否到了播放时间。洗牌状态跨重启持久化，避免重复播放。
/// </summary>
internal class Scheduler : IDisposable
{
    private readonly System.Windows.Forms.Timer _timer;
    private readonly string _voiceDir;
    private readonly AudioPlayer _player;
    private readonly Func<Config> _configLoader;

    private DateTime _lastPlayedMinute = DateTime.MinValue;
    private DateTime _videoLastPlayedMinute = DateTime.MinValue;

    // 持久化洗牌状态
    private ShuffleState _shuffleState = new();
    private const int MaxRecentlyPlayed = 20;

    // 事件：通知 UI 状态变化
    public event Action? OnPlayStarted;
    public event Action? OnPlayFinished;
    /// <summary>视频排班播放进度（索引），供 UI 高亮当前项</summary>
    public event Action<int>? OnVideoStarted;

    public Scheduler(string voiceDir, AudioPlayer player, Func<Config> configLoader)
    {
        _voiceDir = voiceDir;
        _player = player;
        _configLoader = configLoader;

        Directory.CreateDirectory(_voiceDir);

        LoadOrCreateShuffleState();

        _timer = new System.Windows.Forms.Timer { Interval = 1000 }; // 1s 精度
        _timer.Tick += OnTick;
        _timer.Start();
    }

    // ═══════════════════════════════════════
    //  洗牌状态管理
    // ═══════════════════════════════════════

    /// <summary>
    /// 加载持久化洗牌状态，合并文件系统变化
    /// </summary>
    private void LoadOrCreateShuffleState()
    {
        _shuffleState = ShuffleStateManager.Load();
        var currentFiles = new HashSet<string>(GetVoiceFiles());

        // 清理已不存在的文件条目
        _shuffleState.Queue.RemoveAll(f => !currentFiles.Contains(f));
        _shuffleState.RecentlyPlayed.RemoveAll(f => !currentFiles.Contains(f));

        // 检测新文件（不在队列也不在历史）
        var filesInQueue = new HashSet<string>(_shuffleState.Queue);
        var filesInHistory = new HashSet<string>(_shuffleState.RecentlyPlayed);
        var newFiles = currentFiles
            .Where(f => !filesInQueue.Contains(f) && !filesInHistory.Contains(f))
            .ToArray();

        if (newFiles.Length > 0)
        {
            // Fisher-Yates 洗牌后追加到队尾
            var shuffledNew = FisherYatesShuffle([.. newFiles]);
            _shuffleState.Queue.AddRange(shuffledNew);
        }

        // Clamp index
        if (_shuffleState.QueueIndex >= _shuffleState.Queue.Count)
            _shuffleState.QueueIndex = 0;

        SaveState();
    }

    /// <summary>
    /// 重建洗牌队列（文件列表变化或队列耗尽时调用）
    /// </summary>
    public void BuildShuffleQueue()
    {
        var currentFiles = new HashSet<string>(GetVoiceFiles());

        // 清理已删除文件
        _shuffleState.Queue.RemoveAll(f => !currentFiles.Contains(f));
        _shuffleState.RecentlyPlayed.RemoveAll(f => !currentFiles.Contains(f));

        if (_shuffleState.Queue.Count == 0 ||
            _shuffleState.QueueIndex >= _shuffleState.Queue.Count)
        {
            // 从当前文件构建新队列，排除最近播放过的
            var candidates = currentFiles
                .Except(new HashSet<string>(_shuffleState.RecentlyPlayed))
                .ToArray();

            if (candidates.Length == 0)
            {
                // 所有文件都播过了 → 重置历史
                _shuffleState.RecentlyPlayed.Clear();
                candidates = currentFiles.ToArray();
            }

            _shuffleState.Queue = FisherYatesShuffle([.. candidates]);
            _shuffleState.QueueIndex = 0;
        }

        SaveState();
    }

    private void SaveState()
    {
        ShuffleStateManager.Save(_shuffleState);
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

    // ═══════════════════════════════════════
    //  播放控制
    // ═══════════════════════════════════════

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

    // ═══════════════════════════════════════
    //  定时检测
    // ═══════════════════════════════════════

    private void OnTick(object? sender, EventArgs e)
    {
        var cfg = _configLoader();
        if (!TimeSpan.TryParse(cfg.PlayTime, out var playTime)) return;
        _player.Volume = cfg.Volume / 100f;

        var now = DateTime.Now;
        var currentMinute = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0);

        // 音频定时
        if (now.Hour == playTime.Hours && now.Minute == playTime.Minutes
            && _lastPlayedMinute != currentMinute)
        {
            _lastPlayedMinute = currentMinute;
            PlayNow();
        }

        // 视频独立定时
        if (!string.IsNullOrEmpty(cfg.VideoPlayTime) &&
            TimeSpan.TryParse(cfg.VideoPlayTime, out var videoTime))
        {
            if (now.Hour == videoTime.Hours && now.Minute == videoTime.Minutes
                && _videoLastPlayedMinute != currentMinute)
            {
                _videoLastPlayedMinute = currentMinute;
                PlayVideoIfConfigured(cfg);
            }
        }
    }

    // ═══════════════════════════════════════
    //  内部逻辑
    // ═══════════════════════════════════════

    private string? DequeueNext()
    {
        if (_shuffleState.Queue.Count == 0)
            return null;

        if (_shuffleState.QueueIndex >= _shuffleState.Queue.Count)
            BuildShuffleQueue();

        if (_shuffleState.QueueIndex >= _shuffleState.Queue.Count)
            return null;

        var file = _shuffleState.Queue[_shuffleState.QueueIndex];
        _shuffleState.QueueIndex++;
        SaveState();
        return file;
    }

    private void PlayWithIntro(string mainFile)
    {
        OnPlayStarted?.Invoke();

        // 加入播放历史，防止短期重复
        _shuffleState.RecentlyPlayed.Add(mainFile);
        while (_shuffleState.RecentlyPlayed.Count > MaxRecentlyPlayed)
            _shuffleState.RecentlyPlayed.RemoveAt(0);
        SaveState();

        var introPath = Path.Combine(_voiceDir, "intro.mp3");
        string[] chain;
        if (File.Exists(introPath))
            chain = [introPath, mainFile];
        else
            chain = [mainFile];

        _player.PlayContinuous(chain, silenceSeconds: 1.5, () => OnPlayFinished?.Invoke());
    }

    private void PlayVideoIfConfigured(Config cfg)
    {
        // 预检：至少有一个可播放文件；失效项由播放器内部跳过（保持索引与列表对齐）
        if (!cfg.VideoPlaylist.Any(File.Exists))
            return; // 静默跳过

        try
        {
            using var videoForm = new VideoPlayerForm(cfg.VideoPlaylist.ToList());
            videoForm.OnVideoStarted += index => OnVideoStarted?.Invoke(index);
            videoForm.ShowDialog(); // 阻塞 UI 线程，播放期间暂停定时器触发
        }
        catch (Exception ex)
        {
            // 静默容错，不弹框
            Debug.WriteLine($"DailyVoice 视频播放失败: {ex.Message}");
        }
    }

    private static List<string> FisherYatesShuffle(List<string> list)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = Random.Shared.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
        return list;
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Dispose();
        _player.Dispose();
    }
}
