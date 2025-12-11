namespace VentifyAPI.Models.Reports
{
    public class ProductoStockBajoView
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public int StockActual { get; set; }
        public int StockMinimo { get; set; }
        public int NegocioId { get; set; }
    }
}
