using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using FishFarmManager.Data;
using FishFarmManager.Services;
using FishFarmManager.Forms;
using System.IO;
using System.Windows.Forms;
using System.Net.Http;

namespace FishFarmManager
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // تهيئة نظام Logging أولاً
            LoggingService.Initialize();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (_, args) => ShowUnexpectedError(args.Exception);
            AppDomain.CurrentDomain.UnhandledException += (_, args) =>
                ShowUnexpectedError(args.ExceptionObject as Exception ?? new Exception("Unknown fatal error"));

            try
            {
                LoggingService.LogInfo("بدء تشغيل التطبيق - AquaFarm Pro");

                // إعداد حقن التبعيات
                var services = new ServiceCollection();
                ConfigureServices(services);

                using var serviceProvider = services.BuildServiceProvider();

                // إنشاء قاعدة البيانات إذا لم تكن موجودة
                using (var scope = serviceProvider.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<FishFarmContext>();
                    
                    // تطبيق الـ Migrations بدون حذف قاعدة البيانات
                    LoggingService.LogInfo("بدء تطبيق Migrations...");
                    context.Database.Migrate();
                    StartupValidationService.ValidateDatabase(context);
                    LoggingService.LogInfo("✅ تم تطبيق Migrations بنجاح");
                    
                    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
                    var seedDemoData = configuration.GetValue("AppSettings:SeedDemoData", false);
                    if (seedDemoData)
                    {
#if DEBUG
                        // Demo data is useful during development, but must be disabled for production releases.
                        LoggingService.LogInfo("Demo data seeding is enabled.");
                        DataSeeder.SeedData(context);
#else
                        throw new InvalidOperationException("Demo data is not included in Release builds.");
#endif
                    }
                    else
                    {
                        LoggingService.LogInfo("Demo data seeding skipped by configuration.");
                        if (!context.Users.Any())
                        {
                            using var firstRunForm = scope.ServiceProvider.GetRequiredService<FirstRunAdminForm>();
                            if (firstRunForm.ShowDialog() != DialogResult.OK)
                            {
                                LoggingService.LogWarning("First-run administrator setup was cancelled.");
                                return;
                            }
                        }
                    }
                    LoggingService.LogInfo("Demo data seeding step completed.");
                }

                // عرض شاشة تسجيل الدخول أولاً
                LoggingService.LogInfo("عرض شاشة تسجيل الدخول");
                var loginForm = serviceProvider.GetRequiredService<LoginForm>();
                if (loginForm.ShowDialog() != DialogResult.OK)
                {
                    // إذا ألغى المستخدم تسجيل الدخول، أغلق التطبيق
                    LoggingService.LogInfo("تم إلغاء تسجيل الدخول - إغلاق التطبيق");
                    return;
                }

                LoggingService.LogUserActivity(
                    AuthenticationService.CurrentUsername,
                    "تسجيل الدخول",
                    $"الدور: {AuthenticationService.CurrentUserRole}"
                );

                // إذا نجح تسجيل الدخول، افتح النافذة الرئيسية
                LoggingService.LogInfo("فتح النافذة الرئيسية للمستخدم: {Username}", AuthenticationService.CurrentUsername);
                var mainForm = serviceProvider.GetRequiredService<MainForm>();
                Application.Run(mainForm);
            }
            catch (Exception ex)
            {
                LoggingService.LogFatal(ex, "خطأ فادح أثناء تشغيل التطبيق");
                ShowUnexpectedError(ex);
            }
            finally
            {
                // إغلاق نظام Logging عند الخروج
                LoggingService.Shutdown();
            }
        }

        private static void ConfigureServices(ServiceCollection services)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                .AddEnvironmentVariables("AQUAFARM_")
                .Build();

            var startupValidation = StartupValidationService.ValidateAndPrepare(configuration);
            foreach (var warning in startupValidation.Warnings)
            {
                LoggingService.LogWarning("Startup validation warning: {Warning}", warning);
            }

            services.AddSingleton<IConfiguration>(configuration);
            services.AddSingleton(startupValidation);

            // إضافة DbContext - IMPORTANT: Use Transient to avoid disposed context errors
            // Each form gets its own context instance that it can safely dispose
            var connectionString = RuntimePaths.ResolveConnectionString(configuration);
            services.AddDbContext<FishFarmContext>(
                options => options.UseSqlite(connectionString),
                ServiceLifetime.Transient  // ← Changed from Scoped to Transient
            );

            // إضافة الخدمات
            services.AddScoped<PerformanceCalculator>();
            services.AddScoped<NotificationService>();
            services.AddScoped<BackupService>();
            services.AddScoped<AuthenticationService>();
            services.AddScoped<SecurityAuditService>();
            services.AddScoped<GeneralLedgerService>();
            services.AddScoped<GeneralLedgerReportingService>();
            services.AddScoped<GeneralLedgerFinancialStatementService>();
            services.AddScoped<AccountingConfigurationService>();
            services.AddScoped<OperationalPostingService>();
            services.AddScoped<FiscalYearClosingService>();
            services.AddScoped<AccountingAdjustmentService>();
            services.AddScoped<ForeignExchangeRateService>();
            services.AddScoped<ForeignCurrencyMonetaryItemService>();
            services.AddHttpClient();

            // إضافة النماذج
            services.AddTransient<MainForm>();
            services.AddTransient<PondManagementForm>();
            services.AddTransient<ProductionCycleForm>();
            services.AddTransient<FeedingRecordForm>();
            services.AddTransient<WaterQualityForm>();
            services.AddTransient<MortalityRecordForm>();
            services.AddTransient<DashboardForm>();
            
            // Week 1: Customer & Sales Management
            services.AddTransient<CustomerForm>();
            services.AddTransient<SalesOrderForm>();
            services.AddTransient<CustomerPaymentForm>();
            services.AddTransient<SalesReportForm>();
            
            // Week 2: Cost & Supplier Management
            services.AddTransient<SupplierForm>();
            services.AddTransient<CostRecordForm>();
            services.AddTransient<SupplierPaymentForm>();
            services.AddTransient<CostAnalysisReportForm>();
            
            // Week 3: HR Management - Fixed Forms
            services.AddTransient<EmployeeForm>();
            services.AddTransient<AttendanceForm>();
            services.AddTransient<LeaveManagementForm>();
            services.AddTransient<SalaryProcessingForm>();
            services.AddTransient<HRReportsForm>();
            
            // Week 4: Inventory Management - Fixed Forms
            services.AddTransient<InventoryItemForm>();
            services.AddTransient<InventoryCountForm>();
            services.AddTransient<StockMovementForm>();
            
            // Authentication
            services.AddTransient<LoginForm>();
            services.AddTransient<FirstRunAdminForm>();
            
            // Financial Forms
            services.AddTransient<FinancialDashboardForm>();
            services.AddTransient<AccountingManagementForm>();
            services.AddTransient<PurchaseOrderForm>();
            services.AddTransient<PurchaseReceivingForm>();
            services.AddTransient<PurchaseReportsForm>();
            
            // System Tools
            services.AddTransient<LogViewerForm>();
            services.AddTransient<UserManagementForm>();
            services.AddTransient<AddEditUserForm>();
            services.AddTransient<SettingsForm>();
        }

        private static void ShowUnexpectedError(Exception exception)
        {
            var trackingId = Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();
            LoggingService.LogError(exception, "Unhandled application error. Tracking ID: {TrackingId}", trackingId);
            MessageBox.Show(
                $"حدث خطأ غير متوقع. رقم التتبع: {trackingId}",
                "خطأ",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}
