namespace MoodyBlues;

public partial class MainForm : Form
{
    private readonly RecordEngine _recordEngine = new();
    private readonly PlaybackEngine _playbackEngine = new();
    private CancellationTokenSource _playbackCts = new();

    private const int HK_RECORD = 1;
    private const int HK_STOP = 2;
    private const int HK_PLAY = 3;

    public MainForm()
    {
        InitializeComponent();
        RegisterHotKeys();
        RefreshList();

        _recordEngine.OnStarted += () => UpdateStatus("🔴 录制中...", Color.Crimson);
        _recordEngine.OnStopped += () =>
        {
            UpdateStatus("⏸ 录制已保存", Color.DimGray);
            RefreshList();
        };

        _playbackEngine.OnStarted += () =>
        {
            UpdateStatus("▶ 播放中...", Color.DarkGreen);
            btnStop.Invoke(() => btnStop.Enabled = true);
        };
        _playbackEngine.OnFinished += () =>
        {
            UpdateStatus("⏸ 播放完成", Color.DimGray);
            btnStop.Invoke(() => btnStop.Enabled = false);
        };
    }

    // ═══════════════════════════════════════
    //  热键
    // ═══════════════════════════════════════

    private void RegisterHotKeys()
    {
        NativeMethods.RegisterHotKey(this.Handle, HK_RECORD, NativeMethods.MOD_NONE, NativeMethods.VK_F4);
        NativeMethods.RegisterHotKey(this.Handle, HK_STOP, NativeMethods.MOD_NONE, NativeMethods.VK_F5);
        NativeMethods.RegisterHotKey(this.Handle, HK_PLAY, NativeMethods.MOD_NONE, NativeMethods.VK_F6);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == NativeMethods.WM_HOTKEY)
        {
            int id = m.WParam.ToInt32();
            if (id == HK_RECORD) StartRecording();
            else if (id == HK_STOP) StopRecording();
            else if (id == HK_PLAY) PlaySelected();
        }
        base.WndProc(ref m);
    }

    // ═══════════════════════════════════════
    //  录制
    // ═══════════════════════════════════════

    private void StartRecording()
    {
        if (_recordEngine.IsRecording || _playbackEngine.IsPlaying) return;
        _recordEngine.Start();
    }

    private void StopRecording()
    {
        if (!_recordEngine.IsRecording) return;
        _recordEngine.Stop();
    }

    // ═══════════════════════════════════════
    //  播放
    // ═══════════════════════════════════════

    private async void PlaySelected()
    {
        if (_playbackEngine.IsPlaying || _recordEngine.IsRecording) return;
        if (lbRecords.SelectedItem is not string displayName) return;

        var id = displayName.Split(" - ")[0];
        var recording = RecordStore.Load(id);
        if (recording == null) return;

        _playbackCts = new CancellationTokenSource();
        btnStop.Enabled = true;
        await _playbackEngine.PlayAsync(recording, _playbackCts.Token);
        btnStop.Enabled = false;
    }

    // ═══════════════════════════════════════
    //  列表
    // ═══════════════════════════════════════

    private void DeleteSelected()
    {
        if (lbRecords.SelectedItem is not string displayName) return;
        var id = displayName.Split(" - ")[0];
        RecordStore.Delete(id);
        RefreshList();
    }

    private void RefreshList()
    {
        lbRecords.Items.Clear();
        foreach (var rec in RecordStore.ListAll())
        {
            var count = rec.Events.Count;
            var duration = count > 0 ? rec.Events[^1].OffsetMs / 1000.0 : 0;
            lbRecords.Items.Add($"{rec.Id} - {count} 事件, {duration:F1}s");
        }
        lblRecordCount.Text = $"{lbRecords.Items.Count} 条记录";
    }

    private void UpdateStatus(string text, Color color)
    {
        if (InvokeRequired)
        {
            Invoke(() => UpdateStatus(text, color));
            return;
        }
        lblStatus.Text = text;
        lblStatus.ForeColor = color;
    }

    // ═══════════════════════════════════════
    //  窗口
    // ═══════════════════════════════════════

    private void OnFormResize(object? sender, EventArgs e)
    {
        if (this.WindowState == FormWindowState.Minimized) this.Hide();
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (e.CloseReason != CloseReason.UserClosing) return;
        this.Hide();
        e.Cancel = true;
    }

    private void OnShowFromTray(object? sender, EventArgs e)
    {
        this.Show();
        this.WindowState = FormWindowState.Normal;
        this.Activate();
    }

    private void OnExit(object? sender, EventArgs e)
    {
        _recordEngine.Dispose();
        _playbackEngine.Stop();
        NativeMethods.UnregisterHotKey(this.Handle, HK_RECORD);
        NativeMethods.UnregisterHotKey(this.Handle, HK_STOP);
        NativeMethods.UnregisterHotKey(this.Handle, HK_PLAY);
        notifyIcon.Visible = false;
        this.FormClosing -= OnFormClosing;
        Application.Exit();
    }
}
