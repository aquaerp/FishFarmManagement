# 🔧 دليل المطور: حل مشكلة Memory Leak في WinForms

## 🎯 الهدف

هذا الدليل يشرح كيفية منع مشاكل Memory Leak في تطبيقات WinForms C#، بناءً على الإصلاحات المطبقة على FishFarmManager.

---

## 🧩 المكونات الأساسية

### 1. Form Lifecycle Management

```csharp
public partial class MainForm : Form
{
    // Resources to track
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly List<Form> _openChildForms;
    private readonly FishFarmContext _context;
    
    public MainForm()
    {
        _cancellationTokenSource = new CancellationTokenSource();
        _openChildForms = new List<Form>();
        
        InitializeComponent();
        
        // CRITICAL: Register closing event
        this.FormClosing += MainForm_FormClosing;
    }
    
    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        try
        {
            // 1. Cancel pending operations
            _cancellationTokenSource?.Cancel();
            
            // 2. Close child forms
            CloseAllChildForms();
            
            // 3. Dispose managed resources
            _context?.Dispose();
            _cancellationTokenSource?.Dispose();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error during closing: {ex}");
        }
    }
}
```

### 2. Child Form Management

```csharp
private void ShowChildForm(Form childForm)
{
    if (childForm == null) return;
    
    // Track the form
    _openChildForms.Add(childForm);
    
    // Auto-remove when closed
    childForm.FormClosed += (s, e) =>
    {
        _openChildForms.Remove(childForm);
        childForm.Dispose();
    };
    
    // Show modally
    childForm.ShowDialog();
}

private void CloseAllChildForms()
{
    // Copy to avoid modification during iteration
    var formsToClose = _openChildForms.ToList();
    
    foreach (var form in formsToClose)
    {
        try
        {
            if (form != null && !form.IsDisposed)
            {
                form.Close();
                form.Dispose();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error closing form: {ex.Message}");
        }
    }
    
    _openChildForms.Clear();
}
```

### 3. Async Operations with Cancellation

```csharp
private CancellationTokenSource _cts;

private async void StartAsyncOperation()
{
    try
    {
        var result = await DoWorkAsync(_cts.Token);
        
        // Check if still valid
        if (!_cts.IsCancellationRequested)
        {
            UpdateUI(result);
        }
    }
    catch (OperationCanceledException)
    {
        // Expected when cancelled
    }
    catch (Exception ex)
    {
        // Log or handle error
        MessageBox.Show($"Error: {ex.Message}");
    }
}

private async Task<string> DoWorkAsync(CancellationToken ct)
{
    // Check periodically
    ct.ThrowIfCancellationRequested();
    
    await Task.Delay(1000, ct);
    
    return "Done";
}
```

### 4. DbContext Management

```csharp
// ❌ WRONG: Shared context
public class ChildForm : Form
{
    private readonly FishFarmContext _context; // Shared reference
    
    public ChildForm(FishFarmContext context)
    {
        _context = context; // Problem: keeps parent context alive
    }
}

// ✅ CORRECT: Own context or proper disposal
public class ChildForm : Form, IDisposable
{
    private FishFarmContext? _context;
    
    public ChildForm()
    {
        // Create own context
        _context = new FishFarmContext();
    }
    
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _context?.Dispose();
            _context = null;
        }
        base.Dispose(disposing);
    }
}
```

---

## 🚨 Common Pitfalls

### Pitfall 1: Event Handler Leak

```csharp
// ❌ WRONG: Event not unsubscribed
public class MyForm : Form
{
    private Timer _timer;
    
    public MyForm()
    {
        _timer = new Timer();
        _timer.Tick += Timer_Tick; // LEAK!
    }
}

// ✅ CORRECT: Proper cleanup
public class MyForm : Form
{
    private Timer? _timer;
    
    public MyForm()
    {
        _timer = new Timer();
        _timer.Tick += Timer_Tick;
    }
    
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_timer != null)
            {
                _timer.Tick -= Timer_Tick;
                _timer.Dispose();
                _timer = null;
            }
        }
        base.Dispose(disposing);
    }
}
```

### Pitfall 2: Background Thread Not Stopped

```csharp
// ❌ WRONG: Thread keeps running
public class MyForm : Form
{
    private Thread _workerThread;
    
    public MyForm()
    {
        _workerThread = new Thread(DoWork);
        _workerThread.Start(); // LEAK!
    }
}

// ✅ CORRECT: Stop thread on close
public class MyForm : Form
{
    private Thread? _workerThread;
    private bool _isRunning;
    
    public MyForm()
    {
        _isRunning = true;
        _workerThread = new Thread(DoWork);
        _workerThread.IsBackground = true; // Important!
        _workerThread.Start();
    }
    
    private void DoWork()
    {
        while (_isRunning)
        {
            // Work here
            Thread.Sleep(100);
        }
    }
    
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _isRunning = false;
            _workerThread?.Join(1000); // Wait max 1 second
        }
        base.Dispose(disposing);
    }
}
```

### Pitfall 3: Circular References

```csharp
// ❌ WRONG: Circular reference
public class Parent : Form
{
    private Child _child;
    
    public Parent()
    {
        _child = new Child(this); // Child holds reference to parent
    }
}

public class Child : Form
{
    private Parent _parent; // CIRCULAR REFERENCE!
    
    public Child(Parent parent)
    {
        _parent = parent;
    }
}

// ✅ CORRECT: Use weak reference or event
public class Child : Form
{
    public event EventHandler? DataChanged;
    
    public Child()
    {
        // No parent reference
    }
    
    private void NotifyParent()
    {
        DataChanged?.Invoke(this, EventArgs.Empty);
    }
}
```

---

## 📋 Checklist Template

```csharp
/// <summary>
/// Form Cleanup Checklist:
/// [✓] FormClosing event registered
/// [✓] Child forms tracked and disposed
/// [✓] Event handlers unsubscribed
/// [✓] Timers stopped and disposed
/// [✓] Background threads stopped
/// [✓] CancellationTokens cancelled
/// [✓] DbContext disposed
/// [✓] IDisposable resources disposed
/// </summary>
public class ProperForm : Form
{
    // ... implementation
}
```

---

## 🔍 Debugging Memory Leaks

### Using Visual Studio

1. **Memory Usage Tool**

   ```text
   Debug > Performance Profiler > Memory Usage
   ```

2. **Take Snapshots**
   - Before opening form
   - After opening form
   - After closing form
   - Compare differences

3. **Look for**
   - Forms not garbage collected
   - Event handlers still attached
   - Collections not cleared

### Using Code

```csharp
// Add to Program.cs for debugging
[Conditional("DEBUG")]
private static void MonitorMemory()
{
    var timer = new System.Windows.Forms.Timer();
    timer.Interval = 5000; // 5 seconds
    timer.Tick += (s, e) =>
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        var memory = GC.GetTotalMemory(false) / 1024 / 1024;
        Debug.WriteLine($"Memory: {memory} MB");
    };
    timer.Start();
}
```

---

## 🎓 Best Practices Summary

### DO ✅

1. **Always implement Dispose**

   ```csharp
   protected override void Dispose(bool disposing)
   {
       if (disposing) { /* cleanup */ }
       base.Dispose(disposing);
   }
   ```

2. **Use using statements**

   ```csharp
   using (var form = new MyForm())
   {
       form.ShowDialog();
   } // Auto-disposed
   ```

3. **Track child forms**

   ```csharp
   var form = new ChildForm();
   _childForms.Add(form);
   form.FormClosed += (s, e) => _childForms.Remove(form);
   ```

4. **Cancel async on close**

   ```csharp
   _cancellationTokenSource.Cancel();
   ```

5. **Unsubscribe events**

   ```csharp
   button.Click -= Button_Click;
   ```

### DON'T ❌

1. **Don't share DbContext** without proper management
2. **Don't forget to stop timers**
3. **Don't leave background threads running**
4. **Don't create circular references**
5. **Don't ignore Dispose warnings**

---

## 📚 Additional Resources

- [Microsoft: Implementing IDisposable](https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-dispose)
- [Memory Leak Patterns in C#](https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/memory-management-and-gc)
- [WinForms Best Practices](https://docs.microsoft.com/en-us/dotnet/desktop/winforms/advanced/)

---

## 💡 Quick Reference

```csharp
// Complete form template
public class MyForm : Form
{
    // 1. Resources
    private CancellationTokenSource? _cts;
    private List<Form> _childForms = new();
    private Timer? _timer;
    
    // 2. Constructor
    public MyForm()
    {
        _cts = new CancellationTokenSource();
        InitializeComponent();
        this.FormClosing += OnFormClosing;
    }
    
    // 3. Cleanup
    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        _cts?.Cancel();
        CloseChildren();
    }
    
    private void CloseChildren()
    {
        foreach (var child in _childForms.ToList())
        {
            child?.Close();
            child?.Dispose();
        }
        _childForms.Clear();
    }
    
    // 4. Dispose
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer?.Dispose();
            _cts?.Dispose();
        }
        base.Dispose(disposing);
    }
}
```

---

**مُعد بواسطة**: GitHub Copilot  
**التاريخ**: 7 أكتوبر 2025  
**الإصدار**: 1.0
