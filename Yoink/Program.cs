namespace Yoink;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        using var mutex = new Mutex(true, "Yoink_NingQingHan_Singleton", out bool createdNew);
        if (!createdNew)
        {
            MessageBox.Show("Yoink 已经在运行啦~ 看看任务栏喵！", "Yoink",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}
