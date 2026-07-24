using DPUruNet;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.Configuration;

namespace BiometricApp
{
    public partial class frmDBEnrollment : Form
    {
        // La plantilla final, leída por FormAgregar después del enrolamiento
        public Fmd TemplateHuellaFinal { get; private set; }

        // Conexión SQL (mantenida por compatibilidad con Dispose del Designer)
        private readonly SqlConnection conn = new SqlConnection(
            ConfigurationManager.ConnectionStrings["GymDbConnection"].ConnectionString);

        public frmDBEnrollment()
        {
            InitializeComponent();
        }

        #region LOAD / UNLOAD

        private void frmDBEnrollment_Load(object sender, EventArgs e)
        {
            // Suscribirse a los eventos del manager — NUNCA al SDK directamente
            FingerprintManager.Instance.EnrolamientoProgreso += OnProgreso;
            FingerprintManager.Instance.EnrolamientoCompleto += OnCompleto;
            FingerprintManager.Instance.EnrolamientoError += OnError;
            FingerprintManager.Instance.ErrorLector += OnErrorLector;
            FingerprintManager.Instance.ImagenCapturada +=  OnImagenCapturada;

            // Inicializar el lector si todavía no está abierto
            if (!FingerprintManager.Instance.Inicializar())
            {
                ActualizarLabel("No se pudo inicializar el lector.", Color.Red);
                return;
            }

            ActualizarLabel("Coloque su dedo (1/" +
                            FingerprintManager.CAPTURAS_REQUERIDAS + ")...", Color.DarkGoldenrod);

            FingerprintManager.Instance.IniciarEnrolamiento();
        }

        private void frmDBEnrollment_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Desuscribirse — no detener el lector (el manager lo gestiona)
            FingerprintManager.Instance.EnrolamientoProgreso -= OnProgreso;
            FingerprintManager.Instance.EnrolamientoCompleto -= OnCompleto;
            FingerprintManager.Instance.EnrolamientoError -= OnError;
            FingerprintManager.Instance.ErrorLector -= OnErrorLector;
            FingerprintManager.Instance.ImagenCapturada -= OnImagenCapturada;

            // Si se cierra sin completar, detener el modo actual
            if (TemplateHuellaFinal == null)
                FingerprintManager.Instance.Detener();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (conn != null && conn.State == ConnectionState.Open)
                    conn.Close();

                if (components != null)
                    components.Dispose();
            }
            base.Dispose(disposing);
        }

        #endregion

        #region EVENTOS DEL MANAGER

        private void OnProgreso(object sender, int count)
        {
            // Vienen del hilo del SDK — actualizar UI con Invoke
            ActualizarLabel(
                $"Huella {count}/{FingerprintManager.CAPTURAS_REQUERIDAS} capturada. " +
                (count < FingerprintManager.CAPTURAS_REQUERIDAS ? "Continúe..." : "Procesando..."),
                Color.DarkGoldenrod);
        }

        private void OnCompleto(object sender, Fmd template)
        {
            TemplateHuellaFinal = template;

            if (!IsHandleCreated || IsDisposed) return;
            // Actualizar UI desde hilo del SDK — usar Invoke
            this.Invoke((MethodInvoker)delegate
            {
                ActualizarLabel("¡Huella registrada correctamente!", Color.Green);
                pbFingerprint.Image = null;
                // No llamar this.Close() aquí — FormAgregar decide cuándo cerrar
            });
        }

        public void ReiniciarEnrolamiento()
        {
            TemplateHuellaFinal = null;
            ActualizarLabel($"Coloque su dedo (1/{FingerprintManager.CAPTURAS_REQUERIDAS})...", Color.DarkGoldenrod);
            FingerprintManager.Instance.IniciarEnrolamiento();
        }

        private void OnError(object sender, string mensaje)
        {
            ActualizarLabel("Error: " + mensaje + " — Intente de nuevo.", Color.Red);
        }

        private void OnErrorLector(object sender, string mensaje)
        {
            ActualizarLabel("Error del lector: " + mensaje, Color.Red);
        }

        #endregion

        #region UI HELPERS

        // ActualizarLabel — agregar la primera línea del método:
        private void ActualizarLabel(string texto, Color color)
        {
            if (!IsHandleCreated || IsDisposed) return;  // ← AGREGAR

            if (lblPlaceFinger.InvokeRequired)
            {
                lblPlaceFinger.Invoke((MethodInvoker)delegate
                {
                    lblPlaceFinger.Text = texto;
                    lblPlaceFinger.ForeColor = color;
                });
            }
            else
            {
                lblPlaceFinger.Text = texto;
                lblPlaceFinger.ForeColor = color;
            }
        }

        // Método público para que FormAgregar muestre la imagen capturada si lo desea
        public void MostrarImagen(Bitmap bmp)
        {
            if (pbFingerprint.InvokeRequired)
                pbFingerprint.Invoke((MethodInvoker)delegate { pbFingerprint.Image = bmp; });
            else
                pbFingerprint.Image = bmp;
        }

        #endregion

        #region BITMAP
        private void OnImagenCapturada(object sender, FingerprintManager.ImagenCapturaEventArgs e)
        {
            if (!IsHandleCreated || IsDisposed) return;
            // Construir el bitmap desde los bytes crudos (escala de grises → RGB)
            byte[] rgb = new byte[e.RawImage.Length * 3];
            for (int i = 0; i < e.RawImage.Length; i++)
            {
                rgb[i * 3] = e.RawImage[i];
                rgb[i * 3 + 1] = e.RawImage[i];
                rgb[i * 3 + 2] = e.RawImage[i];
            }

            var bmp = new System.Drawing.Bitmap(e.Width, e.Height,
                          System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            var data = bmp.LockBits(
                new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height),
                System.Drawing.Imaging.ImageLockMode.WriteOnly,
                System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            for (int y = 0; y < bmp.Height; y++)
            {
                IntPtr ptr = data.Scan0 + data.Stride * y;
                System.Runtime.InteropServices.Marshal.Copy(rgb, y * bmp.Width * 3, ptr, bmp.Width * 3);
            }

            bmp.UnlockBits(data);

            // Mostrar en el PictureBox — viene del hilo del SDK, usar Invoke
            if (pbFingerprint.InvokeRequired)
                pbFingerprint.Invoke((MethodInvoker)delegate { pbFingerprint.Image = bmp; });
            else
                pbFingerprint.Image = bmp;
        }
    }
    #endregion

}