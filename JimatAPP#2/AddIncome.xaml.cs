namespace JimatAPP_2
{
    public partial class AddIncome : ContentPage
    {
        private readonly IncomeItem? _editItem;

        // Default — today
        public AddIncome()
        {
            InitializeComponent();
            IncomeDatePicker.Date = DateTime.Today;
            CategoryPicker.SelectedIndex = 0;
        }

        // Called from ViewIncome "+" for any day (today or past)
        public AddIncome(DateTime presetDate)
        {
            InitializeComponent();
            IncomeDatePicker.Date = presetDate;
            CategoryPicker.SelectedIndex = 0;
        }

        // Edit flow (called from EditIncome)
        public AddIncome(IncomeItem item)
        {
            InitializeComponent();
            _editItem = item;

            IncomeDatePicker.Date = item.Date;
            AmountEntry.Text = item.Amount.ToString("0.00");
            DescriptionEditor.Text = item.Description;

            var categories = new[] { "Paycheck", "Gift", "Others" };
            CategoryPicker.SelectedIndex = Array.IndexOf(categories, item.Category ?? "Paycheck");
            if (CategoryPicker.SelectedIndex < 0)
                CategoryPicker.SelectedIndex = 0;
        }

        private async void OnDoneTapped(object sender, EventArgs e)
        {
            // ── Validation ────────────────────────────────────────────────────
            if (string.IsNullOrWhiteSpace(AmountEntry.Text) ||
                !decimal.TryParse(AmountEntry.Text, out decimal amount) || amount <= 0)
            {
                await DisplayAlertAsync("Invalid Amount",
                    "Please enter a valid amount greater than 0.", "OK");
                return;
            }

            if (CategoryPicker.SelectedItem == null)
            {
                await DisplayAlertAsync("No Category", "Please select a category.", "OK");
                return;
            }

            string category = CategoryPicker.SelectedItem.ToString()!;
            string description = DescriptionEditor.Text?.Trim() ?? string.Empty;
            DateTime date = IncomeDatePicker.Date ?? DateTime.Now;

            if (_editItem != null)
            {
                // ── Update existing record ────────────────────────────────────
                _editItem.Amount = amount;
                _editItem.Category = category;
                _editItem.Date = date;
                _editItem.Description = description;

                // Save to SQLite (Id != 0 triggers UPDATE)
                await DatabaseService.SaveIncomeAsync(_editItem);

                // Refresh in-memory store so the UI reflects the change
                var index = IncomeStore.Items.IndexOf(_editItem);
                if (index >= 0)
                {
                    IncomeStore.Items.RemoveAt(index);
                    IncomeStore.Items.Insert(index, _editItem);
                }
            }
            else
            {
                // ── Insert new record ─────────────────────────────────────────
                var newItem = new IncomeItem
                {
                    Category = category,
                    Amount = amount,
                    Date = date,
                    Description = description
                    // Id left as 0 — SQLite auto-assigns it on INSERT
                };

                // Save to SQLite first so newItem.Id is populated
                await DatabaseService.SaveIncomeAsync(newItem);

                // Then add to in-memory store
                IncomeStore.Items.Add(newItem);
            }

            await Navigation.PopAsync();
        }
    }
}
