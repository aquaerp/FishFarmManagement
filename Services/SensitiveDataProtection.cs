using System;
using System.Security.Cryptography;
using System.Text;

namespace FishFarmManager.Services
{
    /// <summary>
    /// خدمة حماية البيانات الحساسة
    /// Sensitive Data Protection Service
    /// Uses Windows Data Protection API (DPAPI)
    /// </summary>
    public static class SensitiveDataProtection
    {
        /// <summary>
        /// تشفير البيانات الحساسة باستخدام Windows DPAPI
        /// </summary>
        /// <param name="data">البيانات المراد تشفيرها</param>
        /// <returns>البيانات المشفرة (Base64)</returns>
        public static string EncryptData(string data)
        {
            try
            {
                if (string.IsNullOrEmpty(data))
                    return string.Empty;

                // ✅ Convert to bytes
                byte[] dataBytes = Encoding.UTF8.GetBytes(data);

                // ✅ Encrypt using DPAPI
                byte[] encryptedBytes = ProtectedData.Protect(
                    dataBytes,
                    GetEntropy(),  // Optional entropy for extra security
                    DataProtectionScope.CurrentUser  // User-specific encryption
                );

                // ✅ Convert to Base64
                return Convert.ToBase64String(encryptedBytes);
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"❌ فشل تشفير البيانات: {ex.Message}");
                throw new CryptographicException("فشل تشفير البيانات", ex);
            }
        }

        /// <summary>
        /// فك تشفير البيانات
        /// </summary>
        /// <param name="encryptedData">البيانات المشفرة (Base64)</param>
        /// <returns>البيانات الأصلية</returns>
        public static string DecryptData(string encryptedData)
        {
            try
            {
                if (string.IsNullOrEmpty(encryptedData))
                    return string.Empty;

                // ✅ Convert from Base64
                byte[] encryptedBytes = Convert.FromBase64String(encryptedData);

                // ✅ Decrypt using DPAPI
                byte[] decryptedBytes = ProtectedData.Unprotect(
                    encryptedBytes,
                    GetEntropy(),
                    DataProtectionScope.CurrentUser
                );

                // ✅ Convert to string
                return Encoding.UTF8.GetString(decryptedBytes);
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"❌ فشل فك تشفير البيانات: {ex.Message}");
                throw new CryptographicException("فشل فك تشفير البيانات", ex);
            }
        }

        /// <summary>
        /// التحقق من أن البيانات مشفرة
        /// </summary>
        public static bool IsEncrypted(string data)
        {
            if (string.IsNullOrEmpty(data))
                return false;

            try
            {
                // Try to decode as Base64
                Convert.FromBase64String(data);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Entropy للأمان الإضافي
        /// </summary>
        private static byte[] GetEntropy()
        {
            // ✅ Application-specific entropy
            return Encoding.UTF8.GetBytes("AquaFarmPro_2025_SecureData");
        }

        /// <summary>
        /// تشفير API Key
        /// </summary>
        public static string EncryptApiKey(string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey))
                return string.Empty;

            var encrypted = EncryptData(apiKey);
            LoggingService.LogInfo("🔐 تم تشفير API Key");
            return encrypted;
        }

        /// <summary>
        /// فك تشفير API Key
        /// </summary>
        public static string DecryptApiKey(string encryptedApiKey)
        {
            if (string.IsNullOrEmpty(encryptedApiKey))
                return string.Empty;

            return DecryptData(encryptedApiKey);
        }

        /// <summary>
        /// تشفير معلومات حساسة في قاعدة البيانات
        /// </summary>
        public static string EncryptDatabaseField(string sensitiveData)
        {
            if (string.IsNullOrEmpty(sensitiveData))
                return string.Empty;

            return EncryptData(sensitiveData);
        }

        /// <summary>
        /// فك تشفير معلومات من قاعدة البيانات
        /// </summary>
        public static string DecryptDatabaseField(string encryptedData)
        {
            if (string.IsNullOrEmpty(encryptedData))
                return string.Empty;

            // Check if data is actually encrypted
            if (!IsEncrypted(encryptedData))
                return encryptedData; // Return as-is if not encrypted

            try
            {
                return DecryptData(encryptedData);
            }
            catch
            {
                // If decryption fails, return masked value
                return "***ENCRYPTED***";
            }
        }
    }
}

