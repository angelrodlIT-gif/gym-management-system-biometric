using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gym_System.Core 
{ 

    public class Miembros_agregar
    {
        public long id { get; set; }
        public string Nombre { get; set; }
        public string Telefono { get; set; }
        public string Direccion { get; set; }
        public int Edad { get; set; }
        public string Estado { get; set; }
        public DateTime FechaRegistro { get; set; }
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public long? IdMembresia { get; set; }
        public string TemplateHuella { get; set; }
        public string NombreMembresia { get; set; }



        public Miembros_agregar() { }
        public Miembros_agregar(long id, string nombre, string telefono, string direccion, int edad, string estado, DateTime fechaRegistro, DateTime? fechaInicio, DateTime? fechaFin, long? idMembresia, string templateHuella)
        {
            this.id = id;
            Nombre = nombre;
            Telefono = telefono;
            Direccion = direccion;
            Edad = edad;
            Estado = estado;
            FechaRegistro = fechaRegistro;
            FechaInicio = fechaInicio;
            FechaFin = fechaFin;
            IdMembresia = idMembresia;
            TemplateHuella = templateHuella;
        }
    }
}

