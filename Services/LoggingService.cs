using Serilog;
using Serilog.Events;
using System;
using System.IO;

namespace FishFarmManager.Services
{
    /// <summary>
    /// خدمة تسجيل الأحداث - Logging Service
    /// تدير تسجيل الأحداث والأخطاء باستخدام Serilog
    /// </summary>
    public static class LoggingService
    {
        private static bool _isInitialized = false;
        private static readonly object _lock = new object();

        /// <summary>
        /// تهيئة نظام Logging
        /// </summary>
        public static void Initialize()
        {
            lock (_lock)
            {
                if (_isInitialized)
                    return;

                try
                {
                    // تحديد مسار مجلد Logs
                    string appDataPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "FishFarmManager"
                    );
                    
                    string logsPath = Path.Combine(appDataPath, "Logs");
                    Directory.CreateDirectory(logsPath);

                    // تكوين Serilog
                    Log.Logger = new LoggerConfiguration()
                        .MinimumLevel.Debug()
                        .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                        .MinimumLevel.Override("System", LogEventLevel.Warning)
                        .Enrich.FromLogContext()
                        .Enrich.WithProperty("Application", "AquaFarm Pro")
                        .Enrich.WithProperty("MachineName", Environment.MachineName)
                        .Enrich.WithProperty("UserName", Environment.UserName)
                        .WriteTo.Console(
                            restrictedToMinimumLevel: LogEventLevel.Debug,
                            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
                        )
                        .WriteTo.File(
                            path: Path.Combine(logsPath, "aquafarm-.log"),
                            rollingInterval: RollingInterval.Day,
                            retainedFileCountLimit: 30, // الاحتفاظ بـ 30 يوم
                            fileSizeLimitBytes: 10_485_760, // 10 MB
                            rollOnFileSizeLimit: true,
                            outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}"
                        )
                        .WriteTo.File(
                            path: Path.Combine(logsPath, "errors-.log"),
                            rollingInterval: RollingInterval.Day,
                            retainedFileCountLimit: 90, // الاحتفاظ بسجل الأخطاء لـ 90 يوم
                            restrictedToMinimumLevel: LogEventLevel.Error,
                            outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}{NewLine}---{NewLine}"
                        )
                        .CreateLogger();

                    _isInitialized = true;

                    Log.Information("====================================");
                    Log.Information("نظام AquaFarm Pro - بدء التشغيل");
                    Log.Information("الإصدار: 1.0.0");
                    Log.Information("التاريخ: {Date}", DateTime.Now);
                    Log.Information("المستخدم: {User}", Environment.UserName);
                    Log.Information("الجهاز: {Machine}", Environment.MachineName);
                    Log.Information("نظام التشغيل: {OS}", Environment.OSVersion);
                    Log.Information("مسار Logs: {LogsPath}", logsPath);
                    Log.Information("====================================");
                }
                catch (Exception ex)
                {
                    // في حالة فشل تهيئة Logging، استخدم Console
                    Console.WriteLine($"[خطأ] فشل تهيئة نظام Logging: {ex.Message}");
                    Console.WriteLine(ex.StackTrace);
                }
            }
        }

        /// <summary>
        /// إغلاق وتنظيف نظام Logging
        /// </summary>
        public static void Shutdown()
        {
            if (_isInitialized)
            {
                Log.Information("====================================");
                Log.Information("نظام AquaFarm Pro - إيقاف التشغيل");
                Log.Information("التاريخ: {Date}", DateTime.Now);
                Log.Information("====================================");
                Log.CloseAndFlush();
                _isInitialized = false;
            }
        }

        /// <summary>
        /// تسجيل معلومة عامة
        /// </summary>
        public static void LogInfo(string messageTemplate, params object[] propertyValues)
        {
            Log.Information(messageTemplate, propertyValues);
        }

        /// <summary>
        /// تسجيل تحذير
        /// </summary>
        public static void LogWarning(string messageTemplate, params object[] propertyValues)
        {
            Log.Warning(messageTemplate, propertyValues);
        }

        /// <summary>
        /// تسجيل خطأ
        /// </summary>
        public static void LogError(string messageTemplate, params object[] propertyValues)
        {
            Log.Error(messageTemplate, propertyValues);
        }

        /// <summary>
        /// تسجيل خطأ مع Exception
        /// </summary>
        public static void LogError(Exception exception, string messageTemplate, params object[] propertyValues)
        {
            Log.Error(exception, messageTemplate, propertyValues);
        }

        /// <summary>
        /// تسجيل خطأ فادح
        /// </summary>
        public static void LogFatal(Exception exception, string messageTemplate, params object[] propertyValues)
        {
            Log.Fatal(exception, messageTemplate, propertyValues);
        }

        /// <summary>
        /// تسجيل Debug (للتطوير)
        /// </summary>
        public static void LogDebug(string messageTemplate, params object[] propertyValues)
        {
            Log.Debug(messageTemplate, propertyValues);
        }

        /// <summary>
        /// تسجيل نشاط مستخدم
        /// </summary>
        public static void LogUserActivity(string username, string action, string details = "")
        {
            Log.Information("[نشاط مستخدم] المستخدم: {Username}, الإجراء: {Action}, التفاصيل: {Details}", 
                username, action, details);
        }

        /// <summary>
        /// تسجيل عملية قاعدة بيانات
        /// </summary>
        public static void LogDatabaseOperation(string operation, string tableName, string details = "")
        {
            Log.Debug("[قاعدة البيانات] العملية: {Operation}, الجدول: {Table}, التفاصيل: {Details}", 
                operation, tableName, details);
        }

        /// <summary>
        /// تسجيل بداية عملية
        /// </summary>
        public static void LogOperationStart(string operationName, object? parameters = null)
        {
            if (parameters != null)
            {
                Log.Information("[بداية] {Operation} - المعاملات: {@Parameters}", operationName, parameters);
            }
            else
            {
                Log.Information("[بداية] {Operation}", operationName);
            }
        }

        /// <summary>
        /// تسجيل نهاية عملية ناجحة
        /// </summary>
        public static void LogOperationSuccess(string operationName, object? result = null)
        {
            if (result != null)
            {
                Log.Information("[نجاح] {Operation} - النتيجة: {@Result}", operationName, result);
            }
            else
            {
                Log.Information("[نجاح] {Operation}", operationName);
            }
        }

        /// <summary>
        /// تسجيل فشل عملية
        /// </summary>
        public static void LogOperationFailure(string operationName, Exception exception)
        {
            Log.Error(exception, "[فشل] {Operation} - الخطأ: {ErrorMessage}", operationName, exception.Message);
        }

        /// <summary>
        /// تسجيل أداء عملية
        /// </summary>
        public static void LogPerformance(string operationName, TimeSpan duration, object? additionalInfo = null)
        {
            if (additionalInfo != null)
            {
                Log.Information("[أداء] {Operation} - المدة: {Duration}ms - معلومات إضافية: {@Info}", 
                    operationName, duration.TotalMilliseconds, additionalInfo);
            }
            else
            {
                Log.Information("[أداء] {Operation} - المدة: {Duration}ms", 
                    operationName, duration.TotalMilliseconds);
            }
        }

        /// <summary>
        /// الحصول على مسار مجلد Logs
        /// </summary>
        public static string GetLogsPath()
        {
            string appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "FishFarmManager"
            );
            return Path.Combine(appDataPath, "Logs");
        }

        /// <summary>
        /// هل النظام مهيأ؟
        /// </summary>
        public static bool IsInitialized => _isInitialized;
    }
}
