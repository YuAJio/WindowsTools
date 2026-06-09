namespace ThumbPin;

internal static class Program
{
    private static MainForm _mainForm = null!;

    [STAThread]
    static void Main()
    {
        using var mutex = new Mutex(true, "ThumbPin_NingQingHan_Singleton", out bool createdNew);
        if (!createdNew)
        {
            MessageBox.Show("ThumbPin 已经在运行啦~ 看看系统托盘喵！", "ThumbPin",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        ApplicationConfiguration.Initialize();

        _mainForm = new MainForm();
        _mainForm.Show();

        Application.Run();
    }
}
