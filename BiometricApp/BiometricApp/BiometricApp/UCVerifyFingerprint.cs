using DPUruNet;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BiometricApp
{
    public partial class UCVerifyFingerprint : UserControl
    {
        private bool _inicializado = false;

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

            // Precargar la caché biométrica en segundo plano para respuesta instantánea al escanear
            Task.Run(() =>
            {
                try
                {
                    BiometricCache.CargarPlantillas();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error al precargar caché biométrica: " + ex.Message);
                }
            });

            if (!FingerprintManager.Instance.Inicializar())
            {
                if (!IsDisposed && IsHandleCreated)
                {
                    lblPlaceFinger.Text = "Lector no disponible.";
                    lblPlaceFinger.ForeColor = Color.Red;
                }
                return;
            }

            if (!IsDisposed && IsHandleCreated)
            {
                lblPlaceFinger.Text = "Coloque su dedo en el lector.";
                lblPlaceFinger.ForeColor = Color.Green;
            }

            FingerprintManager.Instance.IniciarVerificacion();
        }

        #endregion

        #region VERIFICACION

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
        /// Evita fugas de memoria nativa si el control se cierra o dispone antes de ejecutar delegados BeginInvoke.
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
                Action asignarBmp = () =>
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
                    SeamEncolarDelegado(asignarBmp);
                    delegadoEncolado = true;
                }
                else if (pbFingerprint != null && pbFingerprint.InvokeRequired)
                {
                    pbFingerprint.BeginInvoke(asignarBmp);
                    delegadoEncolado = true;
                }
                else
                {
                    asignarBmp();
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
                System.Diagnostics.Debug.WriteLine("Error al mostrar imagen en UCVerifyFingerprint: " + ex.Message);
            }
        }

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
                        System.Runtime.InteropServices.Marshal.Copy(
                            rgb, y * bmp.Width * 3, ptr, bmp.Width * 3);
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
                System.Diagnostics.Debug.WriteLine("Error al renderizar imagen de huella: " + ex.Message);
            }
        }

        private void OnHuellaParaVerificar(object sender, Fmd fmd)
        {
            if (!IsHandleCreated || IsDisposed) return;
            VerificarEnBaseDeDatos(fmd);
        }

        private void OnErrorLector(object sender, string mensaje)
        {
            if (!IsHandleCreated || IsDisposed) return;

            Action actualizarTexto = () =>
            {
                if (!IsDisposed && lblPlaceFinger != null)
                {
                    lblPlaceFinger.Text = "Error: " + mensaje;
                    lblPlaceFinger.ForeColor = Color.Red;
                }
            };

            if (lblPlaceFinger.InvokeRequired)
                lblPlaceFinger.BeginInvoke(actualizarTexto);
            else
                actualizarTexto();
        }

        private void VerificarEnBaseDeDatos(Fmd fingerFmd)
        {
            if (IsDisposed || fingerFmd == null) return;

            try
            {
                // Identificación 1:N optimizada utilizando la caché en memoria
                var match = BiometricCache.Identificar(fingerFmd);

                if (match != null && match.Exitoso)
                {
                    if (!IsDisposed)
                    {
                        HuellaVerificada?.Invoke(this,
                            new HuellaVerificadaEventArgs(match.IdMiembro, match.Nombre, match.Membresia, match.FechaFin));
                    }
                }
                else if (match != null && !match.Exitoso && !string.IsNullOrEmpty(match.MensajeError))
                {
                    OnErrorLector(this, match.MensajeError);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error durante la verificación biométrica: " + ex.Message);
            }
        }

        #endregion

        #region DISPOSE

        public void DetenerLector()
        {
            _inicializado = false;
            FingerprintManager.Instance.HuellaParaVerificar -= OnHuellaParaVerificar;
            FingerprintManager.Instance.ErrorLector -= OnErrorLector;
            FingerprintManager.Instance.ImagenCapturada -= OnImagenCapturada;
            FingerprintManager.Instance.Detener();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DetenerLector();
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
    }
}