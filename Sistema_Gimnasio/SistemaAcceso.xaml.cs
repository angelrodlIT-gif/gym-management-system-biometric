using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using BiometricApp;
using static BiometricApp.UCVerifyFingerprint;


namespace Sistema_Gimnasio
{
    

    public partial class SistemaAcceso : Window
    {
        public SistemaAcceso()
        {
            InitializeComponent();
            IniciarVerificacion();

        }
        private UCVerifyFingerprint verificador;
        private void IniciarVerificacion()
        {
            try
            {
                if (verificador != null)
                {
                    verificador.DetenerLector();
                    verificador.HuellaVerificada -= Verificador_HuellaVerificada;
                    winFormsHost.Child = null;
                    verificador.Dispose();
                }

                verificador = new UCVerifyFingerprint();
                winFormsHost.Child = verificador;
                verificador.HuellaVerificada += Verificador_HuellaVerificada;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error iniciando verificación: " + ex.Message);
            }
        }
        protected override void OnClosed(EventArgs e)
        {
            verificador?.DetenerLector();
            base.OnClosed(e);
        }
        private void Verificador_HuellaVerificada(object sender, HuellaVerificadaEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                txtNombre.Text = $"Nombre: {e.Nombre}";
                txtMembresia.Text = $"Membresía: {e.Membresia}";
                txtFechaFin.Text = $"Vigencia: {e.FechaFin:dd/MM/yyyy}";
                Task.Delay(5000).ContinueWith(_ =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        txtNombre.Text = "Nombre: ";
                        txtMembresia.Text = "Membresía: ";
                        txtFechaFin.Text = "Vigencia: ";
                    });
                });
            });
            
        }



    }


}