namespace VentifyAPI.DTOs
{
    // DTO para registrar merma. Se acepta 'Incremento' o alias 'Cantidad'.
    public class MermaRequestDTO
    {
        public int? Incremento { get; set; }
        public int? Cantidad { get; set; } // alias opcional usado en algunos fronts
        public string? Motivo { get; set; }
    }
}
