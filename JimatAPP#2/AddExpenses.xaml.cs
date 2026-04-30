using JimatAPP_2;

namespace JimatAPP_2
{
    public partial class AddExpenses : ContentPage
    {
        private readonly ExpenseItem? _editItem;

        // Default — today
        public AddExpenses()
        {
            InitializeComponent();
            ExpenseDatePicker.Date = DateTime.Today;
            CategoryPicker.SelectedIndex = 0;
        }

        // Called from ViewExpenses "+" for any day (today or past)
        public AddExpenses(DateTime presetDate)
        {
            InitializeComponent();
            ExpenseDatePicker.Date = presetDate;
            CategoryPicker.SelectedIndex = 0;
        }

        // Edit flow
        public AddExpenses(ExpenseItem item)
        {
            InitializeComponent();
            _editItem = item;

            ExpenseDatePicker.Date = item.Date is DateTime dt ? dt : DateTime.Today;
            AmountEntry.Text       = item.Amount.ToString("0.00");
            DescriptionEditor.Text = item.Description;

            var categories = new[] { "Food", "Needs", "Transport", "Entertainment", "Health", "Shopping", "Others" };
            CategoryPicker.SelectedIndex = Array.IndexOf(categories, item.Category ?? "Food");
            if (CategoryPicker.SelectedIndex < 0)
                CategoryPicker.SelectedIndex = 0;
        }

        private async void OnDoneTapped(object sender, EventArgs e)
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

            string category = CategoryPicker.SelectedItem.ToString()!;
            string description = DescriptionEditor.Text?.Trim() ?? string.Empty;
            DateTime date = ExpenseDatePicker.Date ?? DateTime.Now;

            if (_editItem != null)
            {
                // Update existing
                _editItem.Amount = amount;
                _editItem.Category = category;
                _editItem.Date = date;
                _editItem.Description = description;

                await DatabaseService.SaveExpenseAsync(_editItem);

                // Refresh in-memory store
                var index = ExpenseStore.Items.IndexOf(_editItem);
                if (index >= 0)
                {
                    ExpenseStore.Items.RemoveAt(index);
                    ExpenseStore.Items.Insert(index, _editItem);
                }
            }
            else
            {
                // Insert new
                var newItem = new ExpenseItem
                {
                    Category = category,
                    Amount = amount,
                    Date = date,
                    Description = description
                };

                await DatabaseService.SaveExpenseAsync(newItem);
                // Id is now auto-filled by SQLite after insert
                ExpenseStore.Items.Add(newItem);
            }

            await Navigation.PopAsync();
        }
    }
}
