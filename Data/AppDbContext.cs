
using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using SistemaInventario.Models;

namespace SistemaInventario.Data;

public partial class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<categoria> categorias { get; set; }

    public virtual DbSet<cliente> clientes { get; set; }

    public virtual DbSet<compra> compras { get; set; }

    public virtual DbSet<detalle_compra> detalle_compras { get; set; }

    public virtual DbSet<detalle_venta> detalle_ventas { get; set; }

    public virtual DbSet<inventario> inventarios { get; set; }

    public virtual DbSet<producto> productos { get; set; }

    public virtual DbSet<proveedor> proveedores { get; set; }

    public virtual DbSet<rol> roles { get; set; }

    public virtual DbSet<usuario> usuarios { get; set; }

    public virtual DbSet<venta> ventas { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_general_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<categoria>(entity =>
        {
            entity.HasKey(e => e.idcategoria).HasName("PRIMARY");

            entity.ToTable("categoria");

            entity.HasIndex(e => e.nombre, "nombre").IsUnique();

            entity.Property(e => e.idcategoria).HasColumnType("int(11)");
            entity.Property(e => e.descripcion).HasMaxLength(255);
            entity.Property(e => e.nombre).HasMaxLength(100);
            entity.Property(e => e.activo).HasDefaultValue(true);
        });

        modelBuilder.Entity<cliente>(entity =>
        {
            entity.HasKey(e => e.idcliente).HasName("PRIMARY");

            entity.ToTable("cliente");

            entity.HasIndex(e => e.documento, "documento").IsUnique();

            entity.Property(e => e.idcliente).HasColumnType("int(11)");
            entity.Property(e => e.apellido).HasMaxLength(100);
            entity.Property(e => e.correo).HasMaxLength(120);
            entity.Property(e => e.documento).HasMaxLength(30);
            entity.Property(e => e.nombre).HasMaxLength(100);
            entity.Property(e => e.telefono).HasMaxLength(30);
            entity.Property(e => e.activo).HasDefaultValue(true);
        });

        modelBuilder.Entity<compra>(entity =>
        {
            entity.HasKey(e => e.idcompra).HasName("PRIMARY");

            entity.ToTable("compra");

            entity.HasIndex(e => e.proveedor_idproveedor, "proveedor_idproveedor");
            entity.HasIndex(e => e.usuario_idusuario, "usuario_idusuario");

            entity.Property(e => e.idcompra).HasColumnType("int(11)");
            entity.Property(e => e.fecha_compra)
                .HasDefaultValueSql("current_timestamp()")
                .HasColumnType("datetime");
            entity.Property(e => e.numero_factura).HasMaxLength(50);
            entity.Property(e => e.proveedor_idproveedor).HasColumnType("int(11)");
            entity.Property(e => e.total).HasPrecision(12, 2);
            entity.Property(e => e.usuario_idusuario).HasColumnType("int(11)");

            entity.HasOne(d => d.proveedor_idproveedorNavigation)
                .WithMany(p => p.compras)
                .HasForeignKey(d => d.proveedor_idproveedor)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("compra_ibfk_1");

            entity.HasOne(d => d.usuario_idusuarioNavigation)
                .WithMany(p => p.compras)
                .HasForeignKey(d => d.usuario_idusuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("compra_ibfk_2");
        });

        modelBuilder.Entity<detalle_compra>(entity =>
        {
            entity.HasKey(e => e.iddetalle_compra).HasName("PRIMARY");

            entity.ToTable("detalle_compra");

            entity.HasIndex(e => e.compra_idcompra, "compra_idcompra");
            entity.HasIndex(e => e.producto_idproducto, "producto_idproducto");

            entity.Property(e => e.iddetalle_compra).HasColumnType("int(11)");
            entity.Property(e => e.cantidad).HasColumnType("int(11)");
            entity.Property(e => e.compra_idcompra).HasColumnType("int(11)");
            entity.Property(e => e.costo_unitario).HasPrecision(10, 2);
            entity.Property(e => e.producto_idproducto).HasColumnType("int(11)");
            entity.Property(e => e.subtotal).HasPrecision(12, 2);

            entity.HasOne(d => d.compra_idcompraNavigation)
                .WithMany(p => p.detalle_compras)
                .HasForeignKey(d => d.compra_idcompra)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("detalle_compra_ibfk_1");

            entity.HasOne(d => d.producto_idproductoNavigation)
                .WithMany(p => p.detalle_compras)
                .HasForeignKey(d => d.producto_idproducto)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("detalle_compra_ibfk_2");
        });

        modelBuilder.Entity<detalle_venta>(entity =>
        {
            entity.HasKey(e => e.iddetalle_venta).HasName("PRIMARY");

            entity.ToTable("detalle_venta");

            entity.HasIndex(e => e.inventario_idinventario, "inventario_idinventario").IsUnique();
            entity.HasIndex(e => e.venta_idventa, "venta_idventa");

            entity.Property(e => e.iddetalle_venta).HasColumnType("int(11)");
            entity.Property(e => e.cantidad)
                .HasDefaultValueSql("'1'")
                .HasColumnType("int(11)");
            entity.Property(e => e.inventario_idinventario).HasColumnType("int(11)");
            entity.Property(e => e.precio_unitario).HasPrecision(10, 2);
            entity.Property(e => e.subtotal).HasPrecision(12, 2);
            entity.Property(e => e.venta_idventa).HasColumnType("int(11)");

            entity.HasOne(d => d.inventario_idinventarioNavigation)
                .WithOne(p => p.detalle_venta)
                .HasForeignKey<detalle_venta>(d => d.inventario_idinventario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("detalle_venta_ibfk_2");

            entity.HasOne(d => d.venta_idventaNavigation)
                .WithMany(p => p.detalle_venta)
                .HasForeignKey(d => d.venta_idventa)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("detalle_venta_ibfk_1");
        });

        modelBuilder.Entity<inventario>(entity =>
        {
            entity.HasKey(e => e.idinventario).HasName("PRIMARY");

            entity.ToTable("inventario");

            entity.HasIndex(e => e.imei, "imei").IsUnique();
            entity.HasIndex(e => e.numero_serie, "numero_serie").IsUnique();
            entity.HasIndex(e => e.producto_idproducto, "producto_idproducto");

            entity.Property(e => e.idinventario).HasColumnType("int(11)");
            entity.Property(e => e.estado)
                .HasDefaultValueSql("'Disponible'")
                .HasColumnType("enum('Disponible','Vendido','Dañado','Reservado')");
            entity.Property(e => e.fecha_ingreso)
                .HasDefaultValueSql("current_timestamp()")
                .HasColumnType("datetime");
            entity.Property(e => e.imei).HasMaxLength(30);
            entity.Property(e => e.numero_serie).HasMaxLength(150);
            entity.Property(e => e.producto_idproducto).HasColumnType("int(11)");

            entity.HasOne(d => d.producto_idproductoNavigation)
                .WithMany(p => p.inventarios)
                .HasForeignKey(d => d.producto_idproducto)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("inventario_ibfk_1");
        });

        modelBuilder.Entity<producto>(entity =>
        {
            entity.HasKey(e => e.idproducto).HasName("PRIMARY");

            entity.ToTable("producto");

            entity.HasIndex(e => e.categoria_idcategoria, "categoria_idcategoria");
            entity.HasIndex(e => e.codigo_barras, "codigo_barras").IsUnique();

            entity.Property(e => e.idproducto).HasColumnType("int(11)");
            entity.Property(e => e.categoria_idcategoria).HasColumnType("int(11)");
            entity.Property(e => e.codigo_barras).HasMaxLength(100);
            entity.Property(e => e.descripcion).HasColumnType("text");
            entity.Property(e => e.marca).HasMaxLength(100);
            entity.Property(e => e.modelo).HasMaxLength(100);
            entity.Property(e => e.nombre).HasMaxLength(150);
            entity.Property(e => e.precio_venta).HasPrecision(10, 2);
            entity.Property(e => e.stock_minimo)
                .HasDefaultValueSql("'1'")
                .HasColumnType("int(11)");
            entity.Property(e => e.activo).HasDefaultValue(true);

            entity.HasOne(d => d.categoria_idcategoriaNavigation)
                .WithMany(p => p.productos)
                .HasForeignKey(d => d.categoria_idcategoria)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("producto_ibfk_1");
        });

        modelBuilder.Entity<proveedor>(entity =>
        {
            entity.HasKey(e => e.idproveedor).HasName("PRIMARY");

            entity.ToTable("proveedor");

            entity.HasIndex(e => e.nit, "nit").IsUnique();

            entity.Property(e => e.idproveedor).HasColumnType("int(11)");
            entity.Property(e => e.correo).HasMaxLength(120);
            entity.Property(e => e.direccion).HasMaxLength(200);
            entity.Property(e => e.nit).HasMaxLength(30);
            entity.Property(e => e.nombre).HasMaxLength(150);
            entity.Property(e => e.telefono).HasMaxLength(30);
            entity.Property(e => e.activo).HasDefaultValue(true);
        });

        modelBuilder.Entity<rol>(entity =>
        {
            entity.HasKey(e => e.idrol).HasName("PRIMARY");

            entity.ToTable("rol");

            entity.HasIndex(e => e.nombre, "nombre").IsUnique();

            entity.Property(e => e.idrol).HasColumnType("int(11)");
            entity.Property(e => e.descripcion).HasMaxLength(150);
            entity.Property(e => e.nombre).HasMaxLength(50);
            entity.Property(e => e.activo).HasDefaultValue(true);
        });

        modelBuilder.Entity<usuario>(entity =>
        {
            entity.HasKey(e => e.idusuario).HasName("PRIMARY");

            entity.ToTable("usuario");

            entity.HasIndex(e => e.correo, "correo").IsUnique();
            entity.HasIndex(e => e.rol_idrol, "rol_idrol");

            entity.Property(e => e.idusuario).HasColumnType("int(11)");
            entity.Property(e => e.apellido).HasMaxLength(100);
            entity.Property(e => e.correo).HasMaxLength(120);
            entity.Property(e => e.estado)
                .HasDefaultValueSql("'Activo'")
                .HasColumnType("enum('Activo','Inactivo')");
            entity.Property(e => e.nombre).HasMaxLength(100);
            entity.Property(e => e.password).HasMaxLength(255);
            entity.Property(e => e.rol_idrol).HasColumnType("int(11)");

            entity.HasOne(d => d.rol_idrolNavigation)
                .WithMany(p => p.usuarios)
                .HasForeignKey(d => d.rol_idrol)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("usuario_ibfk_1");
        });

        modelBuilder.Entity<venta>(entity =>
        {
            entity.HasKey(e => e.idventa).HasName("PRIMARY");

            entity.ToTable("venta");

            entity.HasIndex(e => e.cliente_idcliente, "cliente_idcliente");
            entity.HasIndex(e => e.usuario_idusuario, "usuario_idusuario");

            entity.Property(e => e.idventa).HasColumnType("int(11)");
            entity.Property(e => e.cliente_idcliente).HasColumnType("int(11)");
            entity.Property(e => e.estado)
                .HasDefaultValueSql("'Completada'")
                .HasColumnType("enum('Completada','Anulada')");
            entity.Property(e => e.fecha_venta)
                .HasDefaultValueSql("current_timestamp()")
                .HasColumnType("datetime");
            entity.Property(e => e.metodo_pago)
                .HasDefaultValueSql("'Efectivo'")
                .HasColumnType("enum('Efectivo','QR','Tarjeta')");
            entity.Property(e => e.total).HasPrecision(12, 2);
            entity.Property(e => e.usuario_idusuario).HasColumnType("int(11)");

            entity.HasOne(d => d.cliente_idclienteNavigation)
                .WithMany(p => p.venta)
                .HasForeignKey(d => d.cliente_idcliente)
                .HasConstraintName("venta_ibfk_1");

            entity.HasOne(d => d.usuario_idusuarioNavigation)
                .WithMany(p => p.venta)
                .HasForeignKey(d => d.usuario_idusuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("venta_ibfk_2");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
