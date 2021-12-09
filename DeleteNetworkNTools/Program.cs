using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Windows;
using Microsoft.Win32;

namespace DeleteNetworkNTools
{
    static class Program
    {
        [DllImport("kernel32.dll")]
        public static extern bool AllocConsole();

        [DllImport("kernel32.dll")]
        public static extern bool FreeConsole();

        [STAThread]
        private static void Main()
        {
            _ = AllocConsole();

            try
            {
                //检查是否是以管理员启动
                if (!IsRunAsAdmin())
                {
                    //创建启动对象
                    var startInfo = new ProcessStartInfo
                    {
                        UseShellExecute = true,
                        WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory,
                        FileName =
                            Process.GetCurrentProcess().MainModule.FileName ??
                            throw new InvalidOperationException("获取本进程的文件位置失败"),
                        Verb = "runas",
                    };
                    _ = Process.Start(startInfo);

                    return;
                }

                删除Profiles中的子节点();
                删除Unmanaged中的子节点();
                var text =
                    $@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\NetworkList\Profiles{
                        Environment.NewLine
                    }HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\NetworkList\Signatures\Unmanaged";
                Clipboard.SetText(text);

                //弹窗提示清理完成
                _ = MessageBox.Show("清理完成！",
                    "提示",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                删除多余Network后(text);
            }
            catch (Exception e)
            {
                //弹窗显示异常信息
                _ = MessageBox.Show(e.Message,
                    "错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private static void 删除多余Network后(string text)
        {
            //在temp目录中创建一个随机文件
            var tempFile = Path.Combine(Path.GetTempPath(),
                $@"{Path.GetRandomFileName()}.txt");

            //如果temp目录不存在，则创建
            if (!Directory.Exists(Path.GetTempPath()))
            {
                _ = Directory.CreateDirectory(Path.GetTempPath());
            }

            //将文件内容写入到文件中
            File.WriteAllText(tempFile, text);
            //使用记事本打开文件
            _ = Process.Start("notepad.exe", tempFile);

            //使用cmd打开regedit
            _ = Process.Start("cmd.exe", "/c regedit");
        }

        private static void 删除Unmanaged中的子节点()
        {
            using var rk = Registry.LocalMachine.CreateSubKey(
                "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\NetworkList\\Signatures\\Unmanaged",
                true);

            foreach (var subKeyName in rk.GetSubKeyNames())
            {
                rk.DeleteSubKeyTree(subKeyName, false);
            }
        }

        private static void 删除Profiles中的子节点()
        {
            //打开注册表HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\NetworkList\Profiles
            using var rk = Registry.LocalMachine.CreateSubKey(
                "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\NetworkList\\Profiles",
                true);

            foreach (var subKeyName in rk.GetSubKeyNames())
            {
                rk.DeleteSubKeyTree(subKeyName, false);
            }
        }

        private static bool IsRunAsAdmin()
        {
            //检查本程序是否是以Windows管理员身份运行
            using var wi = WindowsIdentity.GetCurrent();
            var wp = new WindowsPrincipal(wi);

            return wp.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }
}