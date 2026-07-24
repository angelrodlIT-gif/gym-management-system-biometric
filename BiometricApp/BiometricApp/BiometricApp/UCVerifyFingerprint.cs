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
    public partial class UCVerifyFingerprint : UserControl
    {
        private bool _inicializado = false;
        private const int PROBABILITY_ONE = 0x7fffffff;

        private readonly SqlConnection conn = new SqlConnection(
            ConfigurationManager.ConnectionStrings["GymDbConnection"].ConnectionString);

        public UCVerifyFingerprint()
        {
            InitializeComponent();
        }

        #region EVENTO HACIA WPF

        public class HuellaVerificadaEventArgs : EventArgs
        {
            public long IdMiembro { get; set; }
            public string Nombre { get; set; }
            public string Membresia { get; set; }
            public DateTime FechaFin { get; set; }

            public HuellaVerificadaEventArgs(long id, string nombre, string membresia, DateTime fechaFin)
            {
                IdMiembro = id;
                Nombre = nombre;
                Membresia = membresia;
                FechaFin = fechaFin;
            }
        }

        public event EventHandler<HuellaVerificadaEventArgs> HuellaVerificada;

        #endregion

        #region LOAD / UNLOAD

        private void UCVerifyFingerprint_Load(object sender, EventArgs e)
        {
            if (_inicializado) return;
            _inicializado = true;

            // Suscribirse al manager — nunca al SDK directamente
            FingerprintManager.Instance.HuellaParaVerificar += OnHuellaParaVerificar;
            FingerprintManager.Instance.ErrorLector += OnErrorLector;
            FingerprintManager.Instance.ImagenCapturada += OnImagenCapturada;

            if (!FingerprintManager.Instance.Inicializar())
            {
                lblPlaceFinger.Text = "Lector no disponible.";
                lblPlaceFinger.ForeColor = Color.Red;
                return;
            }

            lblPlaceFinger.Text = "Coloque su dedo en el lector.";
            lblPlaceFinger.ForeColor = Color.Green;

            FingerprintManager.Instance.IniciarVerificacion();
        }

        #endregion

        #region VERIFICACION

        private void OnImagenCapturada(object sender, FingerprintManager.ImagenCapturaEventArgs e)
        {
            if (!IsHandleCreated || IsDisposed) return;

            byte[] rgb = new byte[e.RawImage.Length * 3];
            for (int i = 0; i < e.RawImage.Length; i++)
            {
                rgb[i * 3] = e.RawImage[i];
                rgb[i * 3 + 1] = e.RawImage[i];
                rgb[i * 3 + 2] = e.RawImage[i];
            }

            var bmp = new Bitmap(e.Width, e.Height, PixelFormat.Format24bppRgb);

            var data = bmp.LockBits(
                new Rectangle(0, 0, bmp.Width, bmp.Height),
                ImageLockMode.WriteOnly,
                PixelFormat.Format24bppRgb);

            for (int y = 0; y < bmp.Height; y++)
            {
                IntPtr ptr = data.Scan0 + data.Stride * y;
                System.Runtime.InteropServices.Marshal.Copy(
                    rgb, y * bmp.Width * 3, ptr, bmp.Width * 3);
            }

            bmp.UnlockBits(data);

            if (pbFingerprint.InvokeRequired)
                pbFingerprint.Invoke((MethodInvoker)delegate { pbFingerprint.Image = bmp; });
            else
                pbFingerprint.Image = bmp;
        }

        private void OnHuellaParaVerificar(object sender, Fmd fmd)
        {
            // Viene del hilo del SDK — hacer la DB query aquí está bien
            // (es un hilo separado, no bloquea la UI)
            VerificarEnBaseDeDatos(fmd);
        }

        private void OnErrorLector(object sender, string mensaje)
        {
            if (lblPlaceFinger.InvokeRequired)
                lblPlaceFinger.Invoke((MethodInvoker)delegate
                {
                    lblPlaceFinger.Text = "Error: " + mensaje;
                    lblPlaceFinger.ForeColor = Color.Red;
                });
            else
            {
                lblPlaceFinger.Text = "Error: " + mensaje;
                lblPlaceFinger.ForeColor = Color.Red;
            }
        }

        private void VerificarEnBaseDeDatos(Fmd fingerFmd)
        {
            try
            {
                if (conn.State != ConnectionState.Open)
                    conn.Open();

                SqlDataAdapter adapter = new SqlDataAdapter(
                    "SELECT m.Id, m.Nombre, m.Huella, m.FechaFin, mem.Nombre AS Membresia " +
                    "FROM Miembros m " +
                    "LEFT JOIN Membresias mem ON m.IdMembresia = mem.Id",
                    conn);

                DataTable dt = new DataTable();
                adapter.Fill(dt);
                conn.Close();

                foreach (DataRow row in dt.Rows)
                {
                    if (row["Huella"] == DBNull.Value) continue;

                    string huellaXml = row["Huella"].ToString();
                    Fmd fmdGuardada = Fmd.DeserializeXml(huellaXml);

                    CompareResult compare = Comparison.Compare(fingerFmd, 0, fmdGuardada, 0);

                    if (compare.ResultCode != Constants.ResultCode.DP_SUCCESS) continue;

                    if (compare.Score < PROBABILITY_ONE / 100000)
                    {
                        long id = Convert.ToInt64(row["Id"]);
                        string nombre = row["Nombre"].ToString();
                        string membresia = row["Membresia"]?.ToString() ?? "Sin membresía";
                        DateTime fechaFin = row["FechaFin"] != DBNull.Value
                                            ? Convert.ToDateTime(row["FechaFin"])
                                            : DateTime.MinValue;

                        HuellaVerificada?.Invoke(this,
                            new HuellaVerificadaEventArgs(id, nombre, membresia, fechaFin));

                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error BD verificación: " + ex.Message);
            }
        }

        #endregion

        #region DISPOSE

        public void DetenerLector()
        {
            FingerprintManager.Instance.HuellaParaVerificar -= OnHuellaParaVerificar;
            FingerprintManager.Instance.ErrorLector -= OnErrorLector;
            FingerprintManager.Instance.ImagenCapturada -= OnImagenCapturada;
            FingerprintManager.Instance.Detener();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                FingerprintManager.Instance.HuellaParaVerificar -= OnHuellaParaVerificar;
                FingerprintManager.Instance.ErrorLector -= OnErrorLector;
                FingerprintManager.Instance.ImagenCapturada -= OnImagenCapturada;

                if (conn != null && conn.State == ConnectionState.Open)
                    conn.Close();

                if (components != null)
                    components.Dispose();
            }
            base.Dispose(disposing);
        }

        #endregion
    }
}