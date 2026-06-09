namespace DailyVoice;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        // 单实例 (⁎⁍̴̛ᴗ⁍̴̛⁎)
        using var mutex = new Mutex(true, "DailyVoice_NingQingHan_Singleton", out bool createdNew);
        if (!createdNew)
        {
            MessageBox.Show("DailyVoice 已经在运行啦~ 看看系统托盘喵！", "DailyVoice",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        ApplicationConfiguration.Initialize();

        // 创建托盘
        var trayIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "DailyVoice — 每日语音播放",
            Visible = true,
            ContextMenuStrip = new ContextMenuStrip()
        };
        trayIcon.ContextMenuStrip.Items.Add("显示设置", null, OnShowSettings);
        trayIcon.ContextMenuStrip.Items.Add(new ToolStripSeparator());
        trayIcon.ContextMenuStrip.Items.Add("退出", null, (_, _) =>
        {
            trayIcon.Visible = false;
            Application.Exit();
        });
        trayIcon.DoubleClick += OnShowSettings;

        // 主窗口
        _mainForm = new MainForm();
        _mainForm.Show();

        Application.Run();

        trayIcon.Visible = false;
    }

    private static MainForm _mainForm = null!;

    private static void OnShowSettings(object? sender, EventArgs e)
    {
        if (_mainForm.IsDisposed) return;

        _mainForm.Show();
        _mainForm.WindowState = FormWindowState.Normal;
        _mainForm.Activate();
    }
}
