using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gym_System.Core
{
    public class Visitas_agregar
    {
        public string Nombre { get; set; }
        public decimal Precio { get; set; }
        public DateTime? Fecha { get; set; }
        public long id { get; set; }



        public Visitas_agregar() { }

        public Visitas_agregar(long id, string nombre, DateTime fecha, decimal precio)
        {
            this.id = id;
            Nombre = nombre;
            Fecha = fecha;
            Precio = precio;
        }
    }
}
