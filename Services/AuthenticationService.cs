using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using FishFarmManager.Data;
using FishFarmManager.Models;

namespace FishFarmManager.Services
{
    /// <summary>
    /// خدمة المصادقة المحسّنة - Enhanced Authentication Service
    /// تدير عمليات تسجيل الدخول والخروج وإدارة المستخدمين مع أمان متقدم
    /// Security Enhancements: BCrypt, Rate Limiting, Session Timeout, Password Complexity
    /// </summary>
    public class AuthenticationService
    {
        private readonly FishFarmContext _context;
        private static User? _currentUser;
        private static DateTime _lastActivityTime = DateTime.Now;
        
        // Session Configuration
        private const int SessionTimeoutMinutes = 30;
        
        // Rate Limiting Configuration
        private static readonly Dictionary<string, LoginAttempt> _loginAttempts = new();
        private const int MaxLoginAttempts = 5;
        private const int LockoutMinutes = 15;
        
        // BCrypt Work Factor (cost parameter)
        private const int BCryptWorkFactor = 12;

        public AuthenticationService(FishFarmContext context)
        {
            _context = context;
        }

        #region Properties

        /// <summary>
        /// المستخدم الحالي المسجل دخوله
        /// </summary>
        public static User? CurrentUser => _currentUser;

        /// <summary>
        /// اسم المستخدم الحالي
        /// </summary>
        public static string CurrentUsername => _currentUser?.Username ?? "غير معروف";

        /// <summary>
        /// اسم المستخدم الكامل
        /// </summary>
        public static string CurrentUserFullName => _currentUser?.FullName ?? "غير معروف";

        /// <summary>
        /// دور المستخدم الحالي
        /// </summary>
        public static UserRole CurrentUserRole => _currentUser?.Role ?? UserRole.Viewer;

        /// <summary>
        /// هل المستخدم الحالي مدير؟
        /// </summary>
        public static bool IsAdmin => _currentUser?.Role == UserRole.Admin;

        /// <summary>
        /// هل المستخدم الحالي مدير أو مشرف؟
        /// </summary>
        public static bool IsManagerOrAdmin => _currentUser?.Role == UserRole.Admin || _currentUser?.Role == UserRole.Manager;

        /// <summary>
        /// آخر وقت نشاط
        /// </summary>
        public static DateTime LastActivityTime => _lastActivityTime;

        #endregion

        #region Authentication Methods

        /// <summary>
        /// تسجيل دخول المستخدم مع Rate Limiting
        /// </summary>
        public bool Login(string username, string password)
        {
            try
            {
                // ✅ 1. Validation
                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                {
                    LoggingService.LogWarning("محاولة تسجيل دخول ببيانات فارغة");
                    return false;
                }

                // ✅ 2. Check Rate Limiting
                if (IsAccountLocked(username))
                {
                    LoggingService.LogWarning($"🚫 الحساب مقفل: {username} بسبب محاولات متعددة فاشلة");
                    return false;
                }

                // ✅ 3. Find User
                var user = _context.Users
                    .Include(u => u.Employee)
                    .FirstOrDefault(u => u.Username.ToLower() == username.ToLower());

                if (user == null || !user.IsActive)
                {
                    RecordFailedAttempt(username);
                    LoggingService.LogWarning($"⚠️ محاولة تسجيل دخول فاشلة: مستخدم غير موجود أو معطل - {username}");
                    return false;
                }

                // ✅ 4. Verify Password (BCrypt)
                bool isPasswordValid = VerifyPassword(password, user.PasswordHash);
                
                if (!isPasswordValid)
                {
                    RecordFailedAttempt(username);
                    LoggingService.LogWarning($"⚠️ محاولة تسجيل دخول فاشلة: كلمة مرور خاطئة - {username}");
                    return false;
                }

                // ✅ 5. Success - Clear failed attempts
                ClearFailedAttempts(username);
                
                // ✅ 6. Set Current User
                _currentUser = user;
                _lastActivityTime = DateTime.Now;
                
                // ✅ 7. Update Last Login
                user.LastLoginAt = DateTime.Now;
                _context.SaveChanges();

                LoggingService.LogInfo($"✅ تسجيل دخول ناجح: {username} ({user.Role})");
                return true;
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "خطأ في تسجيل الدخول للمستخدم: {Username}", username);
                return false;
            }
        }

        /// <summary>
        /// تسجيل خروج المستخدم
        /// </summary>
        public void Logout()
        {
            if (_currentUser != null)
            {
                LoggingService.LogInfo($"📤 تسجيل خروج: {_currentUser.Username}");
            _currentUser = null;
                _lastActivityTime = DateTime.Now;
            }
        }

        /// <summary>
        /// التحقق من انتهاء الجلسة (Session Timeout)
        /// </summary>
        public static bool IsSessionExpired()
        {
            if (_currentUser == null)
                return true;
            
            var timeSinceLastActivity = DateTime.Now - _lastActivityTime;
            return timeSinceLastActivity.TotalMinutes > SessionTimeoutMinutes;
        }

        /// <summary>
        /// تحديث وقت آخر نشاط
        /// </summary>
        public static void UpdateActivity()
        {
            _lastActivityTime = DateTime.Now;
        }

        /// <summary>
        /// فرض تسجيل الخروج عند انتهاء الجلسة
        /// </summary>
        public static void CheckSessionTimeout()
        {
            if (IsSessionExpired())
            {
                LoggingService.LogWarning($"⏰ انتهت الجلسة تلقائياً: {CurrentUsername}");
                _currentUser = null;
            }
        }

        #endregion

        #region User Management

        /// <summary>
        /// إنشاء مستخدم جديد مع التحقق من قوة كلمة المرور
        /// </summary>
        public bool CreateUser(string username, string password, string fullName, string email, UserRole role, int? employeeId = null)
        {
            try
            {
                // ✅ 1. Validate Username
                if (!ValidateUsername(username))
                {
                    LoggingService.LogWarning($"❌ اسم المستخدم غير صالح: {username}");
                    return false;
                }

                // ✅ 2. Check Uniqueness
                if (_context.Users.Any(u => u.Username.ToLower() == username.ToLower()))
                {
                    LoggingService.LogWarning($"❌ اسم المستخدم موجود مسبقاً: {username}");
                    return false;
                }

                // ✅ 3. Validate Password Complexity
                var passwordValidation = ValidatePasswordComplexity(password);
                if (!passwordValidation.IsValid)
                {
                    LoggingService.LogWarning($"❌ كلمة المرور ضعيفة: {string.Join(", ", passwordValidation.Errors)}");
                    return false;
                }

                // ✅ 4. Hash Password (BCrypt)
                var passwordHash = HashPassword(password);

                // ✅ 5. Create User
                var user = new User
                {
                    Username = username,
                    PasswordHash = passwordHash,
                    FullName = fullName,
                    Email = email,
                    Role = role,
                    EmployeeId = employeeId,
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                    CreatedBy = CurrentUsername
                };

                _context.Users.Add(user);
                _context.SaveChanges();

                LoggingService.LogInfo($"✅ تم إنشاء مستخدم جديد: {username} بدور {role}");
                return true;
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "خطأ في إنشاء المستخدم: {Username}", username);
                return false;
            }
        }

        /// <summary>
        /// تغيير كلمة المرور مع التحقق من القوة
        /// </summary>
        public bool ChangePassword(int userId, string oldPassword, string newPassword)
        {
            try
            {
                var user = _context.Users.Find(userId);
                if (user == null)
                {
                    LoggingService.LogWarning($"❌ مستخدم غير موجود: {userId}");
                    return false;
                }

                // ✅ Verify old password (BCrypt)
                if (!VerifyPassword(oldPassword, user.PasswordHash))
                {
                    LoggingService.LogWarning($"❌ كلمة المرور القديمة خاطئة: {user.Username}");
                    return false;
                }

                // ✅ Validate new password
                var validation = ValidatePasswordComplexity(newPassword);
                if (!validation.IsValid)
                {
                    LoggingService.LogWarning($"❌ كلمة المرور الجديدة ضعيفة: {string.Join(", ", validation.Errors)}");
                    return false;
                }

                // ✅ Update password (BCrypt)
                user.PasswordHash = HashPassword(newPassword);
                user.UpdatedAt = DateTime.Now;
                user.UpdatedBy = CurrentUsername;

                _context.SaveChanges();
                
                LoggingService.LogInfo($"🔑 تم تغيير كلمة المرور: {user.Username}");
                return true;
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "خطأ في تغيير كلمة المرور للمستخدم: {UserId}", userId);
                return false;
            }
        }

        /// <summary>
        /// إعادة تعيين كلمة المرور (للمدير فقط)
        /// </summary>
        public bool ResetPassword(int userId, string newPassword)
        {
            try
            {
                if (!IsAdmin)
                {
                    LoggingService.LogWarning($"🚫 محاولة غير مصرح بها لإعادة تعيين كلمة المرور بواسطة: {CurrentUsername}");
                    return false;
                }

                var user = _context.Users.Find(userId);
                if (user == null)
                {
                    return false;
                }

                // ✅ Validate password
                var validation = ValidatePasswordComplexity(newPassword);
                if (!validation.IsValid)
                {
                    return false;
                }

                user.PasswordHash = HashPassword(newPassword);
                user.UpdatedAt = DateTime.Now;
                user.UpdatedBy = CurrentUsername;

                _context.SaveChanges();
                
                LoggingService.LogInfo($"🔑 تمت إعادة تعيين كلمة المرور بواسطة المدير: {user.Username}");
                return true;
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "خطأ في إعادة تعيين كلمة المرور للمستخدم: {UserId}", userId);
                return false;
            }
        }

        /// <summary>
        /// تفعيل/إلغاء تفعيل المستخدم
        /// </summary>
        public bool SetUserActiveStatus(int userId, bool isActive)
        {
            try
            {
                if (!IsAdmin)
                {
                    return false;
                }

                var user = _context.Users.Find(userId);
                if (user == null)
                {
                    return false;
                }

                user.IsActive = isActive;
                user.UpdatedAt = DateTime.Now;
                user.UpdatedBy = CurrentUsername;

                _context.SaveChanges();
                
                LoggingService.LogInfo($"👤 تم {(isActive ? "تفعيل" : "تعطيل")} المستخدم: {user.Username}");
                return true;
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "خطأ في تغيير حالة المستخدم: {UserId}", userId);
                return false;
            }
        }

        #endregion

        #region Permission Methods

        /// <summary>
        /// التحقق من الصلاحية مع فحص Session Timeout
        /// </summary>
        public static bool HasPermission(params UserRole[] allowedRoles)
        {
            // ✅ Check session timeout
            if (IsSessionExpired())
            {
                _currentUser = null;
                return false;
            }

            if (_currentUser == null || !_currentUser.IsActive)
            {
                return false;
            }

            // ✅ Update activity
            UpdateActivity();

            // المدير له كل الصلاحيات
            if (_currentUser.Role == UserRole.Admin)
            {
                return true;
            }

            return allowedRoles.Contains(_currentUser.Role);
        }

        #endregion

        #region Password Security - BCrypt

        /// <summary>
        /// تشفير كلمة المرور باستخدام BCrypt (أفضل من SHA256)
        /// BCrypt includes automatic salt generation and configurable work factor
        /// </summary>
        public static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, BCryptWorkFactor);
        }

        /// <summary>
        /// التحقق من كلمة المرور باستخدام BCrypt
        /// </summary>
        public static bool VerifyPassword(string password, string hash)
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(password, hash);
            }
            catch
            {
                // If BCrypt fails, try legacy SHA256 (for backward compatibility)
                return hash == HashPasswordSHA256Legacy(password);
            }
        }

        /// <summary>
        /// Legacy SHA256 hashing (للتوافق مع كلمات المرور القديمة)
        /// </summary>
        private static string HashPasswordSHA256Legacy(string password)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                var builder = new System.Text.StringBuilder();
                foreach (byte b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }

        /// <summary>
        /// ترقية كلمة المرور القديمة إلى BCrypt
        /// </summary>
        public bool MigratePasswordToBCrypt(int userId, string currentPassword)
        {
            try
            {
                var user = _context.Users.Find(userId);
                if (user == null) return false;

                // Check if already BCrypt
                if (user.PasswordHash.StartsWith("$2"))
                    return true; // Already BCrypt

                // Verify with SHA256
                if (user.PasswordHash != HashPasswordSHA256Legacy(currentPassword))
                    return false;

                // Upgrade to BCrypt
                user.PasswordHash = HashPassword(currentPassword);
                user.UpdatedAt = DateTime.Now;
                _context.SaveChanges();

                LoggingService.LogInfo($"🔐 تمت ترقية كلمة المرور إلى BCrypt: {user.Username}");
                return true;
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"خطأ في ترقية كلمة المرور: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Password Complexity Validation

        /// <summary>
        /// التحقق من قوة كلمة المرور
        /// </summary>
        public static PasswordValidationResult ValidatePasswordComplexity(string password)
        {
            var result = new PasswordValidationResult();

            // ✅ Length check
            if (password.Length < 8)
            {
                result.IsValid = false;
                result.Errors.Add("كلمة المرور يجب أن تكون 8 أحرف على الأقل");
            }

            if (password.Length > 128)
            {
                result.IsValid = false;
                result.Errors.Add("كلمة المرور يجب ألا تتجاوز 128 حرف");
            }

            // ✅ Uppercase check
            if (!Regex.IsMatch(password, @"[A-Z]"))
            {
                result.Errors.Add("يجب أن تحتوي على حرف كبير واحد على الأقل (A-Z)");
            }

            // ✅ Lowercase check
            if (!Regex.IsMatch(password, @"[a-z]"))
            {
                result.Errors.Add("يجب أن تحتوي على حرف صغير واحد على الأقل (a-z)");
            }

            // ✅ Digit check
            if (!Regex.IsMatch(password, @"\d"))
            {
                result.Errors.Add("يجب أن تحتوي على رقم واحد على الأقل (0-9)");
            }

            // ✅ Special character check
            if (!Regex.IsMatch(password, @"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>/?]"))
            {
                result.Errors.Add("يجب أن تحتوي على رمز خاص واحد على الأقل (!@#$%...)");
            }

            // ✅ Common passwords check
            if (IsCommonPassword(password))
            {
                result.IsValid = false;
                result.Errors.Add("كلمة المرور شائعة جداً. الرجاء اختيار كلمة أقوى");
            }

            result.IsValid = result.Errors.Count == 0;
            return result;
        }

        /// <summary>
        /// التحقق من كلمات المرور الشائعة
        /// </summary>
        private static bool IsCommonPassword(string password)
        {
            var commonPasswords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "password", "123456", "12345678", "qwerty", "abc123",
                "password123", "admin", "admin123", "letmein", "welcome",
                "monkey", "1234567890", "password1", "123123", "111111"
            };

            return commonPasswords.Contains(password);
        }

        /// <summary>
        /// توليد كلمة مرور قوية
        /// </summary>
        public static string GenerateSecurePassword(int length = 12)
        {
            const string lowercase = "abcdefghijklmnopqrstuvwxyz";
            const string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string digits = "0123456789";
            const string special = "!@#$%^&*()_+-=[]{}|;:,.<>?";

            var random = new Random();
            var allChars = lowercase + uppercase + digits + special;

            // ✅ Ensure at least one from each category
            var password = new System.Text.StringBuilder();
            password.Append(lowercase[random.Next(lowercase.Length)]);
            password.Append(uppercase[random.Next(uppercase.Length)]);
            password.Append(digits[random.Next(digits.Length)]);
            password.Append(special[random.Next(special.Length)]);

            // ✅ Fill remaining with random
            for (int i = 4; i < length; i++)
            {
                password.Append(allChars[random.Next(allChars.Length)]);
            }

            // ✅ Shuffle
            return new string(password.ToString().OrderBy(x => random.Next()).ToArray());
        }

        /// <summary>
        /// التحقق من اسم المستخدم
        /// </summary>
        private static bool ValidateUsername(string username)
        {
            // ✅ Length: 3-50 characters
            if (username.Length < 3 || username.Length > 50)
                return false;

            // ✅ Only alphanumeric and underscore
            return Regex.IsMatch(username, @"^[a-zA-Z0-9_]+$");
        }

        #endregion

        #region Rate Limiting (Brute Force Protection)

        /// <summary>
        /// التحقق من قفل الحساب
        /// </summary>
        private static bool IsAccountLocked(string username)
        {
            if (!_loginAttempts.ContainsKey(username.ToLower()))
                return false;

            var attempt = _loginAttempts[username.ToLower()];

            // ✅ Check if still locked
            if (attempt.Count >= MaxLoginAttempts &&
                attempt.LastAttempt.AddMinutes(LockoutMinutes) > DateTime.Now)
            {
                return true;
            }

            // ✅ Reset if lockout expired
            if (attempt.LastAttempt.AddMinutes(LockoutMinutes) <= DateTime.Now)
            {
                _loginAttempts.Remove(username.ToLower());
            }

            return false;
        }

        /// <summary>
        /// تسجيل محاولة فاشلة
        /// </summary>
        private static void RecordFailedAttempt(string username)
        {
            var key = username.ToLower();
            
            if (!_loginAttempts.ContainsKey(key))
            {
                _loginAttempts[key] = new LoginAttempt();
            }

            _loginAttempts[key].Count++;
            _loginAttempts[key].LastAttempt = DateTime.Now;

            LoggingService.LogWarning(
                $"⚠️ محاولة تسجيل دخول فاشلة: {username} " +
                $"({_loginAttempts[key].Count}/{MaxLoginAttempts})"
            );

            if (_loginAttempts[key].Count >= MaxLoginAttempts)
            {
                LoggingService.LogWarning(
                    $"🚫 تم قفل الحساب: {username} لمدة {LockoutMinutes} دقيقة"
                );
            }
        }

        /// <summary>
        /// مسح المحاولات الفاشلة عند النجاح
        /// </summary>
        private static void ClearFailedAttempts(string username)
        {
            var key = username.ToLower();
            if (_loginAttempts.ContainsKey(key))
            {
                _loginAttempts.Remove(key);
            }
        }

        /// <summary>
        /// الحصول على عدد المحاولات المتبقية
        /// </summary>
        public static int GetRemainingAttempts(string username)
        {
            var key = username.ToLower();
            if (!_loginAttempts.ContainsKey(key))
                return MaxLoginAttempts;

            return Math.Max(0, MaxLoginAttempts - _loginAttempts[key].Count);
        }

        /// <summary>
        /// الحصول على وقت فك القفل
        /// </summary>
        public static DateTime? GetUnlockTime(string username)
        {
            var key = username.ToLower();
            if (!_loginAttempts.ContainsKey(key))
                return null;

            var attempt = _loginAttempts[key];
            if (attempt.Count >= MaxLoginAttempts)
            {
                return attempt.LastAttempt.AddMinutes(LockoutMinutes);
            }

            return null;
        }

        #endregion
    }

    #region Helper Classes

    /// <summary>
    /// محاولة تسجيل الدخول
    /// </summary>
    public class LoginAttempt
    {
        public int Count { get; set; }
        public DateTime LastAttempt { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// نتيجة التحقق من كلمة المرور
    /// </summary>
    public class PasswordValidationResult
    {
        public bool IsValid { get; set; } = true;
        public List<string> Errors { get; set; } = new List<string>();
    }

    #endregion
}
