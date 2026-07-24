using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace Gym_System.Core
{
    public class Visitas_conexion
    {
        private string connectionString = 
            ConfigurationManager.ConnectionStrings["GymDbConnection"].ConnectionString;
        public void InsertarVisita(Visitas_agregar visita)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "INSERT INTO Visitas (Nombre, Fecha, Precio) VALUES (@Nombre, @Fecha, @Precio)";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Nombre", visita.Nombre);
                    cmd.Parameters.AddWithValue("@Fecha", visita.Fecha);
                    cmd.Parameters.AddWithValue("@Precio", visita.Precio);
                    cmd.ExecuteNonQuery();
                }
            }
        }



        public DataTable ObtenerVisitas()
        {
            DataTable dt = new DataTable();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string query = @"
                SELECT

                id,
                nombre,
                fecha,
                precio
                FROM Visitas
                ";

                using (SqlCommand cmd = new SqlCommand(query, connection))
                using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                {
                    adapter.Fill(dt);
                }
            }

            return dt;
        }


        public DataTable ObtenerVisitasDelMesActual()
        {
            DataTable dt = new DataTable();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                // Obtener fechas del primer y último día del mes actual
                DateTime inicioMes = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                DateTime finMes = inicioMes.AddMonths(1).AddDays(-1);

                string query = @"
            SELECT
                id,
                nombre,
                fecha,
                precio
            FROM Visitas
            WHERE fecha >= @InicioMes AND fecha <= @FinMes
        ";

                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@InicioMes", inicioMes);
                    cmd.Parameters.AddWithValue("@FinMes", finMes);

                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        adapter.Fill(dt);
                    }
                }
            }

            return dt;
        }
    }


}
