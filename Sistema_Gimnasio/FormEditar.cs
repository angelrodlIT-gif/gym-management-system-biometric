using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Gym_System.Core;

namespace Sistema_Gimnasio
{
    public partial class FormEditar : Form
    {
        private long _idUsuario;

        public FormEditar(long idUsuario)
        {
            InitializeComponent();
            _idUsuario = idUsuario;
            CargarDatosUsuario(_idUsuario); // Llama al método con el id
        }



        // Evento para el botón de cerrar
        private void closeButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void ContinuarBut_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NombreText.Text) ||
                string.IsNullOrWhiteSpace(EdadText.Text) ||
                string.IsNullOrWhiteSpace(DireccionText.Text) ||
                string.IsNullOrWhiteSpace(TelefonoText.Text))
            {
                MessageBox.Show("Complete todos los campos.");
                return;
            }

            try
            {
                Miembros_agregar miembro = new Miembros_agregar();
                miembro.id = _idUsuario; 
                miembro.Nombre = NombreText.Text;
                miembro.Edad = int.Parse(EdadText.Text);
                miembro.Direccion = DireccionText.Text;
                miembro.Telefono = TelefonoText.Text;

                Miembro_Conexion conexion = new Miembro_Conexion();
                conexion.ActualizarMiembro(miembro); // Método que actualiza en base al ID

                MessageBox.Show("Datos actualizados.");
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al actualizar: " + ex.Message);
            }
        }


        private void CargarDatosUsuario(long idUsuario)
        {
            try
            {
                Miembro_Conexion conexion = new Miembro_Conexion();
                DataTable dt = conexion.ObtenerUsuarioPorId(idUsuario); // Método adaptado a long

                if (dt.Rows.Count > 0)
                {
                    DataRow row = dt.Rows[0];
                    NombreText.Text = row["nombre"].ToString();
                    EdadText.Text = row["edad"].ToString();
                    DireccionText.Text = row["direccion"].ToString();
                    TelefonoText.Text = row["telefono"].ToString();
                }
                else
                {
                    MessageBox.Show("Usuario no encontrado.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar los datos del usuario: " + ex.Message);
            }
        }



        private void NombreText_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
