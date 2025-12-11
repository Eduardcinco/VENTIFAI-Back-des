namespace VentifyAPI.DTOs
{
    /// <summary>
    /// DTO para datos agregados de ventas en reportes
    /// </summary>
    public class ReporteVentasAgregadoDTO
    {
        public string Periodo { get; set; } = string.Empty;
        public DateTime FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public int TotalVentas { get; set; }
        public decimal TotalIngresos { get; set; }
        public decimal TotalSubtotal { get; set; }
        public decimal TotalIva { get; set; }
        public decimal TotalDescuentos { get; set; }
        public decimal TicketPromedio { get; set; }
        public decimal VentaMaxima { get; set; }
        public decimal VentaMinima { get; set; }
        public int CajerosActivos { get; set; }
        // Clientes eliminados
        public int ClientesUnicos { get; set; } = 0;
        
        // Desglose por método de pago
        public decimal TotalEfectivo { get; set; }
        public decimal TotalTarjeta { get; set; }
        public decimal TotalTransferencia { get; set; }
        public int VentasEfectivo { get; set; }
        public int VentasTarjeta { get; set; }
        public int VentasTransferencia { get; set; }
    }

    /// <summary>
    /// DTO para detalle de ventas individuales
    /// </summary>
    public class ReporteVentaDetalleDTO
    {
        public int VentaId { get; set; }
        public string Folio { get; set; } = string.Empty;
        public DateTime FechaVenta { get; set; }
        public decimal Total { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Iva { get; set; }
        public decimal DescuentoAplicado { get; set; }
        public string MetodoPago { get; set; } = string.Empty;
        public string TipoVenta { get; set; } = string.Empty;
        public string NombreCajero { get; set; } = string.Empty;
        // Clientes eliminados
        public string? NombreCliente { get; set; }
        public string? TelefonoCliente { get; set; }
        public int CantidadProductos { get; set; }
        public List<ReporteProductoVendidoDTO> Productos { get; set; } = new();
    }

    /// <summary>
    /// DTO para productos vendidos en detalle
    /// </summary>
    public class ReporteProductoVendidoDTO
    {
        public string ProductoNombre { get; set; } = string.Empty;
        public string? CodigoBarras { get; set; }
        public string? CategoriaNombre { get; set; }
        public string? VarianteNombre { get; set; }
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal SubtotalDetalle { get; set; }
        public decimal DescuentoDetalle { get; set; }
    }

    /// <summary>
    /// DTO para productos más vendidos
    /// </summary>
    public class ProductoMasVendidoDTO
    {
        public int ProductoId { get; set; }
        public string ProductoNombre { get; set; } = string.Empty;
        public string? CodigoBarras { get; set; }
        public string? CategoriaNombre { get; set; }
        public int CantidadVendida { get; set; }
        public decimal TotalVentas { get; set; }
        public int NumeroTransacciones { get; set; }
        public decimal PrecioPromedio { get; set; }
    }

    /// <summary>
    /// DTO principal que encapsula todo el reporte
    /// </summary>
    public class ReporteVentasCompletoDTO
    {
        public string NombreNegocio { get; set; } = string.Empty;
        public DateTime FechaGeneracion { get; set; }
        public string TipoReporte { get; set; } = string.Empty;
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        
        // Ventana real aplicada (p.ej. caja abierta)
        public bool ModoCajaAbierta { get; set; }
        public DateTime InicioReal { get; set; }
        public DateTime FinReal { get; set; }
        
        // Resumen general
        public ReporteVentasAgregadoDTO ResumenGeneral { get; set; } = new();
        
        // Datos por período
        public List<ReporteVentasAgregadoDTO> DatosPorPeriodo { get; set; } = new();
        
        // Top productos
        public List<ProductoMasVendidoDTO> TopProductos { get; set; } = new();
        
        // Ventas detalladas (opcional, para reportes detallados)
        public List<ReporteVentaDetalleDTO>? VentasDetalladas { get; set; }
    }
}
