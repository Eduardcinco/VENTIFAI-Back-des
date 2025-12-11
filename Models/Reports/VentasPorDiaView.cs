namespace VentifyAPI.Models.Reports
{
    public class VentasPorDiaView
    {
        public DateTime Dia { get; set; }
        public int NegocioId { get; set; }
        public int VentasCount { get; set; }
        public decimal TotalPagado { get; set; }
    }
}
