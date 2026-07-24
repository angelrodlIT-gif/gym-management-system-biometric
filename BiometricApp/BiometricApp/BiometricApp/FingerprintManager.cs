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
// En FingerprintManager.cs — línea de la constante:
public const int CAPTURAS_REQUERIDAS = 4;
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
        /// El suscriptor (UCVerifyFingerprint) hace la comparación contra la BD.
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
        /// Abre el lector físico. Llamar una sola vez al inicio de la app.
        /// Devuelve true si el lector quedó listo, false si no hay lector o falló.
        /// </summary>
        public bool Inicializar()
        {
            if (_isOpen) return true;

            try
            {
                var readers = ReaderCollection.GetReaders();

                if (readers == null || readers.Count == 0)
                {
                    ErrorLector?.Invoke(this, "No se encontró lector de huellas.");
                    return false;
                }

                _reader = readers[0];

                var result = _reader.Open(Constants.CapturePriority.DP_PRIORITY_COOPERATIVE);

                if (result != Constants.ResultCode.DP_SUCCESS)
                {
                    ErrorLector?.Invoke(this, "Error al abrir lector: " + result);
                    return false;
                }

                _reader.On_Captured += OnCaptured;
                _isOpen = true;
                return true;
            }
            catch (Exception ex)
            {
                ErrorLector?.Invoke(this, "Excepción al inicializar: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Libera el lector físico completamente.
        /// Llamar al cerrar la aplicación.
        /// </summary>
        public void Dispose()
        {
            _modoActual = Modo.Ninguno;

            try { _reader?.CancelCapture(); } catch { }

            // Esperar a que el SDK termine su callback actual antes de destruir
            Thread.Sleep(400);

            try
            {
                if (_reader != null)
                {
                    _reader.On_Captured -= OnCaptured;
                    _reader.Dispose();
                    _reader = null;
                }
            }
            catch { }

            _isOpen = false;

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
        /// Detiene la captura sin cerrar el lector.
        /// El lector queda abierto para reanudar después.
        /// </summary>
        public void Detener()
        {
            _modoActual = Modo.Ninguno;
            try { _reader?.CancelCapture(); } catch { }
            Interlocked.Exchange(ref _capturandoFlag, 0);
        }

        #endregion

        #region CAPTURA INTERNA

        private void IniciarCaptura()
        {
            if (!_isOpen || _reader == null || _modoActual == Modo.Ninguno) return;

            // Evitar doble llamada simultánea con Interlocked (seguro entre hilos)
            if (Interlocked.CompareExchange(ref _capturandoFlag, 1, 0) != 0) return;

            try
            {
                var result = _reader.CaptureAsync(
                    Constants.Formats.Fid.ANSI,
                    Constants.CaptureProcessing.DP_IMG_PROC_DEFAULT,
                    _reader.Capabilities.Resolutions[0]);

                if (result != Constants.ResultCode.DP_SUCCESS)
                {
                    Interlocked.Exchange(ref _capturandoFlag, 0);

                    if (result != Constants.ResultCode.DP_DEVICE_BUSY)
                        ErrorLector?.Invoke(this, "Error al iniciar captura: " + result);
                }
            }
            catch (Exception ex)
            {
                Interlocked.Exchange(ref _capturandoFlag, 0);
                ErrorLector?.Invoke(this, "Excepción en captura: " + ex.Message);
            }
        }

        private void OnCaptured(CaptureResult captureResult)
        {
            // Liberar la bandera — ya terminó esta captura
            Interlocked.Exchange(ref _capturandoFlag, 0);

            // Si se detuvo el modo mientras esperábamos, ignorar
            if (_modoActual == Modo.Ninguno) return;

            // Captura vacía o fallida — reintentar silenciosamente
            if (captureResult.Data == null ||
                captureResult.ResultCode != Constants.ResultCode.DP_SUCCESS)
            {
                IniciarCaptura();
                return;
            }

            // Disparar imagen a los suscriptores (frmDBEnrollment la muestra en pbFingerprint)
            foreach (Fid.Fiv fiv in captureResult.Data.Views)
            {
                ImagenCapturada?.Invoke(this,
                    new ImagenCapturaEventArgs(fiv.RawImage, fiv.Width, fiv.Height));
                break; // solo la primera vista es necesaria
            }

            // Convertir imagen a FMD (minucias)
            var conversionResult = FeatureExtraction.CreateFmdFromFid(
                captureResult.Data,
                Constants.Formats.Fmd.ANSI);

            if (conversionResult.ResultCode != Constants.ResultCode.DP_SUCCESS)
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

        private void ProcesarVerificacion(Fmd fmd)
        {
            // Notificar al suscriptor con la huella capturada
            HuellaParaVerificar?.Invoke(this, fmd);

            // Reiniciar captura para la siguiente verificación
            IniciarCaptura();
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

            // Notificar progreso (el formulario actualiza su label)
            EnrolamientoProgreso?.Invoke(this, conteoActual);

            if (conteoActual >= CAPTURAS_REQUERIDAS)
            {
                // Intentar generar la plantilla final
                DataResult<Fmd> resultEnrollment;

                lock (_enrollFmds)
                {
                    resultEnrollment = Enrollment.CreateEnrollmentFmd(
                        Constants.Formats.Fmd.ANSI,
                        _enrollFmds);
                }

                if (resultEnrollment.ResultCode == Constants.ResultCode.DP_SUCCESS)
                {
                    // Detener modo enrolamiento ANTES de disparar el evento
                    _modoActual = Modo.Ninguno;
                    EnrolamientoCompleto?.Invoke(this, resultEnrollment.Data);
                }
                else
                {
                    // Falló — reiniciar enrolamiento automáticamente
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
                // Aún faltan capturas
                IniciarCaptura();
            }
        }

        #endregion
    }
}