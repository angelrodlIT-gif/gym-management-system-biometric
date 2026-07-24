
using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using DPUruNet;
using Gym_System.Core;
using BiometricApp;
namespace Sistema_Gimnasio
{
    public partial class FormAgregar : Form
    {
        frmDBEnrollment Huella;
        private bool mostrarEnUnaSolaPagina = false;

        public FormAgregar()
        {
            InitializeComponent();

            NombreText.TextChanged += (s, e) => VerificarTextBoxes();
            TelefonoText.TextChanged += (s, e) => VerificarTextBoxes();
            DireccionText.TextChanged += (s, e) => VerificarTextBoxes();
            EdadText.TextChanged += (s, e) => VerificarTextBoxes();

            AceptarBut.Enabled = false;
            ContinuarBut.Enabled = false;


        }

        private void VerificarTextBoxes()
        {
            ContinuarBut.Enabled =
                !string.IsNullOrWhiteSpace(NombreText.Text) &&
                !string.IsNullOrWhiteSpace(TelefonoText.Text) &&
                !string.IsNullOrWhiteSpace(DireccionText.Text) &&
                !string.IsNullOrWhiteSpace(EdadText.Text);
            AceptarBut.Enabled =
                !string.IsNullOrWhiteSpace(NombreText.Text) &&
                !string.IsNullOrWhiteSpace(TelefonoText.Text) &&
                !string.IsNullOrWhiteSpace(DireccionText.Text) &&
                !string.IsNullOrWhiteSpace(EdadText.Text);
        }

        public void ToggleWindowState()
        {
            AceptarBut.Visible = false;
        }
        private void button2_Click(object sender, EventArgs e)
        {
            if (mostrarEnUnaSolaPagina)
                return;

            panelDatosper.Size = new Size(1214, 407);
            panelDatosper.Location = new Point(16, 63);
            panelDatosper.Visible = true;
            panelDatosper.SendToBack();           

            panelBiometrico.Size = new Size(1214, 56);
            panelBiometrico.Location = new Point(16, 490);
            panelBiometrico.Visible = true;
            panelBiometrico.BringToFront(); 


            
        }

        private void AceptarBut_Click(object sender, EventArgs e)
        {
            Miembros_agregar persona = new Miembros_agregar();

            persona.Nombre = NombreText.Text;
            persona.Telefono = TelefonoText.Text;
            persona.Direccion = DireccionText.Text;
            persona.Edad = Convert.ToInt32(EdadText.Text);
            persona.Estado = "Sin Membresía";
            persona.FechaRegistro = DateTime.Now;
            persona.FechaInicio = null;
            persona.FechaFin = null;
            persona.IdMembresia = null;
            if (Huella != null && Huella.TemplateHuellaFinal != null)
            {
                persona.TemplateHuella = Fmd.SerializeXml(Huella.TemplateHuellaFinal);
            }

            try
            {
                Miembro_Conexion conexion = new Miembro_Conexion();
                conexion.InsertarMiembro(persona);
                MessageBox.Show("Miembro agregado correctamente.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al guardar el miembro: " + ex.Message);
            }
        }

        private void ContinuarBut_Click(object sender, EventArgs e)
        {
            if (mostrarEnUnaSolaPagina)
                return;

            panelDatosper.Size = new Size(1214, 56);
            panelDatosper.Location = new Point(16, 63);
            panelDatosper.Visible = true;
            panelDatosper.SendToBack();

            panelBiometrico.Size = new Size(1214, 408);
            panelBiometrico.Location = new Point(16, 124);
            panelBiometrico.Visible = true;
            panelBiometrico.BringToFront();

            if (Huella == null || Huella.IsDisposed)
            {
                Huella = new frmDBEnrollment();
                AbrirFormularioEnPanel(Huella);
            }
            else
            {
                
                AbrirFormularioEnPanel(Huella);
                Huella.ReiniciarEnrolamiento();
            }
        }

        private void AbrirFormularioEnPanel(Form formulario)
        {
            panelContenedor.Controls.Clear(); 
            formulario.TopLevel = false; 
            formulario.Dock = DockStyle.Fill;
            panelContenedor.Controls.Add(formulario);
            panelContenedor.Tag = formulario;
            formulario.Show();
        }
        private void NombreText_TextChanged(object sender, EventArgs e)
        {

        }


        private void FormAgregar_Load_1(object sender, EventArgs e)
        {
            
        }

        private void panelDatosper_Paint(object sender, PaintEventArgs e)
        {
            
        }

        private void closeButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void NombreText_TextChanged_1(object sender, EventArgs e)
        {

        }
    }

}
