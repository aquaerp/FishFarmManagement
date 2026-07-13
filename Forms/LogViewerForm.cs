using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using FishFarmManager.Services;

namespace FishFarmManager.Forms
{
    /// <summary>
    /// عارض سجلات النظام - Log Viewer Form
    /// عرض وتصفح سجلات التطبيق
    /// </summary>
    public partial class LogViewerForm : Form
    {
        private ComboBox _logFileComboBox = null!;
        private TextBox _logContentTextBox = null!;
        private Button _refreshButton = null!;
        private Button _openFolderButton = null!;
        private Button _clearButton = null!;
        private Label _fileInfoLabel = null!;
        private CheckBox _autoScrollCheckBox = null!;
        private ComboBox _filterComboBox = null!;

        private string _currentLogFile = string.Empty;
        private FileSystemWatcher? _fileWatcher;

        public LogViewerForm()
        {
            InitializeComponent();
            SetupForm();
            LoadLogFiles();
            SetupFileWatcher();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // Form settings
            this.ClientSize = new Size(1000, 700);
            this.Text = "عارض سجلات النظام - AquaFarm Pro";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;

            this.ResumeLayout(false);
        }

        private void SetupForm()
        {
            // Top Panel
            var topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = Color.FromArgb(240, 248, 255)
            };
            this.Controls.Add(topPanel);

            // Log File Label
            var fileLabel = new Label
            {
                Text = "ملف السجل:",
                Location = new Point(920, 15),
                Size = new Size(70, 25),
                Font = new Font("Cairo", 10),
                TextAlign = ContentAlignment.MiddleRight
            };
            topPanel.Controls.Add(fileLabel);

            // Log File ComboBox
            _logFileComboBox = new ComboBox
            {
                Location = new Point(620, 15),
                Size = new Size(290, 30),
                Font = new Font("Cairo", 9),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _logFileComboBox.SelectedIndexChanged += LogFileComboBox_SelectedIndexChanged;
            topPanel.Controls.Add(_logFileComboBox);

            // Filter Label
            var filterLabel = new Label
            {
                Text = "تصفية:",
                Location = new Point(570, 15),
                Size = new Size(40, 25),
                Font = new Font("Cairo", 10),
                TextAlign = ContentAlignment.MiddleRight
            };
            topPanel.Controls.Add(filterLabel);

            // Filter ComboBox
            _filterComboBox = new ComboBox
            {
                Location = new Point(400, 15),
                Size = new Size(160, 30),
                Font = new Font("Cairo", 9),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _filterComboBox.Items.AddRange(new object[] 
            { 
                "الكل", 
                "INF معلومات", 
                "WRN تحذيرات", 
                "ERR أخطاء", 
                "FTL فادح" 
            });
            _filterComboBox.SelectedIndex = 0;
            _filterComboBox.SelectedIndexChanged += FilterComboBox_SelectedIndexChanged;
            topPanel.Controls.Add(_filterComboBox);

            // Refresh Button
            _refreshButton = new Button
            {
                Text = "🔄 تحديث",
                Location = new Point(290, 15),
                Size = new Size(100, 30),
                Font = new Font("Cairo", 9),
                Cursor = Cursors.Hand
            };
            _refreshButton.Click += RefreshButton_Click;
            topPanel.Controls.Add(_refreshButton);

            // Open Folder Button
            _openFolderButton = new Button
            {
                Text = "📁 فتح المجلد",
                Location = new Point(175, 15),
                Size = new Size(110, 30),
                Font = new Font("Cairo", 9),
                Cursor = Cursors.Hand
            };
            _openFolderButton.Click += OpenFolderButton_Click;
            topPanel.Controls.Add(_openFolderButton);

            // Clear Button
            _clearButton = new Button
            {
                Text = "🗑️ مسح",
                Location = new Point(70, 15),
                Size = new Size(100, 30),
                Font = new Font("Cairo", 9),
                Cursor = Cursors.Hand,
                BackColor = Color.FromArgb(220, 53, 69),
                ForeColor = Color.White
            };
            _clearButton.Click += ClearButton_Click;
            topPanel.Controls.Add(_clearButton);

            // Auto Scroll CheckBox
            _autoScrollCheckBox = new CheckBox
            {
                Text = "تمرير تلقائي",
                Location = new Point(890, 50),
                Size = new Size(100, 25),
                Font = new Font("Cairo", 9),
                Checked = true
            };
            topPanel.Controls.Add(_autoScrollCheckBox);

            // File Info Label
            _fileInfoLabel = new Label
            {
                Location = new Point(10, 50),
                Size = new Size(870, 25),
                Font = new Font("Cairo", 8),
                ForeColor = Color.Gray,
                TextAlign = ContentAlignment.MiddleLeft
            };
            topPanel.Controls.Add(_fileInfoLabel);

            // Log Content TextBox
            _logContentTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                Font = new Font("Consolas", 9),
                ReadOnly = true,
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.FromArgb(220, 220, 220),
                WordWrap = false
            };
            this.Controls.Add(_logContentTextBox);

            // Style buttons
            StyleButton(_refreshButton, Color.FromArgb(0, 123, 255));
            StyleButton(_openFolderButton, Color.FromArgb(40, 167, 69));
        }

        private void StyleButton(Button button, Color color)
        {
            button.BackColor = color;
            button.ForeColor = Color.White;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
        }

        private void LoadLogFiles()
        {
            try
            {
                string logsPath = LoggingService.GetLogsPath();
                
                if (!Directory.Exists(logsPath))
                {
                    _fileInfoLabel.Text = "⚠️ مجلد السجلات غير موجود";
                    return;
                }

                var logFiles = Directory.GetFiles(logsPath, "*.log")
                    .OrderByDescending(f => File.GetLastWriteTime(f))
                    .Select(f => Path.GetFileName(f))
                    .ToArray();

                _logFileComboBox.Items.Clear();
                _logFileComboBox.Items.AddRange(logFiles);

                if (logFiles.Length > 0)
                {
                    _logFileComboBox.SelectedIndex = 0;
                }
                else
                {
                    _fileInfoLabel.Text = "⚠️ لا توجد ملفات سجلات";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل ملفات السجلات: {ex.Message}", "خطأ", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadLogContent()
        {
            if (string.IsNullOrEmpty(_currentLogFile))
                return;

            try
            {
                string logsPath = LoggingService.GetLogsPath();
                string fullPath = Path.Combine(logsPath, _currentLogFile);

                if (!File.Exists(fullPath))
                {
                    _logContentTextBox.Text = "⚠️ الملف غير موجود";
                    return;
                }

                // Read file with retry (in case it's locked by Serilog)
                string content = "";
                int retries = 3;
                while (retries > 0)
                {
                    try
                    {
                        using (var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        using (var reader = new StreamReader(stream))
                        {
                            content = reader.ReadToEnd();
                        }
                        break;
                    }
                    catch (IOException)
                    {
                        retries--;
                        System.Threading.Thread.Sleep(100);
                    }
                }

                // Apply filter
                string filterText = _filterComboBox.SelectedItem?.ToString() ?? "الكل";
                if (filterText != "الكل")
                {
                    string levelFilter = filterText.Split(' ')[0]; // Extract level code (INF, WRN, ERR, FTL)
                    var lines = content.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                    var filteredLines = lines.Where(line => line.Contains($"[{levelFilter}]"));
                    content = string.Join(Environment.NewLine, filteredLines);
                }

                _logContentTextBox.Text = content;

                // Auto scroll to bottom
                if (_autoScrollCheckBox.Checked)
                {
                    _logContentTextBox.SelectionStart = _logContentTextBox.Text.Length;
                    _logContentTextBox.ScrollToCaret();
                }

                // Update file info
                var fileInfo = new FileInfo(fullPath);
                _fileInfoLabel.Text = $"📄 {_currentLogFile} | الحجم: {FormatFileSize(fileInfo.Length)} | آخر تحديث: {fileInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss}";
            }
            catch (Exception ex)
            {
                _logContentTextBox.Text = $"❌ خطأ في قراءة الملف: {ex.Message}";
            }
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        private void SetupFileWatcher()
        {
            try
            {
                string logsPath = LoggingService.GetLogsPath();
                
                if (!Directory.Exists(logsPath))
                    return;

                _fileWatcher = new FileSystemWatcher(logsPath)
                {
                    Filter = "*.log",
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
                };

                _fileWatcher.Changed += (s, e) =>
                {
                    // Update on UI thread
                    if (this.InvokeRequired)
                    {
                        this.BeginInvoke(new Action(() =>
                        {
                            if (Path.GetFileName(e.FullPath) == _currentLogFile)
                            {
                                LoadLogContent();
                            }
                        }));
                    }
                };

                _fileWatcher.EnableRaisingEvents = true;
            }
            catch
            {
                // File watcher is optional
            }
        }

        private void LogFileComboBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            _currentLogFile = _logFileComboBox.SelectedItem?.ToString() ?? "";
            LoadLogContent();
        }

        private void FilterComboBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            LoadLogContent();
        }

        private void RefreshButton_Click(object? sender, EventArgs e)
        {
            LoadLogFiles();
            LoadLogContent();
        }

        private void OpenFolderButton_Click(object? sender, EventArgs e)
        {
            try
            {
                string logsPath = LoggingService.GetLogsPath();
                if (Directory.Exists(logsPath))
                {
                    System.Diagnostics.Process.Start("explorer.exe", logsPath);
                }
                else
                {
                    MessageBox.Show("مجلد السجلات غير موجود", "تنبيه", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في فتح المجلد: {ex.Message}", "خطأ", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ClearButton_Click(object? sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "هل تريد مسح محتوى هذا الملف؟\n\nلن يتم حذف الملف، فقط مسح محتواه.",
                "تأكيد المسح",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (result == DialogResult.Yes)
            {
                try
                {
                    string logsPath = LoggingService.GetLogsPath();
                    string fullPath = Path.Combine(logsPath, _currentLogFile);

                    if (File.Exists(fullPath))
                    {
                        File.WriteAllText(fullPath, "");
                        LoadLogContent();
                        MessageBox.Show("تم مسح محتوى الملف بنجاح", "نجح", 
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطأ في مسح الملف: {ex.Message}", "خطأ", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            
            if (_fileWatcher != null)
            {
                _fileWatcher.EnableRaisingEvents = false;
                _fileWatcher.Dispose();
            }
        }
    }
}
