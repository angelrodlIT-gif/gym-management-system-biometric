using BiometricApp;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Sistema_Gimnasio
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            Gym_System.Core.NotificadorCambioMiembro.OnMiembroModificado = BiometricCache.Invalidar;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            FingerprintManager.Instance.Dispose();
            base.OnExit(e);
        }
    }


}
