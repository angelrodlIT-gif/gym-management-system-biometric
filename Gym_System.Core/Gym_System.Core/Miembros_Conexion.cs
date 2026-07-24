using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Windows.Forms;
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
                return rowsAffected > 0;
            }
        }
        public bool RegistrarPago(long idMiembro, DateTime fechaInicio, DateTime fechaFin, long idMembresia)
        {
            bool exito = false;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                SqlTransaction transaccion = conn.BeginTransaction();

                try
                {
                    // 1. Registrar membresía 
                    string queryMembresia = "UPDATE Miembros SET FechaInicio = @Inicio, FechaFin = @Fin, IdMembresia = @IdMembresia WHERE Id = @IdMiembro";

                    using (SqlCommand cmdMembresia = new SqlCommand(queryMembresia, conn, transaccion))
                    {
                        cmdMembresia.Parameters.AddWithValue("@Inicio", fechaInicio);
                        cmdMembresia.Parameters.AddWithValue("@Fin", fechaFin);
                        cmdMembresia.Parameters.AddWithValue("@IdMembresia", idMembresia);
                        cmdMembresia.Parameters.AddWithValue("@IdMiembro", idMiembro);
                        cmdMembresia.ExecuteNonQuery();
                    }

                    // 2. Obtener precio de la membresía
                    decimal monto = 0;
                    string queryPrecio = "SELECT Precio FROM Membresias WHERE id = @IdMembresia";
                    using (SqlCommand cmdPrecio = new SqlCommand(queryPrecio, conn, transaccion))
                    {
                        cmdPrecio.Parameters.AddWithValue("@IdMembresia", idMembresia);
                        object result = cmdPrecio.ExecuteScalar();
                        if (result != null)
                            monto = Convert.ToDecimal(result);
                    }

                    // 3. Insertar en tabla Pagos
                    string queryPago = "INSERT INTO Pagos (IdMembresia, Fecha, Monto) VALUES (@IdMembresia, @Fecha, @Monto)";
                    using (SqlCommand cmdPago = new SqlCommand(queryPago, conn, transaccion))
                    {
                        cmdPago.Parameters.AddWithValue("@IdMembresia", idMembresia);
                        cmdPago.Parameters.AddWithValue("@Fecha", fechaInicio);
                        cmdPago.Parameters.AddWithValue("@Monto", monto);
                        cmdPago.ExecuteNonQuery();
                    }

                    transaccion.Commit();
                    exito = true;
                }
                catch (Exception ex)
                {
                    transaccion.Rollback();
                    MessageBox.Show("Error al registrar en tabla pagos: " + ex.Message);
                }
            }

            return exito;
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
            catch (Exception ex)
            {
                MessageBox.Show("Error al registrar en tabla pagos: " + ex.Message);
                return false;
            }
        }
        //Conexion para verificar Huella desde UCVerifyFingerprint
        public byte[] ObtenerHuella(int idMiembro)
        {
            string connectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=SistemaGimnasio;Integrated Security=True";
            byte[] plantillaHuella = null;

            string query = "SELECT Huella FROM Miembros WHERE IdMiembro = @IdMiembro";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@IdMiembro", idMiembro);

                conn.Open();

                var result = cmd.ExecuteScalar();
                if (result != DBNull.Value)
                {
                    plantillaHuella = (byte[])result;
                }
            }

            return plantillaHuella;
        }





    }
}
