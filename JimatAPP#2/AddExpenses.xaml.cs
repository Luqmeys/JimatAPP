using JimatAPP_2;

namespace JimatAPP_2
{
    public partial class AddExpenses : ContentPage
    {
        private readonly ExpenseItem? _editItem;

        // Tracks whether the checkbox is ticked in the current popup session
        private bool _suppressChecked = false;

        // Preference key stored in Preferences: "overspend_suppress_until"
        private const string SuppressKey = "overspend_suppress_until";

        public AddExpenses()
        {
            InitializeComponent();
            ExpenseDatePicker.Date = DateTime.Today;
            CategoryPicker.SelectedIndex = 0;
        }

        public AddExpenses(DateTime presetDate)
        {
            InitializeComponent();
            ExpenseDatePicker.Date = presetDate;
            CategoryPicker.SelectedIndex = 0;
        }

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

        // ── Check whether the overspending warning should fire ────────────────

        private bool ShouldWarn()
        {
            // Don't warn when editing an existing item
            if (_editItem != null) return false;

            // Check if user silenced warnings until end of today
            string? suppressUntilStr = Preferences.Get(SuppressKey, null);
            if (suppressUntilStr != null &&
                DateTime.TryParse(suppressUntilStr, out DateTime suppressUntil) &&
                DateTime.Now < suppressUntil)
                return false;

            // Compare this month's income vs expenses
            var today  = DateTime.Today;
            var mStart = new DateTime(today.Year, today.Month, 1);
            var mEnd   = mStart.AddMonths(1).AddDays(-1);

            decimal income   = IncomeStore.Items
                .Where(i => i.Date.Date >= mStart && i.Date.Date <= mEnd)
                .Sum(i => i.Amount);
            decimal expenses = ExpenseStore.Items
                .Where(i => i.Date.Date >= mStart && i.Date.Date <= mEnd)
                .Sum(i => i.Amount);

            return expenses >= income; // warn when already over (or equal, no buffer)
        }

        // ── Done tapped — validate then decide whether to warn ────────────────

        private async void OnDoneTapped(object sender, EventArgs e)
        {
            // Validation first
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

            // Show overspending warning if applicable
            if (ShouldWarn())
            {
                ShowOverspendingPopup();
                return; // wait for user to proceed or cancel
            }

            await SaveAndPop();
        }

        // ── Overspending popup ────────────────────────────────────────────────

        private void ShowOverspendingPopup()
        {
            // Populate stats
            var today  = DateTime.Today;
            var mStart = new DateTime(today.Year, today.Month, 1);
            var mEnd   = mStart.AddMonths(1).AddDays(-1);

            decimal income   = IncomeStore.Items
                .Where(i => i.Date.Date >= mStart && i.Date.Date <= mEnd)
                .Sum(i => i.Amount);
            decimal expenses = ExpenseStore.Items
                .Where(i => i.Date.Date >= mStart && i.Date.Date <= mEnd)
                .Sum(i => i.Amount);

            PopupIncomeLabel.Text   = $"RM {income:F2}";
            PopupExpensesLabel.Text = $"RM {expenses:F2}";

            // Reset checkbox state
            _suppressChecked = false;
            CheckboxTick.IsVisible = false;
            CheckboxBorder.Stroke  = new SolidColorBrush(Color.FromArgb("#888888"));

            OverspendingPopup.IsVisible = true;
        }

        private void OnCheckboxTapped(object sender, EventArgs e)
        {
            _suppressChecked = !_suppressChecked;
            CheckboxTick.IsVisible = _suppressChecked;
            CheckboxBorder.Stroke  = _suppressChecked
                ? new SolidColorBrush(Color.FromArgb("#6EE7B7"))
                : new SolidColorBrush(Color.FromArgb("#888888"));
        }

        private async void OnOverspendingProceed(object sender, EventArgs e)
        {
            OverspendingPopup.IsVisible = false;

            // If checked, suppress warnings until midnight tonight
            if (_suppressChecked)
            {
                var endOfDay = DateTime.Today.AddDays(1); // midnight = start of tomorrow
                Preferences.Set(SuppressKey, endOfDay.ToString("O"));
            }

            await SaveAndPop();
        }

        private void OnOverspendingCancel(object sender, EventArgs e)
        {
            OverspendingPopup.IsVisible = false;
        }

        // ── Core save logic ───────────────────────────────────────────────────

        private async Task SaveAndPop()
        {
            if (!decimal.TryParse(AmountEntry.Text, out decimal amount)) return;
            string category    = CategoryPicker.SelectedItem!.ToString()!;
            string description = DescriptionEditor.Text?.Trim() ?? string.Empty;
            DateTime date      = ExpenseDatePicker.Date ?? DateTime.Now;

            if (_editItem != null)
            {
                _editItem.Amount      = amount;
                _editItem.Category    = category;
                _editItem.Date        = date;
                _editItem.Description = description;

                await DatabaseService.SaveExpenseAsync(_editItem);

                var index = ExpenseStore.Items.IndexOf(_editItem);
                if (index >= 0)
                {
                    ExpenseStore.Items.RemoveAt(index);
                    ExpenseStore.Items.Insert(index, _editItem);
                }
            }
            else
            {
                var newItem = new ExpenseItem
                {
                    Category    = category,
                    Amount      = amount,
                    Date        = date,
                    Description = description
                };
                await DatabaseService.SaveExpenseAsync(newItem);
                ExpenseStore.Items.Add(newItem);
            }

            await Navigation.PopAsync();
        }
    }
}
