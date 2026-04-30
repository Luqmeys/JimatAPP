namespace JimatAPP_2
{
    public partial class EditExpenses : ContentPage
    {
        private readonly ExpenseItem _editItem;

        public EditExpenses(ExpenseItem item)
        {
            InitializeComponent();
            _editItem = item;

            ExpenseDatePicker.Date = item.Date is DateTime dt ? dt : DateTime.Now;
            AmountEntry.Text = item.Amount.ToString("0.00");
            DescriptionEditor.Text = item.Description;

            var categories = new[] { "Food", "Transport", "Shopping", "Bills", "Health", "Entertainment", "Others" };
            CategoryPicker.SelectedIndex = Array.IndexOf(categories, item.Category ?? "Others");
            if (CategoryPicker.SelectedIndex < 0)
                CategoryPicker.SelectedIndex = 0;
        }

        private async void OnSaveTapped(object sender, EventArgs e)
        {
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

            _editItem.Amount = amount;
            _editItem.Category = CategoryPicker.SelectedItem.ToString()!;
            // Ensure we assign a non-null DateTime to ExpenseItem.Date
            _editItem.Date = ExpenseDatePicker.Date ?? DateTime.Now;
            _editItem.Description = DescriptionEditor.Text?.Trim() ?? string.Empty;

            await DatabaseService.SaveExpenseAsync(_editItem);

            var index = ExpenseStore.Items.IndexOf(_editItem);
            if (index >= 0)
            {
                ExpenseStore.Items.RemoveAt(index);
                ExpenseStore.Items.Insert(index, _editItem);
            }

            await Navigation.PopAsync();
        }

        private async void OnDeleteTapped(object sender, EventArgs e)
        {
            bool confirmed = await DisplayAlertAsync(
                "Delete Expense",
                $"Are you sure you want to delete \"{_editItem.Category}\"?",
                "Delete", "Cancel");

            if (confirmed)
            {
                await DatabaseService.DeleteExpenseAsync(_editItem);
                ExpenseStore.Items.Remove(_editItem);
                await Navigation.PopAsync();
            }
        }
    }
}
