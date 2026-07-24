using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Gym_System.Core;
using System.Configuration;


namespace Gym_System.Data
{
    public class Membresias_conexion
    {
        private string connectionString = 
            ConfigurationManager.ConnectionStrings["GymDbConnection"].ConnectionString;
        public List<Membresias_agregar> ObtenerMembresias()
        {
            List<Membresias_agregar> lista = new List<Membresias_agregar>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT id, Nombre, Duracion, Precio FROM Membresias";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        lista.Add(new Membresias_agregar
                        {
                            id = reader.GetInt64(0),
                            Nombre = reader.GetString(1),
                            Duracion = reader.GetInt32(2),
                            Precio = reader.GetDecimal(3)
                        });
                    }
                }
            }

            return lista;
        }

        public bool InsertarMembresia(Membresias_agregar membresia)
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = "INSERT INTO Membresias (Nombre, Duracion, Precio, UnidadDuracion) " +
                               "VALUES (@Nombre, @Duracion, @Precio, @UnidadDuracion)";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Nombre", membresia.Nombre);
                cmd.Parameters.AddWithValue("@Duracion", membresia.Duracion);
                cmd.Parameters.AddWithValue("@Precio", membresia.Precio);
                cmd.Parameters.AddWithValue("@UnidadDuracion", membresia.UnidadDuracion); // <--- esto es clave

                con.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }


        public DataTable ObtenerMembresia()
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    string query = "SELECT id, Nombre, Duracion, Precio, UnidadDuracion FROM Membresias";
                   
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        SqlDataAdapter adapter = new SqlDataAdapter(command);
                        DataTable tablaMembresias = new DataTable();
                        adapter.Fill(tablaMembresias);
                        return tablaMembresias;
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Error al obtener membresias: " + ex.Message);
                }
            }
        }

        public bool EliminarMembresia(int idMembresia)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = "UPDATE Miembros SET idMembresia = NULL, estado = 'Sin Membresía' WHERE idMembresia = @id; " +
                               "DELETE FROM Membresias WHERE id = @id;";
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.Add("@id", SqlDbType.BigInt).Value = idMembresia;

                    connection.Open();
                    int rowsAffected = cmd.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
            }
        }
    }
}
