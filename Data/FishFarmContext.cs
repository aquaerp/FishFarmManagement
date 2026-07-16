using Microsoft.EntityFrameworkCore;
using FishFarmManager.Models;

namespace FishFarmManager.Data
{
    public class FishFarmContext : DbContext
    {
        public FishFarmContext(DbContextOptions<FishFarmContext> options) : base(options)
        {
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
        public DbSet<QualityRiskRegister> QualityRiskRegisters { get; set; }
        public DbSet<QualityObjective> QualityObjectives { get; set; }
        public DbSet<ControlledDocument> ControlledDocuments { get; set; }
        public DbSet<CorrectiveAction> CorrectiveActions { get; set; }
        public DbSet<RecallExercise> RecallExercises { get; set; }

        // Maintenance System
        public DbSet<Equipment> Equipment { get; set; }
        public DbSet<MaintenanceRecord> MaintenanceRecords { get; set; }
        public DbSet<MaintenanceSchedule> MaintenanceSchedules { get; set; }
        public DbSet<SparePart> SpareParts { get; set; }

        // Inventory System
        public DbSet<InventoryItem> InventoryItems { get; set; }
        public DbSet<StockMovement> StockMovements { get; set; }
        public DbSet<InventoryValuation> InventoryValuations { get; set; }
        public DbSet<InventoryCount> InventoryCounts { get; set; }
        public DbSet<InventoryCountLine> InventoryCountLines { get; set; }
        public DbSet<TraceabilityLot> TraceabilityLots { get; set; }
        public DbSet<TraceabilityAllocation> TraceabilityAllocations { get; set; }
        public DbSet<ProductionCostEvent> ProductionCostEvents { get; set; }
        public DbSet<InventoryLedgerReconciliation> InventoryLedgerReconciliations { get; set; }

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
        public DbSet<SecurityAuditEvent> SecurityAuditEvents { get; set; }

        // General Ledger / Accounting Core
        public DbSet<LedgerAccount> LedgerAccounts { get; set; }
        public DbSet<FiscalYear> FiscalYears { get; set; }
        public DbSet<FiscalPeriod> FiscalPeriods { get; set; }
        public DbSet<CostCenter> CostCenters { get; set; }
        public DbSet<JournalEntry> JournalEntries { get; set; }
        public DbSet<JournalEntryLine> JournalEntryLines { get; set; }
        public DbSet<AccountingSequence> AccountingSequences { get; set; }
        public DbSet<AccountingAuditEvent> AccountingAuditEvents { get; set; }
        public DbSet<AccountingConfiguration> AccountingConfigurations { get; set; }
        public DbSet<PostingMapping> PostingMappings { get; set; }
        public DbSet<OperationalPostingRecord> OperationalPostingRecords { get; set; }
        public DbSet<VatReturnLedgerReconciliation> VatReturnLedgerReconciliations { get; set; }
        public DbSet<AccountingAdjustment> AccountingAdjustments { get; set; }
        public DbSet<ForeignExchangeRate> ForeignExchangeRates { get; set; }
        public DbSet<ForeignMonetaryItem> ForeignMonetaryItems { get; set; }
        public DbSet<ForeignCurrencySettlement> ForeignCurrencySettlements { get; set; }
        public DbSet<ForeignCurrencyRevaluation> ForeignCurrencyRevaluations { get; set; }

        // VAT & Tax System
        public DbSet<TaxInvoice> TaxInvoices { get; set; }
        public DbSet<TaxInvoiceItem> TaxInvoiceItems { get; set; }
        public DbSet<VATReturn> VATReturns { get; set; }
        public DbSet<VATConfiguration> VATConfigurations { get; set; }
        public DbSet<ZatcaEgsUnit> ZatcaEgsUnits { get; set; }
        public DbSet<ZatcaDocumentEnvelope> ZatcaDocumentEnvelopes { get; set; }
        public DbSet<ZatcaOutboxMessage> ZatcaOutboxMessages { get; set; }
        public DbSet<ZatcaCanonicalizationEvidence> ZatcaCanonicalizationEvidence { get; set; }
        public DbSet<ZatcaSubmissionArchive> ZatcaSubmissionArchives { get; set; }

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
            ConfigureSecurityAuditSystem(modelBuilder);
            ConfigureAccountingSystem(modelBuilder);
            ConfigureZatcaSystem(modelBuilder);
        }

        private static void ConfigureZatcaSystem(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ZatcaEgsUnit>(entity =>
            {
                entity.HasKey(value => value.Id);
                entity.Property(value => value.DeviceId).IsRequired().HasMaxLength(100);
                entity.Property(value => value.PreviousInvoiceHashBase64).IsRequired().HasMaxLength(500);
                entity.Property(value => value.CreatedBy).IsRequired().HasMaxLength(100);
                entity.HasIndex(value => value.DeviceId).IsUnique();
                entity.ToTable(table => table.HasCheckConstraint("CK_ZatcaEgsUnit_Counter",
                    "LastReservedInvoiceCounterValue >= 0"));
            });
            modelBuilder.Entity<ZatcaDocumentEnvelope>(entity =>
            {
                entity.HasKey(value => value.Id);
                entity.Property(value => value.SourceEntityType).IsRequired().HasMaxLength(100);
                entity.Property(value => value.SourceEntityId).IsRequired().HasMaxLength(100);
                entity.Property(value => value.DocumentNumber).IsRequired().HasMaxLength(100);
                entity.Property(value => value.Uuid).IsRequired().HasMaxLength(36);
                entity.Property(value => value.PreviousInvoiceHashBase64).IsRequired().HasMaxLength(500);
                entity.Property(value => value.UnsignedXml).IsRequired();
                entity.Property(value => value.LocalPayloadSha256Base64).IsRequired().HasMaxLength(100);
                entity.Property(value => value.CreatedBy).IsRequired().HasMaxLength(100);
                entity.Property(value => value.Reason).IsRequired().HasMaxLength(500);
                entity.HasIndex(value => value.Uuid).IsUnique();
                entity.HasIndex(value => new { value.ZatcaEgsUnitId, value.InvoiceCounterValue }).IsUnique();
                entity.HasIndex(value => new { value.SourceEntityType, value.SourceEntityId }).IsUnique();
                entity.HasOne(value => value.ZatcaEgsUnit).WithMany(value => value.Envelopes)
                    .HasForeignKey(value => value.ZatcaEgsUnitId).OnDelete(DeleteBehavior.Restrict);
                entity.ToTable(table => table.HasCheckConstraint("CK_ZatcaDocumentEnvelope_ICV",
                    "InvoiceCounterValue > 0"));
            });
            modelBuilder.Entity<ZatcaOutboxMessage>(entity =>
            {
                entity.HasKey(value => value.Id);
                entity.HasIndex(value => value.ZatcaDocumentEnvelopeId).IsUnique();
                entity.HasOne(value => value.ZatcaDocumentEnvelope).WithOne(value => value.OutboxMessage)
                    .HasForeignKey<ZatcaOutboxMessage>(value => value.ZatcaDocumentEnvelopeId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.ToTable(table => table.HasCheckConstraint("CK_ZatcaOutbox_Attempts", "AttemptCount >= 0"));
            });
            modelBuilder.Entity<ZatcaCanonicalizationEvidence>(entity =>
            {
                entity.HasKey(value => value.Id);
                entity.Property(value => value.CanonicalizationAlgorithm).IsRequired().HasMaxLength(200);
                entity.Property(value => value.DigestAlgorithm).IsRequired().HasMaxLength(200);
                entity.Property(value => value.InvoiceHashHex).IsRequired().HasMaxLength(64);
                entity.Property(value => value.InvoiceHashBase64).IsRequired().HasMaxLength(44);
                entity.Property(value => value.CanonicalXmlSha256Base64).IsRequired().HasMaxLength(44);
                entity.Property(value => value.CreatedBy).IsRequired().HasMaxLength(100);
                entity.Property(value => value.Reason).IsRequired().HasMaxLength(500);
                entity.HasIndex(value => value.ZatcaDocumentEnvelopeId).IsUnique();
                entity.HasOne(value => value.ZatcaDocumentEnvelope).WithOne(value => value.CanonicalizationEvidence)
                    .HasForeignKey<ZatcaCanonicalizationEvidence>(value => value.ZatcaDocumentEnvelopeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<ZatcaSubmissionArchive>(entity =>
            {
                entity.HasKey(value => value.Id);
                entity.Property(value => value.EndpointPath).IsRequired().HasMaxLength(200);
                entity.Property(value => value.IdempotencyKey).IsRequired().HasMaxLength(64);
                entity.Property(value => value.RequestPayloadJson).IsRequired();
                entity.Property(value => value.RequestSha256Base64).IsRequired().HasMaxLength(44);
                entity.Property(value => value.SubmittedXml).IsRequired();
                entity.Property(value => value.ResponseBody).IsRequired();
                entity.Property(value => value.ResponseSha256Base64).IsRequired().HasMaxLength(44);
                entity.Property(value => value.AuthorityStatus).IsRequired().HasMaxLength(100);
                entity.Property(value => value.CreatedBy).IsRequired().HasMaxLength(100);
                entity.Property(value => value.Reason).IsRequired().HasMaxLength(500);
                entity.HasIndex(value => new { value.ZatcaDocumentEnvelopeId, value.AttemptNumber }).IsUnique();
                entity.HasIndex(value => value.IdempotencyKey);
                entity.HasOne(value => value.ZatcaDocumentEnvelope).WithMany(value => value.SubmissionArchives)
                    .HasForeignKey(value => value.ZatcaDocumentEnvelopeId).OnDelete(DeleteBehavior.Restrict);
                entity.ToTable(table =>
                {
                    table.HasCheckConstraint("CK_ZatcaSubmissionArchive_Attempt", "AttemptNumber > 0");
                    table.HasCheckConstraint("CK_ZatcaSubmissionArchive_Duration", "DurationMilliseconds >= 0");
                });
            });
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

            modelBuilder.Entity<HACCPRecord>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.RecordNumber);
                entity.Property(e => e.RecordNumber).IsRequired().HasMaxLength(50);
                entity.Property(e => e.RootCause).HasMaxLength(1000);
                entity.Property(e => e.CorrectiveActionOwner).HasMaxLength(100);
                entity.Property(e => e.EffectivenessVerifiedBy).HasMaxLength(100);
                entity.HasOne(e => e.Pond).WithMany().HasForeignKey(e => e.PondId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.ToTable(table =>
                {
                    table.HasCheckConstraint("CK_HACCPRecord_Limits",
                        "CAST(MinimumLimit AS NUMERIC) <= CAST(TargetValue AS NUMERIC) AND CAST(TargetValue AS NUMERIC) <= CAST(MaximumLimit AS NUMERIC)");
                    table.HasCheckConstraint("CK_HACCPRecord_DerivedDeviation",
                        "(CAST(ActualValue AS NUMERIC) >= CAST(MinimumLimit AS NUMERIC) AND CAST(ActualValue AS NUMERIC) <= CAST(MaximumLimit AS NUMERIC) AND IsWithinLimits = 1 AND DeviationOccurred = 0) OR (NOT (CAST(ActualValue AS NUMERIC) >= CAST(MinimumLimit AS NUMERIC) AND CAST(ActualValue AS NUMERIC) <= CAST(MaximumLimit AS NUMERIC)) AND IsWithinLimits = 0 AND DeviationOccurred = 1 AND length(trim(DeviationDescription)) > 0)");
                    table.HasCheckConstraint("CK_HACCPRecord_ControlReference",
                        "ControlMeasureType = 1 OR (ControlMeasureType IN (2, 3) AND ReferenceDocument IS NOT NULL AND length(trim(ReferenceDocument)) > 0)");
                    table.HasCheckConstraint("CK_HACCPRecord_ClosedVerification",
                        "LifecycleStatus <> 4 OR (Verified = 1 AND EffectivenessVerified = 1 AND VerificationDate IS NOT NULL AND EffectivenessVerificationDate IS NOT NULL)");
                });
            });

            modelBuilder.Entity<QualityRiskRegister>(entity =>
            {
                entity.HasKey(value => value.Id);
                entity.Property(value => value.ReferenceNumber).IsRequired().HasMaxLength(50);
                entity.Property(value => value.Title).IsRequired().HasMaxLength(200);
                entity.Property(value => value.Description).IsRequired().HasMaxLength(2000);
                entity.Property(value => value.Category).IsRequired().HasMaxLength(100);
                entity.Property(value => value.Owner).IsRequired().HasMaxLength(100);
                entity.Property(value => value.TreatmentPlan).IsRequired().HasMaxLength(2000);
                entity.Property(value => value.CreatedBy).IsRequired().HasMaxLength(100);
                entity.HasIndex(value => value.ReferenceNumber).IsUnique();
                entity.ToTable(table => table.HasCheckConstraint("CK_QualityRiskRegister_Score",
                    "Likelihood BETWEEN 1 AND 5 AND Impact BETWEEN 1 AND 5 AND Score = Likelihood * Impact"));
            });

            modelBuilder.Entity<QualityObjective>(entity =>
            {
                entity.HasKey(value => value.Id);
                entity.Property(value => value.ObjectiveCode).IsRequired().HasMaxLength(50);
                entity.Property(value => value.Title).IsRequired().HasMaxLength(200);
                entity.Property(value => value.Measure).IsRequired().HasMaxLength(100);
                entity.Property(value => value.Unit).IsRequired().HasMaxLength(50);
                entity.Property(value => value.Owner).IsRequired().HasMaxLength(100);
                entity.Property(value => value.CreatedBy).IsRequired().HasMaxLength(100);
                entity.HasIndex(value => value.ObjectiveCode).IsUnique();
                entity.ToTable(table => table.HasCheckConstraint("CK_QualityObjective_Period",
                    "PeriodStart <= PeriodEnd"));
            });

            modelBuilder.Entity<CorrectiveAction>(entity =>
            {
                entity.HasKey(value => value.Id);
                entity.Property(value => value.ReferenceNumber).IsRequired().HasMaxLength(50);
                entity.Property(value => value.Nonconformity).IsRequired().HasMaxLength(1000);
                entity.Property(value => value.RootCause).IsRequired().HasMaxLength(1000);
                entity.Property(value => value.ActionPlan).IsRequired().HasMaxLength(1000);
                entity.Property(value => value.Owner).IsRequired().HasMaxLength(100);
                entity.Property(value => value.CreatedBy).IsRequired().HasMaxLength(100);
                entity.HasIndex(value => value.ReferenceNumber).IsUnique();
                entity.ToTable(table => table.HasCheckConstraint("CK_CAPA_ClosedVerification",
                    "Status <> 4 OR (EffectivenessVerified = 1 AND VerifiedBy IS NOT NULL AND VerifiedAtUtc IS NOT NULL AND CompletedAtUtc IS NOT NULL)"));
            });

            modelBuilder.Entity<RecallExercise>(entity =>
            {
                entity.HasKey(value => value.Id);
                entity.Property(value => value.ExerciseNumber).IsRequired().HasMaxLength(50);
                entity.Property(value => value.TriggerLotCode).IsRequired().HasMaxLength(100);
                entity.Property(value => value.ConductedBy).IsRequired().HasMaxLength(100);
                entity.HasIndex(value => value.ExerciseNumber).IsUnique();
                entity.ToTable(table => table.HasCheckConstraint("CK_RecallExercise_Timeline", "CompletedAtUtc >= StartedAtUtc"));
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
                entity.Property(e => e.ApprovedByUsername).HasMaxLength(100);
                entity.Property(e => e.ApprovalReason).HasMaxLength(500);
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt);

                entity.HasOne(e => e.InventoryItem)
                    .WithMany()
                    .HasForeignKey(e => e.InventoryItemId);
            });

            modelBuilder.Entity<InventoryCount>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.CountNumber).IsRequired().HasMaxLength(40);
                entity.HasIndex(e => e.CountNumber).IsUnique();
                entity.Property(e => e.Reference).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Notes).HasMaxLength(1000);
                entity.Property(e => e.CreatedBy).IsRequired().HasMaxLength(100);
                entity.Property(e => e.ApprovedBy).HasMaxLength(100);
                entity.Property(e => e.ApprovalReason).HasMaxLength(500);
                entity.Property(e => e.RejectedBy).HasMaxLength(100);
                entity.Property(e => e.RejectionReason).HasMaxLength(500);
            });

            modelBuilder.Entity<InventoryCountLine>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.InventoryCountId, e.InventoryItemId }).IsUnique();
                entity.Property(e => e.BookQuantitySnapshot).HasPrecision(18, 3);
                entity.Property(e => e.UnitCostSnapshot).HasPrecision(18, 2);
                entity.Property(e => e.ActualQuantity).HasPrecision(18, 3);
                entity.Property(e => e.VarianceQuantity).HasPrecision(18, 3);
                entity.Property(e => e.VarianceValue).HasPrecision(18, 2);
                entity.Property(e => e.VarianceReason).HasMaxLength(500);
                entity.HasOne(e => e.InventoryCount).WithMany(e => e.Lines)
                    .HasForeignKey(e => e.InventoryCountId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.InventoryItem).WithMany()
                    .HasForeignKey(e => e.InventoryItemId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.StockMovement).WithMany()
                    .HasForeignKey(e => e.StockMovementId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.JournalEntry).WithMany()
                    .HasForeignKey(e => e.JournalEntryId).OnDelete(DeleteBehavior.Restrict);
                entity.ToTable(table =>
                {
                    table.HasCheckConstraint("CK_InventoryCountLine_BookQuantity", "CAST(BookQuantitySnapshot AS NUMERIC) >= 0");
                    table.HasCheckConstraint("CK_InventoryCountLine_ActualQuantity", "ActualQuantity IS NULL OR CAST(ActualQuantity AS NUMERIC) >= 0");
                });
            });

            modelBuilder.Entity<TraceabilityLot>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.LotCode).IsRequired().HasMaxLength(120);
                entity.HasIndex(e => e.LotCode).IsUnique();
                entity.Property(e => e.InitialQuantity).HasPrecision(18, 3);
                entity.Property(e => e.Unit).IsRequired().HasMaxLength(50);
                entity.Property(e => e.SourceReference).IsRequired().HasMaxLength(100);
                entity.Property(e => e.CreatedBy).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => e.PurchaseReceivingItemId).IsUnique();
                entity.HasOne(e => e.InventoryItem).WithMany().HasForeignKey(e => e.InventoryItemId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.PurchaseReceivingItem).WithMany().HasForeignKey(e => e.PurchaseReceivingItemId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.ProductionCycle).WithMany().HasForeignKey(e => e.ProductionCycleId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Pond).WithMany().HasForeignKey(e => e.PondId).OnDelete(DeleteBehavior.Restrict);
                entity.ToTable(table =>
                {
                    table.HasCheckConstraint("CK_TraceabilityLot_PositiveQuantity", "CAST(InitialQuantity AS NUMERIC) > 0");
                    table.HasCheckConstraint("CK_TraceabilityLot_KindShape",
                        "(Kind = 0 AND InventoryItemId IS NOT NULL AND PurchaseReceivingItemId IS NOT NULL AND ProductionCycleId IS NULL AND PondId IS NULL) OR " +
                        "(Kind = 1 AND InventoryItemId IS NULL AND PurchaseReceivingItemId IS NULL AND ProductionCycleId IS NOT NULL AND PondId IS NOT NULL)");
                });
            });

            modelBuilder.Entity<TraceabilityAllocation>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Quantity).HasPrecision(18, 3);
                entity.Property(e => e.Reference).IsRequired().HasMaxLength(100);
                entity.Property(e => e.CreatedBy).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => new { e.AllocationType, e.SourceLotId, e.Reference }).IsUnique();
                entity.HasOne(e => e.SourceLot).WithMany(e => e.Allocations).HasForeignKey(e => e.SourceLotId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.ProductionCycle).WithMany().HasForeignKey(e => e.ProductionCycleId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Pond).WithMany().HasForeignKey(e => e.PondId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.StockMovement).WithMany().HasForeignKey(e => e.StockMovementId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.SalesOrderItem).WithMany().HasForeignKey(e => e.SalesOrderItemId).OnDelete(DeleteBehavior.Restrict);
                entity.ToTable(table =>
                {
                    table.HasCheckConstraint("CK_TraceabilityAllocation_PositiveQuantity", "CAST(Quantity AS NUMERIC) > 0");
                    table.HasCheckConstraint("CK_TraceabilityAllocation_TypeShape",
                        "(AllocationType = 0 AND ProductionCycleId IS NOT NULL AND PondId IS NOT NULL AND StockMovementId IS NOT NULL AND SalesOrderItemId IS NULL) OR " +
                        "(AllocationType = 1 AND ProductionCycleId IS NULL AND PondId IS NULL AND StockMovementId IS NULL AND SalesOrderItemId IS NOT NULL)");
                });
            });

            modelBuilder.Entity<ProductionCostEvent>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.QuantityKg).HasPrecision(18, 3);
                entity.Property(e => e.Amount).HasPrecision(18, 2);
                entity.Property(e => e.Reference).IsRequired().HasMaxLength(100);
                entity.Property(e => e.SourceEntityType).IsRequired().HasMaxLength(100);
                entity.Property(e => e.SourceEntityId).IsRequired().HasMaxLength(100);
                entity.Property(e => e.MeasurementBasis).IsRequired().HasMaxLength(500);
                entity.Property(e => e.CreatedBy).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => new { e.EventType, e.SourceEntityType, e.SourceEntityId }).IsUnique();
                entity.HasOne(e => e.ProductionCycle).WithMany().HasForeignKey(e => e.ProductionCycleId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Pond).WithMany().HasForeignKey(e => e.PondId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.DestinationPond).WithMany().HasForeignKey(e => e.DestinationPondId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.StockMovement).WithMany().HasForeignKey(e => e.StockMovementId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.MortalityRecord).WithMany().HasForeignKey(e => e.MortalityRecordId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.CostRecord).WithMany().HasForeignKey(e => e.CostRecordId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.HarvestLot).WithMany().HasForeignKey(e => e.HarvestLotId).OnDelete(DeleteBehavior.Restrict);
                entity.ToTable(table =>
                {
                    table.HasCheckConstraint("CK_ProductionCostEvent_NonNegative", "CAST(QuantityKg AS NUMERIC) >= 0 AND CAST(Amount AS NUMERIC) >= 0");
                    table.HasCheckConstraint("CK_ProductionCostEvent_TransferShape",
                        "(EventType = 4 AND DestinationPondId IS NOT NULL AND DestinationPondId <> PondId AND CAST(QuantityKg AS NUMERIC) > 0 AND CAST(Amount AS NUMERIC) > 0) OR " +
                        "(EventType <> 4 AND DestinationPondId IS NULL)");
                });
            });

            modelBuilder.Entity<InventoryLedgerReconciliation>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.InventorySubledgerValue).HasPrecision(18, 2);
                entity.Property(e => e.InventoryGeneralLedgerValue).HasPrecision(18, 2);
                entity.Property(e => e.InventoryDifference).HasPrecision(18, 2);
                entity.Property(e => e.WorkInProgressSubledgerValue).HasPrecision(18, 2);
                entity.Property(e => e.WorkInProgressGeneralLedgerValue).HasPrecision(18, 2);
                entity.Property(e => e.WorkInProgressDifference).HasPrecision(18, 2);
                entity.Property(e => e.EvidenceJson).IsRequired();
                entity.Property(e => e.CreatedBy).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Reason).IsRequired().HasMaxLength(500);
                entity.HasIndex(e => e.AsOfDate);
                entity.ToTable(table =>
                {
                    table.HasCheckConstraint("CK_InventoryLedgerReconciliation_Counts",
                        "InventoryItemCount >= 0 AND QuantityExceptionCount >= 0 AND ValuationExceptionCount >= 0");
                });
            });

            modelBuilder.Entity<ControlledDocument>(entity =>
            {
                entity.HasKey(value => value.Id);
                entity.Property(value => value.DocumentCode).IsRequired().HasMaxLength(50);
                entity.Property(value => value.Version).IsRequired().HasMaxLength(30);
                entity.Property(value => value.Title).IsRequired().HasMaxLength(200);
                entity.Property(value => value.Category).IsRequired().HasMaxLength(100);
                entity.Property(value => value.Owner).IsRequired().HasMaxLength(100);
                entity.Property(value => value.StorageReference).IsRequired().HasMaxLength(500);
                entity.Property(value => value.ContentSha256).IsRequired().HasMaxLength(64);
                entity.Property(value => value.CreatedBy).IsRequired().HasMaxLength(100);
                entity.HasIndex(value => new { value.DocumentCode, value.Version }).IsUnique();
                entity.ToTable(table => table.HasCheckConstraint("CK_ControlledDocument_Approval",
                    "Status <> 2 OR (ApprovedBy IS NOT NULL AND ApprovedAtUtc IS NOT NULL AND EffectiveDate IS NOT NULL AND ApprovalReason IS NOT NULL)"));
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

                entity.Property(e => e.FailedLoginCount)
                    .HasDefaultValue(0);

                entity.Property(e => e.MustChangePassword)
                    .HasDefaultValue(false);
                
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

        private static void ConfigureSecurityAuditSystem(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SecurityAuditEvent>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.OccurredAtUtc).IsRequired();
                entity.Property(e => e.Category).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Action).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Outcome).IsRequired().HasMaxLength(30);
                entity.Property(e => e.ActorUsername).IsRequired().HasMaxLength(100);
                entity.Property(e => e.SubjectType).HasMaxLength(100);
                entity.Property(e => e.SubjectId).HasMaxLength(100);
                entity.Property(e => e.CorrelationId).IsRequired().HasMaxLength(64);
                entity.Property(e => e.Details).HasMaxLength(1000);
                entity.HasIndex(e => e.OccurredAtUtc);
                entity.HasIndex(e => new { e.Category, e.Action });
            });
        }

        private static void ConfigureAccountingSystem(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<LedgerAccount>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Code).IsRequired().HasMaxLength(30);
                entity.HasIndex(e => e.Code).IsUnique();
                entity.Property(e => e.NameAr).IsRequired().HasMaxLength(200);
                entity.Property(e => e.NameEn).HasMaxLength(200);
                entity.Property(e => e.CurrencyCode).IsRequired().HasMaxLength(3);
                entity.HasOne(e => e.ParentAccount).WithMany(e => e.Children)
                    .HasForeignKey(e => e.ParentAccountId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<FiscalYear>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
                entity.HasIndex(e => e.Name).IsUnique();
                entity.Property(e => e.ClosedBy).HasMaxLength(100);
                entity.HasIndex(e => e.OpeningBalanceJournalEntryId).IsUnique();
                entity.HasIndex(e => e.ClosingJournalEntryId).IsUnique();
            });

            modelBuilder.Entity<FiscalPeriod>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
                entity.HasIndex(e => new { e.FiscalYearId, e.StartDate, e.EndDate }).IsUnique();
                entity.Property(e => e.ClosedBy).HasMaxLength(100);
                entity.HasOne(e => e.FiscalYear).WithMany(e => e.Periods)
                    .HasForeignKey(e => e.FiscalYearId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<CostCenter>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Code).IsRequired().HasMaxLength(30);
                entity.HasIndex(e => e.Code).IsUnique();
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.ExternalReference).HasMaxLength(100);
            });

            modelBuilder.Entity<JournalEntry>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.SequenceNumber).IsUnique();
                entity.Property(e => e.EntryNumber).IsRequired().HasMaxLength(30);
                entity.HasIndex(e => e.EntryNumber).IsUnique();
                entity.Property(e => e.Description).IsRequired().HasMaxLength(500);
                entity.Property(e => e.Source).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Reference).HasMaxLength(100);
                entity.Property(e => e.CreatedBy).IsRequired().HasMaxLength(100);
                entity.Property(e => e.ApprovedBy).HasMaxLength(100);
                entity.Property(e => e.PostedBy).HasMaxLength(100);
                entity.Property(e => e.ReversedBy).HasMaxLength(100);
                entity.HasOne(e => e.FiscalPeriod).WithMany(e => e.JournalEntries)
                    .HasForeignKey(e => e.FiscalPeriodId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.ReversalOfJournalEntry).WithMany(e => e.Reversals)
                    .HasForeignKey(e => e.ReversalOfJournalEntryId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<JournalEntryLine>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.JournalEntryId, e.LineNumber }).IsUnique();
                entity.Property(e => e.Debit).HasPrecision(18, 2);
                entity.Property(e => e.Credit).HasPrecision(18, 2);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.ForeignCurrencyCode).HasMaxLength(3);
                entity.Property(e => e.ForeignAmount).HasPrecision(18, 8);
                entity.Property(e => e.ExchangeRateSarPerUnit).HasPrecision(18, 8);
                entity.HasOne(e => e.JournalEntry).WithMany(e => e.Lines)
                    .HasForeignKey(e => e.JournalEntryId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.LedgerAccount).WithMany(e => e.JournalLines)
                    .HasForeignKey(e => e.LedgerAccountId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.CostCenter).WithMany(e => e.JournalLines)
                    .HasForeignKey(e => e.CostCenterId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.ForeignExchangeRate).WithMany()
                    .HasForeignKey(e => e.ForeignExchangeRateId).OnDelete(DeleteBehavior.Restrict);
                entity.ToTable(table =>
                {
                    table.HasCheckConstraint("CK_JournalEntryLine_NonNegative", "CAST(Debit AS NUMERIC) >= 0 AND CAST(Credit AS NUMERIC) >= 0");
                    table.HasCheckConstraint("CK_JournalEntryLine_OneSide", "(CAST(Debit AS NUMERIC) > 0 AND CAST(Credit AS NUMERIC) = 0) OR (CAST(Credit AS NUMERIC) > 0 AND CAST(Debit AS NUMERIC) = 0)");
                    table.HasCheckConstraint("CK_JournalEntryLine_ForeignMeasurementComplete",
                        "(ForeignCurrencyCode IS NULL AND ForeignAmount IS NULL AND ForeignExchangeRateId IS NULL AND ExchangeRateSarPerUnit IS NULL) OR " +
                        "(ForeignCurrencyCode IS NOT NULL AND ForeignAmount IS NOT NULL AND ForeignExchangeRateId IS NOT NULL AND ExchangeRateSarPerUnit IS NOT NULL AND UPPER(ForeignCurrencyCode) <> 'SAR' AND CAST(ForeignAmount AS NUMERIC) > 0 AND CAST(ExchangeRateSarPerUnit AS NUMERIC) > 0)");
                });
            });

            modelBuilder.Entity<AccountingSequence>(entity =>
            {
                entity.HasKey(e => e.Name);
                entity.Property(e => e.Name).HasMaxLength(50);
            });

            modelBuilder.Entity<AccountingAuditEvent>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.EntityType).IsRequired().HasMaxLength(100);
                entity.Property(e => e.EntityId).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Action).IsRequired().HasMaxLength(100);
                entity.Property(e => e.ActorUsername).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Reason).IsRequired().HasMaxLength(500);
                entity.Property(e => e.BeforeJson).HasMaxLength(8000);
                entity.Property(e => e.AfterJson).HasMaxLength(8000);
                entity.Property(e => e.CorrelationId).IsRequired().HasMaxLength(64);
                entity.HasIndex(e => new { e.EntityType, e.EntityId, e.OccurredAtUtc });
            });

            modelBuilder.Entity<AccountingConfiguration>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.CreatedBy).IsRequired().HasMaxLength(100);
                entity.Property(e => e.ApprovedBy).HasMaxLength(100);
                entity.HasIndex(e => new { e.Name, e.Version }).IsUnique();
            });

            modelBuilder.Entity<PostingMapping>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.AccountingConfigurationId, e.EventType, e.Component }).IsUnique();
                entity.HasOne(e => e.AccountingConfiguration).WithMany(e => e.PostingMappings)
                    .HasForeignKey(e => e.AccountingConfigurationId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.LedgerAccount).WithMany()
                    .HasForeignKey(e => e.LedgerAccountId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<OperationalPostingRecord>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.SourceEntityType).IsRequired().HasMaxLength(100);
                entity.Property(e => e.SourceEntityId).IsRequired().HasMaxLength(100);
                entity.Property(e => e.CreatedBy).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => new { e.EventType, e.SourceEntityType, e.SourceEntityId }).IsUnique();
                entity.HasIndex(e => e.JournalEntryId).IsUnique();
                entity.HasOne(e => e.JournalEntry).WithMany()
                    .HasForeignKey(e => e.JournalEntryId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<VatReturnLedgerReconciliation>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.VatReturnId, e.Version }).IsUnique();
                entity.Property(e => e.DeclaredOutputVat).HasPrecision(18, 2);
                entity.Property(e => e.DeclaredInputVat).HasPrecision(18, 2);
                entity.Property(e => e.LedgerOutputVat).HasPrecision(18, 2);
                entity.Property(e => e.LedgerInputVat).HasPrecision(18, 2);
                entity.Property(e => e.OutputDifference).HasPrecision(18, 2);
                entity.Property(e => e.InputDifference).HasPrecision(18, 2);
                entity.Property(e => e.CreatedBy).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Reason).IsRequired().HasMaxLength(500);
                entity.HasOne(e => e.VatReturn).WithMany()
                    .HasForeignKey(e => e.VatReturnId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<AccountingAdjustment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.SupportingDocumentReference).IsRequired().HasMaxLength(200);
                entity.Property(e => e.CreatedBy).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => e.JournalEntryId).IsUnique();
                entity.HasIndex(e => e.ReversalJournalEntryId).IsUnique();
                entity.HasOne(e => e.JournalEntry).WithMany()
                    .HasForeignKey(e => e.JournalEntryId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.ReversalJournalEntry).WithMany()
                    .HasForeignKey(e => e.ReversalJournalEntryId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ForeignExchangeRate>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.CurrencyCode).IsRequired().HasMaxLength(3);
                entity.Property(e => e.SarPerUnit).HasPrecision(18, 8);
                entity.Property(e => e.SourceReference).IsRequired().HasMaxLength(500);
                entity.Property(e => e.EvidenceReference).IsRequired().HasMaxLength(500);
                entity.Property(e => e.CreatedBy).IsRequired().HasMaxLength(100);
                entity.Property(e => e.ApprovedBy).HasMaxLength(100);
                entity.HasIndex(e => new { e.CurrencyCode, e.RateDate, e.Purpose, e.Version }).IsUnique();
                entity.ToTable(table => table.HasCheckConstraint(
                    "CK_ForeignExchangeRate_Positive", "CAST(SarPerUnit AS NUMERIC) > 0"));
            });

            modelBuilder.Entity<ForeignMonetaryItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Reference).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => e.Reference).IsUnique();
                entity.HasIndex(e => e.RecognitionJournalEntryLineId).IsUnique();
                entity.Property(e => e.CurrencyCode).IsRequired().HasMaxLength(3);
                entity.Property(e => e.OriginalForeignAmount).HasPrecision(18, 8);
                entity.Property(e => e.OutstandingForeignAmount).HasPrecision(18, 8);
                entity.Property(e => e.CarryingAmountSar).HasPrecision(18, 2);
                entity.Property(e => e.CreatedBy).IsRequired().HasMaxLength(100);
                entity.HasOne(e => e.RecognitionJournalEntryLine).WithMany()
                    .HasForeignKey(e => e.RecognitionJournalEntryLineId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.LedgerAccount).WithMany()
                    .HasForeignKey(e => e.LedgerAccountId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ForeignCurrencySettlement>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ForeignAmount).HasPrecision(18, 8);
                entity.Property(e => e.CarryingAmountReleasedSar).HasPrecision(18, 2);
                entity.Property(e => e.SettlementAmountSar).HasPrecision(18, 2);
                entity.Property(e => e.RealizedGainLossSar).HasPrecision(18, 2);
                entity.Property(e => e.CreatedBy).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => e.JournalEntryId).IsUnique();
                entity.HasOne(e => e.ForeignMonetaryItem).WithMany(e => e.Settlements)
                    .HasForeignKey(e => e.ForeignMonetaryItemId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.ForeignExchangeRate).WithMany()
                    .HasForeignKey(e => e.ForeignExchangeRateId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.JournalEntry).WithMany()
                    .HasForeignKey(e => e.JournalEntryId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ForeignCurrencyRevaluation>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.PreviousCarryingAmountSar).HasPrecision(18, 2);
                entity.Property(e => e.RevaluedCarryingAmountSar).HasPrecision(18, 2);
                entity.Property(e => e.UnrealizedGainLossSar).HasPrecision(18, 2);
                entity.Property(e => e.CreatedBy).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => new { e.ForeignMonetaryItemId, e.RevaluationDate }).IsUnique();
                entity.HasIndex(e => e.JournalEntryId).IsUnique();
                entity.HasOne(e => e.ForeignMonetaryItem).WithMany(e => e.Revaluations)
                    .HasForeignKey(e => e.ForeignMonetaryItemId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.ForeignExchangeRate).WithMany()
                    .HasForeignKey(e => e.ForeignExchangeRateId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.JournalEntry).WithMany()
                    .HasForeignKey(e => e.JournalEntryId).OnDelete(DeleteBehavior.Restrict);
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
                entity.Property(e => e.AdjustmentReason).HasMaxLength(500);

                // Relationships
                entity.HasOne(e => e.Customer)
                    .WithMany()
                    .HasForeignKey(e => e.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.OriginalTaxInvoice)
                    .WithMany(e => e.AdjustmentDocuments)
                    .HasForeignKey(e => e.OriginalTaxInvoiceId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.ToTable(table => table.HasCheckConstraint(
                    "CK_TaxInvoice_AdjustmentReference",
                    "(InvoiceType IN (3, 4) AND OriginalTaxInvoiceId IS NOT NULL AND length(trim(AdjustmentReason)) > 0) OR (InvoiceType IN (1, 2) AND OriginalTaxInvoiceId IS NULL)"));
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
