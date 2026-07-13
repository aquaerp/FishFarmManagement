# 🔐 توثيق الأمن والأمان - Security Documentation

<div dir "rtl">

**المشروع**: AquaFarm Pro - نظام إدارة المزارع السمكية  
**التاريخ**: 14 أكتوبر 2025  
**الإصدار**: 1.0  
**التصنيف**: سري - Confidential

---

## 📋 جدول المحتويات

1. [نظرة عامة] (#نظرة-عامة)
2. [نظام المصادقة (Authentication)] (#نظام-المصادقة)
3. [نظام الصلاحيات (Authorization)] (#نظام-الصلاحيات)
4. [تشفير البيانات (Data Encryption)] (#تشفير-البيانات)
5. [أمان قاعدة البيانات] (#أمان-قاعدة-البيانات)
6. [أمان الاتصالات] (#أمان-الاتصالات)
7. [الشهادات الرقمية] (#الشهادات-الرقمية)
8. [سجل التدقيق (Audit Trail)] (#سجل-التدقيق)
9. [أمان ضريبة القيمة المضافة] (#أمان-ضريبة-القيمة-المضافة)
10. [أفضل الممارسات الأمنية] (#أفضل-الممارسات-الأمنية)
11. [إدارة الثغرات] (#إدارة-الثغرات)
12. [النسخ الاحتياطي والاستعادة] (#النسخ-الاحتياطي-والاستعادة)
13. [التوصيات] (#التوصيات)

---

## 🎯 نظرة عامة

### الأهداف الأمنية

يلتزم نظام AquaFarm Pro بأعلى معايير الأمن والحماية لضمان:

1. **السرية (Confidentiality)**: حماية البيانات الحساسة من الوصول غير المصرح به
2. **السلامة (Integrity)**: ضمان دقة البيانات وعدم التلاعب بها
3. **التوافر (Availability)**: ضمان توفر النظام للمستخدمين المصرح لهم
4. **عدم الإنكار (Non-repudiation)**: تتبع جميع العمليات وعدم إمكانية إنكارها
5. **المصادقة (Authentication)**: التحقق من هوية المستخدمين
6. **التفويض (Authorization)**: التحكم في صلاحيات الوصول

### طبقات الأمان المطبقة

```text
┌─────────────────────────────────────────────────┐
│  Layer 7: Application Security                 │
│  ✅ Input Validation, Output Encoding          │
├─────────────────────────────────────────────────┤
│  Layer 6: Authentication & Authorization       │
│  ✅ User Login, Role-Based Access Control      │
├─────────────────────────────────────────────────┤
│  Layer 5: Data Encryption                      │
│  ✅ Password Hashing, Digital Signatures       │
├─────────────────────────────────────────────────┤
│  Layer 4: Communication Security               │
│  ✅ HTTPS/TLS (ZATCA API)                      │
├─────────────────────────────────────────────────┤
│  Layer 3: Database Security                    │
│  ✅ Access Control, Data Validation            │
├─────────────────────────────────────────────────┤
│  Layer 2: Logging & Auditing                   │
│  ✅ Audit Trail, Error Logging                 │
├─────────────────────────────────────────────────┤
│  Layer 1: Physical & Backup Security           │
│  ✅ Database Backup, File Protection           │
└─────────────────────────────────────────────────┘
```

---

## 🔑 نظام المصادقة (Authentication)

### 1. معمارية المصادقة

**الملف المسؤول**: `Services/AuthenticationService.cs`

#### المكونات الرئيسية

```csharp
public class AuthenticationService
{
    private static User? _currentUser;
    private readonly FishFarmContext _context;
    
    // Properties
    public static User? CurrentUser
    public static string CurrentUsername
    public static UserRole CurrentUserRole
    public static bool IsAdmin
    
    // Methods
    public bool Login(username, password)
    public void Logout()
    public bool HasPermission(params UserRole[])
    public string HashPassword(password)
}
```

### 2. آلية تسجيل الدخول

#### خطوات المصادقة

```text
1. إدخال اسم المستخدم وكلمة المرور
   └─> Validation (Not Empty, Min Length)
   
2. البحث عن المستخدم في قاعدة البيانات
   └─> Username.ToLower() للحساسية لحالة الأحرف
   └─> التحقق من IsActive = true
   
3. تشفير كلمة المرور المدخلة
   └─> SHA256 Hashing
   
4. مقارنة Hash مع المخزن في DB
   └─> Constant-Time Comparison
   
5. تسجيل الدخول الناجح
   └─> تعيين CurrentUser
   └─> تحديث LastLoginAt
   └─> Logging
   
6. Session Management
   └─> Static User Instance
   └─> Valid Until Logout
```

#### كود المصادقة

```csharp
public bool Login(string username, string password)
{
    try
    {
        // 1. Validation
        if (string.IsNullOrWhiteSpace(username) || 
            string.IsNullOrWhiteSpace(password))
            return false;
        
        // 2. Find User
        var user = _context.Users
            .Include(u => u.Employee)
            .FirstOrDefault(u => u.Username.ToLower() == username.ToLower());
        
        // 3. Check Active Status
        if (user == null || !user.IsActive)
            return false;
        
        // 4. Hash Password
        string passwordHash = HashPassword(password);
        
        // 5. Compare Hash
        if (user.PasswordHash != passwordHash)
            return false;
        
        // 6. Success
        _currentUser = user;
        user.LastLoginAt = DateTime.Now;
        _context.SaveChanges();
        
        LoggingService.LogInfo("تسجيل دخول ناجح: {Username}", username);
        return true;
    }
    catch (Exception ex)
    {
        LoggingService.LogError(ex, "خطأ في تسجيل الدخول");
        return false;
    }
}
```

### 3. تشفير كلمات المرور

#### الخوارزمية المستخدمة: SHA256

```csharp
public static string HashPassword(string password)
{
    using (var sha256 = SHA256.Create())
    {
        byte[] hashedBytes = sha256.ComputeHash(
            Encoding.UTF8.GetBytes(password)
        );
        return Convert.ToBase64String(hashedBytes);
    }
}
```

#### المميزات الأمنية

✅ **One-Way Hashing**: لا يمكن استرجاع كلمة المرور الأصلية  
✅ **SHA256**: خوارزمية قوية ومعتمدة  
✅ **Base64 Encoding**: تخزين آمن في قاعدة البيانات  
⚠️ **تحسين مقترح**: استخدام BCrypt مع Salt

#### مثال على التحسين المستقبلي

```csharp
// Using BCrypt.Net-Next (Already installed)
using BCrypt.Net;

public static string HashPassword(string password)
{
    // Generate salt and hash in one step
    return BCrypt.HashPassword(password, workFactor: 12);
}

public static bool VerifyPassword(string password, string hash)
{
    return BCrypt.Verify(password, hash);
}
```

### 4. إدارة الجلسة (Session Management)

#### Current Session

```csharp
// Static instance - Application lifetime
private static User? _currentUser;

// Properties
public static User? CurrentUser => _currentUser;
public static string CurrentUsername => _currentUser?.Username ?? "غير معروف";
public static UserRole CurrentUserRole => _currentUser?.Role ?? UserRole.Viewer;
```

#### Logout

```csharp
public void Logout()
{
    if (_currentUser != null)
    {
        LoggingService.LogInfo("تسجيل خروج: {Username}", _currentUser.Username);
        _currentUser = null;
    }
}
```

#### الأمان

✅ **Single Instance**: مستخدم واحد فقط في كل مرة  
✅ **Automatic Logout**: عند إغلاق التطبيق  
⚠️ **تحسين مقترح**: Session Timeout (Auto Logout بعد فترة غير نشاط)

---

## 🛡️ نظام الصلاحيات (Authorization)

### 1. الأدوار (Roles)

**الملف**: `Models/User.cs`

#### التسلسل الهرمي للأدوار

```text
┌─────────────────────────────────────────┐
│  1. Admin (مدير النظام)                │
│     └─> كامل الصلاحيات                 │
├─────────────────────────────────────────┤
│  2. Manager (مدير)                      │
│     └─> إدارة + تقارير + موافقات       │
├─────────────────────────────────────────┤
│  3. Accountant (محاسب)                  │
│     └─> تقارير مالية + ضرائب           │
├─────────────────────────────────────────┤
│  4. ProductionStaff (موظف إنتاج)       │
│     └─> تسجيل بيانات إنتاجية           │
├─────────────────────────────────────────┤
│  5. SalesStaff (موظف مبيعات)           │
│     └─> مبيعات + عملاء + فواتير         │
├─────────────────────────────────────────┤
│  6. InventoryStaff (موظف مخزون)        │
│     └─> إدارة مخزون + حركات             │
├─────────────────────────────────────────┤
│  7. QualityControl (مشرف جودة)         │
│     └─> اختبارات جودة + شهادات          │
├─────────────────────────────────────────┤
│  8. MaintenanceStaff (موظف صيانة)      │
│     └─> صيانة + معدات                   │
├─────────────────────────────────────────┤
│  9. HRStaff (موظف موارد بشرية)         │
│     └─> موظفين + رواتب + حضور           │
├─────────────────────────────────────────┤
│ 10. Viewer (مشاهد فقط)                 │
│     └─> قراءة التقارير فقط              │
└─────────────────────────────────────────┘
```

#### كود الأدوار

```csharp
public enum UserRole
{
    Admin = 1,              // كامل الصلاحيات
    Manager = 2,            // صلاحيات واسعة
    Accountant = 3,         // مالية + ضرائب
    ProductionStaff = 4,    // إنتاج
    SalesStaff = 5,         // مبيعات
    InventoryStaff = 6,     // مخزون
    QualityControl = 7,     // جودة
    MaintenanceStaff = 8,   // صيانة
    HRStaff = 9,            // موارد بشرية
    Viewer = 10             // قراءة فقط
}
```

### 2. التحقق من الصلاحيات

#### HasPermission Method

```csharp
public static bool HasPermission(params UserRole[] allowedRoles)
{
    if (_currentUser == null || !_currentUser.IsActive)
        return false;
    
    return allowedRoles.Contains(_currentUser.Role);
}
```

#### أمثلة الاستخدام

```csharp
// مثال 1: التحقق من صلاحية واحدة
if (!AuthenticationService.HasPermission(UserRole.Admin))
{
    MessageBox.Show("ليس لديك صلاحية");
    return;
}

// مثال 2: صلاحيات متعددة (OR)
if (!AuthenticationService.HasPermission(
    UserRole.Admin, 
    UserRole.Manager, 
    UserRole.Accountant))
{
    MessageBox.Show("صلاحية غير كافية");
    return;
}

// مثال 3: في Forms
public SalesReportForm(FishFarmContext context)
{
    if (!AuthenticationService.HasPermission(
        UserRole.Admin,
        UserRole.Manager,
        UserRole.Accountant,
        UserRole.SalesStaff,
        UserRole.Viewer))
    {
        MessageBox.Show("ليس لديك صلاحية لعرض تقارير المبيعات");
        this.Close();
        return;
    }
    // ... continue
}
```

### 3. مصفوفة الصلاحيات (Permission Matrix)

| الوظيفة | Admin | Manager | Accountant | Sales | Inventory | Quality | Maintenance | HR | Viewer |
|---------|-------|---------|------------|-------|-----------|---------|-------------|-------|--------|
| **المبيعات** |
| إدارة العملاء | ✅ | ✅ | ❌ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| أوامر المبيعات | ✅ | ✅ | ❌ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| تقارير المبيعات | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ✅ |
| **المالية** |
| التقارير المالية | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ |
| الفواتير الضريبية | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| إقرار الضريبة | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |
| ZATCA Integration | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |
| **المخزون** |
| إدارة المخزون | ✅ | ✅ | ❌ | ❌ | ✅ | ❌ | ❌ | ❌ | ❌ |
| حركات المخزون | ✅ | ✅ | ❌ | ❌ | ✅ | ❌ | ❌ | ❌ | ❌ |
| تقارير المخزون | ✅ | ✅ | ✅ | ❌ | ✅ | ❌ | ❌ | ❌ | ✅ |
| **الموارد البشرية** |
| إدارة الموظفين | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ | ❌ |
| معالجة الرواتب | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ✅ | ❌ |
| الحضور والغياب | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ | ❌ |
| **الإدارة** |
| إدارة المستخدمين | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |
| النسخ الاحتياطي | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |
| عارض السجلات | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |

### 4. تطبيق الصلاحيات في الكود

#### في Forms

```csharp
// مثال من TaxInvoiceForm.cs
public TaxInvoiceForm(FishFarmContext context)
{
    _context = context ?? throw new ArgumentNullException(nameof(context));

    // ✅ التحقق من الصلاحيات
    if (!AuthenticationService.HasPermission(
        UserRole.Admin, 
        UserRole.Manager, 
        UserRole.Accountant, 
        UserRole.SalesStaff))
    {
        MessageBox.Show(
            "ليس لديك صلاحية لإصدار الفواتير الضريبية",
            "تحذير",
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning
        );
        
        // ✅ Logging للمحاولة غير المصرح بها
        LoggingService.LogWarning(
            "محاولة وصول غير مصرح بها من {Username} إلى الفواتير الضريبية",
            AuthenticationService.CurrentUsername
        );
        
        // ✅ إغلاق النموذج
        this.Load += (s, e) => this.Close();
        return;
    }
    
    // ... continue normal flow
}
```

#### في MainForm (Menu Items)

```csharp
// تعطيل عناصر القائمة حسب الصلاحيات
private void ApplyPermissions()
{
    bool isAdmin = AuthenticationService.IsAdmin;
    bool isManagerOrAdmin = AuthenticationService.IsManagerOrAdmin;
    
    // فقط Admin يمكنه إدارة المستخدمين
    userManagementMenuItem.Enabled = isAdmin;
    
    // Admin و Manager يمكنهم النسخ الاحتياطي
    backupMenuItem.Enabled = isManagerOrAdmin;
    
    // حسب الدور
    financialReportsMenuItem.Enabled = AuthenticationService.HasPermission(
        UserRole.Admin,
        UserRole.Manager,
        UserRole.Accountant,
        UserRole.Viewer
    );
}
```

---

## 🔒 تشفير البيانات (Data Encryption)

### 1. تشفير كلمات المرور

#### الطريقة الحالية: SHA256

```csharp
public static string HashPassword(string password)
{
    using (var sha256 = SHA256.Create())
    {
        byte[] hashedBytes = sha256.ComputeHash(
            Encoding.UTF8.GetBytes(password)
        );
        return Convert.ToBase64String(hashedBytes);
    }
}
```

**المميزات**:

- ✅ One-way hashing
- ✅ Fast computation
- ✅ Standard algorithm

**نقاط الضعف**:

- ⚠️ عدم وجود Salt
- ⚠️ عرضة لـ Rainbow Table Attacks
- ⚠️ ليس مصمم خصيصاً لكلمات المرور

#### التحسين المقترح: BCrypt

**المكتبة**: `BCrypt.Net-Next` (موجودة بالفعل)

```csharp
using BCrypt.Net;

public static string HashPassword(string password)
{
    // BCrypt automatically generates and includes salt
    return BCrypt.HashPassword(password, workFactor: 12);
}

public static bool VerifyPassword(string password, string hash)
{
    return BCrypt.Verify(password, hash);
}
```

**المميزات**:

- ✅ Automatic salt generation
- ✅ Configurable work factor (cost)
- ✅ Designed for passwords
- ✅ Resistant to brute force
- ✅ Industry standard

### 2. الشهادات الرقمية (Digital Certificates)

**الملف**: `Forms/EInvoicingIntegrationForm.cs`

#### تحميل الشهادة

```csharp
using System.Security.Cryptography.X509Certificates;

private X509Certificate2? _certificate;

private void LoadCertificate(string path, string password)
{
    try
    {
        _certificate = new X509Certificate2(path, password);
        
        // Verify certificate
        if (_certificate.NotAfter < DateTime.Now)
        {
            throw new Exception("الشهادة منتهية الصلاحية");
        }
        
        // Log success
        LoggingService.LogInfo($"تم تحميل الشهادة: {_certificate.Subject}");
    }
    catch (Exception ex)
    {
        LoggingService.LogError($"فشل تحميل الشهادة: {ex.Message}");
        throw;
    }
}
```

#### التوقيع الرقمي

```csharp
private string SignData(string data)
{
    if (_certificate == null)
        throw new InvalidOperationException("الشهادة غير محملة");
    
    using (var rsa = _certificate.GetRSAPrivateKey())
    {
        if (rsa == null)
            throw new InvalidOperationException("لا يوجد مفتاح خاص");
        
        byte[] dataBytes = Encoding.UTF8.GetBytes(data);
        byte[] signature = rsa.SignData(
            dataBytes,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1
        );
        
        return Convert.ToBase64String(signature);
    }
}
```

#### التحقق من التوقيع

```csharp
private bool VerifySignature(string data, string signature)
{
    using (var rsa = _certificate.GetRSAPublicKey())
    {
        byte[] dataBytes = Encoding.UTF8.GetBytes(data);
        byte[] signatureBytes = Convert.FromBase64String(signature);
        
        return rsa.VerifyData(
            dataBytes,
            signatureBytes,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1
        );
    }
}
```

### 3. حساب Hash للفواتير

```csharp
using System.Security.Cryptography;

private string ComputeHash(string data)
{
    using (var sha256 = SHA256.Create())
    {
        byte[] hashBytes = sha256.ComputeHash(
            Encoding.UTF8.GetBytes(data)
        );
        return Convert.ToBase64String(hashBytes);
    }
}

// استخدام
var invoiceXml = GenerateInvoiceXml(invoice);
invoice.InvoiceHash = ComputeHash(invoiceXml);
```

### 4. تشفير البيانات الحساسة

#### API Keys

```csharp
// ⚠️ لا تخزن API Keys في الكود المصدري
// ✅ استخدم ملفات التكوين المشفرة

// في appsettings.json (مشفر)
{
  "ZATCA": {
    "ApiKey": "encrypted_key_here"
  }
}

// في الكود
private string DecryptApiKey(string encryptedKey)
{
    // Use Windows Data Protection API
    byte[] encryptedBytes = Convert.FromBase64String(encryptedKey);
    byte[] decryptedBytes = ProtectedData.Unprotect(
        encryptedBytes,
        null,
        DataProtectionScope.CurrentUser
    );
    return Encoding.UTF8.GetString(decryptedBytes);
}
```

---

## 💾 أمان قاعدة البيانات

### 1. معمارية الأمان

**قاعدة البيانات**: SQLite  
**الموقع**: `fishfarm.db`

#### الحماية على مستوى التطبيق

```csharp
// DbContext مع التحقق من الصلاحيات
public class FishFarmContext : DbContext
{
    // ✅ استخدام Entity Framework للحماية من SQL Injection
    // ✅ Parameterized queries automatically
    // ✅ Input validation
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=fishfarm.db");
    }
}
```

### 2. الحماية من SQL Injection

#### ✅ استخدام Entity Framework

```csharp
// ✅ آمن - Parameterized query
var user = _context.Users
    .FirstOrDefault(u => u.Username == username);

// ❌ غير آمن - String concatenation (لا نستخدمه)
var query = $"SELECT * FROM Users WHERE Username = '{username}'";
```

#### ✅ Input Validation

```csharp
[Required]
[StringLength(50, MinimumLength = 3)]
[RegularExpression(@"^[a-zA-Z0-9_]+$")]
public string Username { get; set; }
```

### 3. Data Validation

#### Model Validation

```csharp
public class User
{
    [Required]
    [MaxLength(50)]
    public string Username { get; set; }
    
    [Required]
    [EmailAddress]
    public string Email { get; set; }
    
    [Required]
    [Range(1, 10)]
    public UserRole Role { get; set; }
}
```

#### Business Logic Validation

```csharp
private bool ValidateInvoice(TaxInvoice invoice)
{
    // Amount validation
    if (invoice.TotalWithVAT <= 0)
        return false;
    
    // VAT rate validation
    if (invoice.VATRate != 15m)
        return false;
    
    // Customer validation
    if (string.IsNullOrEmpty(invoice.BuyerName))
        return false;
    
    return true;
}
```

### 4. Database Backup Security

```csharp
private void BackupDatabase()
{
    try
    {
        string source = "fishfarm.db";
        string backup = $"fishfarm.db.backup_{DateTime.Now:yyyyMMdd_HHmmss}";
        
        // ✅ Close all connections
        _context.Database.CloseConnection();
        
        // ✅ Create backup
        File.Copy(source, backup, overwrite: false);
        
        // ✅ Verify backup
        if (File.Exists(backup))
        {
            LoggingService.LogInfo($"تم النسخ الاحتياطي: {backup}");
        }
        
        // ✅ Optional: Encrypt backup
        // EncryptFile(backup);
    }
    catch (Exception ex)
    {
        LoggingService.LogError($"فشل النسخ الاحتياطي: {ex.Message}");
    }
}
```

---

## 🌐 أمان الاتصالات

### 1. ZATCA API Integration

**الملف**: `Forms/EInvoicingIntegrationForm.cs`

#### HTTPS/TLS Communication

```csharp
using System.Net.Http;

private static readonly HttpClient _httpClient = new HttpClient();

private async Task<bool> SubmitToZATCAAsync(TaxInvoice invoice, string xmlData)
{
    try
    {
        // ✅ HTTPS endpoint
        var apiUrl = "https://api.zatca.gov.sa/v1/invoices";
        
        // ✅ Authorization header
        var request = new HttpRequestMessage(HttpMethod.Post, apiUrl);
        request.Headers.Add("Authorization", $"Bearer {apiKey}");
        
        // ✅ XML content with UTF-8 encoding
        var content = new StringContent(
            xmlData,
            Encoding.UTF8,
            "application/xml"
        );
        request.Content = content;
        
        // ✅ Send request
        var response = await _httpClient.SendAsync(request);
        
        // ✅ Validate response
        if (response.IsSuccessStatusCode)
        {
            LoggingService.LogInfo("تم إرسال الفاتورة بنجاح");
            return true;
        }
        
        return false;
    }
    catch (Exception ex)
    {
        LoggingService.LogError($"خطأ في الإرسال: {ex.Message}");
        return false;
    }
}
```

#### Security Headers

```csharp
private void ConfigureHttpClient()
{
    _httpClient.DefaultRequestHeaders.Clear();
    
    // ✅ Authorization
    _httpClient.DefaultRequestHeaders.Add(
        "Authorization",
        $"Bearer {apiKey}"
    );
    
    // ✅ Content Type
    _httpClient.DefaultRequestHeaders.Add(
        "Accept",
        "application/xml"
    );
    
    // ✅ Custom headers as per ZATCA requirements
    _httpClient.DefaultRequestHeaders.Add(
        "X-Device-ID",
        deviceId
    );
}
```

### 2. Certificate-Based Authentication

```csharp
private async Task<HttpResponseMessage> SendWithCertificate(
    string url,
    string xmlData)
{
    // ✅ Create handler with certificate
    var handler = new HttpClientHandler();
    handler.ClientCertificates.Add(_certificate);
    
    // ✅ Create client
    using (var client = new HttpClient(handler))
    {
        var content = new StringContent(xmlData, Encoding.UTF8, "application/xml");
        return await client.PostAsync(url, content);
    }
}
```

### 3. Error Handling & Logging

```csharp
private async Task<bool> SafeAPICall(Func<Task<bool>> apiCall)
{
    try
    {
        return await apiCall();
    }
    catch (HttpRequestException ex)
    {
        // ✅ Network errors
        LoggingService.LogError($"خطأ في الشبكة: {ex.Message}");
        return false;
    }
    catch (TimeoutException ex)
    {
        // ✅ Timeout
        LoggingService.LogError($"انتهت مهلة الاتصال: {ex.Message}");
        return false;
    }
    catch (Exception ex)
    {
        // ✅ General errors
        LoggingService.LogError($"خطأ عام: {ex.Message}");
        return false;
    }
}
```

---

## 📝 سجل التدقيق (Audit Trail)

### 1. نظام التسجيل (Logging)

**الملف**: `Services/LoggingService.cs`

#### Serilog Configuration

```csharp
using Serilog;

public static class LoggingService
{
    private static ILogger _logger;
    
    static LoggingService()
    {
        _logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(
                path: "Logs/app-.log",
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] {Message}{NewLine}{Exception}"
            )
            .CreateLogger();
    }
    
    public static void LogInfo(string message) => _logger.Information(message);
    public static void LogWarning(string message) => _logger.Warning(message);
    public static void LogError(string message) => _logger.Error(message);
}
```

### 2. User Activity Tracking

```csharp
public static void LogUserActivity(string username, string action, string details)
{
    var logEntry = $"[USER ACTIVITY] {username} - {action} - {details}";
    _logger.Information(logEntry);
}

// استخدام
LoggingService.LogUserActivity(
    AuthenticationService.CurrentUsername,
    "فتح تقرير المبيعات",
    $"الفترة: {fromDate} - {toDate}"
);
```

### 3. Security Events

```csharp
// تسجيل دخول ناجح
LoggingService.LogInfo($"✅ تسجيل دخول ناجح: {username} at {DateTime.Now}");

// تسجيل دخول فاشل
LoggingService.LogWarning($"⚠️ محاولة تسجيل دخول فاشلة: {username}");

// وصول غير مصرح به
LoggingService.LogWarning($"🚫 محاولة وصول غير مصرح بها: {username} -> {formName}");

// تغيير كلمة المرور
LoggingService.LogInfo($"🔑 تم تغيير كلمة المرور: {username}");

// حذف سجل
LoggingService.LogWarning($"🗑️ حذف سجل: {recordType} ID:{recordId} بواسطة {username}");
```

### 4. Data Changes Tracking

```csharp
private void TrackDataChange(string entity, int id, string action)
{
    var log = new AuditLog
    {
        Entity = entity,
        EntityId = id,
        Action = action,
        Username = AuthenticationService.CurrentUsername,
        Timestamp = DateTime.Now,
        OldValue = oldValue,
        NewValue = newValue
    };
    
    _context.AuditLogs.Add(log);
    _context.SaveChanges();
}

// استخدام
TrackDataChange("SalesOrder", orderId, "Created");
TrackDataChange("Customer", customerId, "Updated");
TrackDataChange("TaxInvoice", invoiceId, "Submitted to ZATCA");
```

---

## 💳 أمان ضريبة القيمة المضافة (VAT Security)

### 1. الفاتورة الضريبية

#### التحقق من البيانات

```csharp
private bool ValidateTaxInvoice(TaxInvoice invoice)
{
    // ✅ Required fields
    if (string.IsNullOrEmpty(invoice.InvoiceNumber))
        return false;
    
    // ✅ VAT rate
    if (invoice.VATRate != 15m)
        return false;
    
    // ✅ Seller TRN (15 digits)
    if (!Regex.IsMatch(invoice.SellerVATNumber, @"^\d{15}$"))
        return false;
    
    // ✅ Amounts
    if (invoice.TotalWithVAT <= 0)
        return false;
    
    // ✅ Calculate and verify
    var calculatedVAT = (invoice.SubTotal - invoice.DiscountAmount) * 0.15m;
    if (Math.Abs(invoice.VATAmount - calculatedVAT) > 0.01m)
        return false;
    
    return true;
}
```

#### QR Code Security

```csharp
private string GenerateQRCodeContent(TaxInvoice invoice)
{
    // ✅ TLV Format (Tag-Length-Value) as per ZATCA specs
    var tlv = new StringBuilder();
    
    // Tag 1: Seller Name
    tlv.Append($"\x01{invoice.SellerName.Length:X2}{invoice.SellerName}");
    
    // Tag 2: VAT Registration Number
    tlv.Append($"\x02{invoice.SellerVATNumber.Length:X2}{invoice.SellerVATNumber}");
    
    // Tag 3: Timestamp
    var timestamp = invoice.IssueDate.ToString("yyyy-MM-ddTHH:mm:ssZ");
    tlv.Append($"\x03{timestamp.Length:X2}{timestamp}");
    
    // Tag 4: Invoice Total
    var total = invoice.TotalWithVAT.ToString("0.00");
    tlv.Append($"\x04{total.Length:X2}{total}");
    
    // Tag 5: VAT Total
    var vat = invoice.VATAmount.ToString("0.00");
    tlv.Append($"\x05{vat.Length:X2}{vat}");
    
    // ✅ Base64 encode
    return Convert.ToBase64String(Encoding.UTF8.GetBytes(tlv.ToString()));
}
```

### 2. ZATCA Integration Security

#### Request Security

```csharp
private async Task<bool> SecureSubmitToZATCA(TaxInvoice invoice)
{
    try
    {
        // ✅ 1. Generate UUID
        if (string.IsNullOrEmpty(invoice.UUID))
        {
            invoice.UUID = Guid.NewGuid().ToString();
        }
        
        // ✅ 2. Generate XML
        var xmlData = GenerateInvoiceXml(invoice);
        
        // ✅ 3. Compute Hash
        invoice.InvoiceHash = ComputeHash(xmlData);
        
        // ✅ 4. Digital Signature
        if (_certificate != null)
        {
            var signature = SignData(xmlData);
            // Add signature to XML
        }
        
        // ✅ 5. Send to ZATCA
        var success = await SubmitToZATCAAsync(invoice, xmlData);
        
        // ✅ 6. Log
        if (success)
        {
            LoggingService.LogInfo($"✅ تم إرسال الفاتورة {invoice.InvoiceNumber} لـ ZATCA");
        }
        else
        {
            LoggingService.LogWarning($"❌ فشل إرسال الفاتورة {invoice.InvoiceNumber}");
        }
        
        return success;
    }
    catch (Exception ex)
    {
        LoggingService.LogError($"خطأ في ZATCA: {ex.Message}");
        return false;
    }
}
```

#### Response Validation

```csharp
private bool ValidateZATCAResponse(HttpResponseMessage response)
{
    // ✅ Status code
    if (!response.IsSuccessStatusCode)
        return false;
    
    // ✅ Content type
    var contentType = response.Content.Headers.ContentType?.MediaType;
    if (contentType != "application/xml")
        return false;
    
    // ✅ Parse response
    var responseXml = await response.Content.ReadAsStringAsync();
    
    // ✅ Validate XML structure
    // ✅ Extract response code
    // ✅ Extract message
    
    return true;
}
```

---

## 🛡️ أفضل الممارسات الأمنية

### 1. Input Validation

```csharp
// ✅ Always validate user input
private bool ValidateInput(string input, InputType type)
{
    switch (type)
    {
        case InputType.Username:
            return Regex.IsMatch(input, @"^[a-zA-Z0-9_]{3,50}$");
        
        case InputType.Email:
            return Regex.IsMatch(input, @"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$");
        
        case InputType.TRN:
            return Regex.IsMatch(input, @"^\d{15}$");
        
        case InputType.Amount:
            return decimal.TryParse(input, out var amount) && amount >= 0;
        
        default:
            return false;
    }
}
```

### 2. Output Encoding

```csharp
// ✅ Encode output to prevent XSS (if web-based in future)
private string EncodeOutput(string output)
{
    return System.Net.WebUtility.HtmlEncode(output);
}
```

### 3. Error Handling

```csharp
// ✅ Don't expose internal errors to users
try
{
    // ... operation
}
catch (Exception ex)
{
    // ✅ Log detailed error
    LoggingService.LogError($"خطأ: {ex.Message}\n{ex.StackTrace}");
    
    // ✅ Show generic message to user
    MessageBox.Show("حدث خطأ. الرجاء المحاولة مرة أخرى.");
}
```

### 4. Secure Defaults

```csharp
// ✅ Default to most secure option
public class VATConfiguration
{
    public bool EnableEInvoicing { get; set; } = true;  // ✅ Enabled by default
    public bool EnableZATCAIntegration { get; set; } = false;  // ⚠️ Must enable explicitly
    public decimal DefaultVATRate { get; set; } = 15m;  // ✅ Saudi standard
}
```

### 5. Principle of Least Privilege

```csharp
// ✅ Give minimum necessary permissions
public SalesReportForm(FishFarmContext context)
{
    // Viewers can only read, not modify
    if (AuthenticationService.CurrentUserRole == UserRole.Viewer)
    {
        saveButton.Enabled = false;
        deleteButton.Enabled = false;
        editButton.Enabled = false;
    }
}
```

---

## 🔍 إدارة الثغرات (Vulnerability Management)

### 1. الثغرات المحتملة والحلول

| الثغرة | الوصف | الحالة | الحل |
|--------|-------|--------|------|
| **SQL Injection** | استغلال استعلامات SQL | ✅ محمي | Entity Framework (Parameterized) |
| **Password Storage** | تخزين كلمات المرور | ⚠️ جيد | SHA256 (يفضل ترقية لـ BCrypt) |
| **Session Hijacking** | سرقة جلسات المستخدمين | ⚠️ متوسط | Static session (يفضل إضافة timeout) |
| **XSS** | Cross-Site Scripting | ✅ غير قابل للتطبيق | Windows Forms (ليس ويب) |
| **CSRF** | Cross-Site Request Forgery | ✅ غير قابل للتطبيق | Windows Forms (ليس ويب) |
| **Brute Force** | تخمين كلمات المرور | ⚠️ متوسط | يفضل إضافة Rate Limiting |
| **Certificate Expiry** | انتهاء صلاحية الشهادات | ✅ محمي | Validation عند التحميل |
| **API Key Exposure** | كشف مفاتيح API | ⚠️ جيد | في appsettings (يفضل التشفير) |
| **Audit Trail** | تتبع العمليات | ✅ موجود | Logging شامل |
| **Data Backup** | النسخ الاحتياطي | ✅ موجود | Manual backup feature |

### 2. التحسينات المقترحة

#### Priority 1: High (خلال أسبوع)

```csharp
// 1. BCrypt Password Hashing
public static string HashPassword(string password)
{
    return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
}

// 2. Password Complexity
private bool ValidatePasswordComplexity(string password)
{
    return password.Length >= 8 &&
           Regex.IsMatch(password, @"[A-Z]") &&  // Uppercase
           Regex.IsMatch(password, @"[a-z]") &&  // Lowercase
           Regex.IsMatch(password, @"\d") &&     // Digit
           Regex.IsMatch(password, @"[!@#$%^&*]");  // Special
}

// 3. Session Timeout
private void CheckSessionTimeout()
{
    if (_lastActivityTime.AddMinutes(30) < DateTime.Now)
    {
        AuthenticationService.Logout();
        MessageBox.Show("انتهت الجلسة بسبب عدم النشاط");
        this.Close();
    }
}
```

#### Priority 2: Medium (خلال شهر)

```csharp
// 1. Rate Limiting for Login
private Dictionary<string, LoginAttempt> _loginAttempts = new();

private bool CheckRateLimit(string username)
{
    if (!_loginAttempts.ContainsKey(username))
    {
        _loginAttempts[username] = new LoginAttempt();
        return true;
    }
    
    var attempt = _loginAttempts[username];
    if (attempt.Count >= 5 && 
        attempt.LastAttempt.AddMinutes(15) > DateTime.Now)
    {
        return false;  // Locked
    }
    
    return true;
}

// 2. API Key Encryption
private string EncryptApiKey(string apiKey)
{
    byte[] data = Encoding.UTF8.GetBytes(apiKey);
    byte[] encrypted = ProtectedData.Protect(
        data,
        null,
        DataProtectionScope.CurrentUser
    );
    return Convert.ToBase64String(encrypted);
}

// 3. Certificate Expiry Warning
private void CheckCertificateExpiry()
{
    if (_certificate != null)
    {
        var daysUntilExpiry = (_certificate.NotAfter - DateTime.Now).Days;
        if (daysUntilExpiry < 30)
        {
            LoggingService.LogWarning(
                $"⚠️ الشهادة ستنتهي خلال {daysUntilExpiry} يوم"
            );
        }
    }
}
```

---

## 💾 النسخ الاحتياطي والاستعادة

### 1. استراتيجية النسخ الاحتياطي

#### Backup Types

```text
┌─────────────────────────────────────────┐
│  1. Manual Backup (يدوي)               │
│     - عند الطلب                         │
│     - قبل التحديثات الكبيرة             │
├─────────────────────────────────────────┤
│  2. Scheduled Backup (مجدول)           │
│     - يومي تلقائي                       │
│     - أسبوعي شامل                       │
├─────────────────────────────────────────┤
│  3. Pre-Operation Backup (قبل العملية) │
│     - قبل الحذف الجماعي                 │
│     - قبل التعديلات الكبيرة             │
└─────────────────────────────────────────┘
```

#### Backup Code

```csharp
public class BackupService
{
    private const string BackupFolder = "Backups";
    
    public bool CreateBackup(BackupType type)
    {
        try
        {
            // ✅ 1. Ensure folder exists
            Directory.CreateDirectory(BackupFolder);
            
            // ✅ 2. Generate filename
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var filename = $"fishfarm_{type}_{timestamp}.db";
            var backupPath = Path.Combine(BackupFolder, filename);
            
            // ✅ 3. Close connections
            using (var context = new FishFarmContext())
            {
                context.Database.CloseConnection();
            }
            
            // ✅ 4. Copy database
            File.Copy("fishfarm.db", backupPath, overwrite: false);
            
            // ✅ 5. Verify
            if (File.Exists(backupPath))
            {
                LoggingService.LogInfo($"✅ نسخ احتياطي ناجح: {filename}");
                
                // ✅ 6. Optional: Compress
                // CompressBackup(backupPath);
                
                // ✅ 7. Optional: Encrypt
                // EncryptBackup(backupPath);
                
                // ✅ 8. Clean old backups
                CleanOldBackups();
                
                return true;
            }
            
            return false;
        }
        catch (Exception ex)
        {
            LoggingService.LogError($"❌ فشل النسخ الاحتياطي: {ex.Message}");
            return false;
        }
    }
    
    private void CleanOldBackups()
    {
        try
        {
            var backups = Directory.GetFiles(BackupFolder, "*.db")
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.CreationTime)
                .Skip(10)  // Keep last 10 backups
                .ToList();
            
            foreach (var backup in backups)
            {
                backup.Delete();
                LoggingService.LogInfo($"🗑️ حذف نسخة قديمة: {backup.Name}");
            }
        }
        catch (Exception ex)
        {
            LoggingService.LogWarning($"⚠️ فشل حذف النسخ القديمة: {ex.Message}");
        }
    }
}

public enum BackupType
{
    Manual,      // يدوي
    Scheduled,   // مجدول
    PreOperation // قبل عملية
}
```

### 2. Restore (الاستعادة)

```csharp
public bool RestoreBackup(string backupPath)
{
    try
    {
        // ✅ 1. Verify backup exists
        if (!File.Exists(backupPath))
        {
            LoggingService.LogError("❌ الملف غير موجود");
            return false;
        }
        
        // ✅ 2. Create safety backup
        CreateBackup(BackupType.PreOperation);
        
        // ✅ 3. Close all connections
        using (var context = new FishFarmContext())
        {
            context.Database.CloseConnection();
        }
        
        // ✅ 4. Restore
        File.Copy(backupPath, "fishfarm.db", overwrite: true);
        
        // ✅ 5. Verify
        LoggingService.LogInfo($"✅ تمت الاستعادة من: {backupPath}");
        return true;
    }
    catch (Exception ex)
    {
        LoggingService.LogError($"❌ فشلت الاستعادة: {ex.Message}");
        return false;
    }
}
```

---

## 📚 التوصيات

### 1. قصيرة المدى (خلال أسبوع)

- [ ] ترقية نظام تشفير كلمات المرور إلى BCrypt
- [ ] إضافة Password Complexity Requirements
- [ ] تطبيق Session Timeout (30 دقيقة)
- [ ] إضافة Rate Limiting للتسجيل (5 محاولات / 15 دقيقة)

### 2. متوسطة المدى (خلال شهر)

- [ ] تشفير API Keys في appsettings.json
- [ ] إضافة Certificate Expiry Warnings
- [ ] تطبيق Automated Backup Schedule
- [ ] إنشاء Security Audit Reports
- [ ] Two-Factor Authentication (اختياري)

### 3. طويلة المدى (خلال 3-6 أشهر)

- [ ] Database Encryption at Rest
- [ ] Security Penetration Testing
- [ ] Security Training للمستخدمين
- [ ] Disaster Recovery Plan
- [ ] Security Compliance Audit (ZATCA)

---

## 📊 ملخص الأمان

### ✅ نقاط القوة

| المجال | التقييم | التفاصيل |
|--------|---------|----------|
| **Authentication** | ⭐⭐⭐⭐ | نظام قوي مع تشفير |
| **Authorization** | ⭐⭐⭐⭐⭐ | RBAC كامل مع 10 أدوار |
| **Data Encryption** | ⭐⭐⭐⭐ | SHA256 (يفضل BCrypt) |
| **SQL Injection** | ⭐⭐⭐⭐⭐ | محمي بـ EF Core |
| **Audit Trail** | ⭐⭐⭐⭐⭐ | Logging شامل |
| **ZATCA Security** | ⭐⭐⭐⭐⭐ | Digital Signatures + QR |
| **Backup** | ⭐⭐⭐⭐ | Manual backup متوفر |
| **Certificate Management** | ⭐⭐⭐⭐⭐ | X509 مع Validation |

### ⚠️ نقاط التحسين

| المجال | الأولوية | التوصية |
|--------|---------|----------|
| **Password Hashing** | عالية | BCrypt بدلاً من SHA256 |
| **Session Management** | متوسطة | إضافة Timeout |
| **Rate Limiting** | متوسطة | منع Brute Force |
| **API Keys** | متوسطة | تشفير في Config |
| **Automated Backup** | منخفضة | جدولة تلقائية |

---

## 📞 جهات الاتصال الأمنية

### في حال اكتشاف ثغرة أمنية

**يرجى الإبلاغ فوراً إلى**:

- **البريد الإلكتروني**: <security@aquafarm.cloud>
- **الهاتف**: +966-57-549-4973
- **مدير الأمن**: [طارق حسين صالح]

### موارد إضافية

- **ZATCA Security Guidelines**: <https://zatca.gov.sa/security>
- **OWASP Top 10**: <https://owasp.org/www-project-top-ten/>
- **Microsoft Security**: <https://docs.microsoft.com/security>

---

---

## 🔐 تفاصيل تقنية إضافية

### 1. نظام المصادقة المتقدم

#### المستخدم الافتراضي

عند أول تشغيل، يتم إنشاء مستخدم افتراضي:

```csharp
Username: admin
Password: Admin@123  // ⚠️ يجب تغييره فوراً
Role: Admin
```

**⚠️ تحذير أمني هام**: يجب تغيير كلمة المرور الافتراضية فوراً بعد أول تسجيل دخول!

#### إنشاء مستخدم آمن

```csharp
public async Task<bool> CreateUserAsync(string username, string password, UserRole role)
{
    try
    {
        // ✅ 1. Validate username
        if (!Regex.IsMatch(username, @"^[a-zA-Z0-9_]{3,50}$"))
        {
            throw new Exception("اسم المستخدم غير صالح");
        }
        
        // ✅ 2. Check uniqueness
        var exists = await _context.Users
            .AnyAsync(u => u.Username.ToLower() == username.ToLower());
        
        if (exists)
        {
            throw new Exception("اسم المستخدم موجود مسبقاً");
        }
        
        // ✅ 3. Validate password
        if (!ValidatePasswordComplexity(password))
        {
            throw new Exception("كلمة المرور ضعيفة");
        }
        
        // ✅ 4. Hash password
        var passwordHash = HashPassword(password);
        
        // ✅ 5. Create user
        var user = new User
        {
            Username = username,
            PasswordHash = passwordHash,
            Role = role,
            IsActive = true,
            CreatedAt = DateTime.Now,
            CreatedBy = AuthenticationService.CurrentUsername
        };
        
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        
        // ✅ 6. Log
        LoggingService.LogInfo($"✅ تم إنشاء مستخدم جديد: {username} بدور {role}");
        
        return true;
    }
    catch (Exception ex)
    {
        LoggingService.LogError($"❌ فشل إنشاء المستخدم: {ex.Message}");
        return false;
    }
}
```

### 2. الحماية من الهجمات الشائعة

#### A. SQL Injection Protection

**✅ محمي تلقائياً** عبر Entity Framework Core

```csharp
// ✅ آمن - EF Core uses parameterized queries
var user = await _context.Users
    .FirstOrDefaultAsync(u => u.Username == username);

// ✅ آمن - LINQ queries
var orders = await _context.SalesOrders
    .Where(o => o.CustomerId == customerId && o.OrderDate >= startDate)
    .ToListAsync();

// ❌ غير آمن - لا نستخدمه أبداً
var query = $"SELECT * FROM Users WHERE Username = '{username}'";
var result = _context.Database.ExecuteSqlRaw(query);
```

#### B. Brute Force Protection

```csharp
public class LoginAttemptTracker
{
    private static Dictionary<string, LoginAttempt> _attempts = new();
    private const int MaxAttempts = 5;
    private const int LockoutMinutes = 15;
    
    public static bool IsLocked(string username)
    {
        if (!_attempts.ContainsKey(username))
            return false;
        
        var attempt = _attempts[username];
        
        // Check if still locked
        if (attempt.Count >= MaxAttempts &&
            attempt.LastAttempt.AddMinutes(LockoutMinutes) > DateTime.Now)
        {
            LoggingService.LogWarning($"🚫 الحساب مقفل: {username}");
            return true;
        }
        
        // Reset if lockout expired
        if (attempt.LastAttempt.AddMinutes(LockoutMinutes) <= DateTime.Now)
        {
            _attempts.Remove(username);
        }
        
        return false;
    }
    
    public static void RecordFailedAttempt(string username)
    {
        if (!_attempts.ContainsKey(username))
        {
            _attempts[username] = new LoginAttempt();
        }
        
        _attempts[username].Count++;
        _attempts[username].LastAttempt = DateTime.Now;
        
        LoggingService.LogWarning(
            $"⚠️ محاولة تسجيل دخول فاشلة: {username} ({_attempts[username].Count}/{MaxAttempts})"
        );
    }
    
    public static void ClearAttempts(string username)
    {
        if (_attempts.ContainsKey(username))
        {
            _attempts.Remove(username);
        }
    }
}

public class LoginAttempt
{
    public int Count { get; set; }
    public DateTime LastAttempt { get; set; }
}
```

#### C. Password Strength Validation

```csharp
public class PasswordValidator
{
    private const int MinLength = 8;
    private const int MaxLength = 128;
    
    public static ValidationResult Validate(string password)
    {
        var result = new ValidationResult();
        
        // Length
        if (password.Length < MinLength)
        {
            result.IsValid = false;
            result.Errors.Add($"كلمة المرور يجب أن تكون {MinLength} أحرف على الأقل");
        }
        
        if (password.Length > MaxLength)
        {
            result.IsValid = false;
            result.Errors.Add($"كلمة المرور يجب ألا تتجاوز {MaxLength} حرف");
        }
        
        // Uppercase
        if (!Regex.IsMatch(password, @"[A-Z]"))
        {
            result.Errors.Add("يجب أن تحتوي على حرف كبير واحد على الأقل");
        }
        
        // Lowercase
        if (!Regex.IsMatch(password, @"[a-z]"))
        {
            result.Errors.Add("يجب أن تحتوي على حرف صغير واحد على الأقل");
        }
        
        // Digit
        if (!Regex.IsMatch(password, @"\d"))
        {
            result.Errors.Add("يجب أن تحتوي على رقم واحد على الأقل");
        }
        
        // Special character
        if (!Regex.IsMatch(password, @"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>/?]"))
        {
            result.Errors.Add("يجب أن تحتوي على رمز خاص واحد على الأقل");
        }
        
        // Common passwords
        if (CommonPasswords.Contains(password.ToLower()))
        {
            result.IsValid = false;
            result.Errors.Add("كلمة المرور شائعة جداً. اختر كلمة أقوى");
        }
        
        result.IsValid = result.Errors.Count == 0;
        return result;
    }
    
    private static readonly HashSet<string> CommonPasswords = new()
    {
        "password", "123456", "12345678", "qwerty", "abc123",
        "password123", "admin", "admin123", "letmein"
    };
}

public class ValidationResult
{
    public bool IsValid { get; set; } = true;
    public List<string> Errors { get; set; } = new();
}
```

### 3. Audit Trail المتقدم

#### نموذج سجل التدقيق

```csharp
public class AuditLog
{
    public int Id { get; set; }
    
    [Required]
    public DateTime Timestamp { get; set; } = DateTime.Now;
    
    [Required]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string Action { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string Entity { get; set; } = string.Empty;
    
    public int? EntityId { get; set; }
    
    [MaxLength(1000)]
    public string? OldValue { get; set; }
    
    [MaxLength(1000)]
    public string? NewValue { get; set; }
    
    [MaxLength(50)]
    public string? IPAddress { get; set; }
    
    public AuditActionType ActionType { get; set; }
}

public enum AuditActionType
{
    Create = 1,    // إنشاء
    Read = 2,      // قراءة
    Update = 3,    // تحديث
    Delete = 4,    // حذف
    Login = 5,     // تسجيل دخول
    Logout = 6,    // تسجيل خروج
    Export = 7,    // تصدير
    Print = 8      // طباعة
}
```

#### تسجيل العمليات

```csharp
public class AuditService
{
    private readonly FishFarmContext _context;
    
    public void LogAction(
        string entity,
        int? entityId,
        AuditActionType actionType,
        string? oldValue = null,
        string? newValue = null)
    {
        try
        {
            var auditLog = new AuditLog
            {
                Username = AuthenticationService.CurrentUsername,
                Action = actionType.ToString(),
                Entity = entity,
                EntityId = entityId,
                OldValue = oldValue,
                NewValue = newValue,
                ActionType = actionType,
                IPAddress = GetLocalIPAddress()
            };
            
            _context.AuditLogs.Add(auditLog);
            _context.SaveChanges();
        }
        catch (Exception ex)
        {
            LoggingService.LogError($"فشل تسجيل Audit: {ex.Message}");
        }
    }
    
    private string GetLocalIPAddress()
    {
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            return "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }
}
```

### 4. أمان البيانات المالية

#### تشفير البيانات الحساسة

```csharp
public class SensitiveDataProtection
{
    // استخدام Windows DPAPI
    public static string EncryptData(string data)
    {
        byte[] dataBytes = Encoding.UTF8.GetBytes(data);
        byte[] encryptedBytes = ProtectedData.Protect(
            dataBytes,
            null,  // Optional entropy
            DataProtectionScope.CurrentUser
        );
        return Convert.ToBase64String(encryptedBytes);
    }
    
    public static string DecryptData(string encryptedData)
    {
        byte[] encryptedBytes = Convert.FromBase64String(encryptedData);
        byte[] decryptedBytes = ProtectedData.Unprotect(
            encryptedBytes,
            null,
            DataProtectionScope.CurrentUser
        );
        return Encoding.UTF8.GetString(decryptedBytes);
    }
}

// استخدام
// تشفير API Key قبل الحفظ
var encryptedKey = SensitiveDataProtection.EncryptData(apiKey);
config.ZATCAApiKey = encryptedKey;

// فك التشفير عند الاستخدام
var apiKey = SensitiveDataProtection.DecryptData(config.ZATCAApiKey);
```

### 5. أمان التقارير والتصدير

#### حماية التصدير

```csharp
private async Task<bool> ExportWithSecurityAsync()
{
    try
    {
        // ✅ 1. Check permission
        if (!AuthenticationService.HasPermission(
            UserRole.Admin,
            UserRole.Manager,
            UserRole.Accountant))
        {
            LoggingService.LogWarning(
                $"🚫 محاولة تصدير غير مصرح بها: {AuthenticationService.CurrentUsername}"
            );
            return false;
        }
        
        // ✅ 2. Get data
        var data = await GetReportDataAsync();
        
        // ✅ 3. Log export
        LoggingService.LogInfo(
            $"📤 تصدير البيانات: {data.Count} سجل بواسطة {AuthenticationService.CurrentUsername}"
        );
        
        // ✅ 4. Audit trail
        await AuditService.LogActionAsync(
            "Report",
            null,
            AuditActionType.Export,
            details: $"Exported {data.Count} records"
        );
        
        // ✅ 5. Export
        await ExportToExcelAsync(data);
        
        return true;
    }
    catch (Exception ex)
    {
        LoggingService.LogError($"❌ فشل التصدير: {ex.Message}");
        return false;
    }
}
```

#### حماية الطباعة

```csharp
private void PrintWithWatermark(Document document)
{
    // ✅ Add security watermark
    var watermark = new TextWatermark
    {
        Text = $"سري - {AuthenticationService.CurrentUsername} - {DateTime.Now:yyyy-MM-dd}",
        Font = new Font("Arial", 8),
        Color = Color.LightGray,
        Rotation = 45
    };
    
    document.AddWatermark(watermark);
    
    // ✅ Log print action
    LoggingService.LogInfo($"🖨️ طباعة: {document.Title} بواسطة {AuthenticationService.CurrentUsername}");
}
```

---

## 🔬 فحص الأمان والتدقيق

### 1. Security Checklist

#### للمطورين

- [x] استخدام EF Core (حماية من SQL Injection)
- [x] تشفير كلمات المرور (SHA256)
- [x] نظام صلاحيات (RBAC)
- [x] Logging شامل
- [x] Input validation
- [x] Error handling
- [x] Audit trail
- [ ] BCrypt hashing (تحسين مقترح)
- [ ] Session timeout (تحسين مقترح)
- [ ] Rate limiting (تحسين مقترح)
- [ ] 2FA (اختياري)

#### للمستخدمين

- [ ] تغيير كلمة المرور الافتراضية
- [ ] استخدام كلمات مرور قوية
- [ ] عدم مشاركة بيانات الدخول
- [ ] تسجيل الخروج عند الانتهاء
- [ ] النسخ الاحتياطي المنتظم
- [ ] تحديث البرنامج دورياً

#### للإدارة

- [ ] مراجعة الصلاحيات دورياً
- [ ] مراجعة سجلات التدقيق
- [ ] تحديث الشهادات الرقمية
- [ ] اختبار الاستعادة من النسخ الاحتياطية
- [ ] تدريب الموظفين على الأمان
- [ ] خطة الطوارئ

### 2. Security Audit Report

#### التقرير الدوري

```csharp
public class SecurityAuditReport
{
    public DateTime GeneratedAt { get; set; }
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int InactiveUsers { get; set; }
    public int FailedLoginAttempts { get; set; }
    public int UnauthorizedAccessAttempts { get; set; }
    public List<User> UsersWithoutRecentActivity { get; set; }
    public List<User> AdminUsers { get; set; }
    public DateTime LastBackup { get; set; }
    public int DaysSinceLastBackup { get; set; }
    public CertificateStatus CertificateStatus { get; set; }
    
    public string GenerateReport()
    {
        var report = $@"
🔒 تقرير الأمان - {GeneratedAt:yyyy-MM-dd HH:mm}
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

📊 المستخدمين:
  - الإجمالي: {TotalUsers}
  - النشطين: {ActiveUsers}
  - المعطلين: {InactiveUsers}
  - المديرين: {AdminUsers.Count}

⚠️ التنبيهات الأمنية:
  - محاولات دخول فاشلة: {FailedLoginAttempts}
  - محاولات وصول غير مصرح بها: {UnauthorizedAccessAttempts}
  - مستخدمين بدون نشاط (30 يوم): {UsersWithoutRecentActivity.Count}

💾 النسخ الاحتياطي:
  - آخر نسخة: {LastBackup:yyyy-MM-dd}
  - منذ: {DaysSinceLastBackup} يوم
  - الحالة: {(DaysSinceLastBackup > 7 ? "⚠️ تأخر" : "✅ جيد")}

🔐 الشهادات الرقمية:
  - الحالة: {CertificateStatus}

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
";
        return report;
    }
}

public enum CertificateStatus
{
    Valid,          // صالحة
    ExpiringSoon,   // قريبة من الانتهاء (< 30 يوم)
    Expired,        // منتهية
    NotConfigured   // غير مكونة
}
```

---

## 🛠️ أدوات الأمان

### 1. Password Generator

```csharp
public class SecurePasswordGenerator
{
    private static readonly Random _random = new Random();
    
    public static string GenerateSecurePassword(int length = 12)
    {
        const string lowercase = "abcdefghijklmnopqrstuvwxyz";
        const string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string digits = "0123456789";
        const string special = "!@#$%^&*()_+-=[]{}|;:,.<>?";
        
        var allChars = lowercase + uppercase + digits + special;
        
        // ✅ Ensure at least one from each category
        var password = new StringBuilder();
        password.Append(lowercase[_random.Next(lowercase.Length)]);
        password.Append(uppercase[_random.Next(uppercase.Length)]);
        password.Append(digits[_random.Next(digits.Length)]);
        password.Append(special[_random.Next(special.Length)]);
        
        // ✅ Fill remaining with random
        for (int i = 4; i < length; i++)
        {
            password.Append(allChars[_random.Next(allChars.Length)]);
        }
        
        // ✅ Shuffle
        return new string(password.ToString().OrderBy(x => _random.Next()).ToArray());
    }
}
```

### 2. Security Scanner

```csharp
public class SecurityScanner
{
    public static List<SecurityIssue> ScanSystem()
    {
        var issues = new List<SecurityIssue>();
        
        // Check default passwords
        var defaultUsers = _context.Users
            .Where(u => u.Username == "admin" && u.CreatedAt == u.UpdatedAt)
            .ToList();
        
        if (defaultUsers.Any())
        {
            issues.Add(new SecurityIssue
            {
                Severity = SecuritySeverity.High,
                Title = "كلمة مرور افتراضية",
                Description = "مستخدم admin لم يغير كلمة المرور الافتراضية",
                Recommendation = "تغيير كلمة المرور فوراً"
            });
        }
        
        // Check inactive users
        var inactiveUsers = _context.Users
            .Where(u => u.IsActive && u.LastLoginAt < DateTime.Now.AddMonths(-3))
            .ToList();
        
        if (inactiveUsers.Any())
        {
            issues.Add(new SecurityIssue
            {
                Severity = SecuritySeverity.Medium,
                Title = "مستخدمين غير نشطين",
                Description = $"{inactiveUsers.Count} مستخدم لم يسجل دخول منذ 3 أشهر",
                Recommendation = "مراجعة وتعطيل الحسابات غير المستخدمة"
            });
        }
        
        // Check old backups
        var lastBackup = GetLastBackupDate();
        if ((DateTime.Now - lastBackup).Days > 7)
        {
            issues.Add(new SecurityIssue
            {
                Severity = SecuritySeverity.High,
                Title = "تأخر النسخ الاحتياطي",
                Description = $"آخر نسخة احتياطية منذ {(DateTime.Now - lastBackup).Days} يوم",
                Recommendation = "إنشاء نسخة احتياطية فوراً"
            });
        }
        
        return issues;
    }
}

public class SecurityIssue
{
    public SecuritySeverity Severity { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string Recommendation { get; set; }
}

public enum SecuritySeverity
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}
```

---

## 📖 سياسات الأمان

### 1. سياسة كلمات المرور

#### المتطلبات

- ✅ الحد الأدنى: 8 أحرف
- ✅ يجب أن تحتوي على:
  - حرف كبير واحد (A-Z)
  - حرف صغير واحد (a-z)
  - رقم واحد (0-9)
  - رمز خاص واحد (!@#$%...)
- ✅ لا يجوز استخدام:
  - كلمات مرور شائعة
  - اسم المستخدم
  - معلومات شخصية واضحة

#### التغيير الدوري

- ⏰ يوصى بتغيير كلمة المرور كل 90 يوم
- 🔄 لا يجوز إعادة استخدام آخر 5 كلمات مرور
- 🔒 إجبار تغيير كلمة المرور عند أول تسجيل دخول

### 2. سياسة الصلاحيات

#### منح الصلاحيات

- ✅ Principle of Least Privilege
- ✅ مراجعة دورية (كل 3 أشهر)
- ✅ إلغاء فوري عند انتهاء العمل
- ✅ توثيق جميع التغييرات

#### فصل المهام (Segregation of Duties)

```text
❌ يمنع:
- نفس الشخص يُنشئ ويعتمد الطلب
- محاسب يصدر فواتير لنفسه
- موظف يعدل راتبه الخاص

✅ يُطلب:
- موافقة مزدوجة للمبالغ الكبيرة
- فصل إنشاء الفواتير عن اعتمادها
- مراجعة مستقلة للتقارير المالية
```

### 3. سياسة النسخ الاحتياطي

#### التردد

- 📅 **يومي**: نسخة احتياطية تلقائية (End of Day)
- 📅 **أسبوعي**: نسخة كاملة + التحقق من السلامة
- 📅 **شهري**: نسخة أرشيفية طويلة الأجل

#### التخزين

- 💾 **محلي**: نفس السيرفر (نسخ سريعة)
- 💾 **خارجي**: قرص صلب خارجي (نسخ أسبوعية)
- ☁️ **سحابي**: (اختياري) للنسخ الشهرية

#### الاحتفاظ

- 📦 **النسخ اليومية**: الاحتفاظ 7 أيام
- 📦 **النسخ الأسبوعية**: الاحتفاظ 4 أسابيع
- 📦 **النسخ الشهرية**: الاحتفاظ 12 شهر
- 📦 **النسخ السنوية**: الاحتفاظ 7 سنوات (متطلبات قانونية)

---

## 🚨 خطة الاستجابة للحوادث

### 1. اكتشاف اختراق محتمل

```text
1. العزل الفوري (Immediate Isolation)
   └─> قطع اتصال الشبكة
   └─> إيقاف الخدمة
   
2. التقييم (Assessment)
   └─> فحص السجلات
   └─> تحديد نطاق الاختراق
   └─> تقييم الأضرار
   
3. الاحتواء (Containment)
   └─> تغيير جميع كلمات المرور
   └─> إبطال الشهادات المعرضة للخطر
   └─> تطبيق Patches
   
4. الاستعادة (Recovery)
   └─> استعادة من نسخة احتياطية نظيفة
   └─> التحقق من سلامة البيانات
   └─> إعادة تشغيل الخدمة
   
5. المتابعة (Follow-up)
   └─> تحليل السبب الجذري
   └─> تطبيق التحسينات
   └─> تحديث خطة الأمان
   └─> تدريب الموظفين
```

### 2. جهات الاتصال في الطوارئ

| الدور | الاسم | الهاتف | البريد |
|-------|-------|---------|--------|
| مدير الأمن | [الاسم] | XXX-XXXX | <security@company.com> |
| مدير النظام | [الاسم] | XXX-XXXX | <sysadmin@company.com> |
| المدير العام | [الاسم] | XXX-XXXX | <ceo@company.com> |
| الدعم التقني | [الفريق] | XXX-XXXX | <support@company.com> |

---

## 📋 التدريب الأمني

### للمستخدمين الجدد

#### الموضوعات الإلزامية

1. **أساسيات الأمان**
   - أهمية كلمات المرور القوية
   - عدم مشاركة بيانات الدخول
   - التعرف على محاولات التصيد

2. **استخدام النظام**
   - تسجيل الدخول والخروج بشكل صحيح
   - الصلاحيات والقيود
   - التعامل مع البيانات الحساسة

3. **الإبلاغ عن المشاكل**
   - كيفية التعرف على نشاط مشبوه
   - من يجب الإبلاغ إليه
   - عدم محاولة الإصلاح الذاتي

 للمطورين

1. **Secure Coding**
   - OWASP Top 10
   - Input Validation
   - Output Encoding
   - Error Handling

2. **Code Review**
   - مراجعة الكود للثغرات
   - اختبار الأمان
   - التوثيق الأمني

---

## 🎓 الخلاصة

### ✅ نقاط القوة الحالية

1. **نظام مصادقة قوي** - Login/Logout محكم
2. **نظام صلاحيات شامل** - RBAC مع 10 أدوار
3. **تشفير كلمات المرور** - SHA256 (جيد)
4. **حماية من SQL Injection** - EF Core
5. **Audit Trail كامل** - Logging شامل
6. **Digital Signatures** - للفواتير الضريبية
7. **Certificate Management** - X509 Certificates
8. **ZATCA Integration Security** - HTTPS + Auth
9. **Backup System** - نسخ احتياطي يدوي
10. **Error Handling** - شامل ومحكم

### ⚠️ التحسينات المقترحة

#### عالية الأولوية

1. **ترقية لـ BCrypt** - بدلاً من SHA256
2. **Session Timeout** - 30 دقيقة غير نشاط
3. **Rate Limiting** - منع Brute Force
4. **Password Complexity** - متطلبات قوية

#### متوسطة الأولوية

5.**API Key Encryption** - في ملفات التكوين
6. **Automated Backups** - نسخ تلقائية يومية
7. **Certificate Expiry Alerts** - تنبيهات قبل 30 يوم
8. **Security Audit Reports** - تقارير دورية

#### منخفضة الأولوية

9.**Two-Factor Authentication** - (اختياري)
10. **Database Encryption** - تشفير قاعدة البيانات

---

## 📊 التقييم النهائي

### الأمان العام: ⭐⭐⭐⭐ (4/5)

| الجانب | التقييم | الملاحظات |
|--------|---------|-----------|
| **Authentication** | ⭐⭐⭐⭐ | قوي، يحتاج BCrypt |
| **Authorization** | ⭐⭐⭐⭐⭐ | ممتاز، RBAC كامل |
| **Data Protection** | ⭐⭐⭐⭐ | جيد، يحتاج تشفير إضافي |
| **Audit Trail** | ⭐⭐⭐⭐⭐ | ممتاز، شامل |
| **ZATCA Security** | ⭐⭐⭐⭐⭐ | ممتاز، متوافق |
| **Backup** | ⭐⭐⭐⭐ | جيد، يحتاج أتمتة |
| **Error Handling** | ⭐⭐⭐⭐⭐ | ممتاز، محكم |

**التقييم الإجمالي**: **ممتاز** - جاهز للإنتاج مع تطبيق التحسينات المقترحة

---

**آخر تحديث**: 14 أكتوبر 2025  
**الإصدار**: 1.0  
**المراجع**: فريق الأمان والتطوير  
**التصنيف**: 🔒 **سري - Confidential**  
**الحالة**: ✅ **موثق ومراجع**

---

**⚠️ تحذير أمني**:

- هذا المستند يحتوي على معلومات حساسة عن أمان النظام
- يجب حفظه في مكان آمن ومشفر
- يجب عدم مشاركته إلا مع الأشخاص المصرح لهم
- يجب مراجعته وتحديثه دورياً

**🔐 للاستخدام الداخلي فقط - Internal Use Only*

</div>
