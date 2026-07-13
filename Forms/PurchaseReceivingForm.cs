using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.EntityFrameworkCore;
using FishFarmManager.Data;
using FishFarmManager.Models;
using FishFarmManager.Services;

namespace FishFarmManager.Forms
{
    /// <summary>
    /// نموذج استلام المشتريات
    /// Purchase Receiving Form
    /// </summary>
    public partial class PurchaseReceivingForm : Form
    {
        #region Fields

        private readonly FishFarmContext _context;
        private int? _currentReceivingId;
        private List<ReceivingItemTemp> _receivingItems = new List<ReceivingItemTemp>();

        // UI Controls
        private TextBox _receivingNumberTextBox = null!;
        private DateTimePicker _receivingDatePicker = null!;
        private ComboBox _purchaseOrderComboBox = null!;
        private DataGridView _itemsGrid = null!;
        private ComboBox _qualityResultComboBox = null!;
        private TextBox _qualityNotesTextBox = null!;
        private CheckBox _autoAddToStockCheckBox = null!;
        private Label _totalOrderedLabel = null!;
        private Label _totalReceivedLabel = null!;
        private Label _totalRejectedLabel = null!;

        // Buttons
        private Button _saveButton = null!;
        private Button _clearButton = null!;

        #endregion

        #region Helper Classes

        private class ReceivingItemTemp
        {
            public int PurchaseOrderItemId { get; set; }
            public int InventoryItemId { get; set; }
            public string ItemName { get; set; } = "";
            public decimal OrderedQuantity { get; set; }
            public decimal ReceivedQuantity { get; set; }
            public decimal RejectedQuantity { get; set; }
            public string? BatchNumber { get; set; }
            public DateTime? ExpiryDate { get; set; }
            public bool QualityAccepted { get; set; } = true;
        }

        #endregion

        #region Constructor

        public PurchaseReceivingForm(FishFarmContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));

            if (!AuthenticationService.HasPermission(UserRole.Admin, UserRole.Manager, UserRole.InventoryStaff))
            {
                MessageBox.Show("ليس لديك صلاحية لاستلام المشتريات", "تحذير",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.Load += (s, e) => this.Close();
                return;
            }

            InitializeComponent();
            InitializeForm();
        }

        #endregion

        #region Initialization

        private void InitializeComponent()
        {
            this.Text = "استلام المشتريات";
            this.Size = new Size(1400, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.Font = new Font("Cairo", 10F);

            var mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20),
                BackColor = Color.FromArgb(240, 240, 240)
            };

            var titleLabel = new Label
            {
                Text = "نظام استلام المشتريات",
                Location = new Point(20, 10),
                Size = new Size(1340, 40),
                Font = new Font("Cairo", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(46, 92, 138),
                TextAlign = ContentAlignment.MiddleRight
            };
            mainPanel.Controls.Add(titleLabel);

            CreateHeaderSection(mainPanel, 60);
            CreateItemsGridSection(mainPanel, 200);
            CreateQualitySection(mainPanel, 550);
            CreateSummarySection(mainPanel, 630);
            CreateButtonsSection(mainPanel);

            this.Controls.Add(mainPanel);
        }

        private void CreateHeaderSection(Panel parent, int startY)
        {
            var headerPanel = new GroupBox
            {
                Text = "معلومات الاستلام",
                Location = new Point(20, startY),
                Size = new Size(1340, 120),
                Font = new Font("Cairo", 10F, FontStyle.Bold)
            };

            int x = 1000;
            int y = 35;

            var receivingLabel = new Label { Text = "رقم الاستلام:", Location = new Point(x + 280, y), Size = new Size(120, 25), TextAlign = ContentAlignment.MiddleRight };
            headerPanel.Controls.Add(receivingLabel);

            _receivingNumberTextBox = new TextBox { Location = new Point(x, y), Size = new Size(270, 30), ReadOnly = true };
            headerPanel.Controls.Add(_receivingNumberTextBox);

            x -= 450;

            var dateLabel = new Label { Text = "التاريخ:", Location = new Point(x + 270, y), Size = new Size(80, 25), TextAlign = ContentAlignment.MiddleRight };
            headerPanel.Controls.Add(dateLabel);

            _receivingDatePicker = new DateTimePicker { Location = new Point(x, y), Size = new Size(260, 30), Format = DateTimePickerFormat.Short };
            headerPanel.Controls.Add(_receivingDatePicker);

            y += 45;
            x = 1000;

            var orderLabel = new Label { Text = "أمر الشراء:", Location = new Point(x + 280, y), Size = new Size(120, 25), TextAlign = ContentAlignment.MiddleRight };
            headerPanel.Controls.Add(orderLabel);

            _purchaseOrderComboBox = new ComboBox { Location = new Point(x, y), Size = new Size(270, 30), DropDownStyle = ComboBoxStyle.DropDownList };
            _purchaseOrderComboBox.SelectedIndexChanged += PurchaseOrder_Changed;
            headerPanel.Controls.Add(_purchaseOrderComboBox);

            parent.Controls.Add(headerPanel);
        }

        private void CreateItemsGridSection(Panel parent, int startY)
        {
            var itemsPanel = new GroupBox
            {
                Text = "البنود المستلمة",
                Location = new Point(20, startY),
                Size = new Size(1340, 330),
                Font = new Font("Cairo", 10F, FontStyle.Bold)
            };

            _itemsGrid = new DataGridView
            {
                Location = new Point(20, 35),
                Size = new Size(1300, 280),
                Font = new Font("Cairo", 9F),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                BackgroundColor = Color.White,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };

            itemsPanel.Controls.Add(_itemsGrid);
            parent.Controls.Add(itemsPanel);
        }

        private void CreateQualitySection(Panel parent, int startY)
        {
            var qualityPanel = new GroupBox
            {
                Text = "فحص الجودة",
                Location = new Point(20, startY),
                Size = new Size(1340, 60),
                Font = new Font("Cairo", 10F, FontStyle.Bold)
            };

            int x = 1050;

            var resultLabel = new Label { Text = "النتيجة:", Location = new Point(x + 200, 28), Size = new Size(70, 25), TextAlign = ContentAlignment.MiddleRight };
            qualityPanel.Controls.Add(resultLabel);

            _qualityResultComboBox = new ComboBox { Location = new Point(x, 28), Size = new Size(190, 30), DropDownStyle = ComboBoxStyle.DropDownList };
            LoadQualityResults();
            qualityPanel.Controls.Add(_qualityResultComboBox);

            x -= 450;

            _autoAddToStockCheckBox = new CheckBox
            {
                Text = "إضافة للمخزون تلقائياً",
                Location = new Point(x, 30),
                Size = new Size(200, 25),
                Checked = true
            };
            qualityPanel.Controls.Add(_autoAddToStockCheckBox);

            parent.Controls.Add(qualityPanel);
        }

        private void CreateSummarySection(Panel parent, int startY)
        {
            var summaryPanel = new Panel { Location = new Point(20, startY), Size = new Size(1340, 60), BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle };

            int x = 1100;

            CreateSummaryLabel(summaryPanel, "المطلوب:", ref _totalOrderedLabel, x, 15);
            x -= 250;
            CreateSummaryLabel(summaryPanel, "المستلم:", ref _totalReceivedLabel, x, 15);
            _totalReceivedLabel.ForeColor = Color.FromArgb(38, 166, 154);
            x -= 250;
            CreateSummaryLabel(summaryPanel, "المرفوض:", ref _totalRejectedLabel, x, 15);
            _totalRejectedLabel.ForeColor = Color.FromArgb(229, 57, 53);

            parent.Controls.Add(summaryPanel);
        }

        private void CreateSummaryLabel(Panel panel, string labelText, ref Label valueLabel, int x, int y)
        {
            var label = new Label { Text = labelText, Location = new Point(x + 140, y), Size = new Size(90, 25), TextAlign = ContentAlignment.MiddleRight, Font = new Font("Cairo", 9F) };
            panel.Controls.Add(label);

            valueLabel = new Label { Text = "0.00", Location = new Point(x, y), Size = new Size(130, 30), TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Cairo", 11F, FontStyle.Bold), BorderStyle = BorderStyle.FixedSingle };
            panel.Controls.Add(valueLabel);
        }

        private void CreateButtonsSection(Panel parent)
        {
            var buttonPanel = new Panel { Location = new Point(20, 700), Size = new Size(1340, 50), BackColor = Color.Transparent };

            int x = 1100;

            _saveButton = CreateButton("حفظ الاستلام", x, 10, 130);
            _saveButton.Click += async (s, e) => await SaveButton_ClickAsync();
            _saveButton.BackColor = Color.FromArgb(38, 166, 154);
            buttonPanel.Controls.Add(_saveButton);

            x -= 150;

            _clearButton = CreateButton("مسح", x, 10, 110);
            _clearButton.Click += ClearButton_Click;
            buttonPanel.Controls.Add(_clearButton);

            parent.Controls.Add(buttonPanel);
        }

        private Button CreateButton(string text, int x, int y, int width)
        {
            return new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, 35),
                Font = new Font("Cairo", 10F, FontStyle.Bold),
                BackColor = Color.FromArgb(46, 92, 138),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
        }

        private void InitializeForm()
        {
            try
            {
                _receivingNumberTextBox.Text = GenerateReceivingNumber();
                _receivingDatePicker.Value = DateTime.Today;

                _ = LoadPendingOrdersAsync();

                LoggingService.LogInfo("PurchaseReceivingForm initialized");
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Error initializing PurchaseReceivingForm", ex);
                MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region Data Loading

        private async Task LoadPendingOrdersAsync()
        {
            try
            {
                var orders = await _context.PurchaseOrders
                    .Where(p => p.Status == PurchaseOrderStatus.Sent || p.Status == PurchaseOrderStatus.PartiallyReceived)
                    .Include(p => p.Supplier)
                    .OrderByDescending(p => p.OrderDate)
                    .Select(p => new { p.Id, DisplayName = p.OrderNumber + " - " + p.Supplier.Name + " - " + p.OrderDate.ToString("yyyy-MM-dd") })
                    .ToListAsync();

                _purchaseOrderComboBox.DataSource = orders;
                _purchaseOrderComboBox.DisplayMember = "DisplayName";
                _purchaseOrderComboBox.ValueMember = "Id";

                if (_purchaseOrderComboBox.Items.Count > 0)
                    _purchaseOrderComboBox.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Error loading pending orders", ex);
            }
        }

        private void LoadQualityResults()
        {
            var results = new List<dynamic>
            {
                new { Value = QualityTestResult.Excellent, Display = "ممتاز" },
                new { Value = QualityTestResult.Good, Display = "جيد" },
                new { Value = QualityTestResult.Acceptable, Display = "مقبول" },
                new { Value = QualityTestResult.Failed, Display = "فاشل" }
            };

            _qualityResultComboBox.DataSource = results;
            _qualityResultComboBox.DisplayMember = "Display";
            _qualityResultComboBox.ValueMember = "Value";
        }

        #endregion

        #region Event Handlers

        private async void PurchaseOrder_Changed(object? sender, EventArgs e)
        {
            if (_purchaseOrderComboBox.SelectedValue is int orderId && orderId > 0)
            {
                await LoadOrderItemsAsync(orderId);
            }
        }

        private async Task LoadOrderItemsAsync(int orderId)
        {
            try
            {
                var items = await _context.PurchaseOrderItems
                    .Where(i => i.PurchaseOrderId == orderId)
                    .Include(i => i.InventoryItem)
                    .Select(i => new ReceivingItemTemp
                    {
                        PurchaseOrderItemId = i.Id,
                        InventoryItemId = i.InventoryItemId,
                        ItemName = i.ItemName,
                        OrderedQuantity = i.Quantity,
                        ReceivedQuantity = i.Quantity - i.RemainingQuantity, // Already received
                        RejectedQuantity = 0,
                        QualityAccepted = true
                    })
                    .ToListAsync();

                _receivingItems = items;
                DisplayItems();
                UpdateSummary();
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Error loading order items", ex);
            }
        }

        #endregion

        #region Display

        private void DisplayItems()
        {
            var displayData = _receivingItems.Select(item => new
            {
                العنصر = item.ItemName,
                المطلوب = item.OrderedQuantity.ToString("N2"),
                المستلم = item.ReceivedQuantity.ToString("N2"),
                المرفوض = item.RejectedQuantity.ToString("N2"),
                المقبول = (item.ReceivedQuantity - item.RejectedQuantity).ToString("N2"),
                الدفعة = item.BatchNumber ?? "-",
                الصلاحية = item.ExpiryDate?.ToString("yyyy-MM-dd") ?? "-",
                الجودة = item.QualityAccepted ? "✓" : "✗"
            }).ToList();

            _itemsGrid.DataSource = displayData;
        }

        private void UpdateSummary()
        {
            _totalOrderedLabel.Text = _receivingItems.Sum(i => i.OrderedQuantity).ToString("N2");
            _totalReceivedLabel.Text = _receivingItems.Sum(i => i.ReceivedQuantity).ToString("N2");
            _totalRejectedLabel.Text = _receivingItems.Sum(i => i.RejectedQuantity).ToString("N2");
        }

        #endregion

        #region Save Operations

        private async Task SaveButton_ClickAsync()
        {
            try
            {
                if (!ValidateInput())
                    return;

                if (_purchaseOrderComboBox.SelectedValue == null)
                {
                    MessageBox.Show("الرجاء اختيار أمر شراء", "تنبيه", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var confirmResult = MessageBox.Show(
                    "هل أنت متأكد من حفظ استلام المشتريات؟\n\nسيتم تحديث المخزون تلقائياً.",
                    "تأكيد الحفظ",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );

                if (confirmResult != DialogResult.Yes)
                    return;

                // إنشاء سجل الاستلام
                var receiving = new PurchaseReceiving
                {
                    ReceivingNumber = _receivingNumberTextBox.Text,
                    PurchaseOrderId = (int)_purchaseOrderComboBox.SelectedValue,
                    ReceivingDate = _receivingDatePicker.Value,
                    OverallQualityResult = (QualityTestResult)_qualityResultComboBox.SelectedValue,
                    QualityInspectionCompleted = true,
                    InspectedBy = AuthenticationService.CurrentUsername,
                    InspectionDate = DateTime.Now,
                    InspectionNotes = _qualityNotesTextBox.Text,
                    ReceivedBy = AuthenticationService.CurrentUsername,
                    CreatedBy = AuthenticationService.CurrentUsername,
                    CreatedAt = DateTime.Now
                };

                // إضافة البنود
                foreach (var item in _receivingItems)
                {
                    var acceptedQty = item.ReceivedQuantity - item.RejectedQuantity;
                    
                    var receivingItem = new PurchaseReceivingItem
                    {
                        PurchaseOrderItemId = item.PurchaseOrderItemId,
                        ItemName = item.ItemName,
                        OrderedQuantity = item.OrderedQuantity,
                        ReceivedQuantity = item.ReceivedQuantity,
                        RejectedQuantity = item.RejectedQuantity,
                        CreatedAt = DateTime.Now
                    };

                    receiving.Items.Add(receivingItem);

                    // تحديث المخزون تلقائياً إذا كان الخيار مفعلاً
                    if (_autoAddToStockCheckBox.Checked && acceptedQty > 0)
                    {
                        var stockMovement = new StockMovement
                        {
                            MovementType = StockMovementType.Purchase,
                            MovementDate = _receivingDatePicker.Value,
                            ReferenceNumber = _receivingNumberTextBox.Text,
                            Notes = $"استلام من أمر الشراء - {item.ItemName}",
                            CreatedBy = AuthenticationService.CurrentUsername,
                            CreatedAt = DateTime.Now
                        };

                        _context.StockMovements.Add(stockMovement);
                    }
                }

                _context.PurchaseReceivings.Add(receiving);
                await _context.SaveChangesAsync();

                LoggingService.LogInfo($"Purchase receiving saved: {receiving.ReceivingNumber} by {AuthenticationService.CurrentUsername}");
                
                MessageBox.Show(
                    $"تم حفظ استلام المشتريات بنجاح\nرقم الاستلام: {receiving.ReceivingNumber}\nعدد البنود: {receiving.Items.Count}",
                    "نجح",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );

                ClearForm();
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Error saving receiving", ex);
                MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region Validation

        private bool ValidateInput()
        {
            if (_purchaseOrderComboBox.SelectedValue == null || (int)_purchaseOrderComboBox.SelectedValue <= 0)
            {
                MessageBox.Show("الرجاء اختيار أمر الشراء", "تحذير", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (_receivingItems.Count == 0)
            {
                MessageBox.Show("لا توجد بنود للاستلام", "تحذير", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }

        #endregion

        #region Helper Methods

        private void ClearButton_Click(object? sender, EventArgs e)
        {
            ClearForm();
        }

        private void ClearForm()
        {
            _currentReceivingId = null;
            _receivingNumberTextBox.Text = GenerateReceivingNumber();
            _receivingDatePicker.Value = DateTime.Today;
            _purchaseOrderComboBox.SelectedIndex = -1;
            _receivingItems.Clear();
            DisplayItems();
            UpdateSummary();
        }

        private string GenerateReceivingNumber()
        {
            return $"RCV-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}";
        }

        #endregion

        #region Dispose

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // تحرير الموارد
            }
            base.Dispose(disposing);
        }

        #endregion
    }
}

