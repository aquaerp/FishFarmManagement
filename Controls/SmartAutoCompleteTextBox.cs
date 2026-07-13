using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using FishFarmManager.Services;

namespace FishFarmManager.Controls
{
    /// <summary>
    /// صندوق نص ذكي مع إكمال تلقائي متقدم
    /// Smart TextBox with Advanced Autocomplete
    /// </summary>
    public class SmartAutoCompleteTextBox : TextBox
    {
        #region Fields

        private readonly AutocompleteService _autocompleteService;
        private ListBox _suggestionListBox = null!;
        private Form _suggestionForm = null!;
        private System.Windows.Forms.Timer _searchTimer = null!;
        private CancellationTokenSource? _cancellationTokenSource;
        
        // Properties
        private string _entityType = string.Empty;
        private string _searchField = "Name";
        private int _minCharacters = 2;
        private int _searchDelayMs = 300;
        private bool _autoFillDetails = true;
        private bool _recordUsage = true;

        // Events
        public event EventHandler<AutocompleteSelectedEventArgs>? ItemSelected;
        public event EventHandler<AutocompleteResult>? DataFilled;

        // Selected item
        private AutocompleteResult? _selectedItem;

        #endregion

        #region Properties

        /// <summary>
        /// نوع الكيان للبحث (Customer, Supplier, InventoryItem, إلخ)
        /// </summary>
        public string EntityType
        {
            get => _entityType;
            set => _entityType = value;
        }

        /// <summary>
        /// الحقل المستهدف للبحث (Name, Phone, Email, إلخ)
        /// </summary>
        public string SearchField
        {
            get => _searchField;
            set => _searchField = value;
        }

        /// <summary>
        /// الحد الأدنى من الأحرف لبدء البحث
        /// </summary>
        public int MinCharacters
        {
            get => _minCharacters;
            set => _minCharacters = Math.Max(1, value);
        }

        /// <summary>
        /// التأخير بالمللي ثانية قبل بدء البحث
        /// </summary>
        public int SearchDelayMs
        {
            get => _searchDelayMs;
            set => _searchDelayMs = Math.Max(100, value);
        }

        /// <summary>
        /// هل يتم تعبئة التفاصيل تلقائياً عند الاختيار
        /// </summary>
        public bool AutoFillDetails
        {
            get => _autoFillDetails;
            set => _autoFillDetails = value;
        }

        /// <summary>
        /// هل يتم تسجيل الاستخدام
        /// </summary>
        public bool RecordUsage
        {
            get => _recordUsage;
            set => _recordUsage = value;
        }

        /// <summary>
        /// العنصر المحدد حالياً
        /// </summary>
        public AutocompleteResult? SelectedItem
        {
            get => _selectedItem;
            private set => _selectedItem = value;
        }

        #endregion

        #region Constructor

        public SmartAutoCompleteTextBox(AutocompleteService autocompleteService)
        {
            _autocompleteService = autocompleteService ?? throw new ArgumentNullException(nameof(autocompleteService));
            
            InitializeComponents();
            AttachEventHandlers();
        }

        /// <summary>
        /// Constructor بدون Dependency Injection (يُنشئ AutocompleteService داخلياً)
        /// </summary>
        public SmartAutoCompleteTextBox() : this(new AutocompleteService(new Data.FishFarmContext()))
        {
        }

        #endregion

        #region Initialization

        private void InitializeComponents()
        {
            // إعداد Timer للبحث المتأخر
            _searchTimer = new System.Windows.Forms.Timer
            {
                Interval = _searchDelayMs
            };
            _searchTimer.Tick += SearchTimer_Tick;

            // إعداد النموذج المنبثق للاقتراحات
            _suggestionForm = new Form
            {
                FormBorderStyle = FormBorderStyle.None,
                ShowInTaskbar = false,
                StartPosition = FormStartPosition.Manual,
                TopMost = true,
                BackColor = Color.White
            };

            // إعداد قائمة الاقتراحات
            _suggestionListBox = new ListBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Cairo", 10F, FontStyle.Regular),
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = 45,
                BorderStyle = BorderStyle.None,
                BackColor = Color.White,
                RightToLeft = RightToLeft.Yes
            };
            
            _suggestionListBox.DrawItem += SuggestionListBox_DrawItem;
            _suggestionListBox.MouseClick += SuggestionListBox_MouseClick;
            _suggestionListBox.KeyDown += SuggestionListBox_KeyDown;

            _suggestionForm.Controls.Add(_suggestionListBox);

            // خصائص TextBox
            this.Font = new Font("Cairo", 10F);
            this.RightToLeft = RightToLeft.Yes;
        }

        private void AttachEventHandlers()
        {
            this.TextChanged += SmartAutoCompleteTextBox_TextChanged;
            this.KeyDown += SmartAutoCompleteTextBox_KeyDown;
            this.Leave += SmartAutoCompleteTextBox_Leave;
            this.LocationChanged += SmartAutoCompleteTextBox_LocationChanged;
            this.SizeChanged += SmartAutoCompleteTextBox_SizeChanged;
            
            // إغلاق القائمة عند فقدان التركيز
            this.LostFocus += (s, e) =>
            {
                // تأخير صغير للسماح باختيار العنصر
                Task.Delay(200).ContinueWith(_ =>
                {
                    if (!_suggestionListBox.Focused)
                        HideSuggestions();
                }, TaskScheduler.FromCurrentSynchronizationContext());
            };
        }

        #endregion

        #region Event Handlers

        private void SmartAutoCompleteTextBox_TextChanged(object? sender, EventArgs e)
        {
            // إيقاف Timer السابق
            _searchTimer.Stop();

            // مسح الاختيار السابق
            _selectedItem = null;

            // التحقق من الحد الأدنى للأحرف
            if (string.IsNullOrWhiteSpace(this.Text) || this.Text.Length < _minCharacters)
            {
                HideSuggestions();
                return;
            }

            // بدء Timer جديد
            _searchTimer.Interval = _searchDelayMs;
            _searchTimer.Start();
        }

        private async void SearchTimer_Tick(object? sender, EventArgs e)
        {
            _searchTimer.Stop();

            if (string.IsNullOrWhiteSpace(_entityType))
                return;

            await PerformSearchAsync();
        }

        private void SmartAutoCompleteTextBox_KeyDown(object? sender, KeyEventArgs e)
        {
            if (!_suggestionForm.Visible)
                return;

            switch (e.KeyCode)
            {
                case Keys.Down:
                    if (_suggestionListBox.SelectedIndex < _suggestionListBox.Items.Count - 1)
                        _suggestionListBox.SelectedIndex++;
                    e.Handled = true;
                    break;

                case Keys.Up:
                    if (_suggestionListBox.SelectedIndex > 0)
                        _suggestionListBox.SelectedIndex--;
                    e.Handled = true;
                    break;

                case Keys.Enter:
                    if (_suggestionListBox.SelectedIndex >= 0)
                    {
                        SelectItem(_suggestionListBox.SelectedIndex);
                        e.Handled = true;
                        e.SuppressKeyPress = true;
                    }
                    break;

                case Keys.Escape:
                    HideSuggestions();
                    e.Handled = true;
                    break;
            }
        }

        private void SmartAutoCompleteTextBox_Leave(object? sender, EventArgs e)
        {
            // سيتم إخفاء القائمة عبر LostFocus
        }

        private void SmartAutoCompleteTextBox_LocationChanged(object? sender, EventArgs e)
        {
            if (_suggestionForm.Visible)
                PositionSuggestionForm();
        }

        private void SmartAutoCompleteTextBox_SizeChanged(object? sender, EventArgs e)
        {
            if (_suggestionForm.Visible)
                PositionSuggestionForm();
        }

        private void SuggestionListBox_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= _suggestionListBox.Items.Count)
                return;

            var item = (AutocompleteResult)_suggestionListBox.Items[e.Index];

            // الخلفية
            bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            e.Graphics.FillRectangle(
                isSelected ? new SolidBrush(Color.FromArgb(0, 120, 212)) : Brushes.White,
                e.Bounds
            );

            // النص الرئيسي
            var mainTextBrush = isSelected ? Brushes.White : Brushes.Black;
            var mainFont = new Font("Cairo", 10F, FontStyle.Bold);
            e.Graphics.DrawString(
                item.DisplayText,
                mainFont,
                mainTextBrush,
                new PointF(e.Bounds.Right - 10, e.Bounds.Top + 5),
                new StringFormat { Alignment = StringAlignment.Far }
            );

            // النص الثانوي
            if (!string.IsNullOrEmpty(item.SecondaryText))
            {
                var secondaryFont = new Font("Cairo", 8F, FontStyle.Regular);
                var secondaryBrush = isSelected ? Brushes.WhiteSmoke : Brushes.Gray;
                e.Graphics.DrawString(
                    item.SecondaryText,
                    secondaryFont,
                    secondaryBrush,
                    new PointF(e.Bounds.Right - 10, e.Bounds.Top + 25),
                    new StringFormat { Alignment = StringAlignment.Far }
                );
            }

            // خط فاصل
            if (e.Index < _suggestionListBox.Items.Count - 1)
            {
                e.Graphics.DrawLine(
                    Pens.LightGray,
                    e.Bounds.Left + 10,
                    e.Bounds.Bottom - 1,
                    e.Bounds.Right - 10,
                    e.Bounds.Bottom - 1
                );
            }

            e.DrawFocusRectangle();
        }

        private void SuggestionListBox_MouseClick(object? sender, MouseEventArgs e)
        {
            int index = _suggestionListBox.IndexFromPoint(e.Location);
            if (index >= 0)
            {
                SelectItem(index);
            }
        }

        private void SuggestionListBox_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && _suggestionListBox.SelectedIndex >= 0)
            {
                SelectItem(_suggestionListBox.SelectedIndex);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                HideSuggestions();
                this.Focus();
                e.Handled = true;
            }
        }

        #endregion

        #region Search Methods

        private async Task PerformSearchAsync()
        {
            // إلغاء البحث السابق
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                var results = await _autocompleteService.SearchAsync(
                    _entityType,
                    this.Text,
                    _searchField
                );

                // التحقق من الإلغاء
                if (_cancellationTokenSource.Token.IsCancellationRequested)
                    return;

                // عرض النتائج
                if (results != null && results.Any())
                {
                    ShowSuggestions(results);
                }
                else
                {
                    HideSuggestions();
                }
            }
            catch (Exception ex)
            {
                // التعامل مع الأخطاء بصمت أو تسجيلها
                System.Diagnostics.Debug.WriteLine($"Search error: {ex.Message}");
                HideSuggestions();
            }
        }

        #endregion

        #region Display Methods

        private void ShowSuggestions(List<AutocompleteResult> results)
        {
            _suggestionListBox.Items.Clear();
            
            foreach (var result in results)
            {
                _suggestionListBox.Items.Add(result);
            }

            if (_suggestionListBox.Items.Count > 0)
            {
                _suggestionListBox.SelectedIndex = 0;

                // حساب الحجم والموضع
                PositionSuggestionForm();

                // عرض النموذج
                if (!_suggestionForm.Visible)
                    _suggestionForm.Show(this);
            }
        }

        private void PositionSuggestionForm()
        {
            var screenLocation = this.PointToScreen(new Point(0, this.Height));
            
            int width = this.Width;
            int height = Math.Min(_suggestionListBox.Items.Count * _suggestionListBox.ItemHeight, 300);

            _suggestionForm.Location = screenLocation;
            _suggestionForm.Size = new Size(width, height);
        }

        private void HideSuggestions()
        {
            if (_suggestionForm.Visible)
            {
                _suggestionForm.Hide();
            }
        }

        #endregion

        #region Selection Methods

        private void SelectItem(int index)
        {
            if (index < 0 || index >= _suggestionListBox.Items.Count)
                return;

            var selectedResult = (AutocompleteResult)_suggestionListBox.Items[index];
            _selectedItem = selectedResult;

            // تعيين النص
            this.Text = selectedResult.DisplayText;

            // تسجيل الاستخدام
            if (_recordUsage)
            {
                _autocompleteService.RecordUsage(
                    selectedResult.EntityType,
                    selectedResult.Id,
                    selectedResult.DisplayText
                );
            }

            // إخفاء القائمة
            HideSuggestions();

            // إطلاق الأحداث
            OnItemSelected(selectedResult);
            
            if (_autoFillDetails)
                OnDataFilled(selectedResult);

            // نقل التركيز للحقل التالي
            this.Parent?.SelectNextControl(this, true, true, true, true);
        }

        protected virtual void OnItemSelected(AutocompleteResult item)
        {
            ItemSelected?.Invoke(this, new AutocompleteSelectedEventArgs(item));
        }

        protected virtual void OnDataFilled(AutocompleteResult item)
        {
            DataFilled?.Invoke(this, item);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// مسح الاختيار والنص
        /// </summary>
        public void ClearSelection()
        {
            _selectedItem = null;
            this.Clear();
            HideSuggestions();
        }

        /// <summary>
        /// الحصول على البيانات الإضافية للعنصر المحدد
        /// </summary>
        public T? GetAdditionalData<T>(string key)
        {
            if (_selectedItem?.AdditionalData?.ContainsKey(key) == true)
            {
                var value = _selectedItem.AdditionalData[key];
                if (value is T typedValue)
                    return typedValue;
            }
            return default;
        }

        /// <summary>
        /// عرض العناصر الأكثر استخداماً
        /// </summary>
        public Task ShowMostUsedAsync()
        {
            if (string.IsNullOrWhiteSpace(_entityType))
                return Task.CompletedTask;

            var mostUsed = _autocompleteService.GetMostUsed(_entityType, 10);
            
            if (mostUsed.Any())
            {
                ShowSuggestions(mostUsed);
            }

            return Task.CompletedTask;
        }

        #endregion

        #region Cleanup

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _searchTimer?.Dispose();
                _cancellationTokenSource?.Dispose();
                _suggestionListBox?.Dispose();
                _suggestionForm?.Dispose();
            }
            base.Dispose(disposing);
        }

        #endregion
    }

    #region Event Args

    /// <summary>
    /// معاملات حدث اختيار عنصر من الإكمال التلقائي
    /// </summary>
    public class AutocompleteSelectedEventArgs : EventArgs
    {
        public AutocompleteResult SelectedItem { get; }

        public AutocompleteSelectedEventArgs(AutocompleteResult item)
        {
            SelectedItem = item;
        }
    }

    #endregion
}

