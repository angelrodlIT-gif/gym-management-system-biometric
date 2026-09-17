using DPUruNet;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Threading;

namespace BiometricApp
{
    /// <summary>
    /// DTO inmutable de metadatos del registro biométrico retornado por la API pública de snapshot.
    /// No expone la plantilla FMD ni su buffer mutable Bytes, garantizando aislamiento profundo.
    /// </summary>
    public class BiometricRecordMetadata
    {
        public long IdMiembro { get; }
        public string Nombre { get; }
        public string Membresia { get; }
        public DateTime FechaFin { get; }
        public bool TienePlantilla { get; }

        public BiometricRecordMetadata(long id, string nombre, string membresia, DateTime fechaFin, bool tienePlantilla)
        {
            IdMiembro = id;
            Nombre = nombre;
            Membresia = membresia;
            FechaFin = fechaFin;
            TienePlantilla = tienePlantilla;
        }
    }

    /// <summary>
    /// Registro en memoria de un miembro con su plantilla biométrica previamente deserializada.
    /// Inmutable tras su creación para asegurar atomicidad del snapshot de caché.
    /// La plantilla FMD se mantiene internal para acceso exclusivo de comparación dentro de BiometricCache,
    /// impidiendo cualquier mutación del buffer Fmd.Bytes desde la API pública de snapshot.
    /// </summary>
    public class BiometricRecord
    {
        public long IdMiembro { get; }
        public string Nombre { get; }
        public string Membresia { get; }
        public DateTime FechaFin { get; }
        public bool TienePlantilla => TemplateFmd != null;
        internal Fmd TemplateFmd { get; }

        public BiometricRecord(long id, string nombre, string membresia, DateTime fechaFin, Fmd templateFmd)
        {
            IdMiembro = id;
            Nombre = nombre;
            Membresia = membresia;
            FechaFin = fechaFin;
            TemplateFmd = templateFmd;
        }

        public BiometricRecordMetadata ToMetadata()
        {
            return new BiometricRecordMetadata(IdMiembro, Nombre, Membresia, FechaFin, TienePlantilla);
        }
    }

    /// <summary>
    /// Resultado de una operación de identificación 1:N contra la caché biométrica.
    /// </summary>
    public class BiometricMatchResult
    {
        public bool Exitoso { get; set; }
        public long IdMiembro { get; set; }
        public string Nombre { get; set; }
        public string Membresia { get; set; }
        public DateTime FechaFin { get; set; }
        public int Score { get; set; }
        public string MensajeError { get; set; }
    }

    /// <summary>
    /// Caché en memoria de plantillas biométricas FMD deserializadas para optimización 1:N.
    /// Evita consultar la base de datos completa y deserializar XML en cada intento de lectura.
    /// Implementa invalidación sincronizada y versionada contra condiciones de carrera,
    /// serialización de recargas concurrentes, snapshots atómicos copy-on-write,
    /// y estado de cooldown / backoff ante fallos de SQL para no saturar callbacks.
    /// </summary>
    public static class BiometricCache
    {
        private static readonly object _syncLock = new object();
        private static readonly object _reloadLock = new object();
        private static List<BiometricRecord> _cachedRecords = new List<BiometricRecord>();
        private static volatile bool _necesitaRecarga = true;
        private static volatile bool _estaCargada = false;
        private static long _version = 0;
        private static long _totalRecargasPorVersionObsoleta = 0;

        // Métricas de diagnóstico y verificación (seams / validaciones de rendimiento)
        private static long _totalConsultasBd = 0;
        private static long _totalDeserializacionesXml = 0;
        private static long _totalVerificaciones = 0;

        // Estado de error y cooldown / backoff para base de datos caída
        private static long _ultimoFalloUtcTicks = 0;
        private static string _ultimoErrorCarga = null;
        private static int _cooldownFalloSegundos = 5;

        public static long TotalConsultasBd => Interlocked.Read(ref _totalConsultasBd);
        public static long TotalDeserializacionesXml => Interlocked.Read(ref _totalDeserializacionesXml);
        public static long TotalVerificaciones => Interlocked.Read(ref _totalVerificaciones);
        public static long TotalRecargasPorVersionObsoleta => Interlocked.Read(ref _totalRecargasPorVersionObsoleta);
        public static long Version => Interlocked.Read(ref _version);

        public static int CantidadPlantillas
        {
            get
            {
                lock (_syncLock)
                {
                    return _cachedRecords != null ? _cachedRecords.Count : 0;
                }
            }
        }

        public static bool NecesitaRecarga => _necesitaRecarga;

        public static bool EstaCargada
        {
            get
            {
                lock (_syncLock)
                {
                    return _estaCargada;
                }
            }
        }

        /// <summary>
        /// Snapshot atómico de metadatos de los registros cargados en memoria.
        /// Retorna DTOs de solo lectura sin exponer la instancia FMD ni sus bytes mutables.
        /// </summary>
        public static IReadOnlyList<BiometricRecordMetadata> SnapshotActual
        {
            get
            {
                lock (_syncLock)
                {
                    if (_cachedRecords == null || _cachedRecords.Count == 0)
                        return new List<BiometricRecordMetadata>().AsReadOnly();

                    var dtoList = new List<BiometricRecordMetadata>(_cachedRecords.Count);
                    foreach (var r in _cachedRecords)
                    {
                        dtoList.Add(r.ToMetadata());
                    }
                    return dtoList.AsReadOnly();
                }
            }
        }

        public static int CooldownFalloSegundos
        {
            get => _cooldownFalloSegundos;
            set => _cooldownFalloSegundos = value;
        }

        public static string UltimoErrorCarga
        {
            get
            {
                lock (_syncLock)
                {
                    return _ultimoErrorCarga;
                }
            }
        }

        public static bool EnCooldown
        {
            get
            {
                if (SeamForzarCooldown.HasValue) return SeamForzarCooldown.Value;
                lock (_syncLock)
                {
                    long ticks = Interlocked.Read(ref _ultimoFalloUtcTicks);
                    if (ticks == 0) return false;

                    DateTime ahora = SeamTiempoActual != null ? SeamTiempoActual() : DateTime.UtcNow;
                    DateTime ultimoFallo = new DateTime(ticks, DateTimeKind.Utc);
                    return (ahora - ultimoFallo).TotalSeconds < _cooldownFalloSegundos;
                }
            }
        }

        /// <summary>
        /// Seam para inyectar proveedores de datos simulados en pruebas automatizadas sin SQL Server.
        /// </summary>
        public static Func<IEnumerable<BiometricRecord>> SeamCargadorDatos { get; set; }

        /// <summary>
        /// Seam para inyectar comparador biométrico simulado en pruebas automatizadas sin hardware.
        /// </summary>
        public static Func<Fmd, Fmd, CompareResult> SeamComparador { get; set; }

        /// <summary>
        /// Seam para controlar el tiempo de forma determinista en pruebas de cooldown / backoff.
        /// </summary>
        public static Func<DateTime> SeamTiempoActual { get; set; }

        /// <summary>
        /// Seam para forzar el estado de cooldown en pruebas.
        /// </summary>
        public static bool? SeamForzarCooldown { get; set; }

        /// <summary>
        /// Seam hook invocado durante CargarPlantillas para simular carreras concurrentes de invalidación en pruebas.
        /// </summary>
        public static Action SeamDuranteCarga { get; set; }

        /// <summary>
        /// Evento notificado cuando ocurre un fallo al cargar datos desde la BD.
        /// </summary>
        public static event EventHandler<string> OnErrorCarga;

        /// <summary>
        /// Invalida la caché de forma sincronizada y versionada.
        /// Incrementa la versión para que una carga concurrente en vuelo no sobrescriba esta invalidación.
        /// </summary>
        public static void Invalidar()
        {
            lock (_syncLock)
            {
                _version++;
                _necesitaRecarga = true;
                _estaCargada = false;
            }
        }

        /// <summary>
        /// Limpia completamente la memoria caché y reinicia versión y contadores.
        /// </summary>
        public static void Limpiar()
        {
            lock (_syncLock)
            {
                _cachedRecords = new List<BiometricRecord>();
                _necesitaRecarga = true;
                _estaCargada = false;
                _version++;
                Interlocked.Exchange(ref _totalConsultasBd, 0);
                Interlocked.Exchange(ref _totalDeserializacionesXml, 0);
                Interlocked.Exchange(ref _totalVerificaciones, 0);
                Interlocked.Exchange(ref _totalRecargasPorVersionObsoleta, 0);
                Interlocked.Exchange(ref _ultimoFalloUtcTicks, 0);
                _ultimoErrorCarga = null;
                SeamForzarCooldown = null;
                SeamDuranteCarga = null;
            }
        }

        public static void ResetearCooldown()
        {
            lock (_syncLock)
            {
                Interlocked.Exchange(ref _ultimoFalloUtcTicks, 0);
                _ultimoErrorCarga = null;
                SeamForzarCooldown = null;
            }
        }

        public static void RegistrarFalloCarga(string mensaje)
        {
            lock (_syncLock)
            {
                DateTime ahora = SeamTiempoActual != null ? SeamTiempoActual() : DateTime.UtcNow;
                Interlocked.Exchange(ref _ultimoFalloUtcTicks, ahora.Ticks);
                _ultimoErrorCarga = mensaje;
                _necesitaRecarga = true;
            }

            try
            {
                OnErrorCarga?.Invoke(null, mensaje);
            }
            catch { }
        }

        public static void SimularFalloCarga(string mensaje = "Fallo de conexión simulado")
        {
            RegistrarFalloCarga(mensaje);
        }

        /// <summary>
        /// Carga las plantillas biométricas desde la base de datos (o seam inyectado) hacia la memoria.
        /// Utiliza conexiones locales con 'using', serializa recargas simultáneas para evitar consultas
        /// duplicadas a BD, valida backoff/cooldown si la BD está caída y publica el snapshot SÓLO
        /// si la versión capturada al iniciar coincide con la actual al terminar. Si hubo invalidación
        /// concurrente durante la carga, no publica datos obsoletos, deja la caché sucia y recarga.
        /// </summary>
        public static void CargarPlantillas(bool forzarRecarga = false)
        {
            // Verificación rápida bajo _syncLock
            lock (_syncLock)
            {
                if (!forzarRecarga && !_necesitaRecarga && _estaCargada)
                {
                    return;
                }
            }

            // Serializar cargas concurrentes para evitar consultas SQL simultáneas redundantes
            lock (_reloadLock)
            {
                int intentos = 0;
                const int maxIntentos = 5;

                while (intentos++ < maxIntentos)
                {
                    long versionCapturada;
                    lock (_syncLock)
                    {
                        if (!forzarRecarga && !_necesitaRecarga && _estaCargada)
                        {
                            return;
                        }

                        // Si estamos en cooldown por fallo previo y no es un seam de prueba, abortar sin consultar SQL
                        if (EnCooldown && SeamCargadorDatos == null)
                        {
                            return;
                        }

                        versionCapturada = _version;
                    }

                    // Hook de seam para pruebas de concurrencia y carrera
                    try
                    {
                        SeamDuranteCarga?.Invoke();
                    }
                    catch { }

                    List<BiometricRecord> nuevaLista = null;
                    bool errorCarga = false;
                    string detalleError = null;

                    Interlocked.Increment(ref _totalConsultasBd);

                    if (SeamCargadorDatos != null)
                    {
                        try
                        {
                            var datosSeam = SeamCargadorDatos();
                            nuevaLista = datosSeam != null ? new List<BiometricRecord>(datosSeam) : new List<BiometricRecord>();
                        }
                        catch (Exception ex)
                        {
                            errorCarga = true;
                            detalleError = ex.Message;
                        }
                    }
                    else
                    {
                        try
                        {
                            nuevaLista = ConsultarPlantillasDesdeBd();
                        }
                        catch (Exception ex)
                        {
                            errorCarga = true;
                            detalleError = ex.Message;
                        }
                    }

                    lock (_syncLock)
                    {
                        if (errorCarga)
                        {
                            RegistrarFalloCarga(detalleError);
                            _necesitaRecarga = true;
                            _estaCargada = false;
                            return;
                        }

                        // Publicar ÚNICAMENTE si la versión capturada coincide exactamente
                        if (_version == versionCapturada)
                        {
                            _cachedRecords = nuevaLista ?? new List<BiometricRecord>();
                            _necesitaRecarga = false;
                            _estaCargada = true;
                            _ultimoErrorCarga = null;
                            return;
                        }
                        else
                        {
                            // Invalicación concurrente detectada durante la carga.
                            // No publicar datos potencialmente obsoletos; dejar caché sucia y recargar.
                            Interlocked.Increment(ref _totalRecargasPorVersionObsoleta);
                            _necesitaRecarga = true;
                            forzarRecarga = true;
                            // Continúa el bucle while para recargar con la versión nueva
                        }
                    }
                }
            }
        }

        private static List<BiometricRecord> ConsultarPlantillasDesdeBd()
        {
            List<BiometricRecord> nuevaLista = new List<BiometricRecord>();
            string connStr = null;

            try
            {
                connStr = ConfigurationManager.ConnectionStrings["GymDbConnection"]?.ConnectionString;
            }
            catch { }

            if (string.IsNullOrEmpty(connStr))
            {
                return nuevaLista;
            }

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                string query = @"
                    SELECT m.Id, m.Nombre, m.Huella, m.FechaFin, mem.Nombre AS Membresia
                    FROM Miembros m
                    LEFT JOIN Membresias mem ON m.IdMembresia = mem.Id
                    WHERE m.Huella IS NOT NULL";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int idxHuella = reader.GetOrdinal("Huella");
                        if (reader.IsDBNull(idxHuella))
                            continue;

                        string huellaXml = reader.GetString(idxHuella);
                        if (string.IsNullOrWhiteSpace(huellaXml))
                            continue;

                        try
                        {
                            Fmd fmd = Fmd.DeserializeXml(huellaXml);
                            Interlocked.Increment(ref _totalDeserializacionesXml);

                            int idxId = reader.GetOrdinal("Id");
                            int idxNombre = reader.GetOrdinal("Nombre");
                            int idxMembresia = reader.GetOrdinal("Membresia");
                            int idxFechaFin = reader.GetOrdinal("FechaFin");

                            long id = reader.GetInt64(idxId);
                            string nombre = reader.IsDBNull(idxNombre) ? string.Empty : reader.GetString(idxNombre);
                            string membresia = reader.IsDBNull(idxMembresia) ? "Sin membresía" : reader.GetString(idxMembresia);
                            DateTime fechaFin = reader.IsDBNull(idxFechaFin) ? DateTime.MinValue : reader.GetDateTime(idxFechaFin);

                            nuevaLista.Add(new BiometricRecord(id, nombre, membresia, fechaFin, fmd));
                        }
                        catch (Exception exXml)
                        {
                            System.Diagnostics.Debug.WriteLine("Error al deserializar huella XML del miembro: " + exXml.Message);
                        }
                    }
                }
            }

            return nuevaLista;
        }

        /// <summary>
        /// Compara la huella capturada contra todas las plantillas precargadas en memoria (1:N).
        /// NO consulta la base de datos ni deserializa XML si la caché ya está cargada y limpia.
        /// Respeta el estado de error/cooldown para no bloquear el callback ante caídas de SQL.
        /// Opera sobre un snapshot inmutable copy-on-write para garantizar atomicidad y seguridad multihilo.
        /// </summary>
        public static BiometricMatchResult Identificar(Fmd fingerFmd, int threshold = 0x7fffffff / 100000)
        {
            if (fingerFmd == null) return null;

            Interlocked.Increment(ref _totalVerificaciones);

            if (!_estaCargada || _necesitaRecarga || _cachedRecords == null)
            {
                if (EnCooldown)
                {
                    return new BiometricMatchResult
                    {
                        Exitoso = false,
                        MensajeError = "Base de datos en cooldown tras error: " + (UltimoErrorCarga ?? "Conexión rechazada")
                    };
                }

                CargarPlantillas();

                if (EnCooldown)
                {
                    return new BiometricMatchResult
                    {
                        Exitoso = false,
                        MensajeError = "Error al conectar con la base de datos: " + (UltimoErrorCarga ?? "Fallo de conexión")
                    };
                }
            }

            List<BiometricRecord> snapshot;
            lock (_syncLock)
            {
                snapshot = _cachedRecords;
            }

            if (snapshot == null || snapshot.Count == 0)
            {
                return new BiometricMatchResult { Exitoso = false };
            }

            foreach (var record in snapshot)
            {
                if (record.TemplateFmd == null) continue;

                CompareResult compare;
                if (SeamComparador != null)
                {
                    compare = SeamComparador(fingerFmd, record.TemplateFmd);
                }
                else
                {
                    compare = Comparison.Compare(fingerFmd, 0, record.TemplateFmd, 0);
                }

                if (compare != null &&
                    compare.ResultCode == Constants.ResultCode.DP_SUCCESS &&
                    compare.Score < threshold)
                {
                    return new BiometricMatchResult
                    {
                        Exitoso = true,
                        IdMiembro = record.IdMiembro,
                        Nombre = record.Nombre,
                        Membresia = record.Membresia,
                        FechaFin = record.FechaFin,
                        Score = compare.Score
                    };
                }
            }

            return new BiometricMatchResult { Exitoso = false };
        }

        /// <summary>
        /// Actualiza selectivamente los datos de un miembro en la caché sin requerir recarga total de la BD.
        /// Realiza reemplazo copy-on-write e incrementa versión para atomicidad del snapshot.
        /// </summary>
        public static void ActualizarMiembroEnCache(long idMiembro, string nombre, string membresia, DateTime fechaFin, Fmd templateFmd = null)
        {
            lock (_syncLock)
            {
                var nuevaLista = _cachedRecords != null ? new List<BiometricRecord>(_cachedRecords) : new List<BiometricRecord>();
                int idx = nuevaLista.FindIndex(r => r.IdMiembro == idMiembro);
                if (idx >= 0)
                {
                    var existente = nuevaLista[idx];
                    nuevaLista[idx] = new BiometricRecord(
                        idMiembro,
                        nombre,
                        membresia,
                        fechaFin,
                        templateFmd ?? existente.TemplateFmd
                    );
                }
                else if (templateFmd != null)
                {
                    nuevaLista.Add(new BiometricRecord(idMiembro, nombre, membresia, fechaFin, templateFmd));
                }
                _cachedRecords = nuevaLista;
                _estaCargada = true;
                _necesitaRecarga = false;
                _version++;
            }
        }

        /// <summary>
        /// Remueve selectivamente un miembro de la caché de memoria con reemplazo copy-on-write.
        /// </summary>
        public static void EliminarMiembroDeCache(long idMiembro)
        {
            lock (_syncLock)
            {
                if (_cachedRecords != null)
                {
                    var nuevaLista = new List<BiometricRecord>(_cachedRecords);
                    nuevaLista.RemoveAll(r => r.IdMiembro == idMiembro);
                    _cachedRecords = nuevaLista;
                    _version++;
                }
            }
        }

        #region SEAMS Y HELPERS DE PRUEBA

        /// <summary>
        /// Crea una instancia de BiometricRecord para pruebas automatizadas sin base de datos.
        /// </summary>
        public static BiometricRecord CrearRegistroMock(long id, string nombre, string membresia, DateTime fechaFin, byte[] bytesHuella)
        {
            Fmd fmd = new Fmd(bytesHuella ?? new byte[] { 1, 2, 3 }, 0, "1.0.0");
            return new BiometricRecord(id, nombre, membresia, fechaFin, fmd);
        }

        /// <summary>
        /// Crea una instancia de Fmd mock para pruebas automatizadas sin lector físico.
        /// </summary>
        public static Fmd CrearFmdMock(byte[] bytesHuella)
        {
            return new Fmd(bytesHuella ?? new byte[] { 1, 2, 3 }, 0, "1.0.0");
        }

        /// <summary>
        /// Inyecta directamente un conjunto de registros mock en la caché simulando una carga.
        /// </summary>
        public static void CargarColeccionSeam(IEnumerable<BiometricRecord> registros)
        {
            lock (_syncLock)
            {
                Interlocked.Increment(ref _totalConsultasBd);
                _cachedRecords = registros != null ? new List<BiometricRecord>(registros) : new List<BiometricRecord>();
                _necesitaRecarga = false;
                _estaCargada = true;
                _version++;
            }
        }

        /// <summary>
        /// Configura el proveedor de datos de prueba del seam para simular recargas de BD.
        /// </summary>
        public static void ConfigurarCargadorSeam(params BiometricRecord[] registros)
        {
            SeamCargadorDatos = () => registros != null ? new List<BiometricRecord>(registros) : new List<BiometricRecord>();
        }

        /// <summary>
        /// Configura un comparador simulado en el seam que compara los primeros bytes de las huellas mock.
        /// </summary>
        public static void ConfigurarSeamSimulado(int scoreMatch = 50)
        {
            SeamComparador = (target, candidate) =>
            {
                if (target == null || candidate == null)
                    return new CompareResult(Constants.ResultCode.DP_FAILURE, int.MaxValue);

                bool match = false;
                if (target.Bytes != null && candidate.Bytes != null &&
                    target.Bytes.Length > 0 && candidate.Bytes.Length > 0)
                {
                    match = (target.Bytes[0] == candidate.Bytes[0]);
                }

                int score = match ? scoreMatch : 2000000;
                return new CompareResult(Constants.ResultCode.DP_SUCCESS, score);
            };
        }

        #endregion
    }
}
