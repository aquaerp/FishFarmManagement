using FishFarmManager.Services;

namespace FishFarmManager.Forms;

public sealed class AccountingJournalDraftDialog : AquaFarmBaseForm
{
    private readonly DateTimePicker _entryDate = new() { Format = DateTimePickerFormat.Short, Width = 150 };
    private readonly ComboBox _period = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 280 };
    private readonly TextBox _description = new() { Dock = DockStyle.Fill, MaxLength = 500 };
    private readonly TextBox _source = new() { Dock = DockStyle.Fill, MaxLength = 100 };
    private readonly TextBox _reference = new() { Dock = DockStyle.Fill, MaxLength = 100 };
    private readonly DataGridView _grid = new();

    public JournalDraftRequest? Request { get; private set; }

    public AccountingJournalDraftDialog(
        IReadOnlyList<(int Id, string Text)> periods,
        IReadOnlyList<(int Id, string Text)> accounts)
    {
        LocalizationManager.Bind(this, "AddJournalEntry");
        Size = new Size(1000, 680);
        MinimumSize = new Size(820, 560);
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Dpi;

        _period.DataSource = periods.Select(x => new Option(x.Id, x.Text)).ToList();
        _period.DisplayMember = nameof(Option.Text);
        _period.ValueMember = nameof(Option.Id);
        ConfigureGrid(accounts);

        var fields = new TableLayoutPanel { Dock = DockStyle.Top, Height = 180, ColumnCount = 2, Padding = new Padding(14) };
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        AddField(fields, 0, "EntryDate", _entryDate);
        AddField(fields, 1, "FiscalPeriod", _period);
        AddField(fields, 2, "Description", _description);
        AddField(fields, 3, "Source", _source);
        AddField(fields, 4, "Reference", _reference);

        var save = LocalizationManager.Bind(ThemeManager.CreateSuccessButton(string.Empty), "SaveJournalDraft");
        save.Click += (_, _) => AcceptDraft();
        var cancel = LocalizationManager.Bind(ThemeManager.CreateSecondaryButton(string.Empty), "Cancel");
        cancel.DialogResult = DialogResult.Cancel;
        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom, Height = 68, Padding = new Padding(10),
            FlowDirection = FlowDirection.RightToLeft
        };
        actions.Controls.Add(save);
        actions.Controls.Add(cancel);

        Controls.Add(_grid);
        Controls.Add(fields);
        Controls.Add(actions);
        AcceptButton = save;
        CancelButton = cancel;
        LocalizationManager.ApplyResources(this);
        ThemeManager.ApplyCultureDirection(this);
    }

    private void ConfigureGrid(IReadOnlyList<(int Id, string Text)> accounts)
    {
        _grid.Dock = DockStyle.Fill;
        _grid.AutoGenerateColumns = false;
        _grid.AllowUserToAddRows = true;
        _grid.AllowUserToDeleteRows = true;
        _grid.RowHeadersVisible = false;
        LocalizationManager.Bind(_grid, "JournalLines");
        var accountColumn = new DataGridViewComboBoxColumn
        {
            Name = "AccountId", Width = 380,
            DataSource = accounts.Select(x => new Option(x.Id, x.Text)).ToList(),
            DisplayMember = nameof(Option.Text), ValueMember = nameof(Option.Id), FlatStyle = FlatStyle.Flat
        };
        var debitColumn = new DataGridViewTextBoxColumn { Name = "Debit", Width = 140 };
        var creditColumn = new DataGridViewTextBoxColumn { Name = "Credit", Width = 140 };
        var descriptionColumn = new DataGridViewTextBoxColumn
        {
            Name = "Description", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        };
        LocalizationManager.Bind(accountColumn, "Account");
        LocalizationManager.Bind(debitColumn, "DebitSar");
        LocalizationManager.Bind(creditColumn, "CreditSar");
        LocalizationManager.Bind(descriptionColumn, "LineDescription");
        _grid.Columns.AddRange(accountColumn, debitColumn, creditColumn, descriptionColumn);
    }

    private static void AddField(TableLayoutPanel layout, int row, string resourceKey, Control control)
    {
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var label = LocalizationManager.Bind(
            new Label { AutoSize = true, Padding = new Padding(5, 8, 5, 0) }, resourceKey);
        layout.Controls.Add(label, 0, row);
        control.AccessibleName = LocalizationManager.Get(resourceKey);
        layout.Controls.Add(control, 1, row);
    }

    private void AcceptDraft()
    {
        try
        {
            if (_period.SelectedItem is not Option period) throw new InvalidOperationException(LocalizationManager.Get("SelectOpenPeriod"));
            if (string.IsNullOrWhiteSpace(_description.Text)) throw new InvalidOperationException(LocalizationManager.Get("EnterJournalDescription"));
            if (string.IsNullOrWhiteSpace(_source.Text)) throw new InvalidOperationException(LocalizationManager.Get("EnterJournalSource"));
            _grid.EndEdit();
            var lines = new List<JournalLineRequest>();
            foreach (DataGridViewRow row in _grid.Rows)
            {
                if (row.IsNewRow) continue;
                if (row.Cells["AccountId"].Value == null) throw new InvalidOperationException(LocalizationManager.Get("SelectAccountEachLine"));
                lines.Add(new JournalLineRequest(
                    Convert.ToInt32(row.Cells["AccountId"].Value),
                    Amount(row.Cells["Debit"].Value), Amount(row.Cells["Credit"].Value),
                    Description: Convert.ToString(row.Cells["Description"].Value)?.Trim()));
            }
            JournalBalanceValidator.Validate(lines);
            Request = new JournalDraftRequest(_entryDate.Value.Date, period.Id, _description.Text.Trim(),
                _source.Text.Trim(), string.IsNullOrWhiteSpace(_reference.Text) ? null : _reference.Text.Trim(), lines);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            LoggingService.LogWarning("Journal draft validation failed: {Message}", ex.Message);
            var message = BusinessRuleError.TryGetCode(ex, out _)
                ? LocalizationManager.GetUserMessage(ex)
                : ex.Message;
            MessageBox.Show(message, LocalizationManager.Get("InvalidJournalEntry"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private static decimal Amount(object? value)
    {
        if (value == null || string.IsNullOrWhiteSpace(Convert.ToString(value))) return 0m;
        return CultureFormatter.TryParseDecimal(Convert.ToString(value), out var amount)
            ? decimal.Round(amount, 2, MidpointRounding.AwayFromZero)
            : throw new InvalidOperationException(LocalizationManager.Get("EnterValidAmounts"));
    }

    private sealed record Option(int Id, string Text);
}
