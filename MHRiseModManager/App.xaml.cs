using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Threading;

namespace MHRiseModManager
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private Mutex _mutex = new Mutex(false, "MHRiseModManager");

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if(_mutex.WaitOne(0, false))
            {
                return;
            }

            MessageBox.Show("二重起動できません", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
            _mutex.Close();
            _mutex = null;
            Shutdown();
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            if(_mutex != null)
            {
                _mutex.ReleaseMutex();
                _mutex.Close();
            }
        }
    }
}
