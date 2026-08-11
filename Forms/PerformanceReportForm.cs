using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.EntityFrameworkCore;
using FishFarmManager.Data;
using FishFarmManager.Services;

namespace FishFarmManager.Forms
{
    public class PerformanceReportForm : Form
    {
        private readonly FishFarmContext _context;
        private DataGridView _performanceGrid = null!;
        private TextBox _statsTextBox = null!;

        public PerformanceReportForm(FishFarmContext context)
        {
            _context = context;
            InitializeComponent();
            LoadPerformanceData();
            try { ThemeManager.ApplyTheme(this); } catch { }
        }

        private void InitializeComponent()
        {
            this.Text = "تقرير أداء الدورات";
            this.Size = new Size(1100, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            try { this.Font = new Font("Cairo", 10F, FontStyle.Regular); } catch { }

            var mainPanel = new TableLayoutPanel();
            mainPanel.Dock = DockStyle.Fill;
            mainPanel.RowCount = 2;
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 70F));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 30F));

            // جدول الأداء
            _performanceGrid = new DataGridView();
            _performanceGrid.Dock = DockStyle.Fill;
            _performanceGrid.AutoGenerateColumns = false;
            _performanceGrid.ReadOnly = true;
            _performanceGrid.AllowUserToAddRows = false;

            _performanceGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Name", HeaderText = "اسم الدورة", Width = 150 });
            _performanceGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "PondName", HeaderText = "الحوض", Width = 120 });
            _performanceGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Duration", HeaderText = "المدة (يوم)", Width = 100 });
            _performanceGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "SurvivalRate", HeaderText = "معدل البقاء %", Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Format = "F1" } });
            _performanceGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "FCR", HeaderText = "FCR", Width = 80, DefaultCellStyle = new DataGridViewCellStyle { Format = "F2" } });
            _performanceGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ADG", HeaderText = "ADG", Width = 80, DefaultCellStyle = new DataGridViewCellStyle { Format = "F2" } });
            _performanceGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TotalHarvestWeight", HeaderText = "وزن الحصاد", Width = 120, DefaultCellStyle = new DataGridViewCellStyle { Format = "F1" } });

            mainPanel.Controls.Add(_performanceGrid, 0, 0);

            // إحصائيات الأداء
            _statsTextBox = new TextBox();
            _statsTextBox.Dock = DockStyle.Fill;
            _statsTextBox.Multiline = true;
            _statsTextBox.ReadOnly = true;
            _statsTextBox.Font = new Font("Arial", 11);
            mainPanel.Controls.Add(_statsTextBox, 0, 1);

            this.Controls.Add(mainPanel);
        }

        private void LoadPerformanceData()
        {
            var completedCycles = _context.ProductionCycles
                .Include(c => c.ProductionCyclePonds)
                .ThenInclude(pcp => pcp.Pond)
                .Where(c => c.Status == Models.CycleStatus.Completed && c.EndDate.HasValue)
                .Select(c => new
                {
                    c.Name,
                    PondName = string.Join(", ", c.ProductionCyclePonds.Select(pcp => pcp.Pond.Name)),
                    Duration = c.EndDate.HasValue ? (c.EndDate.Value - c.StartDate).Days : 0,
                    c.SurvivalRate,
                    c.FCR,
                    c.ADG,
                    c.TotalHarvestWeight,
                    c.TotalCost
                })
                .ToList();
            _performanceGrid.DataSource = completedCycles;

            // إحصائيات الأداء
            var cyclesWithSurvival = completedCycles.Where(c => c.SurvivalRate.HasValue).ToList();
            var cyclesWithFCR = completedCycles.Where(c => c.FCR.HasValue).ToList();
            var cyclesWithADG = completedCycles.Where(c => c.ADG.HasValue).ToList();
            var avgSurvival = cyclesWithSurvival.Any() ? cyclesWithSurvival.Average(c => c.SurvivalRate.GetValueOrDefault()) : 0;
            var avgFCR = cyclesWithFCR.Any() ? cyclesWithFCR.Average(c => c.FCR.GetValueOrDefault()) : 0;
            var avgADG = cyclesWithADG.Any() ? cyclesWithADG.Average(c => c.ADG.GetValueOrDefault()) : 0;
            var totalProduction = completedCycles.Sum(c => c.TotalHarvestWeight ?? 0);
            var totalCost = completedCycles.Sum(c => c.TotalCost ?? 0m);
            var totalRevenue = _context.SalesOrders
                .Where(order => order.Status != Models.SalesOrderStatus.Cancelled)
                .Sum(order => order.GrandTotal);
            var profit = totalRevenue - totalCost;
            var profitMargin = totalRevenue > 0m ? profit / totalRevenue * 100m : 0m;

            _statsTextBox.Text = $"إحصائيات الأداء:\n\n" +
                $"عدد الدورات المكتملة: {completedCycles.Count}\n" +
                $"متوسط معدل البقاء: {avgSurvival:F1}%\n" +
                $"متوسط معدل التحويل الغذائي: {avgFCR:F2}\n" +
                $"متوسط النمو اليومي: {avgADG:F2} جم/يوم\n" +
                $"إجمالي الإنتاج: {totalProduction:F1} كجم\n" +
                $"إجمالي تكاليف الإنتاج: {totalCost:N2}\n" +
                $"إجمالي المبيعات المسجلة: {totalRevenue:N2}\n" +
                $"هامش الربحية: {profitMargin:F2}%";
        }
    }
}
