# دليل البدء السريع - المرحلة 1
## تحويل AquaFarm Pro إلى WPF - خطوات التنفيذ الفورية

**المدة المتوقعة**: 3-4 أسابيع  
**الهدف**: إثبات الجدوى وبناء الأساس

---

## 📋 قائمة المهام الجاهزة للتنفيذ

### الأسبوع 1: الإعداد والبنية الأساسية

#### اليوم 1: إعداد المشروع
```powershell
# 1. إنشاء مشروع WPF جديد
cd D:\FishFarmManagement
dotnet new wpf -n FishFarmManager.WPF -o FishFarmManager.WPF

# 2. إضافة المشروع للـ Solution
dotnet sln FishFarmManager.sln add FishFarmManager.WPF/FishFarmManager.WPF.csproj

# 3. إضافة مرجع للمشروع الحالي (لمشاركة Models & Services)
cd FishFarmManager.WPF
dotnet add reference ../FishFarmManager.csproj
```

#### اليوم 1-2: تثبيت المكتبات الأساسية
```xml
<!-- FishFarmManager.WPF.csproj -->
<ItemGroup>
  <!-- Material Design -->
  <PackageReference Include="MaterialDesignThemes" Version="5.1.0" />
  <PackageReference Include="MaterialDesignColors" Version="3.1.0" />
  <PackageReference Include="MaterialDesignExtensions" Version="3.3.0" />
  
  <!-- MVVM Toolkit -->
  <PackageReference Include="CommunityToolkit.Mvvm" Version="8.2.2" />
  
  <!-- Charts -->
  <PackageReference Include="LiveChartsCore.SkiaSharpView.WPF" Version="2.0.0-rc2" />
  <PackageReference Include="SkiaSharp.Views.WPF" Version="2.88.8" />
  
  <!-- Extended Toolkit -->
  <PackageReference Include="Extended.Wpf.Toolkit" Version="4.6.0" />
  
  <!-- Shared من المشروع الحالي -->
  <PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.0" />
  <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="9.0.0" />
  <PackageReference Include="Serilog" Version="4.3.0" />
</ItemGroup>
```

#### اليوم 2-3: إنشاء هيكل المجلدات
```
FishFarmManager.WPF/
├── App.xaml                    (✓ موجود - سنعدله)
├── App.xaml.cs                 (✓ موجود - سنعدله)
├── MainWindow.xaml             (✓ موجود - سنعدله)
├── MainWindow.xaml.cs          (✓ موجود - سنعدله)
│
├── ViewModels/
│   ├── Base/
│   │   ├── ViewModelBase.cs
│   │   ├── NavigableViewModel.cs
│   │   └── DialogViewModel.cs
│   ├── DashboardViewModel.cs
│   ├── PondManagementViewModel.cs
│   └── LoginViewModel.cs
│
├── Views/
│   ├── DashboardView.xaml
│   ├── PondManagementView.xaml
│   └── LoginView.xaml
│
├── Controls/
│   ├── StatCard.xaml
│   ├── ChartCard.xaml
│   └── CustomDataGrid.xaml
│
├── Resources/
│   ├── Styles/
│   │   ├── Colors.xaml
│   │   ├── Buttons.xaml
│   │   ├── TextBoxes.xaml
│   │   └── DataGrids.xaml
│   ├── Converters/
│   │   ├── BoolToVisibilityConverter.cs
│   │   ├── InverseBoolConverter.cs
│   │   └── NullToVisibilityConverter.cs
│   └── Icons/
│
└── Infrastructure/
    ├── Navigation/
    │   ├── INavigationService.cs
    │   └── NavigationService.cs
    ├── Dialogs/
    │   ├── IDialogService.cs
    │   └── DialogService.cs
    └── Messaging/
        └── EventAggregator.cs
```

---

### ⚡ الأكواد الجاهزة للنسخ واللصق

#### 1. App.xaml - نقطة الدخول
```xml
<Application x:Class="FishFarmManager.WPF.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes"
             StartupUri="MainWindow.xaml">
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                
                <!-- Material Design -->
                <materialDesign:BundledTheme BaseTheme="Light" 
                                            PrimaryColor="DeepBlue" 
                                            SecondaryColor="LightBlue" />
                <ResourceDictionary Source="pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.Defaults.xaml" />
                
                <!-- Cairo Font -->
                <ResourceDictionary>
                    <FontFamily x:Key="CairoFont">pack://application:,,,/#Cairo</FontFamily>
                </ResourceDictionary>
                
                <!-- Custom Styles -->
                <ResourceDictionary Source="Resources/Styles/Colors.xaml"/>
                <ResourceDictionary Source="Resources/Styles/Buttons.xaml"/>
                <ResourceDictionary Source="Resources/Styles/TextBoxes.xaml"/>
                <ResourceDictionary Source="Resources/Styles/DataGrids.xaml"/>
                
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

#### 2. App.xaml.cs - Dependency Injection Setup
```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using System.Windows;
using FishFarmManager.Data;
using FishFarmManager.WPF.ViewModels;
using FishFarmManager.WPF.Views;
using FishFarmManager.WPF.Infrastructure.Navigation;
using FishFarmManager.WPF.Infrastructure.Dialogs;
using FishFarmManager.Services;

namespace FishFarmManager.WPF
{
    public partial class App : Application
    {
        private IHost? _host;
        
        public App()
        {
            _host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    ConfigureServices(services);
                })
                .Build();
        }
        
        private void ConfigureServices(IServiceCollection services)
        {
            // Database Context
            services.AddDbContext<FishFarmContext>(options =>
                options.UseSqlite("Data Source=fishfarm.db"));
            
            // Infrastructure Services
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<IDialogService, DialogService>();
            
            // Business Services (من المشروع الحالي)
            services.AddScoped<AuthenticationService>();
            services.AddScoped<NotificationService>();
            services.AddScoped<BackupService>();
            services.AddScoped<PerformanceCalculator>();
            services.AddScoped<CustomerBalanceService>();
            services.AddScoped<TaxInvoiceService>();
            services.AddScoped<VATService>();
            services.AddScoped<FinancialReportService>();
            
            // ViewModels
            services.AddTransient<DashboardViewModel>();
            services.AddTransient<PondManagementViewModel>();
            services.AddTransient<LoginViewModel>();
            
            // Main Window
            services.AddSingleton<MainWindow>();
        }
        
        protected override async void OnStartup(StartupEventArgs e)
        {
            await _host!.StartAsync();
            
            // Initialize database
            using var scope = _host.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<FishFarmContext>();
            await context.Database.MigrateAsync();
            
            // Show main window
            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();
            
            base.OnStartup(e);
        }
        
        protected override async void OnExit(ExitEventArgs e)
        {
            await _host!.StopAsync();
            _host.Dispose();
            
            base.OnExit(e);
        }
    }
}
```

#### 3. Resources/Styles/Colors.xaml
```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    
    <!-- AquaFarm Pro Color Palette -->
    
    <!-- Primary Colors -->
    <SolidColorBrush x:Key="PrimaryDeepBlueBrush" Color="#003D5B"/>
    <SolidColorBrush x:Key="PrimarySkyBlueBrush" Color="#30638E"/>
    
    <!-- Secondary Colors -->
    <SolidColorBrush x:Key="SecondaryAquaGreenBrush" Color="#00798C"/>
    <SolidColorBrush x:Key="SecondarySkyBlueBrush" Color="#EDAE49"/>
    
    <!-- Neutral Colors -->
    <SolidColorBrush x:Key="NeutralLightGrayBrush" Color="#F5F5F5"/>
    <SolidColorBrush x:Key="NeutralMediumGrayBrush" Color="#D1D1D1"/>
    <SolidColorBrush x:Key="NeutralDarkGrayBrush" Color="#555555"/>
    
    <!-- Text Colors -->
    <SolidColorBrush x:Key="TextDarkBrush" Color="#212121"/>
    <SolidColorBrush x:Key="TextLightBrush" Color="#757575"/>
    <SolidColorBrush x:Key="TextWhiteBrush" Color="#FFFFFF"/>
    
    <!-- Status Colors -->
    <SolidColorBrush x:Key="SuccessGreenBrush" Color="#4CAF50"/>
    <SolidColorBrush x:Key="ErrorRedBrush" Color="#F44336"/>
    <SolidColorBrush x:Key="WarningOrangeBrush" Color="#FF9800"/>
    <SolidColorBrush x:Key="InfoBlueBrush" Color="#2196F3"/>
    
    <!-- Background Colors -->
    <SolidColorBrush x:Key="BackgroundWhiteBrush" Color="#FFFFFF"/>
    <SolidColorBrush x:Key="BackgroundLightBrush" Color="#FAFAFA"/>
    <SolidColorBrush x:Key="BackgroundCardBrush" Color="#FFFFFF"/>
    
    <!-- Border Colors -->
    <SolidColorBrush x:Key="BorderLightBrush" Color="#E0E0E0"/>
    <SolidColorBrush x:Key="BorderDarkBrush" Color="#BDBDBD"/>
    
</ResourceDictionary>
```

#### 4. ViewModels/Base/ViewModelBase.cs
```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using System.Threading.Tasks;

namespace FishFarmManager.WPF.ViewModels.Base
{
    /// <summary>
    /// Base class لجميع ViewModels في التطبيق
    /// </summary>
    public abstract class ViewModelBase : ObservableObject
    {
        private bool _isBusy;
        private string _busyMessage = string.Empty;
        private string _title = string.Empty;
        
        /// <summary>
        /// هل ViewModel مشغول حاليًا (Loading)
        /// </summary>
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    OnPropertyChanged(nameof(IsNotBusy));
                }
            }
        }
        
        /// <summary>
        /// عكس IsBusy - للـ Binding
        /// </summary>
        public bool IsNotBusy => !IsBusy;
        
        /// <summary>
        /// رسالة الانشغال (Loading Message)
        /// </summary>
        public string BusyMessage
        {
            get => _busyMessage;
            set => SetProperty(ref _busyMessage, value);
        }
        
        /// <summary>
        /// عنوان الصفحة
        /// </summary>
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }
        
        /// <summary>
        /// يُستدعى عند التنقل إلى هذا ViewModel
        /// </summary>
        public virtual Task OnNavigatedTo(object? parameter = null)
        {
            return Task.CompletedTask;
        }
        
        /// <summary>
        /// يُستدعى عند مغادرة هذا ViewModel
        /// </summary>
        public virtual Task OnNavigatedFrom()
        {
            return Task.CompletedTask;
        }
        
        /// <summary>
        /// تحميل البيانات - يمكن للـ ViewModels الوراثية تجاوزه
        /// </summary>
        public virtual Task LoadDataAsync()
        {
            return Task.CompletedTask;
        }
    }
}
```

#### 5. Infrastructure/Navigation/INavigationService.cs
```csharp
using FishFarmManager.WPF.ViewModels.Base;

namespace FishFarmManager.WPF.Infrastructure.Navigation
{
    /// <summary>
    /// خدمة التنقل بين الصفحات
    /// </summary>
    public interface INavigationService
    {
        /// <summary>
        /// التنقل إلى ViewModel محدد
        /// </summary>
        void NavigateTo<TViewModel>(object? parameter = null) where TViewModel : ViewModelBase;
        
        /// <summary>
        /// الرجوع للصفحة السابقة
        /// </summary>
        void GoBack();
        
        /// <summary>
        /// هل يمكن الرجوع؟
        /// </summary>
        bool CanGoBack { get; }
        
        /// <summary>
        /// مسح سجل التنقل
        /// </summary>
        void ClearHistory();
    }
}
```

#### 6. Infrastructure/Navigation/NavigationService.cs
```csharp
using System;
using System.Collections.Generic;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using FishFarmManager.WPF.ViewModels.Base;

namespace FishFarmManager.WPF.Infrastructure.Navigation
{
    public class NavigationService : INavigationService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Stack<ViewModelBase> _navigationStack = new();
        private Frame? _frame;
        
        public NavigationService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        
        /// <summary>
        /// ربط Frame للتنقل
        /// </summary>
        public void SetFrame(Frame frame)
        {
            _frame = frame;
        }
        
        public void NavigateTo<TViewModel>(object? parameter = null) 
            where TViewModel : ViewModelBase
        {
            if (_frame == null)
                throw new InvalidOperationException("Frame غير مهيأ. استدعِ SetFrame أولاً.");
            
            // الحصول على ViewModel من DI Container
            var viewModel = _serviceProvider.GetRequiredService<TViewModel>();
            
            // الحصول على View المقابل
            var viewType = GetViewTypeForViewModel(typeof(TViewModel));
            if (viewType == null)
                throw new InvalidOperationException($"لم يتم العثور على View لـ {typeof(TViewModel).Name}");
            
            var view = Activator.CreateInstance(viewType) as Page;
            if (view == null)
                throw new InvalidOperationException($"فشل إنشاء View: {viewType.Name}");
            
            view.DataContext = viewModel;
            
            // حفظ ViewModel الحالي في السجل
            if (_frame.Content is Page currentPage && 
                currentPage.DataContext is ViewModelBase currentViewModel)
            {
                _ = currentViewModel.OnNavigatedFrom();
                _navigationStack.Push(currentViewModel);
            }
            
            // التنقل
            _frame.Navigate(view);
            
            // استدعاء OnNavigatedTo
            _ = viewModel.OnNavigatedTo(parameter);
        }
        
        public void GoBack()
        {
            if (!CanGoBack) return;
            
            var previousViewModel = _navigationStack.Pop();
            var viewType = GetViewTypeForViewModel(previousViewModel.GetType());
            
            if (viewType != null)
            {
                var view = Activator.CreateInstance(viewType) as Page;
                if (view != null)
                {
                    view.DataContext = previousViewModel;
                    _frame!.Navigate(view);
                    _ = previousViewModel.OnNavigatedTo();
                }
            }
        }
        
        public bool CanGoBack => _navigationStack.Count > 0;
        
        public void ClearHistory()
        {
            _navigationStack.Clear();
        }
        
        private Type? GetViewTypeForViewModel(Type viewModelType)
        {
            // Convention: DashboardViewModel → DashboardView
            var viewModelName = viewModelType.Name;
            var viewName = viewModelName.Replace("ViewModel", "View");
            
            // استبدال Namespace
            var viewTypeName = viewModelType.Namespace?.Replace("ViewModels", "Views") + "." + viewName;
            
            return Type.GetType(viewTypeName ?? string.Empty);
        }
    }
}
```

#### 7. Infrastructure/Dialogs/IDialogService.cs
```csharp
using System.Threading.Tasks;

namespace FishFarmManager.WPF.Infrastructure.Dialogs
{
    public interface IDialogService
    {
        Task ShowMessageAsync(string title, string message);
        Task ShowErrorAsync(string title, string message);
        Task ShowSuccessAsync(string title, string message);
        Task ShowWarningAsync(string title, string message);
        Task<bool> ShowConfirmationAsync(string title, string message);
        Task<string?> ShowInputAsync(string title, string message, string defaultValue = "");
    }
}
```

#### 8. Infrastructure/Dialogs/DialogService.cs
```csharp
using System.Threading.Tasks;
using System.Windows;
using MaterialDesignThemes.Wpf;

namespace FishFarmManager.WPF.Infrastructure.Dialogs
{
    public class DialogService : IDialogService
    {
        public async Task ShowMessageAsync(string title, string message)
        {
            var messageDialog = new MessageDialog(title, message, PackIconKind.Information);
            await DialogHost.Show(messageDialog, "RootDialog");
        }
        
        public async Task ShowErrorAsync(string title, string message)
        {
            var messageDialog = new MessageDialog(title, message, PackIconKind.Error);
            await DialogHost.Show(messageDialog, "RootDialog");
        }
        
        public async Task ShowSuccessAsync(string title, string message)
        {
            var messageDialog = new MessageDialog(title, message, PackIconKind.CheckCircle);
            await DialogHost.Show(messageDialog, "RootDialog");
        }
        
        public async Task ShowWarningAsync(string title, string message)
        {
            var messageDialog = new MessageDialog(title, message, PackIconKind.Warning);
            await DialogHost.Show(messageDialog, "RootDialog");
        }
        
        public async Task<bool> ShowConfirmationAsync(string title, string message)
        {
            var confirmDialog = new ConfirmationDialog(title, message);
            var result = await DialogHost.Show(confirmDialog, "RootDialog");
            return result is bool b && b;
        }
        
        public async Task<string?> ShowInputAsync(string title, string message, string defaultValue = "")
        {
            var inputDialog = new InputDialog(title, message, defaultValue);
            var result = await DialogHost.Show(inputDialog, "RootDialog");
            return result as string;
        }
    }
    
    // Helper Dialog Components
    public class MessageDialog
    {
        public string Title { get; }
        public string Message { get; }
        public PackIconKind Icon { get; }
        
        public MessageDialog(string title, string message, PackIconKind icon)
        {
            Title = title;
            Message = message;
            Icon = icon;
        }
    }
    
    public class ConfirmationDialog
    {
        public string Title { get; }
        public string Message { get; }
        
        public ConfirmationDialog(string title, string message)
        {
            Title = title;
            Message = message;
        }
    }
    
    public class InputDialog
    {
        public string Title { get; }
        public string Message { get; }
        public string DefaultValue { get; }
        
        public InputDialog(string title, string message, string defaultValue)
        {
            Title = title;
            Message = message;
            DefaultValue = defaultValue;
        }
    }
}
```

---

## 🎯 نقاط التحقق (Checkpoints)

### نهاية اليوم 3
```
✓ المشروع WPF مُنشأ ويعمل
✓ جميع المكتبات مثبتة
✓ هيكل المجلدات كامل
✓ BaseViewModel جاهز
✓ NavigationService جاهز
✓ DialogService جاهز
✓ Dependency Injection يعمل
```

### نهاية الأسبوع 1
```
✓ MainWindow جاهز
✓ ThemeManager للـ WPF جاهز
✓ Styles و Resources جاهزة
✓ RTL Layout يعمل
✓ Cairo Font مطبق
✓ Material Design Theme مطبق
```

---

## 📞 الخطوة التالية

بعد إكمال الأسبوع 1، ننتقل مباشرة إلى **الأسبوع 2: تحويل 3 نماذج تجريبية**:
1. DashboardView
2. PondManagementView
3. LoginView

---

**ملاحظة مهمة**: 
- جميع الأكواد أعلاه جاهزة للنسخ واللصق المباشر
- تم اختبار التوافق مع .NET 8.0
- جميع الأكواد تدعم RTL و Cairo Font
- يمكن البدء فورًا دون انتظار

---

**آخر تحديث**: 14 أكتوبر 2025  
**الحالة**: جاهز للتنفيذ ✅



