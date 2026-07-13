using System;
using System.Collections.Generic;
using System.Linq;
using FishFarmManager.Data;
using FishFarmManager.Models;

namespace FishFarmManager.Services
{
    /// <summary>
    /// ماسح الأمان - Security Scanner
    /// يفحص النظام بحثاً عن مشاكل أمنية محتملة
    /// </summary>
    public class SecurityScanner
    {
        private readonly FishFarmContext _context;

        public SecurityScanner(FishFarmContext context)
        {
            _context = context;
        }

        /// <summary>
        /// فحص شامل للنظام
        /// </summary>
        public List<SecurityIssue> ScanSystem()
        {
            var issues = new List<SecurityIssue>();

            try
            {
                // ✅ 1. Check default passwords
                CheckDefaultPasswords(issues);

                // ✅ 2. Check inactive users
                CheckInactiveUsers(issues);

                // ✅ 3. Check old backups
                CheckBackupStatus(issues);

                // ✅ 4. Check admin accounts
                CheckAdminAccounts(issues);

                // ✅ 5. Check password ages
                CheckPasswordAges(issues);

                // ✅ 6. Check user permissions
                CheckUserPermissions(issues);

                LoggingService.LogInfo($"🔍 فحص أمني مكتمل: {issues.Count} مشكلة محتملة");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"❌ خطأ في الفحص الأمني: {ex.Message}");
            }

            return issues;
        }

        /// <summary>
        /// فحص كلمات المرور الافتراضية
        /// </summary>
        private void CheckDefaultPasswords(List<SecurityIssue> issues)
        {
            try
            {
                // Check for users who haven't changed password since creation
                var usersWithDefaultPasswords = _context.Users
                    .Where(u => u.IsActive && u.UpdatedAt == null && u.Username == "admin")
                    .ToList();

                if (usersWithDefaultPasswords.Any())
                {
                    issues.Add(new SecurityIssue
                    {
                        Severity = SecuritySeverity.Critical,
                        Category = "Authentication",
                        Title = "كلمة مرور افتراضية - Default Password",
                        Description = $"مستخدم 'admin' لم يغير كلمة المرور الافتراضية منذ الإنشاء",
                        Recommendation = "⚠️ يجب تغيير كلمة المرور فوراً! هذه ثغرة أمنية حرجة",
                        AffectedUsers = usersWithDefaultPasswords.Select(u => u.Username).ToList()
                    });
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogWarning($"⚠️ خطأ في فحص كلمات المرور الافتراضية: {ex.Message}");
            }
        }

        /// <summary>
        /// فحص المستخدمين غير النشطين
        /// </summary>
        private void CheckInactiveUsers(List<SecurityIssue> issues)
        {
            try
            {
                var threeMonthsAgo = DateTime.Now.AddMonths(-3);
                
                var inactiveUsers = _context.Users
                    .Where(u => u.IsActive && 
                               (u.LastLoginAt == null || u.LastLoginAt < threeMonthsAgo))
                    .ToList();

                if (inactiveUsers.Any())
                {
                    issues.Add(new SecurityIssue
                    {
                        Severity = SecuritySeverity.Medium,
                        Category = "User Management",
                        Title = "مستخدمين غير نشطين - Inactive Users",
                        Description = $"{inactiveUsers.Count} مستخدم لم يسجل دخول منذ 3 أشهر",
                        Recommendation = "مراجعة وتعطيل الحسابات غير المستخدمة لتقليل نقاط الضعف المحتملة",
                        AffectedUsers = inactiveUsers.Select(u => u.Username).ToList()
                    });
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogWarning($"⚠️ خطأ في فحص المستخدمين غير النشطين: {ex.Message}");
            }
        }

        /// <summary>
        /// فحص حالة النسخ الاحتياطي
        /// </summary>
        private void CheckBackupStatus(List<SecurityIssue> issues)
        {
            try
            {
                var daysSinceLastBackup = BackupService.GetDaysSinceLastBackup();

                if (daysSinceLastBackup > 7)
                {
                    issues.Add(new SecurityIssue
                    {
                        Severity = daysSinceLastBackup > 30 ? SecuritySeverity.Critical : SecuritySeverity.High,
                        Category = "Data Protection",
                        Title = "تأخر النسخ الاحتياطي - Backup Delay",
                        Description = $"آخر نسخة احتياطية منذ {daysSinceLastBackup} يوم",
                        Recommendation = daysSinceLastBackup > 30 
                            ? "⚠️ حرج جداً! قم بإنشاء نسخة احتياطية فوراً"
                            : "قم بإنشاء نسخة احتياطية في أقرب وقت"
                    });
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogWarning($"⚠️ خطأ في فحص النسخ الاحتياطي: {ex.Message}");
            }
        }

        /// <summary>
        /// فحص حسابات المديرين
        /// </summary>
        private void CheckAdminAccounts(List<SecurityIssue> issues)
        {
            try
            {
                var adminUsers = _context.Users
                    .Where(u => u.Role == UserRole.Admin && u.IsActive)
                    .ToList();

                // Too many admins?
                if (adminUsers.Count > 3)
                {
                    issues.Add(new SecurityIssue
                    {
                        Severity = SecuritySeverity.Medium,
                        Category = "Authorization",
                        Title = "عدد كبير من المديرين - Too Many Admins",
                        Description = $"يوجد {adminUsers.Count} مدير نشط في النظام",
                        Recommendation = "تقليل عدد المديرين إلى الحد الأدنى الضروري (يفضل 1-2)",
                        AffectedUsers = adminUsers.Select(u => u.Username).ToList()
                    });
                }

                // No admin accounts?
                if (adminUsers.Count == 0)
                {
                    issues.Add(new SecurityIssue
                    {
                        Severity = SecuritySeverity.Critical,
                        Category = "Authorization",
                        Title = "لا يوجد مدير - No Admin Account",
                        Description = "لا يوجد أي حساب مدير نشط في النظام",
                        Recommendation = "⚠️ حرج! قم بتفعيل أو إنشاء حساب مدير فوراً"
                    });
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogWarning($"⚠️ خطأ في فحص حسابات المديرين: {ex.Message}");
            }
        }

        /// <summary>
        /// فحص أعمار كلمات المرور
        /// </summary>
        private void CheckPasswordAges(List<SecurityIssue> issues)
        {
            try
            {
                var ninetyDaysAgo = DateTime.Now.AddDays(-90);
                
                var usersWithOldPasswords = _context.Users
                    .Where(u => u.IsActive && 
                               (u.UpdatedAt == null || u.UpdatedAt < ninetyDaysAgo))
                    .ToList();

                if (usersWithOldPasswords.Any())
                {
                    issues.Add(new SecurityIssue
                    {
                        Severity = SecuritySeverity.Low,
                        Category = "Password Policy",
                        Title = "كلمات مرور قديمة - Old Passwords",
                        Description = $"{usersWithOldPasswords.Count} مستخدم لم يغير كلمة المرور منذ 90 يوم",
                        Recommendation = "يُنصح بتغيير كلمات المرور بشكل دوري (كل 90 يوم)",
                        AffectedUsers = usersWithOldPasswords.Select(u => u.Username).ToList()
                    });
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogWarning($"⚠️ خطأ في فحص أعمار كلمات المرور: {ex.Message}");
            }
        }

        /// <summary>
        /// فحص صلاحيات المستخدمين
        /// </summary>
        private void CheckUserPermissions(List<SecurityIssue> issues)
        {
            try
            {
                // Check for users with elevated permissions
                var elevatedUsers = _context.Users
                    .Where(u => u.IsActive && 
                               (u.Role == UserRole.Admin || u.Role == UserRole.Manager))
                    .ToList();

                var totalUsers = _context.Users.Count(u => u.IsActive);
                var elevatedPercentage = totalUsers > 0 ? (elevatedUsers.Count * 100.0 / totalUsers) : 0;

                if (elevatedPercentage > 30)
                {
                    issues.Add(new SecurityIssue
                    {
                        Severity = SecuritySeverity.Medium,
                        Category = "Authorization",
                        Title = "نسبة عالية من الصلاحيات المرتفعة",
                        Description = $"{elevatedPercentage:F1}% من المستخدمين لديهم صلاحيات Admin أو Manager",
                        Recommendation = "مراجعة الصلاحيات وتطبيق مبدأ Least Privilege (أقل صلاحية ضرورية)",
                        AffectedUsers = elevatedUsers.Select(u => u.Username).ToList()
                    });
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogWarning($"⚠️ خطأ في فحص الصلاحيات: {ex.Message}");
            }
        }

        /// <summary>
        /// إنشاء تقرير أمني
        /// </summary>
        public SecurityAuditReport GenerateSecurityReport()
        {
            try
            {
                var report = new SecurityAuditReport
                {
                    GeneratedAt = DateTime.Now,
                    TotalUsers = _context.Users.Count(),
                    ActiveUsers = _context.Users.Count(u => u.IsActive),
                    InactiveUsers = _context.Users.Count(u => !u.IsActive),
                    AdminUsers = _context.Users.Where(u => u.Role == UserRole.Admin && u.IsActive).ToList(),
                    DaysSinceLastBackup = BackupService.GetDaysSinceLastBackup(),
                    Issues = ScanSystem()
                };

                // Get users without recent activity
                var threeMonthsAgo = DateTime.Now.AddMonths(-3);
                report.UsersWithoutRecentActivity = _context.Users
                    .Where(u => u.IsActive && (u.LastLoginAt == null || u.LastLoginAt < threeMonthsAgo))
                    .ToList();

                LoggingService.LogInfo($"📊 تم إنشاء تقرير أمني: {report.Issues.Count} مشكلة محتملة");

                return report;
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"❌ خطأ في إنشاء التقرير الأمني: {ex.Message}");
                return new SecurityAuditReport { GeneratedAt = DateTime.Now };
            }
        }
    }

    #region Supporting Classes

    /// <summary>
    /// مشكلة أمنية
    /// </summary>
    public class SecurityIssue
    {
        public SecuritySeverity Severity { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Recommendation { get; set; } = string.Empty;
        public List<string>? AffectedUsers { get; set; }

        public string GetSeverityIcon()
        {
            return Severity switch
            {
                SecuritySeverity.Critical => "🔴",
                SecuritySeverity.High => "🟠",
                SecuritySeverity.Medium => "🟡",
                SecuritySeverity.Low => "🟢",
                _ => "⚪"
            };
        }
    }

    /// <summary>
    /// مستوى خطورة المشكلة الأمنية
    /// </summary>
    public enum SecuritySeverity
    {
        Low = 1,        // منخفض
        Medium = 2,     // متوسط
        High = 3,       // عالي
        Critical = 4    // حرج
    }

    /// <summary>
    /// تقرير تدقيق أمني
    /// </summary>
    public class SecurityAuditReport
    {
        public DateTime GeneratedAt { get; set; }
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int InactiveUsers { get; set; }
        public List<User> AdminUsers { get; set; } = new List<User>();
        public List<User> UsersWithoutRecentActivity { get; set; } = new List<User>();
        public int DaysSinceLastBackup { get; set; }
        public List<SecurityIssue> Issues { get; set; } = new List<SecurityIssue>();

        /// <summary>
        /// إنشاء تقرير نصي
        /// </summary>
        public string GenerateTextReport()
        {
            var report = new System.Text.StringBuilder();

            report.AppendLine("🔒 تقرير التدقيق الأمني - Security Audit Report");
            report.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            report.AppendLine($"📅 التاريخ: {GeneratedAt:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine();

            report.AppendLine("📊 إحصائيات المستخدمين:");
            report.AppendLine($"  - الإجمالي: {TotalUsers}");
            report.AppendLine($"  - النشطين: {ActiveUsers}");
            report.AppendLine($"  - المعطلين: {InactiveUsers}");
            report.AppendLine($"  - المديرين: {AdminUsers.Count}");
            report.AppendLine();

            report.AppendLine("💾 النسخ الاحتياطي:");
            report.AppendLine($"  - منذ آخر نسخة: {DaysSinceLastBackup} يوم");
            report.AppendLine($"  - الحالة: {(DaysSinceLastBackup > 7 ? "⚠️ تأخر" : "✅ جيد")}");
            report.AppendLine();

            report.AppendLine($"⚠️ المشاكل الأمنية المكتشفة: {Issues.Count}");
            report.AppendLine();

            if (Issues.Any())
            {
                var groupedIssues = Issues.GroupBy(i => i.Severity).OrderByDescending(g => g.Key);

                foreach (var group in groupedIssues)
                {
                    report.AppendLine($"{GetSeverityName(group.Key)} ({group.Count()}):");
                    foreach (var issue in group)
                    {
                        report.AppendLine($"  {issue.GetSeverityIcon()} {issue.Title}");
                        report.AppendLine($"     {issue.Description}");
                        report.AppendLine($"     💡 {issue.Recommendation}");
                        
                        if (issue.AffectedUsers != null && issue.AffectedUsers.Any())
                        {
                            report.AppendLine($"     👥 المتأثرين: {string.Join(", ", issue.AffectedUsers)}");
                        }
                        
                        report.AppendLine();
                    }
                }
            }
            else
            {
                report.AppendLine("✅ لم يتم اكتشاف أي مشاكل أمنية!");
            }

            report.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

            return report.ToString();
        }

        private string GetSeverityName(SecuritySeverity severity)
        {
            return severity switch
            {
                SecuritySeverity.Critical => "🔴 حرج (Critical)",
                SecuritySeverity.High => "🟠 عالي (High)",
                SecuritySeverity.Medium => "🟡 متوسط (Medium)",
                SecuritySeverity.Low => "🟢 منخفض (Low)",
                _ => "⚪ غير محدد"
            };
        }
    }

    #endregion
}

