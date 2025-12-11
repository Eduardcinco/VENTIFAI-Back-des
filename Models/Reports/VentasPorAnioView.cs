namespace VentifyAPI.Models.Reports
{
    public class VentasPorAnioView
    {
        public DateTime Anio { get; set; }
        public int NegocioId { get; set; }
        public int VentasCount { get; set; }
        public decimal TotalPagado { get; set; }
    }
}
