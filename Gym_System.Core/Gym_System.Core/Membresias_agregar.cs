using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gym_System.Core
{
    public class Membresias_agregar
    {
        public long id { get; set; }
        public string Nombre { get; set; }
        public int Duracion { get; set; }
        public decimal Precio { get; set; }
        public string UnidadDuracion { get; set; } // "Días" o "Meses"


        public Membresias_agregar() { }

        public Membresias_agregar(long id, string nombre, int duracion, decimal precio, string unidadDuracion)
        {
            this.id = id;
            Nombre = nombre;
            Duracion = duracion;
            Precio = precio;
            UnidadDuracion = unidadDuracion;
        }
    }
}
