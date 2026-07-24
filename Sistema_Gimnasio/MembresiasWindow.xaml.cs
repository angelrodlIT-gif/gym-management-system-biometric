using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
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
using Gym_System.Data;

namespace Sistema_Gimnasio.Views
{
  
    public partial class MembresiasView : UserControl
    {
        public MembresiasView()
        {
            InitializeComponent();
            CargarMembresia();

        }
        private void trashButton_Click(object sender, RoutedEventArgs e)
        {
            if (membershipsDataGrid.SelectedItem is DataRowView selectedRow)
            {
                if (int.TryParse(selectedRow.Row["id"].ToString(), out int idMembresia))
                {
                    // Confirmación
                    MessageBoxResult result = System.Windows.MessageBox.Show(
                        "¿Estás seguro que deseas eliminar esta membresía?",
                        "Confirmación",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning
                    );

                    if (result == MessageBoxResult.Yes)
                    {
                        try
                        {
                            Membresias_conexion conexion = new Membresias_conexion();
                            bool eliminado = conexion.EliminarMembresia(idMembresia);

                            if (eliminado)
                            {
                                System.Windows.MessageBox.Show(
                                    "Membresía eliminada correctamente.",
                                    "Éxito",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Information
                                );
                                CargarMembresia(); // Recargar tabla
                            }
                            else
                            {
                                System.Windows.MessageBox.Show(
                                    "No se pudo eliminar la membresía.",
                                    "Error",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Error
                                );
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Windows.MessageBox.Show(
                                $"Error al eliminar la membresía: {ex.Message}",
                                "Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error
                            );
                        }
                    }
                }
                else
                {
                    System.Windows.MessageBox.Show(
                        "ID de membresía no válido.",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                }
            }
            else
            {
                System.Windows.MessageBox.Show(
                    "Selecciona una membresía primero.",
                    "Advertencia",
                    MessageBoxButton.OK,
                    MessageBoxImage.Exclamation
                );
            }
        }



        private void membersDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
        }

        private void editButton_Click(object sender, RoutedEventArgs e)
        {
        }


        //Boton de agregar membresia
        private void addButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Membresias_agregar membresia = new Membresias_agregar();

                membresia.Nombre = txtNombre.Text;

                if (!int.TryParse(txtDuracion.Text, out int duracion))
                {
                    MessageBox.Show("La duración debe ser un número.");
                    return;
                }
                membresia.Duracion = duracion;

                if (!decimal.TryParse(txtPrecio.Text, out decimal precio))
                {
                    MessageBox.Show("El precio debe ser un número válido.");
                    return;
                }
                membresia.Precio = precio;

                // Obtener la unidad de duración desde el ComboBox
                membresia.UnidadDuracion = ((ComboBoxItem)cbUnidadTiempo.SelectedItem).Content.ToString();

                Membresias_conexion conexion = new Membresias_conexion();
                conexion.InsertarMembresia(membresia); 

                MessageBox.Show("Membresía agregada correctamente.");
                CargarMembresia(); // Si deseas recargar la tabla
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al agregar la membresía: " + ex.Message);
            }
        }

        private void CargarMembresia()
        {
            try
            {
                Membresias_conexion conexion = new Membresias_conexion();
                DataTable dt = conexion.ObtenerMembresia();

                if (dt != null)
                {
                    membershipsDataGrid.ItemsSource = null;
                    membershipsDataGrid.ItemsSource = dt.DefaultView;
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

        private void membershipsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }
    }
}
