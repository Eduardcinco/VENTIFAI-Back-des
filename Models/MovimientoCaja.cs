using System;

namespace VentifyAPI.Models
{
    /// <summary>
    /// Movimiento de caja: entrada o salida de dinero
    /// </summary>
    public class MovimientoCaja
    {
        public int Id { get; set; }
        public int CajaId { get; set; }
        public Caja? Caja { get; set; }
        
        public int NegocioId { get; set; }
        public int UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }
        
        /// <summary>
        /// Tipo: "entrada" o "salida"
        /// </summary>
        public string Tipo { get; set; } = string.Empty;
        
        /// <summary>
        /// Monto del movimiento (siempre positivo, el tipo indica entrada/salida)
        /// </summary>
        public decimal Monto { get; set; }
        
        /// <summary>
        /// Categoría del movimiento
        /// Ejemplos: "Pago de Renta", "Pago de Internet", "Pago de Luz", "Préstamo", "Ingreso Extra", "Venta", "Retiro Efectivo", etc.
        /// </summary>
        public string Categoria { get; set; } = string.Empty;
        
        /// <summary>
        /// Descripción detallada del movimiento
        /// </summary>
        public string? Descripcion { get; set; }
        
        /// <summary>
        /// Método de pago: "Efectivo", "Transferencia", "Cheque", etc.
        /// </summary>
        public string? MetodoPago { get; set; }
        
        /// <summary>
        /// Fecha y hora del movimiento
        /// </summary>
        public DateTime FechaHora { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Balance de caja después del movimiento (para auditoría)
        /// </summary>
        public decimal SaldoDespues { get; set; }
        
        /// <summary>
        /// Comprobante o referencia del movimiento (opcional)
        /// </summary>
        public string? Referencia { get; set; }
    }
}
