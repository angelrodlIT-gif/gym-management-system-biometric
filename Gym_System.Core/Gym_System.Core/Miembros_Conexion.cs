using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Configuration;

namespace Gym_System.Core
{
    public class Miembro_Conexion
    {
        private string connectionString = ConfigurationManager.ConnectionStrings["GymDbConnection"].ConnectionString;
        public void InsertarMiembro(Miembros_agregar miembro)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    string query = @"
                INSERT INTO Miembros 
                  (nombre, telefono, direccion, edad,
                   estado, fechaRegistro, fechaInicio,
                   fechaFin, idMembresia, huella)
                VALUES 
                  (@nombre, @telefono, @direccion, @edad,
                   @estado, @fechaRegistro, @fechaInicio,
                   @fechaFin, @idMembresia, @huella)";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@nombre", miembro.Nombre);
                        command.Parameters.AddWithValue("@telefono", miembro.Telefono);
                        command.Parameters.AddWithValue("@direccion", miembro.Direccion);
                        command.Parameters.AddWithValue("@edad", miembro.Edad);
                        command.Parameters.AddWithValue("@estado", !string.IsNullOrEmpty(miembro.Estado) ? miembro.Estado : "Activo");
                        command.Parameters.AddWithValue("@fechaRegistro", miembro.FechaRegistro);
                        command.Parameters.AddWithValue("@fechaInicio", miembro.FechaInicio.HasValue ? (object)miembro.FechaInicio.Value : DBNull.Value);
                        command.Parameters.AddWithValue("@fechaFin", miembro.FechaFin.HasValue ? (object)miembro.FechaFin.Value : DBNull.Value);
                        command.Parameters.AddWithValue("@idMembresia", miembro.IdMembresia.HasValue ? (object)miembro.IdMembresia.Value : DBNull.Value);

                        // Conversión segura del template de huella
                        command.Parameters.AddWithValue("@huella",
        !string.IsNullOrEmpty(miembro.TemplateHuella)
            ? (object)miembro.TemplateHuella
            : DBNull.Value);

                        int rows = command.ExecuteNonQuery();
                        if (rows <= 0)
                        {
                            throw new Exception("No se insertó ninguna fila.");
                        }
                        NotificadorCambioMiembro.Notificar();
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Error al insertar miembro: " + ex.Message);
                }
            }
        }


        public void ActualizarMiembro(Miembros_agregar miembro)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    string query = @"
                UPDATE Miembros
                SET 
                    nombre = @nombre,
                    edad = @edad,
                    direccion = @direccion,
                    telefono = @telefono
                WHERE id = @id"; 

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", miembro.id); 
                        command.Parameters.AddWithValue("@nombre", miembro.Nombre);
                        command.Parameters.AddWithValue("@edad", miembro.Edad);
                        command.Parameters.AddWithValue("@direccion", miembro.Direccion);
                        command.Parameters.AddWithValue("@telefono", miembro.Telefono);

                        int rowsAffected = command.ExecuteNonQuery();
                        if (rowsAffected <= 0)
                        {
                            throw new Exception("No se actualizó el miembro.");
                        }
                        NotificadorCambioMiembro.Notificar();
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Error al actualizar el miembro: " + ex.Message);
                }
            }
        }

        public DataTable ObtenerMiembros()
        {
            DataTable dt = new DataTable();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string query = @"
            SELECT 
                m.id, 
                m.nombre, 
                m.telefono, 
                m.direccion, 
                m.edad, 
                m.estado, 
                m.fechaRegistro, 
                m.fechaInicio, 
                m.fechaFin, 
                m.idMembresia,
                me.nombre AS NombreMembresia
            FROM Miembros m
            LEFT JOIN Membresias me ON m.idMembresia = me.id";

                using (SqlCommand cmd = new SqlCommand(query, connection))
                using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                {
                    adapter.Fill(dt);
                }
            }

            return dt;
        }

        public DataTable ObtenerUsuarioPorId(long idUsuario)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = "SELECT * FROM Miembros WHERE id = @Id";
                SqlCommand cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@Id", idUsuario);

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                return dt;
            }
        }

        public bool EliminarUsuario(long idUsuario)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = "DELETE FROM Miembros WHERE id = @id";
                SqlCommand cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@id", idUsuario);

                connection.Open();
                int rowsAffected = cmd.ExecuteNonQuery();
                if (rowsAffected > 0)
                {
                    NotificadorCambioMiembro.Notificar();
                }
                return rowsAffected > 0;
            }
        }
        /// <summary>
        /// Registra un pago y actualiza la membresía del miembro de forma atómica y thread-safe.
        /// Consulta la vigencia actual del miembro bajo la misma transacción (con bloqueo UPDLOCK/ROWLOCK)
        /// y calcula la nueva vigencia preservando días activos restantes mediante VigenciaCalculador.
        /// Valida filas afectadas para garantizar que el miembro exista antes de registrar el pago.
        /// </summary>
        public bool RegistrarPago(long idMiembro, long idMembresia, DateTime? fechaPago = null, decimal? monto = null)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlTransaction transaccion = conn.BeginTransaction())
                {
                    try
                    {
                        DateTime fechaOperacion = fechaPago ?? DateTime.Now;

                        // 1. Consultar miembro bajo bloqueo transaccional (UPDLOCK) para evitar condiciones de carrera
                        DateTime? fechaInicioActual = null;
                        DateTime? fechaFinActual = null;
                        bool miembroExiste = false;

                        string queryMiembro = "SELECT FechaInicio, FechaFin FROM Miembros WITH (UPDLOCK, ROWLOCK) WHERE Id = @IdMiembro";
                        using (SqlCommand cmdMiembro = new SqlCommand(queryMiembro, conn, transaccion))
                        {
                            cmdMiembro.Parameters.AddWithValue("@IdMiembro", idMiembro);
                            using (SqlDataReader reader = cmdMiembro.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    miembroExiste = true;
                                    if (!reader.IsDBNull(0))
                                        fechaInicioActual = reader.GetDateTime(0);
                                    if (!reader.IsDBNull(1))
                                        fechaFinActual = reader.GetDateTime(1);
                                }
                            }
                        }

                        if (!miembroExiste)
                        {
                            transaccion.Rollback();
                            return false;
                        }

                        // 2. Consultar duración, unidad y precio de la membresía bajo la transacción
                        int duracion = 0;
                        string unidadDuracion = "Meses";
                        decimal precioMembresia = 0;
                        bool membresiaExiste = false;

                        string queryMembresia = "SELECT Duracion, UnidadDuracion, Precio FROM Membresias WHERE Id = @IdMembresia";
                        using (SqlCommand cmdMembresia = new SqlCommand(queryMembresia, conn, transaccion))
                        {
                            cmdMembresia.Parameters.AddWithValue("@IdMembresia", idMembresia);
                            using (SqlDataReader reader = cmdMembresia.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    membresiaExiste = true;
                                    duracion = reader.GetInt32(0);
                                    unidadDuracion = reader.IsDBNull(1) ? "Meses" : reader.GetString(1);
                                    if (!reader.IsDBNull(2))
                                        precioMembresia = reader.GetDecimal(2);
                                }
                            }
                        }

                        if (!membresiaExiste)
                        {
                            transaccion.Rollback();
                            return false;
                        }

                        // 3. Calcular vigencia atómicamente dentro de la transacción
                        var (nuevaFechaInicio, nuevaFechaFin) = VigenciaCalculador.CalcularVigencia(
                            fechaInicioActual,
                            fechaFinActual,
                            fechaOperacion,
                            duracion,
                            unidadDuracion
                        );

                        decimal montoFinal = monto ?? precioMembresia;

                        // 4. Actualizar fechas, membresía y estado 'Activo' del miembro
                        string updateMiembro = @"
                            UPDATE Miembros
                            SET FechaInicio = @Inicio,
                                FechaFin = @Fin,
                                IdMembresia = @IdMembresia,
                                Estado = 'Activo'
                            WHERE Id = @IdMiembro";

                        using (SqlCommand cmdUpdate = new SqlCommand(updateMiembro, conn, transaccion))
                        {
                            cmdUpdate.Parameters.AddWithValue("@Inicio", nuevaFechaInicio);
                            cmdUpdate.Parameters.AddWithValue("@Fin", nuevaFechaFin);
                            cmdUpdate.Parameters.AddWithValue("@IdMembresia", idMembresia);
                            cmdUpdate.Parameters.AddWithValue("@IdMiembro", idMiembro);

                            int rowsUpdated = cmdUpdate.ExecuteNonQuery();
                            if (rowsUpdated <= 0)
                            {
                                transaccion.Rollback();
                                return false;
                            }
                        }

                        // 5. Insertar registro en la tabla Pagos
                        string insertPago = "INSERT INTO Pagos (IdMembresia, Fecha, Monto) VALUES (@IdMembresia, @Fecha, @Monto)";
                        using (SqlCommand cmdPago = new SqlCommand(insertPago, conn, transaccion))
                        {
                            cmdPago.Parameters.AddWithValue("@IdMembresia", idMembresia);
                            cmdPago.Parameters.AddWithValue("@Fecha", fechaOperacion);
                            cmdPago.Parameters.AddWithValue("@Monto", montoFinal);

                            int rowsPago = cmdPago.ExecuteNonQuery();
                            if (rowsPago <= 0)
                            {
                                transaccion.Rollback();
                                return false;
                            }
                        }

                        transaccion.Commit();
                        NotificadorCambioMiembro.Notificar();
                        return true;
                    }
                    catch (Exception)
                    {
                        try { transaccion.Rollback(); } catch { }
                        throw;
                    }
                }
            }
        }


        public int ActualizarEstadosMasivo(DateTime? fechaReferencia = null)
        {
            DateTime fecha = fechaReferencia ?? DateTime.Now;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
                    UPDATE Miembros
                    SET estado = CASE
                        WHEN fechaInicio <= @fecha AND fechaFin >= @fecha THEN 'Activo'
                        ELSE 'Inactivo'
                    END
                    WHERE fechaInicio IS NOT NULL AND fechaFin IS NOT NULL";

                using (SqlCommand command = new SqlCommand(query, conn))
                {
                    command.Parameters.AddWithValue("@fecha", fecha);
                    int affected = command.ExecuteNonQuery();
                    NotificadorCambioMiembro.Notificar();
                    return affected;
                }
            }
        }

        public List<Miembros_agregar> ObtenerTodosLosMiembros()
        {
            List<Miembros_agregar> lista = new List<Miembros_agregar>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT id, fechaInicio, fechaFin FROM miembros";
                SqlCommand command = new SqlCommand(query, conn);
                SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    lista.Add(new Miembros_agregar
                    {
                        id = Convert.ToInt64(reader["id"]),
                        FechaInicio = reader["fechaInicio"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["fechaInicio"]) : null,
                        FechaFin = reader["fechaFin"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["fechaFin"]) : null
                    });
                }
            }

            return lista;
        }


        public void ActualizarEstadoMiembro(long id, string nuevoEstado)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "UPDATE miembros SET estado = @estado WHERE id = @id";
                SqlCommand command = new SqlCommand(query, conn);
                command.Parameters.AddWithValue("@estado", nuevoEstado);
                command.Parameters.AddWithValue("@id", id);
                command.ExecuteNonQuery();
                NotificadorCambioMiembro.Notificar();
            }
        }

        public bool RegistrarPagoHistorial(long idMembresia, DateTime fechaPago, decimal monto)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    string query = "INSERT INTO Pagos (IdMembresia, Fecha, Monto) VALUES (@IdMembresia, @Fecha, @Monto)";

                    using (SqlCommand cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@IdMembresia", idMembresia);
                        cmd.Parameters.AddWithValue("@Fecha", fechaPago); 
                        cmd.Parameters.AddWithValue("@Monto", monto);

                        connection.Open();
                        int result = cmd.ExecuteNonQuery();
                        return result > 0;
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }
        }
        /// <summary>
        /// Obtiene la plantilla biométrica (XML) asociada a un miembro desde la base de datos.
        /// La huella se almacena como NVARCHAR(MAX) en formato XML serializado por el SDK DigitalPersona.
        /// </summary>
        /// <param name="idMiembro">Identificador único del miembro (columna id en tabla Miembros).</param>
        /// <returns>Cadena XML de la huella, o null si no se encuentra o es nula.</returns>
        public string ObtenerHuella(long idMiembro)
        {
            string plantillaHuella = null;
            string query = "SELECT huella FROM Miembros WHERE id = @id";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", idMiembro);

                conn.Open();

                var result = cmd.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                {
                    plantillaHuella = result.ToString();
                }
            }

            return plantillaHuella;
        }





    }
}
