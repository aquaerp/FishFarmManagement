using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace FishFarmManager.Controls
{
    public class ChartControl : Panel
    {
        private List<ChartSeries> _series = new List<ChartSeries>();
        private ChartType _chartType = ChartType.Bar;
        private string[]? _customLabels = null;
        private Color[] _defaultColors = new Color[]
        {
            Color.Blue, Color.Green, Color.Red, Color.Orange, Color.Purple,
            Color.Brown, Color.Pink, Color.Cyan, Color.Magenta, Color.Yellow
        };

        public enum ChartType
        {
            Bar,
            Line,
            Pie
        }

        public ChartType ChartTypeProperty
        {
            get { return _chartType; }
            set
            {
                _chartType = value;
                Invalidate();
            }
        }

        public void AddSeries(string name, double[] values, Color? color = null)
        {
            var seriesColor = color ?? _defaultColors[_series.Count % _defaultColors.Length];
            _series.Add(new ChartSeries { Name = name, Values = values, Color = seriesColor });
            Invalidate();
        }

        public void ClearSeries()
        {
            _series.Clear();
            _customLabels = null;
            Invalidate();
        }

        public void SetCustomLabels(string[] labels)
        {
            _customLabels = labels;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (_series.Count == 0 || _series[0].Values.Length == 0)
                return;

            switch (_chartType)
            {
                case ChartType.Bar:
                    DrawBarChart(e.Graphics);
                    break;
                case ChartType.Line:
                    DrawLineChart(e.Graphics);
                    break;
                case ChartType.Pie:
                    DrawPieChart(e.Graphics);
                    break;
            }
        }

        private void DrawBarChart(Graphics graphics)
        {
            var padding = 40;
            var chartWidth = Width - padding * 2;
            var chartHeight = Height - padding * 2;
            var barWidth = chartWidth / (_series[0].Values.Length * (_series.Count + 1));
            var maxValue = GetMaxValue();

            // رسم المحاور
            graphics.DrawLine(Pens.Black, padding, padding, padding, Height - padding);
            graphics.DrawLine(Pens.Black, padding, Height - padding, Width - padding, Height - padding);

            // رسم الأعمدة
            for (int i = 0; i < _series[0].Values.Length; i++)
            {
                for (int j = 0; j < _series.Count; j++)
                {
                    var value = _series[j].Values[i];
                    var barHeight = (float)(value / maxValue * chartHeight);
                    var x = padding + (i * (_series.Count + 1) + j + 1) * barWidth;
                    var y = Height - padding - barHeight;

                    graphics.FillRectangle(new SolidBrush(_series[j].Color), x, y, barWidth, barHeight);
                    graphics.DrawRectangle(Pens.Black, x, y, barWidth, barHeight);
                }
            }

            // رسم التسميات
            using (var font = new Font("Arial", 8))
            {
                // تسميات المحور السيني
                for (int i = 0; i < _series[0].Values.Length; i++)
                {
                    var x = padding + (i * (_series.Count + 1) + _series.Count / 2.0f) * barWidth;
                    var label = _customLabels != null && i < _customLabels.Length ? _customLabels[i] : $"اليوم {i + 1}";
                    graphics.DrawString(label, font, Brushes.Black, x, Height - padding + 5);
                }

                // التسميات الأساسية
                for (int j = 0; j < _series.Count; j++)
                {
                    graphics.DrawString(_series[j].Name, font, new SolidBrush(_series[j].Color), 10, padding + j * 20);
                }
            }
        }

        private void DrawLineChart(Graphics graphics)
        {
            var padding = 40;
            var chartWidth = Width - padding * 2;
            var chartHeight = Height - padding * 2;
            var pointSpacing = chartWidth / (_series[0].Values.Length - 1);
            var maxValue = GetMaxValue();

            // رسم المحاور
            graphics.DrawLine(Pens.Black, padding, padding, padding, Height - padding);
            graphics.DrawLine(Pens.Black, padding, Height - padding, Width - padding, Height - padding);

            // رسم الخطوط
            for (int j = 0; j < _series.Count; j++)
            {
                var points = new PointF[_series[j].Values.Length];
                for (int i = 0; i < _series[j].Values.Length; i++)
                {
                    var value = _series[j].Values[i];
                    var x = padding + i * pointSpacing;
                    var y = Height - padding - (float)(value / maxValue * chartHeight);
                    points[i] = new PointF(x, y);
                }

                // رسم الخط
                graphics.DrawLines(new Pen(_series[j].Color, 2), points);

                // رسم النقاط
                foreach (var point in points)
                {
                    graphics.FillEllipse(new SolidBrush(_series[j].Color), point.X - 4, point.Y - 4, 8, 8);
                    graphics.DrawEllipse(Pens.Black, point.X - 4, point.Y - 4, 8, 8);
                }
            }

            // رسم التسميات
            using (var font = new Font("Arial", 8))
            {
                // تسميات المحور السيني
                for (int i = 0; i < _series[0].Values.Length; i++)
                {
                    var x = padding + i * pointSpacing;
                    var label = _customLabels != null && i < _customLabels.Length ? _customLabels[i] : $"اليوم {i + 1}";
                    graphics.DrawString(label, font, Brushes.Black, x, Height - padding + 5);
                }

                // التسميات الأساسية
                for (int j = 0; j < _series.Count; j++)
                {
                    graphics.DrawString(_series[j].Name, font, new SolidBrush(_series[j].Color), 10, padding + j * 20);
                }
            }
        }

        private void DrawPieChart(Graphics graphics)
        {
            var padding = 40;
            var diameter = Math.Min(Width, Height) - padding * 2;
            var centerX = Width / 2;
            var centerY = Height / 2;
            var total = GetTotalValue();

            // حساب الزوايا
            var angles = new float[_series.Count];
            for (int i = 0; i < _series.Count; i++)
            {
                angles[i] = (float)(_series[i].Values.Sum() / total * 360);
            }

            // رسم الشرائح
            float startAngle = 0;
            for (int i = 0; i < _series.Count; i++)
            {
                graphics.FillPie(new SolidBrush(_series[i].Color), centerX - diameter / 2, centerY - diameter / 2, diameter, diameter, startAngle, angles[i]);
                graphics.DrawPie(Pens.Black, centerX - diameter / 2, centerY - diameter / 2, diameter, diameter, startAngle, angles[i]);
                startAngle += angles[i];
            }

            // رسم التسميات
            using (var font = new Font("Arial", 8))
            {
                for (int i = 0; i < _series.Count; i++)
                {
                    var percentage = (float)(_series[i].Values.Sum() / total * 100);
                    graphics.DrawString($"{_series[i].Name}: {percentage:F1}%", font, new SolidBrush(_series[i].Color), 10, padding + i * 20);
                }
            }
        }

        private double GetMaxValue()
        {
            double max = 0;
            foreach (var series in _series)
            {
                foreach (var value in series.Values)
                {
                    if (value > max) max = value;
                }
            }
            return max;
        }

        private double GetTotalValue()
        {
            double total = 0;
            foreach (var series in _series)
            {
                total += series.Values.Sum();
            }
            return total;
        }

        private class ChartSeries
        {
            public string Name { get; set; } = null!;
            public double[] Values { get; set; } = null!;
            public Color Color { get; set; }
        }
    }
}
