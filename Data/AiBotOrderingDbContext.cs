using System;
using System.Collections.Generic;
using AiBotOrderingSystem.Models.DbFirst;
using Microsoft.EntityFrameworkCore;

namespace AiBotOrderingSystem.Data;

public partial class AiBotOrderingDbContext : DbContext
{
    public AiBotOrderingDbContext()
    {
    }

    public AiBotOrderingDbContext(DbContextOptions<AiBotOrderingDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Addon> Addons { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<MenuItem> MenuItems { get; set; }

    public virtual DbSet<MenuItemAddon> MenuItemAddons { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderItem> OrderItems { get; set; }

    public virtual DbSet<OrderItemAddon> OrderItemAddons { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
//#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=AiBotOrderingDB;Username=postgres;Password=Jay@2410");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Addon>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("addons_pkey");

            entity.ToTable("addons");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Name)
                .HasMaxLength(150)
                .HasColumnName("name");
            entity.Property(e => e.Price)
                .HasPrecision(10, 2)
                .HasColumnName("price");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("categories_pkey");

            entity.ToTable("categories");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Name)
                .HasMaxLength(150)
                .HasColumnName("name");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<MenuItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("menu_items_pkey");

            entity.ToTable("menu_items");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.ImageUrl).HasColumnName("image_url");
            entity.Property(e => e.IsAvailable)
                .HasDefaultValue(true)
                .HasColumnName("is_available");
            entity.Property(e => e.Name)
                .HasMaxLength(150)
                .HasColumnName("name");
            entity.Property(e => e.Price)
                .HasPrecision(10, 2)
                .HasColumnName("price");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Category).WithMany(p => p.MenuItems)
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("menu_items_category_id_fkey");
        });

        modelBuilder.Entity<MenuItemAddon>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("menu_item_addons_pkey");

            entity.ToTable("menu_item_addons");

            entity.HasIndex(e => e.AddonId, "idx_menu_item_addons_addon_id");

            entity.HasIndex(e => e.MenuItemId, "idx_menu_item_addons_menu_item_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AddonId).HasColumnName("addon_id");
            entity.Property(e => e.MenuItemId).HasColumnName("menu_item_id");

            entity.HasOne(d => d.Addon).WithMany(p => p.MenuItemAddons)
                .HasForeignKey(d => d.AddonId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("menu_item_addons_addon_id_fkey");

            entity.HasOne(d => d.MenuItem).WithMany(p => p.MenuItemAddons)
                .HasForeignKey(d => d.MenuItemId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("menu_item_addons_menu_item_id_fkey");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("orders_pkey");

            entity.ToTable("orders");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.CustomerName)
                .HasMaxLength(150)
                .HasColumnName("customer_name");
            entity.Property(e => e.IsOrderFromWhatsapp)
                .HasDefaultValue(false)
                .HasColumnName("is_order_from_whatsapp");
            entity.Property(e => e.OrderType).HasColumnName("order_type");
            entity.Property(e => e.PaymentMode).HasColumnName("payment_mode");
            entity.Property(e => e.PaymentStatus)
                //.HasDefaultValue(1)
                .HasColumnName("payment_status");
            entity.Property(e => e.Status)
                //.HasDefaultValue(1)
                .HasColumnName("status");
            entity.Property(e => e.TotalAmount)
                .HasPrecision(10, 2)
                .HasDefaultValueSql("0")
                .HasColumnName("total_amount");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("order_items_pkey");

            entity.ToTable("order_items");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.LineTotal)
                .HasPrecision(12, 2)
                .HasColumnName("line_total");
            entity.Property(e => e.MenuItemId).HasColumnName("menu_item_id");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.PriceAtOrder)
                .HasPrecision(10, 2)
                .HasColumnName("price_at_order");
            entity.Property(e => e.Quantity)
                .HasDefaultValue(1)
                .HasColumnName("quantity");

            entity.HasOne(d => d.MenuItem).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.MenuItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("order_items_menu_item_id_fkey");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("order_items_order_id_fkey");
        });

        modelBuilder.Entity<OrderItemAddon>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("order_item_addons_pkey");

            entity.ToTable("order_item_addons");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AddonId).HasColumnName("addon_id");
            entity.Property(e => e.AddonPriceAtOrder)
                .HasPrecision(10, 2)
                .HasColumnName("addon_price_at_order");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.OrderItemId).HasColumnName("order_item_id");

            entity.HasOne(d => d.Addon).WithMany(p => p.OrderItemAddons)
                .HasForeignKey(d => d.AddonId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("order_item_addons_addon_id_fkey");

            entity.HasOne(d => d.OrderItem).WithMany(p => p.OrderItemAddons)
                .HasForeignKey(d => d.OrderItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("order_item_addons_order_item_id_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
