using FishFarmManager.Models;
using FishFarmManager.Services;

namespace FishFarmManager.Forms;

public sealed class AccountingJournalLinesDialog : Form
{
    private readonly DataGridView _grid = new();
    public IReadOnlyList<JournalLineRequest> Lines { get; private set; } = Array.Empty<JournalLineRequest>();

    public AccountingJournalLinesDialog(
        IReadOnlyList<(int Id, string Text)> accounts,
        string title,
        bool balanceSheetOnly)
    {
        Text = title;
        Width = 900;
        Height = 520;
        StartPosition = FormStartPosition.CenterParent;
        RightToLeft = RightToLeft.Yes;
        RightToLeftLayout = true;
        MinimizeBox = false;
        MaximizeBox = false;

        var accountColumn = new DataGridViewComboBoxColumn
        {
            Name = "AccountId",
            HeaderText = "الحساب",
            DataSource = accounts.Select(value => new AccountOption(value.Id, value.Text)).ToList(),
            DisplayMember = nameof(AccountOption.Text),
            ValueMember = nameof(AccountOption.Id),
            Width = 360,
            FlatStyle = FlatStyle.Flat
        };
        _grid.Dock = DockStyle.Fill;
        _grid.AutoGenerateColumns = false;
        _grid.AllowUserToAddRows = true;
        _grid.AllowUserToDeleteRows = true;
        _grid.RowHeadersVisible = false;
        _grid.Columns.Add(accountColumn);
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Debit", HeaderText = "مدين (SAR)", Width = 130 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Credit", HeaderText = "دائن (SAR)", Width = 130 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Description", HeaderText = "البيان", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });

        var ok = new Button { Text = "اعتماد الأسطر", DialogResult = DialogResult.None, Width = 150, Height = 34 };
        var cancel = new Button { Text = "إلغاء", DialogResult = DialogResult.Cancel, Width = 110, Height = 34 };
        ThemeManager.StylePrimaryButton(ok);
        ThemeManager.StyleSecondaryButton(cancel);
        ok.Click += (_, _) => AcceptLines(balanceSheetOnly);
        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom, Height = 55, Padding = new Padding(10),
            FlowDirection = FlowDirection.RightToLeft
        };
        actions.Controls.Add(ok);
        actions.Controls.Add(cancel);

        Controls.Add(_grid);
        Controls.Add(actions);
        AcceptButton = ok;
        CancelButton = cancel;
    }

    private void AcceptLines(bool balanceSheetOnly)
    {
        try
        {
            _grid.EndEdit();
            var lines = new List<JournalLineRequest>();
            foreach (DataGridViewRow row in _grid.Rows)
            {
                if (row.IsNewRow) continue;
                if (row.Cells["AccountId"].Value == null)
                    throw new InvalidOperationException("حدد الحساب لكل سطر.");
                var debit = Amount(row.Cells["Debit"].Value);
                var credit = Amount(row.Cells["Credit"].Value);
                if (debit < 0m || credit < 0m || (debit == 0m && credit == 0m) || (debit > 0m && credit > 0m))
                    throw new InvalidOperationException("كل سطر يجب أن يحتوي مبلغًا موجبًا في طرف واحد فقط.");
                lines.Add(new JournalLineRequest(
                    Convert.ToInt32(row.Cells["AccountId"].Value), debit, credit,
                    Description: Convert.ToString(row.Cells["Description"].Value)?.Trim()));
            }
            if (lines.Count < 2)
                throw new InvalidOperationException("أدخل سطرين محاسبيين على الأقل.");
            if (lines.Sum(value => value.Debit) != lines.Sum(value => value.Credit))
                throw new InvalidOperationException("إجمالي المدين يجب أن يساوي إجمالي الدائن.");

            Lines = lines;
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, balanceSheetOnly ? "أرصدة افتتاحية غير صالحة" : "قيد تسوية غير صالح",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private static decimal Amount(object? value)
    {
        if (value == null || string.IsNullOrWhiteSpace(Convert.ToString(value))) return 0m;
        return decimal.TryParse(Convert.ToString(value), out var amount)
            ? decimal.Round(amount, 2, MidpointRounding.AwayFromZero)
            : throw new InvalidOperationException("أدخل مبالغ رقمية صحيحة.");
    }

    private sealed record AccountOption(int Id, string Text);
}
