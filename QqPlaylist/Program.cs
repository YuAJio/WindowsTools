namespace QqPlaylist;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        // 防止多开 — 一个 QqPlaylist 就够了 (⁎⁍̴̛ᴗ⁍̴̛⁎)
        using var mutex = new Mutex(true, "QqPlaylist_NingQingHan_Singleton", out bool createdNew);
        if (!createdNew)
        {
            MessageBox.Show("QqPlaylist 已经在运行啦~ 看看任务栏喵！", "QqPlaylist", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}