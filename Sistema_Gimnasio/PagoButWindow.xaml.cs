using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;
using Gym_System.Core;
using Gym_System.Data;

namespace Sistema_Gimnasio
{
    public partial class PagoButWindow : Window
    {
        private long _idMiembro;

        public PagoButWindow(long idMiembro)
        {
            InitializeComponent();
            _idMiembro = idMiembro;

            CargarComboMembresias();
            datePickerPago.SelectedDate = DateTime.Today;
        }

        private void CargarComboMembresias()
        {
            try
            {
                Membresias_conexion conexion = new Membresias_conexion();
                List<Membresias_agregar> membresias = conexion.ObtenerMembresias();
                cmbMembresias.ItemsSource = membresias;
                cmbMembresias.DisplayMemberPath = "Nombre";
                cmbMembresias.SelectedValuePath = "id";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar membresías: " + ex.Message);
            }
        }

        private void cmbMembresias_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (cmbMembresias.SelectedItem is Membresias_agregar seleccionada)
            {
                txtPrecio.Text = seleccionada.Precio.ToString("C");
            }
        }

        private void regPagoButton_Click(object sender, RoutedEventArgs e)
        {
            if (cmbMembresias.SelectedItem is Membresias_agregar seleccionada)
            {
                try
                {
                    DateTime fechaPago = datePickerPago.SelectedDate ?? DateTime.Today;
                    Miembro_Conexion conexion = new Miembro_Conexion();

                    long idMembresia = seleccionada.id;
                    decimal monto = seleccionada.Precio;

                    // Registro atómico transaccional: calcula vigencia restante bajo bloqueo SQL,
                    // actualiza fechas del miembro, estado Activo e inserta pago en una sola transacción.
                    bool exito = conexion.RegistrarPago(_idMiembro, idMembresia, fechaPago, monto);

                    if (exito)
                    {
                        BiometricApp.BiometricCache.Invalidar();
                        MessageBox.Show("Pago registrado exitosamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                        this.Close();
                    }
                    else
                    {
                        MessageBox.Show("Error al registrar el pago.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al registrar el pago: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Selecciona una membresía.", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

    }
}
