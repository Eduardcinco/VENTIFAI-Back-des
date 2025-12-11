namespace VentifyAPI.Models.Reports
{
    public class VentasPorMesView
    {
        public DateTime Mes { get; set; }
        public int NegocioId { get; set; }
        public int VentasCount { get; set; }
        public decimal TotalPagado { get; set; }
    }
}
