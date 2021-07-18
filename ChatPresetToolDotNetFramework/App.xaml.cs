using System.Reflection;
using System.Threading;
using System.Windows;

namespace ChatPresetTool
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private readonly Mutex _mutex = new Mutex(false, Assembly.GetExecutingAssembly().GetName().Name);

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // ミューテックスの所有権を要求
            if (!_mutex.WaitOne(0, false))
            {
                MessageBox.Show("Minecraftチャット便利ツールは多重起動出来ません。", "多重起動", MessageBoxButton.OK, MessageBoxImage.Warning);
                Shutdown();
            }
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            if (_mutex != null)
            {
                _mutex.ReleaseMutex();
                _mutex.Close();
            }
        }
    }
}