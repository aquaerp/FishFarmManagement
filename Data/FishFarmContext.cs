using Microsoft.EntityFrameworkCore;
using FishFarmManager.Models;

namespace FishFarmManager.Data
{
    public class FishFarmContext : DbContext
    {
        public FishFarmContext(DbContextOptions<FishFarmContext> options) : base(options)
        {
        }

        // Default constructor for forms that don't inject dependencies
        public FishFarmContext() : base(GetDefaultOptions())
        {
        }

        private static DbContextOptions<FishFarmContext> GetDefaultOptions()
        {
            var optionsBuilder = new DbContextOptionsBuilder<FishFarmContext>();
            optionsBuilder.UseSqlite("Data Source=fishfarm.db");
            return optionsBuilder.Options;
        }

        // Production System
        public DbSet<ProductionCycle> ProductionCycles { get; set; }
        public DbSet<ProductionCyclePond> ProductionCyclePonds { get; set; }
        public DbSet<Pond> Ponds { get; set; }
        public DbSet<BatchRecord> BatchRecords { get; set; }

        // Sales System
        public DbSet<Customer> Customers { get; set; }
        public DbSet<SalesOrder> SalesOrders { get; set; }
        public DbSet<SalesOrderItem> SalesOrderItems { get; set; }
        public DbSet<CustomerPayment> CustomerPayments { get; set; }

        // Cost System
        public DbSet<CostRecord> CostRecords { get; set; }

        // HR System
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<EmployeeLeave> EmployeeLeaves { get; set; }
        public DbSet<Salary> Salaries { get; set; }
        public DbSet<StaffRecord> StaffRecords { get; set; }

        // Quality & Health System
        public DbSet<WaterQualityRecord> WaterQualityRecords { get; set; }
        public DbSet<FishHealthRecord> FishHealthRecords { get; set; }
        public DbSet<TreatmentRecord> TreatmentRecords { get; set; }
        public DbSet<EnvironmentalRecord> EnvironmentalRecords { get; set; }
        public DbSet<QualityTest> QualityTests { get; set; }
        public DbSet<HACCPRecord> HACCPRecords { get; set; }
        public DbSet<HealthInspection> HealthInspections { get; set; }
        public DbSet<Certification> Certifications { get; set; }
        public DbSet<CertificationRecord> CertificationRecords { get; set; }

        // Maintenance System
        public DbSet<Equipment> Equipment { get; set; }
        public DbSet<MaintenanceRecord> MaintenanceRecords { get; set; }
        public DbSet<MaintenanceSchedule> MaintenanceSchedules { get; set; }
        public DbSet<SparePart> SpareParts { get; set; }

        // Inventory System
        public DbSet<InventoryItem> InventoryItems { get; set; }
        public DbSet<StockMovement> StockMovements { get; set; }
        public DbSet<InventoryValuation> InventoryValuations { get; set; }

        // Supplier System
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<SupplierPayment> SupplierPayments { get; set; }

        // Purchasing System - Week 7
        public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
        public DbSet<PurchaseOrderItem> PurchaseOrderItems { get; set; }
        public DbSet<PurchaseReceiving> PurchaseReceivings { get; set; }
        public DbSet<PurchaseReceivingItem> PurchaseReceivingItems { get; set; }

        // Additional Records
        public DbSet<FeedingRecord> FeedingRecords { get; set; }
        public DbSet<MortalityRecord> MortalityRecords { get; set; }

        // Authentication System
        public DbSet<User> Users { get; set; }

        // VAT & Tax System
        public DbSet<TaxInvoice> TaxInvoices { get; set; }
        public DbSet<TaxInvoiceItem> TaxInvoiceItems { get; set; }
        public DbSet<VATReturn> VATReturns { get; set; }
        public DbSet<VATConfiguration> VATConfigurations { get; set; }

        // Fixed Assets System
        public DbSet<FixedAsset> FixedAssets { get; set; }
        public DbSet<AssetDepreciation> AssetDepreciations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships and constraints
            ConfigureProductionSystem(modelBuilder);
            ConfigureSalesSystem(modelBuilder);
            ConfigureHRSystem(modelBuilder);
            ConfigureQualitySystem(modelBuilder);
            ConfigureMaintenanceSystem(modelBuilder);
            ConfigureInventorySystem(modelBuilder);
            ConfigurePurchasingSystem(modelBuilder);
            ConfigureVATSystem(modelBuilder);
            ConfigureAuthenticationSystem(modelBuilder);
        }

        private void ConfigureProductionSystem(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ProductionCycle>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.StartDate).IsRequired();
                entity.Property(e => e.EndDate);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt);
            });

            modelBuilder.Entity<ProductionCyclePond>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.ProductionCycle)
                    .WithMany(p => p.ProductionCyclePonds)
                    .HasForeignKey(e => e.ProductionCycleId);
                entity.HasOne(e => e.Pond)
                    .WithMany()
                    .HasForeignKey(e => e.PondId);
            });

            modelBuilder.Entity<Pond>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.Capacity).IsRequired();
                entity.Property(e => e.Area).IsRequired();
                entity.Property(e => e.Depth).IsRequired();
                entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt);
            });
        }

        private void ConfigureSalesSystem(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Customer>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Email).HasMaxLength(100);
                entity.Property(e => e.Phone).HasMaxLength(20);
                entity.Property(e => e.Address).HasMaxLength(500);
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt);
            });

            modelBuilder.Entity<SalesOrder>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.OrderNumber).IsRequired().HasMaxLength(50);
                entity.Property(e => e.OrderDate).IsRequired();
                entity.Property(e => e.DeliveryDate);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
                entity.Property(e => e.TotalAmount).IsRequired();
                entity.Property(e => e.TaxAmount).IsRequired();
                entity.Property(e => e.GrandTotal).IsRequired();
                entity.Property(e => e.Notes).HasMaxLength(1000);
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt);

                entity.HasOne(e => e.Customer)
                    .WithMany()
                    .HasForeignKey(e => e.CustomerId);
            });

            modelBuilder.Entity<SalesOrderItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ProductName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Quantity).IsRequired();
                entity.Property(e => e.UnitPrice).IsRequired();
                entity.Property(e => e.TotalPrice).IsRequired();
                entity.Property(e => e.Notes).HasMaxLength(500);

                entity.HasOne(e => e.SalesOrder)
                    .WithMany(s => s.Items)
                    .HasForeignKey(e => e.SalesOrderId);
            });
        }

        private void ConfigureHRSystem(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Employee>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Position).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Department).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).HasMaxLength(100);
                entity.Property(e => e.Phone).HasMaxLength(20);
                entity.Property(e => e.Address).HasMaxLength(500);
                entity.Property(e => e.HireDate).IsRequired();
                // Salary navigation property is configured with [NotMapped] attribute in Employee model
                entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt);
            });

            modelBuilder.Entity<Attendance>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Date).IsRequired();
                entity.Property(e => e.CheckIn);
                entity.Property(e => e.CheckOut);
                entity.Property(e => e.HoursWorked);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Notes).HasMaxLength(500);
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt);

                entity.HasOne(e => e.Employee)
                    .WithMany()
                    .HasForeignKey(e => e.EmployeeId);
            });
        }

        private void ConfigureQualitySystem(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<WaterQualityRecord>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.RecordDate).IsRequired();
                entity.Property(e => e.Temperature).IsRequired();
                entity.Property(e => e.pH).IsRequired();
                entity.Property(e => e.DissolvedOxygen).IsRequired();
                entity.Property(e => e.Ammonia).IsRequired();
                entity.Property(e => e.Nitrite).IsRequired();
                entity.Property(e => e.Nitrate).IsRequired();
                entity.Property(e => e.Notes).HasMaxLength(1000);
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt);

                entity.HasOne(e => e.Pond)
                    .WithMany()
                    .HasForeignKey(e => e.PondId);
            });
        }

        private void ConfigureMaintenanceSystem(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Equipment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.SerialNumber).HasMaxLength(100);
                entity.Property(e => e.PurchaseDate);
                entity.Property(e => e.WarrantyExpiry);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt);
            });
        }

        private void ConfigureInventorySystem(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<InventoryItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.Category).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Unit).IsRequired().HasMaxLength(50);
                entity.Property(e => e.CurrentStock).IsRequired();
                entity.Property(e => e.MinimumStock).IsRequired();
                entity.Property(e => e.MaximumStock).IsRequired();
                entity.Property(e => e.UnitCost).IsRequired();
                entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt);
            });

            modelBuilder.Entity<StockMovement>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.MovementType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Quantity).IsRequired();
                entity.Property(e => e.UnitCost).IsRequired();
                entity.Property(e => e.TotalCost).IsRequired();
                entity.Property(e => e.Reference).HasMaxLength(100);
                entity.Property(e => e.Notes).HasMaxLength(500);
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt);

                entity.HasOne(e => e.InventoryItem)
                    .WithMany()
                    .HasForeignKey(e => e.InventoryItemId);
            });
        }

        private void ConfigurePurchasingSystem(ModelBuilder modelBuilder)
        {
            // Purchase Order Configuration
            modelBuilder.Entity<PurchaseOrder>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.OrderNumber)
                    .IsRequired()
                    .HasMaxLength(50);
                
                entity.HasIndex(e => e.OrderNumber)
                    .IsUnique();
                
                entity.Property(e => e.OrderDate).IsRequired();
                entity.Property(e => e.ExpectedDeliveryDate).IsRequired();
                entity.Property(e => e.Status).IsRequired();
                
                // Financial properties
                entity.Property(e => e.SubTotal).HasColumnType("decimal(18,2)").IsRequired();
                entity.Property(e => e.VATRate).HasColumnType("decimal(5,2)").IsRequired();
                entity.Property(e => e.VATAmount).HasColumnType("decimal(18,2)").IsRequired();
                entity.Property(e => e.Total).HasColumnType("decimal(18,2)").IsRequired();
                entity.Property(e => e.DiscountAmount).HasColumnType("decimal(18,2)");
                
                // String properties
                entity.Property(e => e.PaymentTerms).HasMaxLength(500);
                entity.Property(e => e.DeliveryAddress).HasMaxLength(500);
                entity.Property(e => e.Notes).HasMaxLength(1000);
                entity.Property(e => e.Department).HasMaxLength(100);
                
                // Relationships
                entity.HasOne(e => e.Supplier)
                    .WithMany()
                    .HasForeignKey(e => e.SupplierId)
                    .OnDelete(DeleteBehavior.Restrict);
                
                entity.HasOne(e => e.RequestedBy)
                    .WithMany()
                    .HasForeignKey(e => e.RequestedById)
                    .OnDelete(DeleteBehavior.SetNull);
                
                entity.HasOne(e => e.ApprovedBy)
                    .WithMany()
                    .HasForeignKey(e => e.ApprovedById)
                    .OnDelete(DeleteBehavior.SetNull);
                
                entity.HasOne(e => e.SentBy)
                    .WithMany()
                    .HasForeignKey(e => e.SentById)
                    .OnDelete(DeleteBehavior.SetNull);
                
                entity.HasOne(e => e.ReceivedBy)
                    .WithMany()
                    .HasForeignKey(e => e.ReceivedById)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Purchase Order Item Configuration
            modelBuilder.Entity<PurchaseOrderItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.Quantity)
                    .HasColumnType("decimal(18,3)")
                    .IsRequired();
                
                entity.Property(e => e.ReceivedQuantity)
                    .HasColumnType("decimal(18,3)");
                
                entity.Property(e => e.UnitPrice)
                    .HasColumnType("decimal(18,2)")
                    .IsRequired();
                
                entity.Property(e => e.DiscountPercentage)
                    .HasColumnType("decimal(5,2)");
                
                entity.Property(e => e.Notes).HasMaxLength(500);
                
                // Relationships
                entity.HasOne(e => e.PurchaseOrder)
                    .WithMany(p => p.Items)
                    .HasForeignKey(e => e.PurchaseOrderId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasOne(e => e.InventoryItem)
                    .WithMany()
                    .HasForeignKey(e => e.InventoryItemId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private void ConfigureAuthenticationSystem(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.UserId);
                
                entity.Property(e => e.Username)
                    .IsRequired()
                    .HasMaxLength(50);
                
                entity.HasIndex(e => e.Username)
                    .IsUnique();
                
                entity.Property(e => e.PasswordHash)
                    .IsRequired()
                    .HasMaxLength(255);
                
                entity.Property(e => e.FullName)
                    .IsRequired()
                    .HasMaxLength(100);
                
                entity.Property(e => e.Email)
                    .HasMaxLength(100);
                
                entity.Property(e => e.Role)
                    .IsRequired();
                
                entity.Property(e => e.CreatedBy)
                    .HasMaxLength(50);
                
                entity.Property(e => e.UpdatedBy)
                    .HasMaxLength(50);
                
                // Relationship with Employee
                entity.HasOne(e => e.Employee)
                    .WithMany()
                    .HasForeignKey(e => e.EmployeeId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }

        private void ConfigureVATSystem(ModelBuilder modelBuilder)
        {
            // TaxInvoice Configuration
            modelBuilder.Entity<TaxInvoice>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.InvoiceNumber).IsRequired().HasMaxLength(50);
                entity.Property(e => e.SubTotal).HasColumnType("decimal(18,2)");
                entity.Property(e => e.DiscountAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.VATRate).HasColumnType("decimal(5,2)");
                entity.Property(e => e.VATAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TotalWithVAT).HasColumnType("decimal(18,2)");
                entity.Property(e => e.SellerVATNumber).IsRequired().HasMaxLength(15);
                entity.Property(e => e.BuyerVATNumber).HasMaxLength(15);

                // Relationships
                entity.HasOne(e => e.Customer)
                    .WithMany()
                    .HasForeignKey(e => e.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // TaxInvoiceItem Configuration
            modelBuilder.Entity<TaxInvoiceItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Quantity).HasColumnType("decimal(18,3)");
                entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.VATRate).HasColumnType("decimal(5,2)");
                entity.Property(e => e.VATAmount).HasColumnType("decimal(18,2)");

                // Relationship with TaxInvoice
                entity.HasOne(e => e.TaxInvoice)
                    .WithMany(e => e.Items)
                    .HasForeignKey(e => e.TaxInvoiceId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // VATReturn Configuration
            modelBuilder.Entity<VATReturn>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.PeriodNumber).IsRequired().HasMaxLength(20);
                entity.Property(e => e.TaxRegistrationNumber).IsRequired().HasMaxLength(15);
                
                // All VAT amounts as decimal(18,2)
                entity.Property(e => e.Box1_TaxableSalesInKSA).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Box2_ZeroRatedSales).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Box3_Exports).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Box4_ExemptSales).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Box5_TotalSales).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Box6_VATOnSales).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Box7_TotalPurchases).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Box8_GCCPurchases).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Box9_TaxableImports).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Box10_VATOnPurchases).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Box11_NetVATDue).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Box12_Adjustments).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Box13_TotalVATDue).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Box14_RecoverablePreviousPeriod).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Box15_NetVATDueForPeriod).HasColumnType("decimal(18,2)");

                // Relationships
                entity.HasOne(e => e.CreatedBy)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedById)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.ModifiedBy)
                    .WithMany()
                    .HasForeignKey(e => e.ModifiedById)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // VATConfiguration Configuration
            modelBuilder.Entity<VATConfiguration>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.TaxRegistrationNumber).IsRequired().HasMaxLength(15);
                entity.Property(e => e.DefaultVATRate).HasColumnType("decimal(5,2)");
            });
        }
    }
}