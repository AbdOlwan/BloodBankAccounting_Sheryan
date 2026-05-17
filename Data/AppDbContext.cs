using BloodBankAccounting.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace BloodBankAccounting.Data;

public class AppDbContext : DbContext
{
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Safe> Safes { get; set; }
    public DbSet<BloodBag> BloodBags { get; set; }
    public DbSet<Invoice> Invoices { get; set; }
    public DbSet<InvoiceLine> InvoiceLines { get; set; }
    public DbSet<ReceiptVoucher> ReceiptVouchers { get; set; }
    public DbSet<JournalEntry> JournalEntries { get; set; }
    public DbSet<OrganizationSettings> OrgSettings { get; set; }
    public DbSet<InvoiceAuditLog> InvoiceAuditLogs => Set<InvoiceAuditLog>();
    public DbSet<ElectronicPaymentDetail> ElectronicPaymentDetails { get; set; }
    public DbSet<BloodProduct> BloodProducts { get; set; }
    public DbSet<BloodStock> BloodStocks { get; set; }
    public DbSet<ProductCatalogItem> ProductCatalogItems { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .Build();

        optionsBuilder.UseSqlServer(
            config.GetConnectionString("DefaultConnection"),
            sql => sql.CommandTimeout(60));
    }

    protected override void OnModelCreating(ModelBuilder mb)
    {
        // ─── Customer ────────────────────────────────────────
        mb.Entity<Customer>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.Phone).HasMaxLength(50);
            e.Property(x => x.Address).HasMaxLength(500);
            e.Property(x => x.Balance).HasColumnType("decimal(18,2)");
            e.Property(x => x.CreditLimit).HasColumnType("decimal(18,2)");
            e.Property(x => x.Type).HasConversion<string>();
            e.HasIndex(x => x.Name);
        });

        // ─── Safe ─────────────────────────────────────────────
        mb.Entity<Safe>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(100);
            e.Property(x => x.Balance).HasColumnType("decimal(18,2)");
            e.Property(x => x.Type).HasConversion<string>();
        });

        // ─── BloodBag ─────────────────────────────────────────
        mb.Entity<BloodBag>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.BagNumber).IsRequired().HasMaxLength(50);
            e.HasIndex(x => x.BagNumber).IsUnique();
            e.Property(x => x.BloodGroup).HasConversion<string>();
            e.Property(x => x.Status).HasConversion<string>();
            e.Property(x => x.CostPrice).HasColumnType("decimal(18,2)");
            e.Property(x => x.SalePrice).HasColumnType("decimal(18,2)");
        });

        // ─── Invoice ──────────────────────────────────────────
        mb.Entity<Invoice>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.InvoiceNumber).IsRequired().HasMaxLength(20);
            e.HasIndex(x => x.InvoiceNumber).IsUnique();
            e.Property(x => x.PaymentType).HasConversion<string>();
            e.Property(x => x.SubTotal).HasColumnType("decimal(18,2)");
            e.Property(x => x.DiscountAmount).HasColumnType("decimal(18,2)");
            e.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)");
            e.Property(x => x.PaidAmount).HasColumnType("decimal(18,2)");

            e.HasOne(x => x.Customer)
             .WithMany(x => x.Invoices)
             .HasForeignKey(x => x.CustomerId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Safe)
             .WithMany(x => x.Invoices)
             .HasForeignKey(x => x.SafeId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ─── InvoiceLine ──────────────────────────────────────
        mb.Entity<InvoiceLine>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.ProductName).IsRequired().HasMaxLength(300);
            e.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
            e.Property(x => x.Total).HasColumnType("decimal(18,2)");

            e.HasOne(x => x.Invoice)
             .WithMany(x => x.Lines)
             .HasForeignKey(x => x.InvoiceId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.BloodBag)
             .WithMany(x => x.InvoiceLines)
             .HasForeignKey(x => x.BloodBagId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.Restrict);
        });

        mb.Entity<InvoiceAuditLog>(e =>
        {
            e.HasOne(a => a.Invoice)
             .WithMany()
             .HasForeignKey(a => a.InvoiceId)
             .OnDelete(DeleteBehavior.Cascade);

            e.Property(x => x.OldDiscountAmount).HasColumnType("decimal(18,2)");
            e.Property(x => x.OldTotalAmount).HasColumnType("decimal(18,2)");
            e.Property(x => x.OldPaidAmount).HasColumnType("decimal(18,2)");
            e.Property(x => x.NewDiscountAmount).HasColumnType("decimal(18,2)");
            e.Property(x => x.NewTotalAmount).HasColumnType("decimal(18,2)");
            e.Property(x => x.NewPaidAmount).HasColumnType("decimal(18,2)");
        });

        mb.Entity<ElectronicPaymentDetail>(e =>
        {
            e.HasKey(x => x.Id);
            //e.Property(x => x.FromAccountName).IsRequired().HasMaxLength(200);
            //e.Property(x => x.ToAccountName).IsRequired().HasMaxLength(200);
            //e.Property(x => x.TransactionReference).HasMaxLength(100);
            e.Property(x => x.Method).HasConversion<string>();

            e.HasOne(x => x.Invoice)
             .WithOne(x => x.ElectronicDetail)
             .HasForeignKey<ElectronicPaymentDetail>(x => x.InvoiceId)
             .IsRequired(false).OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.ReceiptVoucher)
             .WithOne()
             .HasForeignKey<ElectronicPaymentDetail>(x => x.ReceiptVoucherId)
             .IsRequired(false).OnDelete(DeleteBehavior.Cascade);
        });

        // ─── ReceiptVoucher ───────────────────────────────────
        mb.Entity<ReceiptVoucher>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.VoucherNumber).IsRequired().HasMaxLength(20);
            e.HasIndex(x => x.VoucherNumber).IsUnique();
            e.Property(x => x.Type).HasConversion<string>();
            e.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            e.Property(x => x.PayeeName).HasMaxLength(200);

            e.HasOne(x => x.Safe)
             .WithMany(x => x.ReceiptVouchers)
             .HasForeignKey(x => x.SafeId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Customer)
             .WithMany(x => x.ReceiptVouchers)
             .HasForeignKey(x => x.CustomerId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.RelatedInvoice)
             .WithMany(x => x.ReceiptVouchers)
             .HasForeignKey(x => x.RelatedInvoiceId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ─── JournalEntry ─────────────────────────────────────
        mb.Entity<JournalEntry>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.AccountCode).IsRequired().HasMaxLength(20);
            e.Property(x => x.AccountName).IsRequired().HasMaxLength(100);
            e.Property(x => x.RefType).HasMaxLength(50);
            e.Property(x => x.DebitAmount).HasColumnType("decimal(18,2)");
            e.Property(x => x.CreditAmount).HasColumnType("decimal(18,2)");
            e.HasIndex(x => new { x.RefType, x.RefId });

            e.HasOne(x => x.Invoice)
             .WithMany(x => x.JournalEntries)
             .HasForeignKey(x => x.InvoiceId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.ReceiptVoucher)
             .WithMany(x => x.JournalEntries)
             .HasForeignKey(x => x.ReceiptVoucherId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ─── OrganizationSettings ─────────────────────────────
        mb.Entity<OrganizationSettings>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.BankName).IsRequired().HasMaxLength(200);
            e.Property(x => x.SubTitle).HasMaxLength(200);
            e.Property(x => x.Address).HasMaxLength(500);
            e.Property(x => x.Phone).HasMaxLength(100);
            e.Property(x => x.LogoPath).HasMaxLength(500);
            e.Property(x => x.OwnerPinHash).HasMaxLength(64);
        });

        // ─── BloodProduct ──────────────────────────────────────────
        mb.Entity<BloodProduct>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.IsbtCode).HasMaxLength(20);
            // ← جديد
            e.Property(x => x.DefaultSalePrice).HasColumnType("decimal(18,2)");
            // ReorderLevel هو int? — EF يتعامل معه تلقائيًا بدون تهيئة إضافية

            // فهرس فريد على الاسم
            e.HasIndex(x => x.Name).IsUnique();
        });
        // ─── BloodStock ────────────────────────────────────────────
        mb.Entity<BloodStock>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.BloodGroup).HasConversion<string>();

            e.HasOne(x => x.Product)
             .WithMany(x => x.StockEntries)
             .HasForeignKey(x => x.ProductId)
             .OnDelete(DeleteBehavior.Cascade);

            // فهرس فريد للصنف + الفصيلة (مع استثناء NULL من القيد)
            e.HasIndex(x => new { x.ProductId, x.BloodGroup })
             .IsUnique()
             .HasFilter("[BloodGroup] IS NOT NULL");
        });

        // ─── ProductCatalogItem ────────────────────────────────────────
        // الفصيلة مُحذوفة — الصنف يُعرَّف بالاسم فقط
        mb.Entity<ProductCatalogItem>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.DefaultSalePrice).HasColumnType("decimal(18,2)");

            // قيد فريد على الاسم فقط
            e.HasIndex(x => x.Name).IsUnique();
        });

        // ─── Seed ─────────────────────────────────────────────
        mb.Entity<Safe>().HasData(
             new Safe { Id = 1, Name = "الخزينة الرئيسية", Type = SafeType.Cash, Balance = 0, IsMain = true },
             new Safe { Id = 2, Name = "الخزينة الإلكترونية", Type = SafeType.Electronic, Balance = 0, IsMain = false }
             );

        mb.Entity<OrganizationSettings>().HasData(
            new OrganizationSettings
            {
                Id = 1,
                BankName = "بنك الدم",
                SubTitle = "إدارة المحاسبة والمستودعات",
                Address = string.Empty,
                Phone = string.Empty
            }
        );

        mb.Entity<BloodProduct>().HasData(
            new BloodProduct { Id = 1, Name = "دم كامل", SortOrder = 1 },
            new BloodProduct { Id = 2, Name = "كرات دم مكدسة", IsbtCode = "E7428", SortOrder = 2 },
            new BloodProduct { Id = 3, Name = "بلازما طازجة مجمدة", SortOrder = 3 },
            new BloodProduct { Id = 4, Name = "كرايو - راسب أبيض", SortOrder = 4 },
            new BloodProduct { Id = 5, Name = "وحدة صفائح", IsbtCode = "E0582", SortOrder = 5 },
            new BloodProduct { Id = 6, Name = "كيس دم أطفال", SortOrder = 6 },
            new BloodProduct { Id = 7, Name = "فلترة كيس دم", SortOrder = 7 },
            new BloodProduct { Id = 8, Name = "غسيل كيس دم", SortOrder = 8 }
        );
    }
}