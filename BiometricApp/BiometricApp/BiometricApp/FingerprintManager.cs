using DPUruNet;
using System;
using System.Collections.Generic;
using System.Threading;

namespace BiometricApp
{
    /// <summary>
    /// Administrador singleton del lector biométrico.
    /// Es la ÚNICA clase que habla directamente con el hardware.
    /// Los formularios solo se suscriben a sus eventos.
    /// </summary>
    public class FingerprintManager : IDisposable
    {
        #region SINGLETON

        private static FingerprintManager _instance;
        private static readonly object _lockInstance = new object();

        public static FingerprintManager Instance
        {
            get
            {
                lock (_lockInstance)
                {
                    if (_instance == null)
                        _instance = new FingerprintManager();
                    return _instance;
                }
            }
        }

        private FingerprintManager() { }

        #endregion

        #region ESTADO INTERNO

        private Reader _reader;
        private bool _isOpen = false;

        // Bandera para evitar llamadas simultáneas a CaptureAsync
        private int _capturandoFlag = 0; // 0 = libre, 1 = capturando (Interlocked)

        public enum Modo { Ninguno, Verificacion, Enrolamiento }
        private volatile Modo _modoActual = Modo.Ninguno;

        // Estado de enrolamiento
        private readonly List<Fmd> _enrollFmds = new List<Fmd>();
        private int _enrollCount = 0;
        public const int CAPTURAS_REQUERIDAS = 4;

        public bool IsOpen => _isOpen;
        public Modo ModoActual => _modoActual;
        public bool EstaCapturando => _capturandoFlag == 1;

        #endregion

        #region EVENTOS

        /// <summary>Progreso de enrolamiento: cuántas capturas van (1–4).</summary>
        public event EventHandler<int> EnrolamientoProgreso;

        /// <summary>Enrolamiento completado con éxito. Contiene la plantilla FMD final.</summary>
        public event EventHandler<Fmd> EnrolamientoCompleto;

        /// <summary>Error recuperable durante el enrolamiento (se reinicia automáticamente).</summary>
        public event EventHandler<string> EnrolamientoError;

        /// <summary>
        /// Una huella fue capturada en modo Verificacion.
        /// El suscriptor (UCVerifyFingerprint) hace la comparación contra la caché.
        /// </summary>
        public event EventHandler<Fmd> HuellaParaVerificar;

        /// <summary>Error general del hardware o del SDK.</summary>
        public event EventHandler<string> ErrorLector;

        /// <summary>
        /// Imagen cruda capturada (bytes, ancho, alto).
        /// Se dispara en ambos modos para que el formulario muestre la vista previa.
        /// </summary>
        public event EventHandler<ImagenCapturaEventArgs> ImagenCapturada;

        public class ImagenCapturaEventArgs : EventArgs
        {
            public byte[] RawImage { get; }
            public int Width { get; }
            public int Height { get; }

            public ImagenCapturaEventArgs(byte[] raw, int width, int height)
            {
                RawImage = raw;
                Width = width;
                Height = height;
            }
        }

        #endregion

        #region INICIALIZAR / CERRAR

        /// <summary>
        /// Abre el lector físico. Llamar una sola vez al inicio de la app o al reintentar conexión.
        /// Devuelve true si el lector quedó listo, false si no hay lector o falló la apertura.
        /// </summary>
        public bool Inicializar()
        {
            if (_isOpen) return true;

            try
            {
                var readers = ReaderCollection.GetReaders();

                if (readers == null || readers.Count == 0)
                {
                    _isOpen = false;
                    NotificarErrorLector("No se encontró lector de huellas conectado.");
                    return false;
                }

                _reader = readers[0];

                var result = _reader.Open(Constants.CapturePriority.DP_PRIORITY_COOPERATIVE);

                if (result != Constants.ResultCode.DP_SUCCESS)
                {
                    try { _reader.Dispose(); } catch { }
                    _reader = null;
                    _isOpen = false;
                    NotificarErrorLector("Error al abrir lector: " + result);
                    return false;
                }

                _reader.On_Captured += OnCaptured;
                _isOpen = true;
                return true;
            }
            catch (Exception ex)
            {
                try { _reader?.Dispose(); } catch { }
                _reader = null;
                _isOpen = false;
                NotificarErrorLector("Excepción al inicializar lector: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Libera el lector físico completamente, cancela capturas activas y desuscribe eventos.
        /// </summary>
        public void Dispose()
        {
            _modoActual = Modo.Ninguno;

            try { _reader?.CancelCapture(); } catch { }

            if (_reader != null)
            {
                // Espera mínima para permitir que termine cualquier callback en vuelo
                Thread.Sleep(150);

                try
                {
                    _reader.On_Captured -= OnCaptured;
                    _reader.Dispose();
                }
                catch { }
                _reader = null;
            }

            _isOpen = false;
            Interlocked.Exchange(ref _capturandoFlag, 0);

            // Desuscribir todos los manejadores para prevenir fugas de memoria
            HuellaParaVerificar = null;
            ErrorLector = null;
            ImagenCapturada = null;
            EnrolamientoProgreso = null;
            EnrolamientoCompleto = null;
            EnrolamientoError = null;

            lock (_lockInstance)
            {
                _instance = null;
            }
        }

        #endregion

        #region MODOS

        /// <summary>
        /// Cambia a modo Verificación y arranca la captura continua.
        /// </summary>
        public void IniciarVerificacion()
        {
            if (!_isOpen) return;
            _modoActual = Modo.Verificacion;
            IniciarCaptura();
        }

        /// <summary>
        /// Cambia a modo Enrolamiento y reinicia el conteo.
        /// Requiere 4 capturas para generar la plantilla final.
        /// </summary>
        public void IniciarEnrolamiento()
        {
            if (!_isOpen) return;

            lock (_enrollFmds)
            {
                _enrollFmds.Clear();
                _enrollCount = 0;
            }

            _modoActual = Modo.Enrolamiento;
            IniciarCaptura();
        }

        /// <summary>
        /// Detiene la captura sin destruir el lector.
        /// El lector queda listo para reanudar operaciones.
        /// </summary>
        public void Detener()
        {
            _modoActual = Modo.Ninguno;
            try { _reader?.CancelCapture(); } catch { }
            Interlocked.Exchange(ref _capturandoFlag, 0);
        }

        #endregion

        #region CAPTURA INTERNA

        public static Action SeamAlIniciarCaptura { get; set; }
        private static long _totalIniciosCaptura = 0;
        public static long TotalIniciosCaptura => Interlocked.Read(ref _totalIniciosCaptura);

        private void IniciarCaptura()
        {
            Interlocked.Increment(ref _totalIniciosCaptura);
            try
            {
                SeamAlIniciarCaptura?.Invoke();
            }
            catch { }

            if (!_isOpen || _reader == null || _modoActual == Modo.Ninguno) return;

            // Evitar doble llamada simultánea con Interlocked (seguro entre hilos)
            if (Interlocked.CompareExchange(ref _capturandoFlag, 1, 0) != 0) return;

            try
            {
                if (_reader.Capabilities == null ||
                    _reader.Capabilities.Resolutions == null ||
                    _reader.Capabilities.Resolutions.Length == 0)
                {
                    Interlocked.Exchange(ref _capturandoFlag, 0);
                    _isOpen = false;
                    _modoActual = Modo.Ninguno;
                    try { _reader?.Dispose(); } catch { }
                    _reader = null;
                    NotificarErrorLector("El lector biométrico no responde o fue desconectado.");
                    return;
                }

                var result = _reader.CaptureAsync(
                    Constants.Formats.Fid.ANSI,
                    Constants.CaptureProcessing.DP_IMG_PROC_DEFAULT,
                    _reader.Capabilities.Resolutions[0]);

                if (result != Constants.ResultCode.DP_SUCCESS)
                {
                    Interlocked.Exchange(ref _capturandoFlag, 0);

                    if (result == Constants.ResultCode.DP_DEVICE_FAILURE ||
                        result == Constants.ResultCode.DP_INVALID_DEVICE)
                    {
                        _isOpen = false;
                        _modoActual = Modo.Ninguno;
                        try { _reader?.Dispose(); } catch { }
                        _reader = null;
                        NotificarErrorLector("El dispositivo biométrico fue desconectado.");
                    }
                    else if (result != Constants.ResultCode.DP_DEVICE_BUSY)
                    {
                        NotificarErrorLector("Error al iniciar captura: " + result);
                    }
                }
            }
            catch (Exception ex)
            {
                Interlocked.Exchange(ref _capturandoFlag, 0);
                NotificarErrorLector("Excepción en captura: " + ex.Message);
            }
        }

        private void OnCaptured(CaptureResult captureResult)
        {
            try
            {
                // Liberar la bandera — ya concluyó el intento de captura
                Interlocked.Exchange(ref _capturandoFlag, 0);

                // Si se detuvo el modo mientras esperábamos, ignorar
                if (_modoActual == Modo.Ninguno) return;

                if (captureResult == null)
                {
                    IniciarCaptura();
                    return;
                }

                // Manejo seguro ante desconexión física del hardware en caliente
                if (captureResult.ResultCode == Constants.ResultCode.DP_DEVICE_FAILURE ||
                    captureResult.ResultCode == Constants.ResultCode.DP_INVALID_DEVICE)
                {
                    _isOpen = false;
                    _modoActual = Modo.Ninguno;
                    try { _reader?.Dispose(); } catch { }
                    _reader = null;
                    NotificarErrorLector("El lector biométrico fue desconectado o presentó una falla de hardware.");
                    return;
                }

                // Captura vacía o incompleta — reintentar captura silenciosamente
                if (captureResult.Data == null ||
                    captureResult.ResultCode != Constants.ResultCode.DP_SUCCESS)
                {
                    IniciarCaptura();
                    return;
                }

                // Notificar imagen capturada si existen vistas válidas
                if (captureResult.Data.Views != null)
                {
                    foreach (Fid.Fiv fiv in captureResult.Data.Views)
                    {
                        if (fiv != null && fiv.RawImage != null)
                        {
                            ImagenCapturada?.Invoke(this,
                                new ImagenCapturaEventArgs(fiv.RawImage, fiv.Width, fiv.Height));
                            break;
                        }
                    }
                }

                // Extraer características minucias (FMD)
                var conversionResult = FeatureExtraction.CreateFmdFromFid(
                    captureResult.Data,
                    Constants.Formats.Fmd.ANSI);

                if (conversionResult.ResultCode != Constants.ResultCode.DP_SUCCESS || conversionResult.Data == null)
                {
                    IniciarCaptura();
                    return;
                }

                Fmd fmd = conversionResult.Data;

                if (_modoActual == Modo.Verificacion)
                {
                    ProcesarVerificacion(fmd);
                }
                else if (_modoActual == Modo.Enrolamiento)
                {
                    ProcesarEnrolamiento(fmd);
                }
            }
            catch (Exception ex)
            {
                NotificarErrorLector("Excepción al procesar captura biométrica: " + ex.Message);
            }
        }

        /// <summary>
        /// Invoca defensivamente a cada suscriptor de HuellaParaVerificar de forma aislada.
        /// Si un suscriptor lanza una excepción, se notifica ErrorLector y se continúa con los demás.
        /// </summary>
        private void NotificarHuellaParaVerificar(Fmd fmd)
        {
            var handlers = HuellaParaVerificar;
            if (handlers == null) return;

            foreach (EventHandler<Fmd> handler in handlers.GetInvocationList())
            {
                try
                {
                    handler?.Invoke(this, fmd);
                }
                catch (Exception ex)
                {
                    NotificarErrorLector("Excepción en suscriptor de verificación: " + ex.Message);
                }
            }
        }

        /// <summary>
        /// Invoca defensivamente a cada suscriptor de ErrorLector de forma aislada.
        /// Si un suscriptor lanza una excepción, se aísla y se continúa con los demás.
        /// </summary>
        public void NotificarErrorLector(string mensaje)
        {
            var handlers = ErrorLector;
            if (handlers == null) return;

            foreach (EventHandler<string> handler in handlers.GetInvocationList())
            {
                try
                {
                    handler?.Invoke(this, mensaje);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Excepción en suscriptor de ErrorLector: " + ex.Message);
                }
            }
        }

        internal void ProcesarVerificacion(Fmd fmd)
        {
            try
            {
                NotificarHuellaParaVerificar(fmd);
            }
            catch (Exception ex)
            {
                try
                {
                    NotificarErrorLector("Excepción en verificación: " + ex.Message);
                }
                catch { }
            }
            finally
            {
                // Garantizar la reanudación de captura para la siguiente verificación
                IniciarCaptura();
            }
        }

        private void ProcesarEnrolamiento(Fmd fmd)
        {
            int conteoActual;

            lock (_enrollFmds)
            {
                _enrollFmds.Add(fmd);
                _enrollCount++;
                conteoActual = _enrollCount;
            }

            EnrolamientoProgreso?.Invoke(this, conteoActual);

            if (conteoActual >= CAPTURAS_REQUERIDAS)
            {
                DataResult<Fmd> resultEnrollment;

                lock (_enrollFmds)
                {
                    resultEnrollment = Enrollment.CreateEnrollmentFmd(
                        Constants.Formats.Fmd.ANSI,
                        _enrollFmds);
                }

                if (resultEnrollment.ResultCode == Constants.ResultCode.DP_SUCCESS)
                {
                    _modoActual = Modo.Ninguno;
                    EnrolamientoCompleto?.Invoke(this, resultEnrollment.Data);
                }
                else
                {
                    lock (_enrollFmds)
                    {
                        _enrollFmds.Clear();
                        _enrollCount = 0;
                    }

                    EnrolamientoError?.Invoke(this, "No se pudo procesar la huella. Intente de nuevo.");
                    IniciarCaptura();
                }
            }
            else
            {
                IniciarCaptura();
            }
        }

        #endregion

        #region SEAMS Y PRUEBAS SIN HARDWARE

        /// <summary>
        /// Permite simular una captura biométrica para pruebas sin lector físico conectado.
        /// </summary>
        public void SimularHuellaCapturada(Fmd fmd)
        {
            if (_modoActual == Modo.Enrolamiento)
            {
                ProcesarEnrolamiento(fmd);
            }
            else
            {
                ProcesarVerificacion(fmd);
            }
        }

        /// <summary>
        /// Permite simular un error o desconexión del lector en pruebas automatizadas.
        /// </summary>
        public void SimularErrorLector(string mensaje)
        {
            NotificarErrorLector(mensaje);
        }

        /// <summary>
        /// Reinicia el singleton para pruebas aisladas.
        /// </summary>
        public static void ResetearParaPruebas()
        {
            lock (_lockInstance)
            {
                if (_instance != null)
                {
                    _instance.Dispose();
                    _instance = null;
                }
                SeamAlIniciarCaptura = null;
                Interlocked.Exchange(ref _totalIniciosCaptura, 0);
            }
        }

        #endregion
    }
}