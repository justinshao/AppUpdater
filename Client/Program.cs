using System;
using System.Windows.Forms;

namespace Justin.Updater.Client
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();

            //处理UI线程异常
            Application.ThreadException += Application_ThreadException;
            //处理非UI线程异常
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            //if(UpdateHelper.IsThisAppRunning())
            //{
            //    MessageBox.Show("程序正在运行。");
            //}
            //else
            //{
            //    Application.SetCompatibleTextRenderingDefault(false);
            //    Application.Run(new FrmMain());
            //}

            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new FrmMain());
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            LogHelper.LogError("应用程序未处理异常", e.ExceptionObject as Exception);
        }

        private static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            LogHelper.LogError("应用程序未处理异常", e.Exception);
        }
    }
}
