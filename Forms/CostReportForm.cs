using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.EntityFrameworkCore;
using FishFarmManager.Data;

namespace FishFarmManager.Forms
{
    public class CostReportForm : Form
    {
        private readonly FishFarmContext _context;
        private DataGridView _costGrid = null!;
        private TextBox _summaryTextBox = null!;

        public CostReportForm(FishFarmContext context)
        {
            _context = context;
            InitializeComponent();
            LoadCostData();
        }

        private void InitializeComponent()
        {
            this.Text = "تقرير التكاليف";
            this.Size = new Size(1000, 650);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            try { this.Font = new Font("Cairo", 10F, FontStyle.Regular); } catch { }

            var mainPanel = new TableLayoutPanel();
            mainPanel.Dock = DockStyle.Fill;
            mainPanel.RowCount = 2;
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 70F));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 30F));

            // جدول التكاليف
            _costGrid = new DataGridView();
            _costGrid.Dock = DockStyle.Fill;
            _costGrid.AutoGenerateColumns = false;
            _costGrid.ReadOnly = true;
            _costGrid.AllowUserToAddRows = false;

            _costGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "CycleName", HeaderText = "الدورة", Width = 150 });
            _costGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "FeedCost", HeaderText = "تكلفة الأعلاف", Width = 120, DefaultCellStyle = new DataGridViewCellStyle { Format = "F0" } });
            _costGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Revenue", HeaderText = "الإيرادات", Width = 120, DefaultCellStyle = new DataGridViewCellStyle { Format = "F0" } });
            _costGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Profit", HeaderText = "الربح", Width = 120, DefaultCellStyle = new DataGridViewCellStyle { Format = "F0" } });
            _costGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ProfitMargin", HeaderText = "هامش الربح %", Width = 120, DefaultCellStyle = new DataGridViewCellStyle { Format = "F1" } });

            mainPanel.Controls.Add(_costGrid, 0, 0);

            // ملخص التكاليف
            _summaryTextBox = new TextBox();
            _summaryTextBox.Dock = DockStyle.Fill;
            _summaryTextBox.Multiline = true;
            _summaryTextBox.ReadOnly = true;
            _summaryTextBox.Font = new Font("Arial", 11);
            mainPanel.Controls.Add(_summaryTextBox, 0, 1);

            this.Controls.Add(mainPanel);
        }

        private void LoadCostData()
        {
            var completedCycles = _context.ProductionCycles
                .Where(c => c.Status == Models.CycleStatus.Completed)
                .ToList();

            var financialData = completedCycles.Select(c =>
            {
                var feedingRecords = _context.FeedingRecords
                    .Where(f => f.CycleId == c.Id)
                    .ToList();
                var feedCost = feedingRecords.Sum(f => f.Quantity * f.FeedPrice);
                var revenue = (double)(c.TotalHarvestWeight ?? 0m) * 15; // سعر افتراضي 15 ريال للكيلو
                var profit = revenue - feedCost;
                var profitMargin = revenue > 0 ? (profit / revenue) * 100 : 0;

                return new
                {
                    CycleName = c.Name,
                    FeedCost = feedCost,
                    Revenue = revenue,
                    Profit = profit,
                    ProfitMargin = profitMargin
                };
            }).ToList();

            _costGrid.DataSource = financialData;

            // ملخص التكاليف
            var feedingRecords = _context.FeedingRecords.ToList();
            if (!feedingRecords.Any())
            {
                _summaryTextBox.Text = "لا توجد بيانات تغذية متاحة";
                return;
            }
            var totalFeedCost = feedingRecords.Sum(f => f.Quantity * f.FeedPrice);
            var monthlyFeedCost = feedingRecords.Where(f => f.FeedingDate >= DateTime.Now.AddDays(-30)).Sum(f => f.Quantity * f.FeedPrice);
            var avgCostPerKg = feedingRecords.Any() ? feedingRecords.Average(f => f.FeedPrice) : 0;
            var totalQuantity = feedingRecords.Sum(f => f.Quantity);

            _summaryTextBox.Text = $"ملخص التكاليف:\n\n" +
                $"إجمالي تكلفة الأعلاف: {totalFeedCost:F0} ريال\n" +
                $"تكلفة الأعلاف الشهرية: {monthlyFeedCost:F0} ريال\n" +
                $"متوسط سعر العلف: {avgCostPerKg:F2} ريال/كجم\n" +
                $"إجمالي كمية العلف: {totalQuantity:F1} كجم";
        }
    }
}
