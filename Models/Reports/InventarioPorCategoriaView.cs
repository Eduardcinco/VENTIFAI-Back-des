namespace VentifyAPI.Models.Reports
{
    public class InventarioPorCategoriaView
    {
        public int? CategoryId { get; set; }
        public string? Categoria { get; set; }
        public int NegocioId { get; set; }
        public int ProductosCount { get; set; }
        public int StockTotal { get; set; }
        public int CantidadInicialTotal { get; set; }
        public int MermaTotal { get; set; }
    }
}
