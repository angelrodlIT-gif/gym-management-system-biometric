using DPUruNet;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace BiometricApp
{
    public partial class frmDBEnrollment : Form
    {
        // La plantilla final, leída por FormAgregar después del enrolamiento
        public Fmd TemplateHuellaFinal { get; private set; }

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
            FingerprintManager.Instance.ImagenCapturada += OnImagenCapturada;

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
            DesuscribirEventos();
            DrenarBitmapsPendientes();

            // Si se cierra sin completar, detener el modo actual
            if (TemplateHuellaFinal == null)
                FingerprintManager.Instance.Detener();
        }

        private void DesuscribirEventos()
        {
            FingerprintManager.Instance.EnrolamientoProgreso -= OnProgreso;
            FingerprintManager.Instance.EnrolamientoCompleto -= OnCompleto;
            FingerprintManager.Instance.EnrolamientoError -= OnError;
            FingerprintManager.Instance.ErrorLector -= OnErrorLector;
            FingerprintManager.Instance.ImagenCapturada -= OnImagenCapturada;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DesuscribirEventos();
                lock (_bitmapsPendientesLock)
                {
                    DrenarBitmapsPendientes();

                    if (pbFingerprint != null && pbFingerprint.Image != null)
                    {
                        var img = pbFingerprint.Image;
                        pbFingerprint.Image = null;
                        try { img.Dispose(); } catch { }
                    }
                }

                if (components != null)
                    components.Dispose();
            }
            base.Dispose(disposing);
        }

        #endregion

        #region EVENTOS DEL MANAGER

        private void OnProgreso(object sender, int count)
        {
            ActualizarLabel(
                $"Huella {count}/{FingerprintManager.CAPTURAS_REQUERIDAS} capturada. " +
                (count < FingerprintManager.CAPTURAS_REQUERIDAS ? "Continúe..." : "Procesando..."),
                Color.DarkGoldenrod);
        }

        private void OnCompleto(object sender, Fmd template)
        {
            TemplateHuellaFinal = template;

            if (!IsHandleCreated || IsDisposed) return;

            Action accion = () =>
            {
                if (!IsDisposed)
                {
                    ActualizarLabel("¡Huella registrada correctamente!", Color.Green);
                    if (pbFingerprint != null)
                    {
                        lock (_bitmapsPendientesLock)
                        {
                            var img = pbFingerprint.Image;
                            pbFingerprint.Image = null;
                            img?.Dispose();
                        }
                    }
                }
            };

            if (this.InvokeRequired)
                this.BeginInvoke(accion);
            else
                accion();
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

        private void ActualizarLabel(string texto, Color color)
        {
            if (!IsHandleCreated || IsDisposed) return;

            Action accion = () =>
            {
                if (!IsDisposed && lblPlaceFinger != null)
                {
                    lblPlaceFinger.Text = texto;
                    lblPlaceFinger.ForeColor = color;
                }
            };

            if (lblPlaceFinger.InvokeRequired)
                lblPlaceFinger.BeginInvoke(accion);
            else
                accion();
        }

        private readonly object _bitmapsPendientesLock = new object();
        private readonly System.Collections.Generic.HashSet<Bitmap> _bitmapsPendientes = new System.Collections.Generic.HashSet<Bitmap>();

        /// <summary>
        /// Seam para interceptar el encolado de delegados UI en pruebas unitarias deterministas.
        /// </summary>
        public Action<Action> SeamEncolarDelegado { get; set; }

        /// <summary>
        /// Seam para simular/verificar intercalación de concurrencia antes de asignar el bitmap en UI.
        /// </summary>
        public Action SeamAntesDeAsignar { get; set; }

        /// <summary>
        /// Objeto de sincronización para bitmaps pendientes y transición de ownership hacia la UI.
        /// </summary>
        public object BitmapsPendientesLock => _bitmapsPendientesLock;

        /// <summary>
        /// Método de verificación de regresión para asegurar que otro hilo no puede intercalar
        /// DrenarBitmapsPendientes ni adquirir el lock mientras la sección crítica está activa.
        /// </summary>
        public bool ProbarExclusionConcurrenteDrenar()
        {
            lock (_bitmapsPendientesLock)
            {
                bool lockAdquiridoPorOtroHilo = false;
                var t = new System.Threading.Thread(() =>
                {
                    if (System.Threading.Monitor.TryEnter(_bitmapsPendientesLock, 0))
                    {
                        try
                        {
                            lockAdquiridoPorOtroHilo = true;
                        }
                        finally
                        {
                            System.Threading.Monitor.Exit(_bitmapsPendientesLock);
                        }
                    }
                });
                t.Start();
                t.Join(200);
                return !lockAdquiridoPorOtroHilo;
            }
        }

        /// <summary>
        /// Cantidad de bitmaps encolados actualmente pendientes de renderizado por la UI.
        /// </summary>
        public int CantidadBitmapsPendientes
        {
            get
            {
                lock (_bitmapsPendientesLock)
                {
                    return _bitmapsPendientes.Count;
                }
            }
        }

        /// <summary>
        /// Drena y dispone todos los bitmaps pendientes de renderizado de forma thread-safe e idempotente.
        /// Evita fugas de memoria nativa si el formulario se cierra o dispone antes de ejecutar delegados BeginInvoke.
        /// </summary>
        public void DrenarBitmapsPendientes()
        {
            lock (_bitmapsPendientesLock)
            {
                if (_bitmapsPendientes.Count == 0) return;
                var pendientes = new System.Collections.Generic.List<Bitmap>(_bitmapsPendientes);
                _bitmapsPendientes.Clear();

                foreach (var b in pendientes)
                {
                    try { b?.Dispose(); } catch { }
                }
            }
        }

        public void MostrarImagen(Bitmap bmp)
        {
            if (bmp == null) return;
            if ((!IsHandleCreated && SeamEncolarDelegado == null) || IsDisposed)
            {
                try { bmp?.Dispose(); } catch { }
                return;
            }

            lock (_bitmapsPendientesLock)
            {
                if (IsDisposed)
                {
                    try { bmp?.Dispose(); } catch { }
                    return;
                }
                _bitmapsPendientes.Add(bmp);
            }

            bool delegadoEncolado = false;
            try
            {
                Action accion = () =>
                {
                    bool eliminadoDelConjunto = false;
                    Image imagenPreviaADisponer = null;
                    bool debeDisponerBmp = false;

                    try
                    {
                        lock (_bitmapsPendientesLock)
                        {
                            eliminadoDelConjunto = _bitmapsPendientes.Remove(bmp);

                            // Si ya no estaba en el conjunto, fue drenado y dispuesto por Dispose() / DrenarBitmapsPendientes()
                            if (!eliminadoDelConjunto)
                            {
                                return;
                            }

                            SeamAntesDeAsignar?.Invoke();

                            if (IsDisposed || pbFingerprint == null || pbFingerprint.IsDisposed)
                            {
                                // Si el control está disposed, el delegado debe disponer el bitmap dentro de la sección crítica y no asignarlo.
                                try { bmp.Dispose(); } catch { }
                                return;
                            }

                            try
                            {
                                imagenPreviaADisponer = pbFingerprint.Image;
                                pbFingerprint.Image = bmp;
                                if (imagenPreviaADisponer != null)
                                {
                                    try { imagenPreviaADisponer.Dispose(); } catch { }
                                    imagenPreviaADisponer = null;
                                }
                            }
                            catch
                            {
                                debeDisponerBmp = true;
                            }
                        }
                    }
                    catch
                    {
                        debeDisponerBmp = true;
                    }
                    finally
                    {
                        // Asegura remoción del conjunto de pendientes
                        lock (_bitmapsPendientesLock)
                        {
                            _bitmapsPendientes.Remove(bmp);
                        }

                        if (imagenPreviaADisponer != null)
                        {
                            try { imagenPreviaADisponer.Dispose(); } catch { }
                        }

                        if (debeDisponerBmp && eliminadoDelConjunto)
                        {
                            try { bmp?.Dispose(); } catch { }
                        }
                    }
                };

                if (SeamEncolarDelegado != null)
                {
                    SeamEncolarDelegado(accion);
                    delegadoEncolado = true;
                }
                else if (pbFingerprint != null && pbFingerprint.InvokeRequired)
                {
                    pbFingerprint.BeginInvoke(accion);
                    delegadoEncolado = true;
                }
                else
                {
                    accion();
                    delegadoEncolado = true;
                }
            }
            catch (Exception ex)
            {
                if (!delegadoEncolado)
                {
                    lock (_bitmapsPendientesLock)
                    {
                        _bitmapsPendientes.Remove(bmp);
                    }
                    try { bmp?.Dispose(); } catch { }
                }
                System.Diagnostics.Debug.WriteLine("Error al mostrar imagen en frmDBEnrollment: " + ex.Message);
            }
        }

        #endregion

        #region BITMAP

        private void OnImagenCapturada(object sender, FingerprintManager.ImagenCapturaEventArgs e)
        {
            if (!IsHandleCreated || IsDisposed || e == null || e.RawImage == null) return;

            Bitmap bmp = null;
            BitmapData data = null;
            bool entregadoAMostrar = false;

            try
            {
                byte[] rgb = new byte[e.RawImage.Length * 3];
                for (int i = 0; i < e.RawImage.Length; i++)
                {
                    rgb[i * 3] = e.RawImage[i];
                    rgb[i * 3 + 1] = e.RawImage[i];
                    rgb[i * 3 + 2] = e.RawImage[i];
                }

                bmp = new Bitmap(e.Width, e.Height, PixelFormat.Format24bppRgb);

                data = bmp.LockBits(
                    new Rectangle(0, 0, bmp.Width, bmp.Height),
                    ImageLockMode.WriteOnly,
                    PixelFormat.Format24bppRgb);

                try
                {
                    for (int y = 0; y < bmp.Height; y++)
                    {
                        IntPtr ptr = data.Scan0 + data.Stride * y;
                        System.Runtime.InteropServices.Marshal.Copy(rgb, y * bmp.Width * 3, ptr, bmp.Width * 3);
                    }
                }
                finally
                {
                    bmp.UnlockBits(data);
                    data = null;
                }

                if (IsDisposed || !IsHandleCreated)
                {
                    bmp.Dispose();
                    bmp = null;
                    return;
                }

                entregadoAMostrar = true;
                MostrarImagen(bmp);
            }
            catch (Exception ex)
            {
                if (!entregadoAMostrar && bmp != null)
                {
                    try { bmp.Dispose(); } catch { }
                    bmp = null;
                }
                System.Diagnostics.Debug.WriteLine("Error al renderizar imagen de enrolamiento: " + ex.Message);
            }
        }

        #endregion
    }
}