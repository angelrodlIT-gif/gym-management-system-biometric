using System;

namespace Gym_System.Core
{
    /// <summary>
    /// Contrato y seam neutral para notificar cambios en miembros o membresías a capas superiores
    /// (como la caché biométrica o adaptadores de UI) sin crear dependencias inversas desde el Core.
    /// </summary>
    public static class NotificadorCambioMiembro
    {
        /// <summary>
        /// Delegado opcional registrado por capas superiores (ej. UI o BiometricApp).
        /// </summary>
        public static Action OnMiembroModificado { get; set; }

        /// <summary>
        /// Invoca el delegado de notificación si está registrado.
        /// No requiere bloques catch vacíos ni referencias a bibliotecas externas.
        /// </summary>
        public static void Notificar()
        {
            OnMiembroModificado?.Invoke();
        }
    }
}
