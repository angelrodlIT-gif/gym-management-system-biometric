using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Configuration;

namespace Gym_System.Core
{
    public class Pagos_conexion
    {
        private string connectionString =
            ConfigurationManager.ConnectionStrings["GymDbConnection"].ConnectionString;


        // 2. Reporte sin fecha (solo activos)
        public ObservableCollection<Pagos_agregar> ObtenerReporteMembresiasActivos()
        {
            var lista = new ObservableCollection<Pagos_agregar>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand(@"
                    SELECT 
                        m.Id AS IdMembresia,
                        m.Nombre AS NombreMembresia,
                        m.Precio,
                        COUNT(DISTINCT u.Id) AS TotalUsuarios,
                        (m.Precio * COUNT(DISTINCT u.Id)) AS TotalEsperado
                    FROM Membresias m
                    LEFT JOIN Miembros u ON m.Id = u.IdMembresia AND u.Estado = 'Activo'
                    GROUP BY m.Id, m.Nombre, m.Precio
                ", conn);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        lista.Add(new Pagos_agregar
                        {
                            NombreMembresia = reader["NombreMembresia"].ToString(),
                            Precio = Convert.ToDecimal(reader["Precio"]),
                            TotalUsuarios = Convert.ToInt32(reader["TotalUsuarios"]),
                            TotalEsperado = Convert.ToDecimal(reader["TotalEsperado"]),
                        });
                    }
                }
            }

            return lista;
        }


        //3. Total de pagos
        public decimal ObtenerTotalEsperadoPagado()
        {
            decimal total = 0;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand(@"
            SELECT SUM(TotalEsperado) AS Total
            FROM (
                SELECT 
                    m.Precio * COUNT(DISTINCT u.Id) AS TotalEsperado
                FROM Membresias m
                LEFT JOIN Miembros u ON m.Id = u.IdMembresia AND u.Estado = 'Activo'
                GROUP BY m.Id, m.Precio
            ) AS Totales
        ", conn);

                object result = cmd.ExecuteScalar();
                if (result != DBNull.Value)
                    total = Convert.ToDecimal(result);
            }

            return total;
        }
        // 3. Reporte con fecha (Sin contar activos)
        public ObservableCollection<Pagos_agregar> ReportePagosConNombreMembresia(DateTime fechaInicio, DateTime fechaFin)
        {
            var lista = new ObservableCollection<Pagos_agregar>();
            DateTime inicio = fechaInicio.Date;
            DateTime finExclusivo = fechaFin.Date.AddDays(1);

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand(@"
            SELECT 
                p.IdMembresia,
                ISNULL(m.Nombre, 'Sin Membresía') AS NombreMembresia,
                SUM(p.Monto) AS TotalMonto
            FROM Pagos p
            LEFT JOIN Membresias m ON p.IdMembresia = m.Id
            WHERE p.Fecha >= @FechaInicio AND p.Fecha < @FinExclusivo
            GROUP BY p.IdMembresia, m.Nombre
        ", conn);

                cmd.Parameters.AddWithValue("@FechaInicio", inicio);
                cmd.Parameters.AddWithValue("@FinExclusivo", finExclusivo);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string nombre = reader["NombreMembresia"] != DBNull.Value ? reader["NombreMembresia"].ToString() : "Sin Membresía";
                        decimal monto = reader["TotalMonto"] != DBNull.Value ? Convert.ToDecimal(reader["TotalMonto"]) : 0m;

                        lista.Add(new Pagos_agregar
                        {
                            NombreMembresia = nombre,
                            NombreMembresia2 = nombre,
                            Monto = monto,
                            FechaInicio = fechaInicio,
                            FechaFin = fechaFin
                        });
                    }
                }
            }

            return lista;
        }

        // 4. Reporte con fecha de visitas
        public (decimal TotalVisitas, int TotalIds) ObtenerTotalesVisitasPorFecha(DateTime fechaInicio, DateTime fechaFin)
        {
            decimal totalVisitas = 0;
            int totalIds = 0;
            DateTime inicio = fechaInicio.Date;
            DateTime finExclusivo = fechaFin.Date.AddDays(1);

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand(@"
            SELECT 
                SUM(TotalPorGrupo) AS Total,
                COUNT(*) AS TotalIds
            FROM (
                SELECT 
                    v.Id,
                    COUNT(*) AS Cantidad,
                    v.Precio,
                    COUNT(*) * v.Precio AS TotalPorGrupo
                FROM Visitas v
                WHERE v.Fecha >= @FechaInicio AND v.Fecha < @FinExclusivo
                GROUP BY v.Id, v.Precio
            ) AS Subtotales;
        ", conn);

                cmd.Parameters.AddWithValue("@FechaInicio", inicio);
                cmd.Parameters.AddWithValue("@FinExclusivo", finExclusivo);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        totalVisitas = reader["Total"] != DBNull.Value ? Convert.ToDecimal(reader["Total"]) : 0;
                        totalIds = reader["TotalIds"] != DBNull.Value ? Convert.ToInt32(reader["TotalIds"]) : 0;
                    }
                }
            }

            return (totalVisitas, totalIds);
        }
        // 5. Reporte de pagos por semana (meses)
        public ObservableCollection<(string Semana, decimal Total)> ObtenerPagosPorSemanaDelMesActual()
        {
            var lista = new ObservableCollection<(string, decimal)>();

            DateTime hoy = DateTime.Today;
            DateTime fechaInicio = new DateTime(hoy.Year, hoy.Month, 1);
            DateTime fechaFinExclusivo = fechaInicio.AddMonths(1);

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand(@"
            SELECT 
                DATEPART(WEEK, p.Fecha) AS Semana,
                MIN(p.Fecha) AS FechaInicioSemana,
                SUM(p.Monto) AS TotalMonto
            FROM Pagos p
            WHERE p.Fecha >= @FechaInicio AND p.Fecha < @FechaFinExclusivo
            GROUP BY DATEPART(WEEK, p.Fecha)
            ORDER BY FechaInicioSemana
        ", conn);

                cmd.Parameters.AddWithValue("@FechaInicio", fechaInicio);
                cmd.Parameters.AddWithValue("@FechaFinExclusivo", fechaFinExclusivo);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int semana = Convert.ToInt32(reader["Semana"]);
                        DateTime fechaInicioSemana = Convert.ToDateTime(reader["FechaInicioSemana"]);
                        decimal monto = Convert.ToDecimal(reader["TotalMonto"]);

                        string etiqueta = $"Semana {semana} ({fechaInicioSemana:dd MMM})";
                        lista.Add((etiqueta, monto));
                    }
                }
            }

            return lista;
        }


        //6. Reporte por semana (Lunes-Domingo)
        public ObservableCollection<(string Dia, decimal Total)> ObtenerPagosSemanaActual()
        {
            var lista = new ObservableCollection<(string, decimal)>();

            DateTime hoy = DateTime.Today;
            int delta = ((int)hoy.DayOfWeek == 0) ? -6 : (1 - (int)hoy.DayOfWeek);
            DateTime lunes = hoy.AddDays(delta).Date;
            DateTime finExclusivo = lunes.AddDays(7);

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand(@"
            SET LANGUAGE Spanish;

            WITH Movimientos AS (
                SELECT 
                    CONVERT(date, Fecha) AS Fecha,
                    DATENAME(WEEKDAY, Fecha) AS DiaSemana,
                    SUM(Monto) AS Total
                FROM Pagos
                WHERE Fecha >= @Lunes AND Fecha < @FinExclusivo
                GROUP BY CONVERT(date, Fecha), DATENAME(WEEKDAY, Fecha)

                UNION ALL

                SELECT 
                    CONVERT(date, Fecha) AS Fecha,
                    DATENAME(WEEKDAY, Fecha) AS DiaSemana,
                    SUM(Precio) AS Total
                FROM Visitas
                WHERE Fecha >= @Lunes AND Fecha < @FinExclusivo
                GROUP BY CONVERT(date, Fecha), DATENAME(WEEKDAY, Fecha)
            )

            SELECT 
                DiaSemana,
                Fecha,
                SUM(Total) AS TotalMonto
            FROM Movimientos
            GROUP BY Fecha, DiaSemana
            ORDER BY Fecha;
        ", conn);

                cmd.Parameters.AddWithValue("@Lunes", lunes);
                cmd.Parameters.AddWithValue("@FinExclusivo", finExclusivo);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string dia = reader["DiaSemana"].ToString();
                        decimal monto = Convert.ToDecimal(reader["TotalMonto"]);
                        lista.Add((dia, monto));
                    }
                }
            }

            return lista;
        }
        //7. Obtener pagos por Mes (Anual)
        public ObservableCollection<(int Anio, int Mes, decimal Total)> ObtenerPagosPorMesDeCadaAnio()
        {
            var lista = new ObservableCollection<(int, int, decimal)>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand(@"
            SELECT
    ISNULL(p.Anio, v.Anio) AS Anio,
    ISNULL(p.Mes, v.Mes) AS Mes,
    ISNULL(p.TotalPagos, 0) AS TotalPagos,
    ISNULL(v.TotalVisitas, 0) AS TotalVisitas,
    ISNULL(p.TotalPagos, 0) + ISNULL(v.TotalVisitas, 0) AS TotalGeneral
FROM
    (
        SELECT 
            YEAR(Fecha) AS Anio,
            MONTH(Fecha) AS Mes,
            SUM(Monto) AS TotalPagos
        FROM Pagos
        GROUP BY YEAR(Fecha), MONTH(Fecha)
    ) p
FULL OUTER JOIN
    (
        SELECT 
            YEAR(Fecha) AS Anio,
            MONTH(Fecha) AS Mes,
            SUM(Precio) AS TotalVisitas
        FROM Visitas
        GROUP BY YEAR(Fecha), MONTH(Fecha)
    ) v
    ON p.Anio = v.Anio AND p.Mes = v.Mes
ORDER BY Anio, Mes;
", conn);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int anio = reader.GetInt32(0);
                        int mes = reader.GetInt32(1);
                        decimal total = reader.GetDecimal(2);
                        lista.Add((anio, mes, total));
                    }
                }
            }

            return lista;
        }
        //8. Total de Pago
        public decimal ObtenerTotal()
        {
            decimal total = 0;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand(@"
                SELECT 
                    (SELECT SUM(Monto) FROM Pagos) + 
                    (SELECT SUM(Precio) FROM Visitas) AS Total;        
                ", conn);

                object result = cmd.ExecuteScalar();
                if (result != DBNull.Value)
                    total = Convert.ToDecimal(result);
            }

            return total;
        }
        //9. Total de Pagos por día
        public decimal ObtenerTotalDias()
        {
            decimal total = 0;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand(@"
            SELECT 
                ISNULL((SELECT SUM(Monto) FROM Pagos WHERE CONVERT(date, Fecha) = CONVERT(date, GETDATE())), 0) +
                ISNULL((SELECT SUM(Precio) FROM Visitas WHERE CONVERT(date, Fecha) = CONVERT(date, GETDATE())), 0) 
            AS Total;
        ", conn);

                object result = cmd.ExecuteScalar();
                if (result != DBNull.Value)
                    total = Convert.ToDecimal(result);
            }

            return total;
        }
        //10. Total de Pagos por semana
        public decimal ObtenerTotalSemana()
        {
            decimal total = 0;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand(@"
          SELECT 
                ISNULL((SELECT SUM(Monto) FROM Pagos 
                        WHERE DATEPART(WEEK, Fecha) = DATEPART(WEEK, GETDATE()) 
                          AND YEAR(Fecha) = YEAR(GETDATE())), 0)
              +
                ISNULL((SELECT SUM(Precio) FROM Visitas 
                        WHERE DATEPART(WEEK, Fecha) = DATEPART(WEEK, GETDATE()) 
                          AND YEAR(Fecha) = YEAR(GETDATE())), 0)
              AS Total;
        ", conn);

                object result = cmd.ExecuteScalar();
                if (result != DBNull.Value)
                    total = Convert.ToDecimal(result);
            }

            return total;
        }
        //11. Total de Pagos por mes
        public decimal ObtenerTotalMes()
        {
            decimal total = 0;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand(@"
            SELECT 
                ISNULL((SELECT SUM(Monto) FROM Pagos 
                        WHERE MONTH(Fecha) = MONTH(GETDATE()) 
                          AND YEAR(Fecha) = YEAR(GETDATE())), 0)
              +
                ISNULL((SELECT SUM(Precio) FROM Visitas 
                        WHERE MONTH(Fecha) = MONTH(GETDATE()) 
                          AND YEAR(Fecha) = YEAR(GETDATE())), 0)
              AS Total;
        ", conn);

                object result = cmd.ExecuteScalar();
                if (result != DBNull.Value)
                    total = Convert.ToDecimal(result);
            }

            return total;
        }
        //12. Pagos Recientes (Top 10)
        public List<PagoReciente> ObtenerPagosRecientes()
        {
            List<PagoReciente> lista = new List<PagoReciente>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string query = @"SELECT TOP 10 
                            me.nombre AS NombreMembresia,
                            p.Monto,
                            p.Fecha
                         FROM Pagos p
                         INNER JOIN Membresias me ON p.IdMembresia = me.id
                         ORDER BY p.Fecha DESC";

                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        lista.Add(new PagoReciente
                        {
                            NombreMembresia = reader["NombreMembresia"].ToString(),
                            Monto = Convert.ToDecimal(reader["Monto"]),
                            Fecha = Convert.ToDateTime(reader["Fecha"])
                        });
                    }
                }
            }

            return lista;
        }

    }
}
