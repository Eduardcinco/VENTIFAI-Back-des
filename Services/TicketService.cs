using System.Text;
using Microsoft.EntityFrameworkCore;
using VentifyAPI.Data;

namespace VentifyAPI.Services
{
    public class TicketService
    {
        private readonly AppDbContext _context;

        public TicketService(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Genera un ticket de 58mm (32 columnas) en texto plano monoespaciado
        /// </summary>
        public async Task<string> GenerateTicketTextAsync(int ventaId, int negocioId)
        {
            var venta = await _context.Ventas
                .Include(v => v.Detalles)
                .Include(v => v.Usuario)
                .FirstOrDefaultAsync(v => v.Id == ventaId && v.NegocioId == negocioId);

            if (venta == null)
                throw new InvalidOperationException("Venta no encontrada");

            var negocio = await _context.Negocios.FindAsync(venta.NegocioId);
            var nombreNegocio = negocio?.NombreNegocio ?? "VENTIFY";
            var cajero = venta.Usuario?.Nombre ?? "Cajero";
            // Cliente eliminado

            // 58mm => ~32 chars por línea
            const int width = 32;
            var sb = new StringBuilder();

            sb.AppendLine(Center(nombreNegocio, width).ToUpper());
            sb.AppendLine(Center($"Ticket #{venta.Id}", width));
            sb.AppendLine(Center(venta.FechaHora.ToString("dd/MM/yyyy HH:mm"), width));
            sb.AppendLine(new string('-', width));
            sb.AppendLine(AlignKV("Cajero", cajero, width));
            // Cliente eliminado
            sb.AppendLine(AlignKV("Pago", venta.FormaPago ?? "N/A", width));
            sb.AppendLine(new string('-', width));

            decimal subtotal = 0m;
            foreach (var d in venta.Detalles)
            {
                var nombre = d.Producto?.Nombre ?? $"Producto {d.ProductoId}";
                var lineaTitulo = Truncate(nombre, width);
                sb.AppendLine(lineaTitulo);
                var importe = d.Subtotal;
                subtotal += importe;
                sb.AppendLine(AlignKV($"{d.Cantidad} x {d.PrecioUnitario:N2}", $"${importe:N2}", width));
            }

            sb.AppendLine(new string('-', width));
            var iva = subtotal * 0.16m;
            var total = venta.TotalPagado;
            sb.AppendLine(AlignKV("Subtotal", $"${subtotal:N2}", width));
            sb.AppendLine(AlignKV("IVA (16%)", $"${iva:N2}", width));
            sb.AppendLine(AlignKV("TOTAL", $"${total:N2}", width));
            if (venta.MontoRecibido.HasValue)
                sb.AppendLine(AlignKV("Recibido", $"${venta.MontoRecibido.Value:N2}", width));
            if (venta.Cambio.HasValue)
                sb.AppendLine(AlignKV("Cambio", $"${venta.Cambio.Value:N2}", width));

            sb.AppendLine(new string('-', width));
            sb.AppendLine(Center("Gracias por su compra", width));
            sb.AppendLine(Center("ventify.mx", width));

            return sb.ToString();
        }

        /// <summary>
        /// Genera un HTML simple (58mm/80mm) para imprimir desde el navegador
        /// </summary>
                public async Task<string> GenerateTicketHtmlAsync(int ventaId, int negocioId)
                {
                        var text = await GenerateTicketTextAsync(ventaId, negocioId);
                        var encoded = System.Net.WebUtility.HtmlEncode(text);
                        var sb = new StringBuilder();
                        sb.AppendLine("<!DOCTYPE html>");
                        sb.AppendLine("<html>");
                        sb.AppendLine("<head>");
                        sb.AppendLine("  <meta charset=\"utf-8\" />");
                        sb.AppendLine($"  <title>Ticket {ventaId}</title>");
                        sb.AppendLine("  <style>");
                        sb.AppendLine("    @page { margin: 5mm; }");
                        sb.AppendLine("    body { font-family: 'Courier New', monospace; font-size: 12px; }");
                        sb.AppendLine("    .ticket { width: 58mm; max-width: 58mm; white-space: pre-wrap; }");
                        sb.AppendLine("    @media print { .no-print { display: none; } }");
                        sb.AppendLine("  </style>");
                        sb.AppendLine("</head>");
                        sb.AppendLine("<body>");
                        sb.AppendLine($"  <div class=\"ticket\">{encoded}</div>");
                        sb.AppendLine("  <button class=\"no-print\" onclick=\"window.print()\">Imprimir</button>");
                        sb.AppendLine("  <script>window.onload = () => window.print();</script>");
                        sb.AppendLine("</body>");
                        sb.AppendLine("</html>");
                        return sb.ToString();
                }

        private static string Truncate(string s, int max)
            => s.Length <= max ? s : s.Substring(0, max);

        private static string Center(string s, int width)
        {
            if (s.Length >= width) return Truncate(s, width);
            var pad = (width - s.Length) / 2;
            return new string(' ', pad) + s + new string(' ', width - s.Length - pad);
        }

        private static string AlignKV(string key, string value, int width)
        {
            key = key ?? string.Empty; value = value ?? string.Empty;
            var raw = $"{key}: {value}";
            if (raw.Length <= width)
            {
                var spaces = width - raw.Length;
                return key + ":" + new string(' ', spaces - 1) + value;
            }
            // si overflow, recortar key
            var maxKey = Math.Max(0, width - (value.Length + 1));
            key = Truncate(key, maxKey);
            var spaces2 = Math.Max(1, width - (key.Length + 1 + value.Length));
            return key + ":" + new string(' ', spaces2) + value;
        }
    }
}
