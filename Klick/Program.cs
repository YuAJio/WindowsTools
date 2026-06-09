namespace Klick;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        // 防止多开 — 一个 Klick 就够了 (⁎⁍̴̛ᴗ⁍̴̛⁎)
        using var mutex = new Mutex(true, "Klick_NingQingHan_Singleton", out bool createdNew);
        if (!createdNew)
        {
            MessageBox.Show("Klick 已经在运行啦~ 看看系统托盘喵！", "Klick", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}
