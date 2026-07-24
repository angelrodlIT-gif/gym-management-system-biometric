using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gym_System.Core
{
    public class Pagos_agregar
    {

        public string NombreMembresia { get; set; }
        public decimal Precio { get; set; }
        public int TotalUsuarios { get; set; }
        public decimal TotalEsperado { get; set; }
        public decimal Monto { get; internal set; }
        public string NombreMembresia2 { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }


    }
    public class PagoReciente
    {
        public string NombreMiembro { get; set; }
        public string NombreMembresia { get; set; }
        public decimal Monto { get; set; }
        public DateTime Fecha { get; set; }

        public string TiempoRelativo
        {
            get
            {
                TimeSpan diferencia = DateTime.Now - Fecha;

                if (diferencia.TotalMinutes < 1)
                    return "Hace unos segundos";
                if (diferencia.TotalMinutes < 60)
                    return $"Hace {(int)diferencia.TotalMinutes} minutos";
                if (diferencia.TotalHours < 24)
                    return $"Hace {(int)diferencia.TotalHours} horas";

                return Fecha.ToString("dd/MM/yyyy");
            }
        }
    }
}