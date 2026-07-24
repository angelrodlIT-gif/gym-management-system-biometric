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
  
    public partial class MiembrosView : UserControl
    {
        public MiembrosView()
        {
            InitializeComponent();
            CargarMiembros();

        }
        private void CargarMiembros()
        {
            try
            {
                Miembro_Conexion conexion = new Miembro_Conexion();
                DataTable dt = conexion.ObtenerMiembros();

                if (dt != null)
                {
                    membersDataGrid.ItemsSource = null;
                    membersDataGrid.ItemsSource = dt.DefaultView;
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






        // Lógica para agregar miembro 
        private void addButton_Click(object sender, RoutedEventArgs e)
        {
            FormAgregar formAgregar = new FormAgregar();

            formAgregar.ShowDialog();
        }

         private void membersDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
           
        }

        // Lógica para buscar un miembro con el buscador
        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            string filtro = txtSearch.Text.Trim().ToLower().Replace("'", "''");

            if (string.IsNullOrEmpty(filtro))
            {
                CargarMiembros();
                return;
            }

            try
            {
                Miembro_Conexion conexion = new Miembro_Conexion();
                DataTable dt = conexion.ObtenerMiembros();

                if (dt != null)
                {
                    DataView dv = dt.DefaultView;

                    dv.RowFilter = $"nombre LIKE '%{filtro}%'"; 

                    membersDataGrid.ItemsSource = null;
                    membersDataGrid.ItemsSource = dv; 
                }
                else
                {
                    System.Windows.MessageBox.Show("No se pudo obtener la lista de miembros.");
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Error al filtrar miembros: " + ex.Message);
            }
        }
        // Lógica para editar un miembro
        private void editButton_Click(object sender, RoutedEventArgs e)
        {
            if (membersDataGrid.SelectedItem is DataRowView selectedRow)
            {
                long idUsuario = (long)selectedRow.Row["id"]; 
                FormEditar formEditar = new FormEditar(idUsuario); 
                formEditar.ShowDialog();
            }
            else
            {
                System.Windows.MessageBox.Show("Seleccione un miembro para editar.");
            }
        }
        // Lógica para borrar un miembro
        private void trashButton_Click(object sender, RoutedEventArgs e)
        {
            if (membersDataGrid.SelectedItem is DataRowView selectedRow)
            {
                long idUsuario = (long)selectedRow.Row["id"];

                // Mostrar confirmación
                MessageBoxResult result = System.Windows.MessageBox.Show(
                    "¿Estás seguro que deseas eliminar este usuario?",
                    "Confirmación",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning
                );

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        Miembro_Conexion conexion = new Miembro_Conexion();
                        bool eliminado = conexion.EliminarUsuario(idUsuario);

                        if (eliminado)
                        {
                            System.Windows.MessageBox.Show("Usuario eliminado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                            CargarMiembros(); // recarga la tabla
                        }
                        else
                        {
                            System.Windows.MessageBox.Show("No se pudo eliminar el usuario.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show("Error al eliminar el usuario: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                System.Windows.MessageBox.Show("Selecciona un usuario primero.", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }


        private void payButton_Click(object sender, RoutedEventArgs e)
        {
            if (membersDataGrid.SelectedItem is DataRowView selectedRow)
            {
                long idUsuario = (long)selectedRow.Row["id"];

                // Abrir la nueva ventana WPF como ventana emergente 
                PagoButWindow pagoWindow = new PagoButWindow(idUsuario);
                pagoWindow.ShowDialog();

                // Refrescar los datos después del pago
                CargarMiembros();

                // Actualizar los estados de los miembros después de registrar el pag
                ActualizarEstados_Automatico();
            }
            else
            {
                System.Windows.MessageBox.Show("Seleccione un miembro para registrar el pago.");
            }
        }


        private void ActualizarEstados_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Miembro_Conexion conexion = new Miembro_Conexion();
                List<Miembros_agregar> miembros = conexion.ObtenerTodosLosMiembros();

                foreach (var miembro in miembros)
                {
                    if (miembro.FechaInicio.HasValue && miembro.FechaFin.HasValue)
                    {
                        if (miembro.FechaInicio.Value <= DateTime.Now && miembro.FechaFin.Value >= DateTime.Now)
                        {
                            conexion.ActualizarEstadoMiembro(miembro.id, "Activo");
                        }
                        else
                        {
                            conexion.ActualizarEstadoMiembro(miembro.id, "Inactivo");
                        }
                    }
                }

                System.Windows.MessageBox.Show("Estados actualizados correctamente.");
                CargarMiembros(); // Método que refresca el DataGrid
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Error al actualizar estados: " + ex.Message);
            }
        }

        private void ActualizarEstados_Automatico()
        {
            try
            {
                Miembro_Conexion conexion = new Miembro_Conexion();
                List<Miembros_agregar> miembros = conexion.ObtenerTodosLosMiembros();

                foreach (var miembro in miembros)
                {
                    if (miembro.FechaInicio.HasValue && miembro.FechaFin.HasValue)
                    {
                        if (miembro.FechaInicio.Value <= DateTime.Now && miembro.FechaFin.Value >= DateTime.Now)
                        {
                            conexion.ActualizarEstadoMiembro(miembro.id, "Activo");
                        }
                        else
                        {
                            conexion.ActualizarEstadoMiembro(miembro.id, "Inactivo");
                        }
                    }
                }

                System.Windows.MessageBox.Show("Estados actualizados correctamente.");
                CargarMiembros(); // Método que refresca el DataGrid
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Error al actualizar estados: " + ex.Message);
            }
        }



    }
}
