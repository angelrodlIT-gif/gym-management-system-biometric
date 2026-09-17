using System;

namespace Gym_System.Core
{
    /// <summary>
    /// Provee lógica desacoplada y determinista para calcular la vigencia de membresías,
    /// preservando días activos restantes en renovaciones anticipadas.
    /// </summary>
    public static class VigenciaCalculador
    {
        /// <summary>
        /// Calcula la fecha de inicio y fin de una membresía considerando la vigencia restante del miembro.
        /// Si el miembro aún cuenta con días activos (fechaFinActual > fechaPago), la duración se suma
        /// a partir de la vigencia restante para no perder los días previos.
        /// </summary>
        /// <param name="fechaInicioActual">Fecha de inicio previa del miembro (si existe).</param>
        /// <param name="fechaFinActual">Fecha de fin previa del miembro (si existe).</param>
        /// <param name="fechaPago">Fecha en que se registra/aplica el pago (generalmente la fecha seleccionada o actual).</param>
        /// <param name="duracion">Valor numérico de la duración (ej. 1, 15, 30).</param>
        /// <param name="unidadDuracion">"Meses" o "Días".</param>
        /// <returns>Tupla con la nueva fecha de inicio y la nueva fecha de fin calculadas.</returns>
        public static (DateTime FechaInicio, DateTime FechaFin) CalcularVigencia(
            DateTime? fechaInicioActual,
            DateTime? fechaFinActual,
            DateTime fechaPago,
            int duracion,
            string unidadDuracion)
        {
            DateTime fechaInicio;
            DateTime fechaFin;

            bool tieneVigenciaRestante = fechaFinActual.HasValue && fechaFinActual.Value > fechaPago;

            if (tieneVigenciaRestante)
            {
                // Preservar la fecha de inicio original si ya era activa; en caso de inconsistencia usar fechaPago
                fechaInicio = (fechaInicioActual.HasValue && fechaInicioActual.Value <= fechaPago)
                    ? fechaInicioActual.Value
                    : (fechaInicioActual ?? fechaPago);

                // Sumar duración a partir de la vigencia restante (fechaFinActual)
                DateTime baseFin = fechaFinActual.Value;
                if (string.Equals(unidadDuracion, "Meses", StringComparison.OrdinalIgnoreCase))
                {
                    fechaFin = baseFin.AddMonths(duracion);
                }
                else
                {
                    fechaFin = baseFin.AddDays(duracion);
                }
            }
            else
            {
                // Miembro nuevo o con membresía vencida: vigencia inicia a partir de la fecha de pago
                fechaInicio = fechaPago;

                if (string.Equals(unidadDuracion, "Meses", StringComparison.OrdinalIgnoreCase))
                {
                    fechaFin = fechaInicio.AddMonths(duracion);
                }
                else
                {
                    fechaFin = fechaInicio.AddDays(duracion);
                }
            }

            return (fechaInicio, fechaFin);
        }
    }
}
