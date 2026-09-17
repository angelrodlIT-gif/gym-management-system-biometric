using BiometricApp;
using System;
using System.Threading.Tasks;
using System.Windows;
using static BiometricApp.UCVerifyFingerprint;

namespace Sistema_Gimnasio
{
    public partial class SistemaAcceso : Window
    {
        private UCVerifyFingerprint verificador;

        public SistemaAcceso()
        {
            InitializeComponent();
            IniciarVerificacion();
        }

        private void IniciarVerificacion()
        {
            try
            {
                LimpiarVerificador();

                verificador = new UCVerifyFingerprint();
                winFormsHost.Child = verificador;
                verificador.HuellaVerificada += Verificador_HuellaVerificada;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error iniciando verificación: " + ex.Message);
            }
        }

        private void LimpiarVerificador()
        {
            try
            {
                if (verificador != null)
                {
                    verificador.HuellaVerificada -= Verificador_HuellaVerificada;
                    verificador.DetenerLector();
                    if (winFormsHost != null)
                    {
                        winFormsHost.Child = null;
                    }
                    verificador.Dispose();
                    verificador = null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error al limpiar verificador biométrico: " + ex.Message);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            LimpiarVerificador();
            base.OnClosed(e);
        }

        private void Verificador_HuellaVerificada(object sender, HuellaVerificadaEventArgs e)
        {
            if (Dispatcher == null || Dispatcher.HasShutdownStarted) return;

            Dispatcher.BeginInvoke((Action)(() =>
            {
                if (!this.IsLoaded) return;

                txtNombre.Text = $"Nombre: {e.Nombre}";
                txtMembresia.Text = $"Membresía: {e.Membresia}";
                txtFechaFin.Text = $"Vigencia: {e.FechaFin:dd/MM/yyyy}";

                Task.Delay(5000).ContinueWith(_ =>
                {
                    if (Dispatcher != null && !Dispatcher.HasShutdownStarted)
                    {
                        Dispatcher.BeginInvoke((Action)(() =>
                        {
                            if (this.IsLoaded)
                            {
                                txtNombre.Text = "Nombre: ";
                                txtMembresia.Text = "Membresía: ";
                                txtFechaFin.Text = "Vigencia: ";
                            }
                        }));
                    }
                });
            }));
        }
    }
}