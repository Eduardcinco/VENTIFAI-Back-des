using Microsoft.EntityFrameworkCore;
using VentifyAPI.Models;
using VentifyAPI.Models.Reports;

namespace VentifyAPI.Data
{
    public class AppDbContext : DbContext
    {
        private readonly VentifyAPI.Services.ITenantContext _tenant;

        public AppDbContext(DbContextOptions<AppDbContext> options, VentifyAPI.Services.ITenantContext tenant) : base(options)
        {
            _tenant = tenant;
        }

        public DbSet<Negocio> Negocios { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<PuntoDeVenta> PuntosDeVenta { get; set; }
        public DbSet<Producto> Productos { get; set; }
        public DbSet<VarianteProducto> VariantesProducto { get; set; }
        public DbSet<Venta> Ventas { get; set; }
        public DbSet<DetalleVenta> DetallesVenta { get; set; }
        public DbSet<Proveedor> Proveedores { get; set; }
        public DbSet<Caja> Cajas { get; set; }
        public DbSet<MovimientoCaja> MovimientosCaja { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<MermaEvento> MermaEventos { get; set; }
        // Report views
        public DbSet<VentasPorDiaView> VentasPorDia { get; set; }
        public DbSet<VentasPorMesView> VentasPorMes { get; set; }
        public DbSet<VentasPorAnioView> VentasPorAnio { get; set; }
        public DbSet<InventarioPorCategoriaView> InventarioPorCategoria { get; set; }
        public DbSet<ProductoStockBajoView> ProductosStockBajo { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuración para tabla 'negocios'
            modelBuilder.Entity<Negocio>(entity =>
            {
                entity.ToTable("negocios");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.NombreNegocio).HasColumnName("nombre_negocio").HasMaxLength(150).IsRequired();
                entity.Property(e => e.OwnerId).HasColumnName("owner_id").IsRequired();
                entity.Property(e => e.CreadoEn).HasColumnName("creado_en");

                // Relaciones
                entity.HasMany(e => e.Usuarios)
                    .WithOne(u => u.Negocio)
                    .HasForeignKey(u => u.NegocioId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(e => e.PuntosDeVenta)
                    .WithOne(p => p.Negocio)
                    .HasForeignKey(p => p.NegocioId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.OwnerId);
            });

            // Configuración para tabla 'usuarios'
            modelBuilder.Entity<Usuario>(entity =>
            {
                entity.ToTable("usuarios");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.NegocioId).HasColumnName("negocio_id").IsRequired(false);
                entity.Property(e => e.Nombre).HasColumnName("nombre").HasMaxLength(100).IsRequired();
                entity.Property(e => e.Correo).HasColumnName("correo").HasMaxLength(100).IsRequired();
                entity.Property(e => e.Password).HasColumnName("password").HasMaxLength(255).IsRequired();
                entity.Property(e => e.Rol).HasColumnName("rol").HasDefaultValue("dueño");
                entity.Property(e => e.PrimerAcceso).HasColumnName("primer_acceso").HasDefaultValue(false);
                entity.Property(e => e.CreadoEn).HasColumnName("creado_en");

                // Índices
                entity.HasIndex(e => e.Correo).IsUnique();
                entity.HasIndex(e => e.NegocioId);
            });

            // Configuración para tabla 'puntos_de_venta'
            modelBuilder.Entity<PuntoDeVenta>(entity =>
            {
                entity.ToTable("puntos_de_venta");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.NombrePunto).HasColumnName("nombre_punto").HasMaxLength(150).IsRequired();
                entity.Property(e => e.NegocioId).HasColumnName("negocio_id").IsRequired();
            });

            // Configuración para tabla 'productos'
            modelBuilder.Entity<Producto>(entity =>
            {
                entity.ToTable("productos");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Nombre).HasColumnName("nombre").HasMaxLength(150).IsRequired();
                entity.Property(e => e.Descripcion).HasColumnName("descripcion");
                entity.Property(e => e.PrecioCompra).HasColumnName("precio_compra");
                entity.Property(e => e.PrecioVenta).HasColumnName("precio_venta");
                entity.Property(e => e.Categoria).HasColumnName("categoria");
                entity.Property(e => e.Subcategoria).HasColumnName("subcategoria");
                entity.Property(e => e.CategoryId).HasColumnName("category_id");
                entity.HasOne(e => e.Category)
                    .WithMany()
                    .HasForeignKey(e => e.CategoryId)
                    .OnDelete(DeleteBehavior.SetNull);
                entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");
                entity.HasOne(e => e.Usuario)
                    .WithMany()
                    .HasForeignKey(e => e.UsuarioId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.Property(e => e.NegocioId).HasColumnName("negocio_id");
                entity.Property(e => e.CantidadInicial).HasColumnName("cantidad_inicial");
                entity.Property(e => e.Merma).HasColumnName("merma");
                entity.Property(e => e.StockActual).HasColumnName("stock_actual");
                entity.Property(e => e.StockMinimo).HasColumnName("stock_minimo");
                entity.Property(e => e.UnidadMedida).HasColumnName("unidad_medida");
                entity.Property(e => e.CodigoBarras).HasColumnName("codigo_barras");
                entity.Property(e => e.ImagenUrl).HasColumnName("imagen_url");
                entity.Property(e => e.FechaRegistro).HasColumnName("fecha_registro");
                entity.Property(e => e.Activo).HasColumnName("activo");
                // Descuentos
                entity.Property(e => e.DescuentoPorcentaje).HasColumnName("descuento_porcentaje");
                entity.Property(e => e.DescuentoFechaInicio).HasColumnName("descuento_fecha_inicio");
                entity.Property(e => e.DescuentoFechaFin).HasColumnName("descuento_fecha_fin");
                entity.Property(e => e.DescuentoHoraInicio).HasColumnName("descuento_hora_inicio");
                entity.Property(e => e.DescuentoHoraFin).HasColumnName("descuento_hora_fin");
            });

            // Aplicar filtros globales para entidades multi-tenant
            // Producto: ahora tiene NegocioId directo; filtrar por esa columna cuando el tenant esté presente
            modelBuilder.Entity<Producto>().HasQueryFilter(p => _tenant.NegocioId == null || p.NegocioId == _tenant.NegocioId);
            // Usuario (empleados): permitir usuarios con NegocioId nulo cuando se requiera por flujo administrativo
            modelBuilder.Entity<Usuario>().HasQueryFilter(u => _tenant.NegocioId == null || u.NegocioId == _tenant.NegocioId);
            // Venta
            modelBuilder.Entity<Venta>().HasQueryFilter(v => _tenant.NegocioId == null || v.NegocioId == _tenant.NegocioId);
            // Cliente eliminado: sin filtro
            // Proveedor
            modelBuilder.Entity<Proveedor>().HasQueryFilter(p => _tenant.NegocioId == null || p.NegocioId == _tenant.NegocioId);
            // Caja
            modelBuilder.Entity<Caja>().HasQueryFilter(c => _tenant.NegocioId == null || c.NegocioId == _tenant.NegocioId);
            // MovimientoCaja
            modelBuilder.Entity<MovimientoCaja>().HasQueryFilter(m => _tenant.NegocioId == null || m.NegocioId == _tenant.NegocioId);
            // MermaEvento
            modelBuilder.Entity<MermaEvento>().HasQueryFilter(m => _tenant.NegocioId == null || m.NegocioId == _tenant.NegocioId);
            
            // Filtros para entidades relacionadas (evita warnings de EF Core)
            // Category: relacionada con Usuario
            modelBuilder.Entity<Category>().HasQueryFilter(c => _tenant.NegocioId == null || c.Usuario != null && c.Usuario.NegocioId == _tenant.NegocioId);
            // DetalleVenta: relacionada con Venta (que ya tiene filtro) y Producto (que ya tiene filtro)
            modelBuilder.Entity<DetalleVenta>().HasQueryFilter(d => _tenant.NegocioId == null || d.Venta != null && d.Venta.NegocioId == _tenant.NegocioId);
            // RefreshToken: relacionada con Usuario
            modelBuilder.Entity<RefreshToken>().HasQueryFilter(r => _tenant.NegocioId == null || r.Usuario != null && r.Usuario.NegocioId == _tenant.NegocioId);
            // VarianteProducto: relacionada con Producto
            modelBuilder.Entity<VarianteProducto>().HasQueryFilter(v => _tenant.NegocioId == null || v.Producto != null && v.Producto.NegocioId == _tenant.NegocioId);

            // Configuración para tabla 'variantes_producto'
            modelBuilder.Entity<VarianteProducto>(entity =>
            {
                entity.ToTable("variantes_producto");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.ProductoId).HasColumnName("producto_id");
                entity.Property(e => e.Nombre).HasColumnName("nombre");
                entity.Property(e => e.Precio).HasColumnName("precio");
                entity.Property(e => e.Stock).HasColumnName("stock");
                entity.Property(e => e.Codigo).HasColumnName("codigo");
                entity.HasOne(e => e.Producto)
                    .WithMany(p => p.Variantes)
                    .HasForeignKey(e => e.ProductoId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configuración para tabla 'ventas'
            modelBuilder.Entity<Venta>(entity =>
            {
                entity.ToTable("ventas");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.NegocioId).HasColumnName("negocio_id");
                entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");
                // Cliente eliminado: no mapear cliente_id
                entity.Property(e => e.TotalPagado).HasColumnName("total_pagado");
                entity.Property(e => e.FormaPago).HasColumnName("forma_pago");
                entity.Property(e => e.MontoRecibido).HasColumnName("monto_recibido");
                entity.Property(e => e.Cambio).HasColumnName("cambio");
                entity.Property(e => e.FechaHora).HasColumnName("fecha_hora");
                entity.Property(e => e.Ticket).HasColumnName("ticket");
                entity.HasOne(e => e.Negocio)
                    .WithMany()
                    .HasForeignKey(e => e.NegocioId);
                entity.HasOne(e => e.Usuario)
                    .WithMany()
                    .HasForeignKey(e => e.UsuarioId);
            });

            // Configuración para tabla 'detalles_venta'
            modelBuilder.Entity<DetalleVenta>(entity =>
            {
                entity.ToTable("detalles_venta");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.VentaId).HasColumnName("venta_id");
                entity.Property(e => e.ProductoId).HasColumnName("producto_id");
                entity.Property(e => e.VarianteProductoId).HasColumnName("variante_producto_id");
                entity.Property(e => e.Cantidad).HasColumnName("cantidad");
                entity.Property(e => e.PrecioUnitario).HasColumnName("precio_unitario");
                entity.Property(e => e.Subtotal).HasColumnName("subtotal");
                entity.HasOne(e => e.Venta)
                    .WithMany(v => v.Detalles)
                    .HasForeignKey(e => e.VentaId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Producto)
                    .WithMany()
                    .HasForeignKey(e => e.ProductoId);
                entity.HasOne(e => e.VarianteProducto)
                    .WithMany()
                    .HasForeignKey(e => e.VarianteProductoId);
            });

            // Configuración para tabla 'categories'
            modelBuilder.Entity<Category>(entity =>
            {
                entity.ToTable("categories");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
                entity.Property(e => e.ParentId).HasColumnName("parent_id");
                entity.Property(e => e.UsuarioId).HasColumnName("usuario_id").IsRequired();

                entity.HasOne(e => e.Parent)
                    .WithMany(e => e.Children)
                    .HasForeignKey(e => e.ParentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Usuario)
                    .WithMany()
                    .HasForeignKey(e => e.UsuarioId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configuración para tabla 'refresh_tokens'
            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.ToTable("refresh_tokens");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.UsuarioId).HasColumnName("usuario_id").IsRequired();
                entity.Property(e => e.Token).HasColumnName("token").HasMaxLength(512).IsRequired();
                entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
                entity.Property(e => e.Revoked).HasColumnName("revoked");
                entity.HasIndex(e => new { e.UsuarioId, e.Revoked });
            });

            // Configuración para tabla 'merma_eventos'
            modelBuilder.Entity<MermaEvento>(entity =>
            {
                entity.ToTable("merma_eventos");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.ProductoId).HasColumnName("producto_id");
                entity.Property(e => e.Cantidad).HasColumnName("cantidad");
                entity.Property(e => e.Motivo).HasColumnName("motivo");
                entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");
                entity.Property(e => e.NegocioId).HasColumnName("negocio_id");
                entity.Property(e => e.FechaUtc).HasColumnName("fecha_utc");
                entity.Property(e => e.StockAntes).HasColumnName("stock_antes");
                entity.Property(e => e.StockDespues).HasColumnName("stock_despues");
                entity.Property(e => e.MermaAntes).HasColumnName("merma_antes");
                entity.Property(e => e.MermaDespues).HasColumnName("merma_despues");

                entity.HasOne(e => e.Producto)
                    .WithMany()
                    .HasForeignKey(e => e.ProductoId)
                    .OnDelete(Microsoft.EntityFrameworkCore.DeleteBehavior.Cascade);
                entity.HasOne(e => e.Usuario)
                    .WithMany()
                    .HasForeignKey(e => e.UsuarioId)
                    .OnDelete(Microsoft.EntityFrameworkCore.DeleteBehavior.Restrict);
            });

            // Tabla 'clientes' eliminada del modelo

            // Configuración para tabla 'proveedores'
            modelBuilder.Entity<Proveedor>(entity =>
            {
                entity.ToTable("proveedores");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id_proveedor");
                entity.Property(e => e.Nombre).HasColumnName("nombre").HasMaxLength(150).IsRequired();
                entity.Property(e => e.Correo).HasColumnName("correo");
                entity.Property(e => e.Telefono).HasColumnName("telefono");
                entity.Property(e => e.Direccion).HasColumnName("direccion");
                entity.Property(e => e.NegocioId).HasColumnName("negocio_id");
            });

            // Configuración para tabla 'cajas'
            modelBuilder.Entity<Caja>(entity =>
            {
                entity.ToTable("cajas");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id_caja");
                entity.Property(e => e.NegocioId).HasColumnName("negocio_id");
                entity.Property(e => e.UsuarioAperturaId).HasColumnName("usuario_apertura_id");
                entity.Property(e => e.FechaApertura).HasColumnName("fecha_apertura");
                entity.Property(e => e.MontoInicial).HasColumnName("monto_inicial");
                entity.Property(e => e.MontoActual).HasColumnName("monto_actual");
                entity.Property(e => e.Abierta).HasColumnName("abierta");
                entity.Property(e => e.FechaCierre).HasColumnName("fecha_cierre");
                entity.Property(e => e.MontoCierre).HasColumnName("monto_cierre");
                entity.Property(e => e.ResumenCierre).HasColumnName("resumen_cierre");
                entity.Property(e => e.AbiertaPor).HasColumnName("abierta_por").HasMaxLength(150);
                entity.Property(e => e.Turno).HasColumnName("turno").HasMaxLength(50);
                entity.Property(e => e.UsuarioCierreId).HasColumnName("usuario_cierre_id");
            });

            // Configuración para tabla 'movimientos_caja'
            modelBuilder.Entity<MovimientoCaja>(entity =>
            {
                entity.ToTable("movimientos_caja");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.CajaId).HasColumnName("caja_id");
                entity.Property(e => e.NegocioId).HasColumnName("negocio_id");
                entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");
                entity.Property(e => e.Tipo).HasColumnName("tipo").HasMaxLength(20).IsRequired();
                entity.Property(e => e.Monto).HasColumnName("monto").IsRequired();
                entity.Property(e => e.Categoria).HasColumnName("categoria").HasMaxLength(100).IsRequired();
                entity.Property(e => e.Descripcion).HasColumnName("descripcion").HasMaxLength(500);
                entity.Property(e => e.MetodoPago).HasColumnName("metodo_pago").HasMaxLength(50);
                entity.Property(e => e.FechaHora).HasColumnName("fecha_hora");
                entity.Property(e => e.SaldoDespues).HasColumnName("saldo_despues");
                entity.Property(e => e.Referencia).HasColumnName("referencia").HasMaxLength(100);

                entity.HasOne(e => e.Caja)
                    .WithMany()
                    .HasForeignKey(e => e.CajaId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Usuario)
                    .WithMany()
                    .HasForeignKey(e => e.UsuarioId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => new { e.CajaId, e.FechaHora });
                entity.HasIndex(e => new { e.NegocioId, e.FechaHora });
            });

            // Vistas de reportes (keyless)
            modelBuilder.Entity<VentasPorDiaView>().HasNoKey().ToView("vw_ventas_por_dia");
            modelBuilder.Entity<VentasPorMesView>().HasNoKey().ToView("vw_ventas_por_mes");
            modelBuilder.Entity<VentasPorAnioView>().HasNoKey().ToView("vw_ventas_por_anio");
            modelBuilder.Entity<InventarioPorCategoriaView>().HasNoKey().ToView("vw_inventario_por_categoria");
            modelBuilder.Entity<ProductoStockBajoView>().HasNoKey().ToView("vw_productos_stock_bajo");
        }
    }
}
