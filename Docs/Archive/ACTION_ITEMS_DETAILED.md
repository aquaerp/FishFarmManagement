# 📋 قائمة المهام التفصيلية - Action Items Checklist

**مشتق من:** تقرير التحليل الشامل (COMPREHENSIVE_AUDIT_REPORT.md)  
**التاريخ:** 8 أكتوبر 2025

---

## 🔴 مهام حرجة (Critical - يجب البدء فوراً)

### 1. إصلاح إدارة DbContext

**المشكلة:**

```csharp
// ❌ في 7 Forms
_context = new FishFarmContext();
```

**الملفات المتأثرة:**

- `Forms/StockAdjustmentForm.cs` (Line 107)
- `Forms/SalaryProcessingForm.cs` (Line 97)
- `Forms/LeaveManagementForm.cs` (Line 72)
- `Forms/InventoryItemForm.cs` (Line 77)
- `Forms/HRReportsForm.cs` (Line 56)
- `Forms/EmployeeForm.cs` (Line 120)
- `Forms/AttendanceForm.cs` (Line 57)

**الحل:**

```csharp
// ✅ استخدام Constructor Injection
public StockAdjustmentForm(FishFarmContext context)
{
    _context = context;
    InitializeComponent();
}
```

**الخطوات:**

1. حذف `new FishFarmContext()` من كل Form
2. تحديث Constructor ليستقبل FishFarmContext
3. تسجيل Form في `Program.cs`:

```csharp
services.AddTransient<StockAdjustmentForm>();
```

**الوقت المقدر:** 2-3 ساعات  
**الأولوية:** ⚠️⚠️⚠️ حرجة جداً

---

### 2. إضافة نظام Authentication

**المهمة:** بناء نظام تسجيل دخول كامل

**الخطوات التفصيلية:**

#### Step 1: إنشاء Models

```csharp
// Models/User.cs
public class User
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(50)]
    public string Username { get; set; } = string.Empty;
    
    [Required]
    public string PasswordHash { get; set; } = string.Empty;
    
    [Required]
    public string FullName { get; set; } = string.Empty;
    
    [Required]
    public UserRole Role { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    public DateTime? LastLoginAt { get; set; }
}

// Models/Enums.cs (إضافة)
public enum UserRole
{
    Admin = 1,
    Manager = 2,
    Supervisor = 3,
    Employee = 4
}
```

#### Step 2: إضافة إلى DbContext

```csharp
// Data/FishFarmContext.cs
public DbSet<User> Users { get; set; }
```

#### Step 3: إنشاء Migration

```powershell
dotnet ef migrations add AddUserAuthentication
dotnet ef database update
```

#### Step 4: إنشاء AuthenticationService

```csharp
// Services/AuthenticationService.cs
public class AuthenticationService
{
    private readonly FishFarmContext _context;
    private static User? _currentUser;
    
    public AuthenticationService(FishFarmContext context)
    {
        _context = context;
    }
    
    public async Task<User?> LoginAsync(string username, string password)
    {
        // Hash password
        var passwordHash = HashPassword(password);
        
        var user = await _context.Users
            .FirstOrDefaultAsync(u => 
                u.Username == username && 
                u.PasswordHash == passwordHash &&
                u.IsActive);
        
        if (user != null)
        {
            _currentUser = user;
            user.LastLoginAt = DateTime.Now;
            await _context.SaveChangesAsync();
        }
        
        return user;
    }
    
    public void Logout()
    {
        _currentUser = null;
    }
    
    public User? GetCurrentUser() => _currentUser;
    
    public bool IsAuthenticated() => _currentUser != null;
    
    public bool HasRole(UserRole role) => _currentUser?.Role == role;
    
    private string HashPassword(string password)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }
}
```

#### Step 5: إنشاء LoginForm

```csharp
// Forms/LoginForm.cs
public partial class LoginForm : Form
{
    private readonly AuthenticationService _authService;
    private TextBox _usernameTextBox = null!;
    private TextBox _passwordTextBox = null!;
    private Button _loginButton = null!;
    
    public LoginForm(AuthenticationService authService)
    {
        _authService = authService;
        InitializeComponent();
    }
    
    private void InitializeComponent()
    {
        // ... تصميم النافذة
    }
    
    private async void LoginButton_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_usernameTextBox.Text))
        {
            MessageBox.Show("يرجى إدخال اسم المستخدم");
            return;
        }
        
        var user = await _authService.LoginAsync(
            _usernameTextBox.Text, 
            _passwordTextBox.Text);
        
        if (user != null)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
        else
        {
            MessageBox.Show("اسم المستخدم أو كلمة المرور غير صحيحة");
        }
    }
}
```

#### Step 6: تحديث Program.cs

```csharp
// Program.cs
static void Main()
{
    // ...
    var serviceProvider = services.BuildServiceProvider();
    
    // عرض شاشة تسجيل الدخول
    var loginForm = serviceProvider.GetRequiredService<LoginForm>();
    if (loginForm.ShowDialog() == DialogResult.OK)
    {
        // تشغيل MainForm
        var mainForm = serviceProvider.GetRequiredService<MainForm>();
        Application.Run(mainForm);
    }
}

private static void ConfigureServices(ServiceCollection services)
{
    // ...
    services.AddScoped<AuthenticationService>();
    services.AddTransient<LoginForm>();
    // ...
}
```

#### Step 7: إنشاء مستخدم افتراضي

```csharp
// Data/DataSeeder.cs (إضافة)
public static void SeedDefaultUser(FishFarmContext context)
{
    if (!context.Users.Any())
    {
        var adminUser = new User
        {
            Username = "admin",
            PasswordHash = HashPassword("admin123"), // يجب تغييره
            FullName = "المدير العام",
            Role = UserRole.Admin,
            IsActive = true
        };
        
        context.Users.Add(adminUser);
        context.SaveChanges();
    }
}
```

**الوقت المقدر:** 1-2 يوم  
**الأولوية:** ⚠️⚠️⚠️ حرجة جداً

---

### 3. إضافة Authorization للنماذج

**المهمة:** تقييد الوصول حسب الصلاحيات

**الخطوات:**

#### Step 1: إنشاء AuthorizationService

```csharp
// Services/AuthorizationService.cs
public class AuthorizationService
{
    private readonly AuthenticationService _authService;
    
    public AuthorizationService(AuthenticationService authService)
    {
        _authService = authService;
    }
    
    public bool CanAccessForm(string formName)
    {
        var user = _authService.GetCurrentUser();
        if (user == null) return false;
        
        // Admin يمكنه الوصول لكل شيء
        if (user.Role == UserRole.Admin) return true;
        
        // تحديد الصلاحيات حسب الـ Role
        return formName switch
        {
            "CustomerForm" => user.Role <= UserRole.Manager,
            "SalesOrderForm" => user.Role <= UserRole.Supervisor,
            "EmployeeForm" => user.Role == UserRole.Admin,
            "SalaryProcessingForm" => user.Role == UserRole.Admin,
            _ => false
        };
    }
    
    public bool CanDelete() => _authService.GetCurrentUser()?.Role <= UserRole.Manager;
    
    public bool CanEdit() => _authService.GetCurrentUser()?.Role <= UserRole.Supervisor;
}
```

#### Step 2: تطبيق في MainForm

```csharp
// Forms/MainForm.cs
private void ManageCustomers_Click(object? sender, EventArgs e)
{
    if (!_authService.CanAccessForm("CustomerForm"))
    {
        MessageBox.Show("ليس لديك صلاحية للوصول إلى هذه الصفحة");
        return;
    }
    
    var form = _serviceProvider.GetRequiredService<CustomerForm>();
    ShowChildForm(form);
}
```

**الوقت المقدر:** 1 يوم  
**الأولوية:** ⚠️⚠️⚠️ حرجة

---

### 4. إضافة Logging موحد

**المهمة:** تسجيل جميع العمليات والأخطاء

**الخطوات:**

#### Step 1: إضافة NuGet Package

```powershell
dotnet add package Serilog.Extensions.Logging.File
```

#### Step 2: تكوين Logging

```csharp
// Program.cs
services.AddLogging(builder =>
{
    builder.AddFile("Logs/fishfarm-{Date}.log", LogLevel.Information);
    builder.AddDebug();
});
```

#### Step 3: استخدام ILogger

```csharp
// في أي Form أو Service
private readonly ILogger<CustomerForm> _logger;

public CustomerForm(FishFarmContext context, ILogger<CustomerForm> logger)
{
    _context = context;
    _logger = logger;
}

private async Task SaveCustomer()
{
    try
    {
        _logger.LogInformation("Saving customer: {CustomerName}", customer.Name);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Customer saved successfully: {CustomerId}", customer.Id);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error saving customer: {CustomerName}", customer.Name);
        throw;
    }
}
```

**الوقت المقدر:** 4-6 ساعات  
**الأولوية:** ⚠️⚠️ عالية

---

## 🟠 مهام عالية الأولوية (High Priority)

### 5. تطبيق Repository Pattern

**المهمة:** فصل Data Access عن Business Logic

**الخطوات التفصيلية:**

#### Step 1: إنشاء Generic Repository

```csharp
// Data/IRepository.cs
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task<T> AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
    Task<int> CountAsync();
}

// Data/Repository.cs
public class Repository<T> : IRepository<T> where T : class
{
    protected readonly FishFarmContext _context;
    protected readonly DbSet<T> _dbSet;
    
    public Repository(FishFarmContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }
    
    public virtual async Task<T?> GetByIdAsync(int id)
    {
        return await _dbSet.FindAsync(id);
    }
    
    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _dbSet.AsNoTracking().ToListAsync();
    }
    
    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.Where(predicate).AsNoTracking().ToListAsync();
    }
    
    public virtual async Task<T> AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }
    
    public virtual async Task UpdateAsync(T entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
    }
    
    public virtual async Task DeleteAsync(int id)
    {
        var entity = await GetByIdAsync(id);
        if (entity != null)
        {
            _dbSet.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
    
    public virtual async Task<bool> ExistsAsync(int id)
    {
        return await _dbSet.FindAsync(id) != null;
    }
    
    public virtual async Task<int> CountAsync()
    {
        return await _dbSet.CountAsync();
    }
}
```

#### Step 2: إنشاء Specific Repositories

```csharp
// Data/Repositories/ICustomerRepository.cs
public interface ICustomerRepository : IRepository<Customer>
{
    Task<Customer?> GetByNameAsync(string name);
    Task<IEnumerable<Customer>> GetActiveCustomersAsync();
    Task<IEnumerable<Customer>> SearchAsync(string searchTerm);
}

// Data/Repositories/CustomerRepository.cs
public class CustomerRepository : Repository<Customer>, ICustomerRepository
{
    public CustomerRepository(FishFarmContext context) : base(context) { }
    
    public async Task<Customer?> GetByNameAsync(string name)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Name == name);
    }
    
    public async Task<IEnumerable<Customer>> GetActiveCustomersAsync()
    {
        return await _dbSet
            .AsNoTracking()
            .Where(c => c.Status == CustomerStatus.Active)
            .ToListAsync();
    }
    
    public async Task<IEnumerable<Customer>> SearchAsync(string searchTerm)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(c => 
                c.Name.Contains(searchTerm) ||
                c.Phone.Contains(searchTerm) ||
                c.Email.Contains(searchTerm))
            .ToListAsync();
    }
}
```

#### Step 3: تسجيل Repositories

```csharp
// Program.cs
services.AddScoped<ICustomerRepository, CustomerRepository>();
services.AddScoped<ISalesOrderRepository, SalesOrderRepository>();
// ... باقي الـ Repositories
```

#### Step 4: استخدام في Forms

```csharp
// Forms/CustomerForm.cs
private readonly ICustomerRepository _customerRepository;

public CustomerForm(ICustomerRepository customerRepository)
{
    _customerRepository = customerRepository;
}

private async void LoadCustomers()
{
    var customers = await _customerRepository.GetAllAsync();
    _customersGrid.DataSource = customers;
}

private async void SaveButton_Click(object? sender, EventArgs e)
{
    // ...
    await _customerRepository.AddAsync(customer);
    // ...
}
```

**الوقت المقدر:** 3-4 أيام  
**الأولوية:** ⚠️⚠️ عالية

---

### 6. إنشاء Service Layer

**المهمة:** نقل Business Logic من Forms إلى Services

**مثال: CustomerService:**

```csharp
// Services/ICustomerService.cs
public interface ICustomerService
{
    Task<Customer?> GetByIdAsync(int id);
    Task<IEnumerable<Customer>> GetAllAsync();
    Task<IEnumerable<Customer>> SearchAsync(string searchTerm);
    Task<Customer> CreateAsync(Customer customer);
    Task UpdateAsync(Customer customer);
    Task DeleteAsync(int id);
    Task<bool> ExistsAsync(string name);
    Task<decimal> GetTotalDebtAsync(int customerId);
}

// Services/CustomerService.cs
public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ILogger<CustomerService> _logger;
    
    public CustomerService(
        ICustomerRepository customerRepository,
        ILogger<CustomerService> logger)
    {
        _customerRepository = customerRepository;
        _logger = logger;
    }
    
    public async Task<Customer> CreateAsync(Customer customer)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(customer.Name))
            throw new ArgumentException("اسم العميل مطلوب");
        
        // Check duplicates
        var existing = await _customerRepository.GetByNameAsync(customer.Name);
        if (existing != null)
            throw new InvalidOperationException("العميل موجود بالفعل");
        
        // Set timestamps
        customer.CreatedAt = DateTime.Now;
        
        // Save
        _logger.LogInformation("Creating customer: {CustomerName}", customer.Name);
        var result = await _customerRepository.AddAsync(customer);
        _logger.LogInformation("Customer created: {CustomerId}", result.Id);
        
        return result;
    }
    
    public async Task<decimal> GetTotalDebtAsync(int customerId)
    {
        // Business logic لحساب الديون
        // ...
    }
    
    // ... باقي الـ Methods
}
```

**تسجيل:**

```csharp
services.AddScoped<ICustomerService, CustomerService>();
```

**استخدام في Form:**

```csharp
private readonly ICustomerService _customerService;

private async void SaveButton_Click(object? sender, EventArgs e)
{
    try
    {
        var customer = GetCustomerFromForm();
        await _customerService.CreateAsync(customer);
        MessageBox.Show("تم الحفظ بنجاح");
        LoadCustomers();
    }
    catch (ArgumentException ex)
    {
        MessageBox.Show(ex.Message);
    }
    catch (InvalidOperationException ex)
    {
        MessageBox.Show(ex.Message);
    }
}
```

**الوقت المقدر:** 5-7 أيام  
**الأولوية:** ⚠️⚠️ عالية

---

### 7. إضافة Unit Tests

**المهمة:** اختبار الـ Services و Repositories

**الخطوات:**

#### Step 1: إنشاء Test Project

```powershell
dotnet new xunit -n FishFarmManager.Tests
cd FishFarmManager.Tests
dotnet add reference ../FishFarmManager/FishFarmManager.csproj
dotnet add package Moq
dotnet add package Microsoft.EntityFrameworkCore.InMemory
```

#### Step 2: كتابة Tests للـ CustomerService

```csharp
// Tests/CustomerServiceTests.cs
public class CustomerServiceTests
{
    private readonly Mock<ICustomerRepository> _mockRepo;
    private readonly Mock<ILogger<CustomerService>> _mockLogger;
    private readonly CustomerService _service;
    
    public CustomerServiceTests()
    {
        _mockRepo = new Mock<ICustomerRepository>();
        _mockLogger = new Mock<ILogger<CustomerService>>();
        _service = new CustomerService(_mockRepo.Object, _mockLogger.Object);
    }
    
    [Fact]
    public async Task CreateAsync_ValidCustomer_ShouldAddSuccessfully()
    {
        // Arrange
        var customer = new Customer { Name = "Test Customer" };
        _mockRepo.Setup(r => r.GetByNameAsync(It.IsAny<string>()))
                 .ReturnsAsync((Customer?)null);
        _mockRepo.Setup(r => r.AddAsync(It.IsAny<Customer>()))
                 .ReturnsAsync(customer);
        
        // Act
        var result = await _service.CreateAsync(customer);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Customer", result.Name);
        _mockRepo.Verify(r => r.AddAsync(customer), Times.Once);
    }
    
    [Fact]
    public async Task CreateAsync_EmptyName_ShouldThrowException()
    {
        // Arrange
        var customer = new Customer { Name = "" };
        
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _service.CreateAsync(customer));
    }
    
    [Fact]
    public async Task CreateAsync_DuplicateName_ShouldThrowException()
    {
        // Arrange
        var customer = new Customer { Name = "Existing" };
        _mockRepo.Setup(r => r.GetByNameAsync("Existing"))
                 .ReturnsAsync(new Customer { Id = 1, Name = "Existing" });
        
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _service.CreateAsync(customer));
    }
}
```

#### Step 3: كتابة Tests للـ Repository

```csharp
// Tests/CustomerRepositoryTests.cs
public class CustomerRepositoryTests
{
    private DbContextOptions<FishFarmContext> GetInMemoryOptions()
    {
        return new DbContextOptionsBuilder<FishFarmContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }
    
    [Fact]
    public async Task AddAsync_ShouldAddCustomer()
    {
        // Arrange
        using var context = new FishFarmContext(GetInMemoryOptions());
        var repository = new CustomerRepository(context);
        var customer = new Customer { Name = "Test" };
        
        // Act
        var result = await repository.AddAsync(customer);
        
        // Assert
        Assert.NotEqual(0, result.Id);
        Assert.Equal(1, await context.Customers.CountAsync());
    }
    
    [Fact]
    public async Task GetByNameAsync_ExistingCustomer_ShouldReturn()
    {
        // Arrange
        using var context = new FishFarmContext(GetInMemoryOptions());
        var repository = new CustomerRepository(context);
        context.Customers.Add(new Customer { Name = "Test" });
        await context.SaveChangesAsync();
        
        // Act
        var result = await repository.GetByNameAsync("Test");
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test", result.Name);
    }
}
```

**تشغيل الاختبارات:**

```powershell
dotnet test
```

**الوقت المقدر:** 7-10 أيام (تدريجياً)  
**الأولوية:** ⚠️⚠️ عالية

---

## 🟡 مهام متوسطة الأولوية (Medium Priority)

### 8. إضافة Validation موحد

```csharp
// Helpers/ValidationHelper.cs
public static class ValidationHelper
{
    public static bool ValidateRequired(Control control, string fieldName, ErrorProvider errorProvider)
    {
        if (string.IsNullOrWhiteSpace(control.Text))
        {
            errorProvider.SetError(control, $"{fieldName} مطلوب");
            return false;
        }
        errorProvider.SetError(control, "");
        return true;
    }
    
    public static bool ValidateEmail(TextBox textBox, ErrorProvider errorProvider)
    {
        if (string.IsNullOrWhiteSpace(textBox.Text))
            return true;
        
        var regex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        if (!regex.IsMatch(textBox.Text))
        {
            errorProvider.SetError(textBox, "البريد الإلكتروني غير صحيح");
            return false;
        }
        errorProvider.SetError(textBox, "");
        return true;
    }
    
    public static bool ValidateSaudiPhone(TextBox textBox, ErrorProvider errorProvider)
    {
        if (string.IsNullOrWhiteSpace(textBox.Text))
            return true;
        
        var regex = new Regex(@"^05\d{8}$");
        if (!regex.IsMatch(textBox.Text))
        {
            errorProvider.SetError(textBox, "رقم الجوال يجب أن يبدأ بـ 05 ويتكون من 10 أرقام");
            return false;
        }
        errorProvider.SetError(textBox, "");
        return true;
    }
}
```

**الوقت المقدر:** 1-2 يوم  
**الأولوية:** ⚠️ متوسطة

---

### 9. إضافة Caching

```csharp
// Services/CacheService.cs
public class CacheService
{
    private readonly IMemoryCache _cache;
    
    public CacheService(IMemoryCache cache)
    {
        _cache = cache;
    }
    
    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan? expiration = null)
    {
        if (!_cache.TryGetValue(key, out T value))
        {
            value = await factory();
            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(10)
            };
            _cache.Set(key, value, options);
        }
        return value;
    }
    
    public void Remove(string key)
    {
        _cache.Remove(key);
    }
}

// استخدام
public class CustomerService
{
    private readonly CacheService _cache;
    
    public async Task<IEnumerable<Customer>> GetAllAsync()
    {
        return await _cache.GetOrCreateAsync(
            "customers_all",
            () => _repository.GetAllAsync(),
            TimeSpan.FromMinutes(5));
    }
}
```

**الوقت المقدر:** 1 يوم  
**الأولوية:** ⚠️ متوسطة

---

### 10. إضافة Configuration Management

```csharp
// Models/AppSettings.cs
public class AppSettings
{
    public string ApplicationName { get; set; } = "";
    public string Version { get; set; } = "";
    public decimal DefaultFishPrice { get; set; }
    public int BackupInterval { get; set; }
    public int AlertCheckInterval { get; set; }
}

// Program.cs
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

services.Configure<AppSettings>(configuration.GetSection("AppSettings"));

// استخدام
public class SomeService
{
    private readonly AppSettings _settings;
    
    public SomeService(IOptions<AppSettings> options)
    {
        _settings = options.Value;
    }
}
```

**الوقت المقدر:** 4-6 ساعات  
**الأولوية:** ⚠️ متوسطة

---

## 🟢 مهام منخفضة الأولوية (Low Priority - UX Enhancements)

### 11. إضافة Loading Indicators

```csharp
// Controls/LoadingPanel.cs
public class LoadingPanel : Panel
{
    private PictureBox _spinner;
    private Label _label;
    
    public LoadingPanel()
    {
        this.Dock = DockStyle.Fill;
        this.BackColor = Color.FromArgb(200, 255, 255, 255);
        
        _spinner = new PictureBox
        {
            // Add spinner gif
            SizeMode = PictureBoxSizeMode.CenterImage
        };
        
        _label = new Label
        {
            Text = "جاري التحميل...",
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Bottom
        };
        
        this.Controls.Add(_spinner);
        this.Controls.Add(_label);
        this.Visible = false;
    }
    
    public void Show(string message = "جاري التحميل...")
    {
        _label.Text = message;
        this.Visible = true;
        this.BringToFront();
    }
    
    public void Hide()
    {
        this.Visible = false;
    }
}

// استخدام
private LoadingPanel _loadingPanel = new LoadingPanel();

private async void LoadData()
{
    _loadingPanel.Show("جاري تحميل البيانات...");
    try
    {
        await LoadDataAsync();
    }
    finally
    {
        _loadingPanel.Hide();
    }
}
```

---

### 12. إضافة Dark Mode

```csharp
// Services/ThemeManager.cs (تحديث)
public static bool IsDarkMode { get; set; }

public static void ApplyTheme(Form form, bool darkMode = false)
{
    IsDarkMode = darkMode;
    
    if (darkMode)
    {
        form.BackColor = ColorTranslator.FromHtml("#1E1E1E");
        form.ForeColor = Color.White;
        // ... باقي الألوان
    }
    else
    {
        form.BackColor = PureWhite;
        form.ForeColor = Color.Black;
        // ...
    }
    
    // ... تطبيق على Controls
}
```

---

### 13. إضافة Tooltips

```csharp
// Helpers/TooltipHelper.cs
public static class TooltipHelper
{
    private static ToolTip _toolTip = new ToolTip();
    
    public static void AddTooltip(Control control, string text)
    {
        _toolTip.SetToolTip(control, text);
    }
}

// استخدام في Form
TooltipHelper.AddTooltip(_creditLimitNumeric, 
    "الحد الأقصى للائتمان المسموح به للعميل بالريال السعودي");
```

---

## 📊 ملخص الأولويات

| المهمة | الأولوية | الوقت | التأثير |
|--------|----------|-------|---------|
| إصلاح DbContext | ⚠️⚠️⚠️ | 2-3h | حرج |
| Authentication | ⚠️⚠️⚠️ | 1-2d | حرج |
| Authorization | ⚠️⚠️⚠️ | 1d | حرج |
| Logging | ⚠️⚠️ | 4-6h | عالي |
| Repository Pattern | ⚠️⚠️ | 3-4d | عالي |
| Service Layer | ⚠️⚠️ | 5-7d | عالي |
| Unit Tests | ⚠️⚠️ | 7-10d | عالي |
| Validation Helper | ⚠️ | 1-2d | متوسط |
| Caching | ⚠️ | 1d | متوسط |
| Configuration | ⚠️ | 4-6h | متوسط |
| Loading Indicators | 🟢 | 1d | منخفض |
| Dark Mode | 🟢 | 2d | منخفض |
| Tooltips | 🟢 | 1d | منخفض |

**إجمالي الوقت المقدر:** 6-8 أسابيع

---

## ✅ Checklist للمتابعة

- [ ] إصلاح DbContext في جميع Forms
- [ ] إضافة User Model + Migration
- [ ] إنشاء AuthenticationService
- [ ] إنشاء LoginForm
- [ ] إضافة AuthorizationService
- [ ] تكوين Logging
- [ ] إضافة ILogger في 3 Forms كتجربة
- [ ] إنشاء IRepository و Repository<T
- [ ] إنشاء CustomerRepository
- [ ] تحديث CustomerForm لاستخدام Repository
- [ ] إنشاء CustomerService
- [ ] تحديث CustomerForm لاستخدام Service
- [ ] إنشاء Test Project
- [ ] كتابة 5 Tests للـ CustomerService
- [ ] مراجعة وتحديث README.md

---

**ملاحظة:** هذه القائمة قابلة للتحديث. يُنصح بإنشاء GitHub Issues لكل مهمة ومتابعتها.

**آخر تحديث:** 8 أكتوبر 2025
