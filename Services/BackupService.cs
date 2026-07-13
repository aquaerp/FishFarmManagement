using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FishFarmManager.Data;

namespace FishFarmManager.Services
{
    /// <summary>
    /// خدمة النسخ الاحتياطي التلقائي
    /// Automated Backup Service
    /// </summary>
    public class BackupService
    {
        private const string BackupFolder = "Backups";
        private const string DatabaseFile = "fishfarm.db";
        private const int MaxBackupsToKeep = 10; // Keep last 10 backups

        /// <summary>
        /// إنشاء نسخة احتياطية
        /// </summary>
        public async Task<bool> CreateBackup(BackupType type = BackupType.Manual)
        {
            return await Task.Run(() => CreateBackupInternal(type));
        }

        /// <summary>
        /// إنشاء نسخة احتياطية (Internal)
        /// </summary>
        private static bool CreateBackupInternal(BackupType type = BackupType.Manual)
        {
            try
            {
                // ✅ 1. Ensure backup folder exists
                if (!Directory.Exists(BackupFolder))
                {
                    Directory.CreateDirectory(BackupFolder);
                    LoggingService.LogInfo($"📁 تم إنشاء مجلد النسخ الاحتياطي: {BackupFolder}");
                }

                // ✅ 2. Generate filename
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var typePrefix = type.ToString().ToLower();
                var filename = $"fishfarm_{typePrefix}_{timestamp}.db";
                var backupPath = Path.Combine(BackupFolder, filename);

                // ✅ 3. Check if database exists
                if (!File.Exists(DatabaseFile))
                {
                    LoggingService.LogError($"❌ ملف قاعدة البيانات غير موجود: {DatabaseFile}");
                    return false;
                }

                // ✅ 4. Wait a moment for any pending operations
                System.Threading.Thread.Sleep(500);

                // ✅ 5. Create backup
                File.Copy(DatabaseFile, backupPath, overwrite: false);

                // ✅ 6. Verify backup
                if (!File.Exists(backupPath))
                {
                    LoggingService.LogError($"❌ فشل إنشاء النسخة الاحتياطية");
                    return false;
                }

                var fileInfo = new FileInfo(backupPath);
                var fileSizeMB = fileInfo.Length / (1024.0 * 1024.0);

                LoggingService.LogInfo(
                    $"✅ نسخ احتياطي ناجح ({type}): {filename} ({fileSizeMB:F2} MB)"
                );

                // ✅ 7. Clean old backups
                CleanOldBackups();

                return true;
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"❌ فشل النسخ الاحتياطي: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// استعادة من نسخة احتياطية
        /// </summary>
        public async Task<bool> RestoreBackup(string backupPath)
        {
            return await Task.Run(() => RestoreBackupInternal(backupPath));
        }

        /// <summary>
        /// استعادة من نسخة احتياطية (Internal)
        /// </summary>
        private static bool RestoreBackupInternal(string backupPath)
        {
            try
            {
                // ✅ 1. Verify backup exists
                if (!File.Exists(backupPath))
                {
                    LoggingService.LogError($"❌ النسخة الاحتياطية غير موجودة: {backupPath}");
                    return false;
                }

                // ✅ 2. Create safety backup before restore
                LoggingService.LogInfo("📦 إنشاء نسخة أمان قبل الاستعادة...");
                CreateBackupInternal(BackupType.PreRestore);

                // ✅ 3. Wait for any pending operations
                System.Threading.Thread.Sleep(500);

                // ✅ 4. Wait a moment for connections to close
                System.Threading.Thread.Sleep(1000);

                // ✅ 5. Restore
                File.Copy(backupPath, DatabaseFile, overwrite: true);

                // ✅ 6. Verify restoration
                if (File.Exists(DatabaseFile))
                {
                    LoggingService.LogInfo($"✅ تمت الاستعادة بنجاح من: {Path.GetFileName(backupPath)}");
                    return true;
                }

                LoggingService.LogError("❌ فشلت الاستعادة");
                return false;
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"❌ خطأ في الاستعادة: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// حذف النسخ القديمة (الاحتفاظ بآخر 10)
        /// </summary>
        private static void CleanOldBackups()
        {
            try
            {
                if (!Directory.Exists(BackupFolder))
                    return;

                var backups = Directory.GetFiles(BackupFolder, "*.db")
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.CreationTime)
                    .Skip(MaxBackupsToKeep)
                    .ToList();

                foreach (var backup in backups)
                {
                    try
                    {
                        backup.Delete();
                        LoggingService.LogInfo($"🗑️ حذف نسخة قديمة: {backup.Name}");
                    }
                    catch (Exception ex)
                    {
                        LoggingService.LogWarning($"⚠️ فشل حذف {backup.Name}: {ex.Message}");
                    }
                }

                if (backups.Count > 0)
                {
                    LoggingService.LogInfo($"🧹 تم تنظيف {backups.Count} نسخة قديمة");
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogWarning($"⚠️ فشل تنظيف النسخ القديمة: {ex.Message}");
            }
        }

        /// <summary>
        /// تصدير البيانات (placeholder)
        /// </summary>
        public async Task<bool> ExportToCSV()
        {
            await Task.Delay(100);
            LoggingService.LogInfo("📤 تصدير البيانات");
            return true;
        }

        /// <summary>
        /// الحصول على قائمة النسخ الاحتياطية
        /// </summary>
        public static BackupInfo[] GetBackups()
        {
            try
            {
                if (!Directory.Exists(BackupFolder))
                    return Array.Empty<BackupInfo>();

                return Directory.GetFiles(BackupFolder, "*.db")
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.CreationTime)
                    .Select(f => new BackupInfo
                    {
                        FileName = f.Name,
                        FullPath = f.FullName,
                        CreatedDate = f.CreationTime,
                        SizeMB = f.Length / (1024.0 * 1024.0),
                        Type = DetectBackupType(f.Name)
                    })
                    .ToArray();
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"❌ خطأ في قراءة النسخ الاحتياطية: {ex.Message}");
                return Array.Empty<BackupInfo>();
            }
        }

        /// <summary>
        /// اكتشاف نوع النسخة من اسم الملف
        /// </summary>
        private static BackupType DetectBackupType(string filename)
        {
            if (filename.Contains("_manual_"))
                return BackupType.Manual;
            if (filename.Contains("_scheduled_"))
                return BackupType.Scheduled;
            if (filename.Contains("_prerestore_"))
                return BackupType.PreRestore;
            if (filename.Contains("_preoperation_"))
                return BackupType.PreOperation;

            return BackupType.Manual;
        }

        /// <summary>
        /// الحصول على آخر نسخة احتياطية
        /// </summary>
        public static BackupInfo? GetLatestBackup()
        {
            var backups = GetBackups();
            return backups.Length > 0 ? backups[0] : null;
        }

        /// <summary>
        /// الحصول على عدد الأيام منذ آخر نسخة
        /// </summary>
        public static int GetDaysSinceLastBackup()
        {
            var latest = GetLatestBackup();
            if (latest == null)
                return int.MaxValue;

            return (DateTime.Now - latest.CreatedDate).Days;
        }

        /// <summary>
        /// التحقق من الحاجة للنسخ الاحتياطي
        /// </summary>
        public static bool NeedsBackup(int daysSinceLastBackup = 7)
        {
            return GetDaysSinceLastBackup() >= daysSinceLastBackup;
        }
    }

    /// <summary>
    /// نوع النسخة الاحتياطية
    /// </summary>
    public enum BackupType
    {
        Manual,         // يدوي
        Scheduled,      // مجدول تلقائي
        PreOperation,   // قبل عملية حرجة
        PreRestore      // قبل الاستعادة
    }

    /// <summary>
    /// معلومات النسخة الاحتياطية
    /// </summary>
    public class BackupInfo
    {
        public string FileName { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public double SizeMB { get; set; }
        public BackupType Type { get; set; }
    }
}
