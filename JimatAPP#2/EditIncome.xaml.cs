namespace JimatAPP_2
{
    public partial class EditIncome : ContentPage
    {
        private readonly IncomeItem _editItem;

        public EditIncome(IncomeItem item)
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

        private async void OnSaveTapped(object sender, EventArgs e)
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

            // ── Apply changes to the item ─────────────────────────────────────
            _editItem.Amount = amount;
            _editItem.Category = CategoryPicker.SelectedItem.ToString()!;
            _editItem.Date = IncomeDatePicker.Date ?? DateTime.Now;
            _editItem.Description = DescriptionEditor.Text?.Trim() ?? string.Empty;

            // Save to SQLite (Id != 0 triggers UPDATE)
            await DatabaseService.SaveIncomeAsync(_editItem);

            // Refresh the in-memory store so ViewIncome reflects the change
            var index = IncomeStore.Items.IndexOf(_editItem);
            if (index >= 0)
            {
                IncomeStore.Items.RemoveAt(index);
                IncomeStore.Items.Insert(index, _editItem);
            }

            await Navigation.PopAsync();
        }

        private async void OnDeleteTapped(object sender, EventArgs e)
        {
            bool confirmed = await DisplayAlertAsync(
                "Delete Income",
                $"Are you sure you want to delete \"{_editItem.Category}\"?",
                "Delete", "Cancel");

            if (confirmed)
            {
                // Delete from SQLite first
                await DatabaseService.DeleteIncomeAsync(_editItem);

                // Then remove from in-memory store
                IncomeStore.Items.Remove(_editItem);

                await Navigation.PopAsync();
            }
        }
    }
}
