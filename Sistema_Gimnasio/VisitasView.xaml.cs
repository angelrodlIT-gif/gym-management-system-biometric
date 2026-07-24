using System;
using System.Collections.Generic;
using System.Data;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Gym_System.Core;

namespace Sistema_Gimnasio
{
    /// <summary>
    /// Interaction logic for VisitasView.xaml
    /// </summary>
    public partial class VisitasView : UserControl
    {
        public VisitasView()
        {
            InitializeComponent(); 
            CargarVisitas();
        }

        private void regPagoButton_Click(object sender, RoutedEventArgs e)
        {
            Visitas_agregar visita = new Visitas_agregar();

            visita.Nombre = txtNombre.Text;
            visita.Precio = Convert.ToDecimal(txtPrecio.Text);
          
            if (datePickerPago.SelectedDate.HasValue)
            {
                visita.Fecha = datePickerPago.SelectedDate.Value;
            }
            else
            {
                MessageBox.Show("Por favor, selecciona una fecha válida.");
                return; 
            }


            Visitas_conexion conexion = new Visitas_conexion();
            conexion.InsertarVisita(visita);
            MessageBox.Show("Visita registrada correctamente.");

            CargarVisitas();
        }

        private void CargarVisitas()
        {
            try
            {
                Visitas_conexion conexion = new Visitas_conexion();
                DataTable dt = conexion.ObtenerVisitasDelMesActual();

                if (dt != null)
                {
                    visitasDataGrid.ItemsSource = null;
                    visitasDataGrid.ItemsSource = dt.DefaultView;
                }
                else
                {
                    System.Windows.MessageBox.Show("La tabla devuelta es null.");
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Error al cargar miembros: " + ex.Message);
            }
        }
        private void visitasDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
