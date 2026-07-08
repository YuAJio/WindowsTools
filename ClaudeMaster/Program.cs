namespace ClaudeMaster;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        using var mutex = new Mutex(true, "ClaudeMaster_NingQingHan_Singleton", out bool createdNew);
        if (!createdNew)
        {
            MessageBox.Show(
                "ClaudeMaster 已经在运行啦~ 看看系统托盘喵！",
                "ClaudeMaster",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}
