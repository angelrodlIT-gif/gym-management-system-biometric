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
                DateTime fechaInicio = datePickerPago.SelectedDate ?? DateTime.Today;
                DateTime fechaFin;

                // Determina si la duración es en días o meses
                if (seleccionada.UnidadDuracion == "Meses")
                {
                    fechaFin = fechaInicio.AddMonths(seleccionada.Duracion);
                }
                else // Asumir "Días"
                {
                    fechaFin = fechaInicio.AddDays(seleccionada.Duracion);
                }

                long idMembresia = seleccionada.id;
                decimal monto = seleccionada.Precio;

                Miembro_Conexion conexion = new Miembro_Conexion();
                bool exito = conexion.RegistrarPago(_idMiembro, fechaInicio, fechaFin, idMembresia);

                if (exito)
                {
                    bool pagoGuardado = conexion.RegistrarPagoHistorial(idMembresia, fechaInicio, monto);
                    if (!pagoGuardado)
                    {
                        MessageBox.Show("Se registró la membresía, pero ocurrió un error al guardar el historial de pago.");
                    }
                    else
                    {
                        MessageBox.Show("Pago registrado exitosamente.");
                        this.Close();
                    }
                }
                else
                {
                    MessageBox.Show("Error al registrar el pago.");
                }
            }
            else
            {
                MessageBox.Show("Selecciona una membresía.");
            }
        }

    }
}
