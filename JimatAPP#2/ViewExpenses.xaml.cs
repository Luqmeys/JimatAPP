using JimatAPP_2;

namespace JimatAPP_2
{
    public partial class ViewExpenses : ContentPage
    {
        private DateTime _currentDate = DateTime.Today;
        private DateTime _lastRendered = DateTime.MinValue; // dirty-check
        private ExpenseItem? _detailItem;

        // ── Pre-cached colors (parsed once, never again) ──────────────────────
        private static readonly Color CardBg = Color.FromArgb("#2E2E2E");
        private static readonly Color NoteColor = Color.FromArgb("#888888");
        private static readonly Color GreenAccent = Color.FromArgb("#6EE7B7");
        private static readonly Color RedAccent = Color.FromArgb("#FF7C7C");

        // ── Pre-built RoundRectangle shape (reused every card) ────────────────
        private static readonly Microsoft.Maui.Controls.Shapes.RoundRectangle CardShape =
            new() { CornerRadius = 20 };

        public ViewExpenses()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            // Only rebuild if the store may have changed (returning from Add/Edit)
            // or the date changed. We always rebuild on appearing to catch edits/deletes.
            RefreshExpensesList();
        }

        // ── Date navigation ───────────────────────────────────────────────────

        private void OnPreviousDayTapped(object sender, EventArgs e)
        {
            _currentDate = _currentDate.AddDays(-1);
            RefreshExpensesList();
        }

        private void OnNextDayTapped(object sender, EventArgs e)
        {
            _currentDate = _currentDate.AddDays(1);
            RefreshExpensesList();
        }

        // ── List refresh ──────────────────────────────────────────────────────

        private void RefreshExpensesList()
        {
            bool isToday = _currentDate.Date == DateTime.Today;

            // Update labels
            DateLabel.Text = isToday
                ? $"Today · {_currentDate:M/d/yyyy}"
                : _currentDate.ToString("dddd · M/d/yyyy");
            DayHeadingLabel.Text = isToday ? "Today's Expenses :" : $"{_currentDate:MMM d} Expenses :";
            TotalHeadingLabel.Text = isToday ? "Today's Total:" : $"{_currentDate:MMM d} Total:";
            AddButtonFrame.IsVisible = true;

            // Filter once — store in a local list to avoid multiple enumerations
            var items = ExpenseStore.Items
                .Where(i => i.Date.Date == _currentDate.Date)
                .ToList();

            EmptyLabel.IsVisible = items.Count == 0;

            // ── Recycle existing card slots instead of destroy/recreate ───────
            // We keep (Border + editLabel) pairs inside VerticalStackLayouts.
            // If the count matches we just update text; otherwise we rebuild.
            bool needsRebuild = ExpensesList.Children.Count != items.Count;

            if (needsRebuild)
            {
                ExpensesList.Children.Clear();

                foreach (var item in items)
                    ExpensesList.Children.Add(BuildItemStack(item));
            }
            else
            {
                // Update in-place — no new objects allocated
                for (int i = 0; i < items.Count; i++)
                    UpdateItemStack(
                        (VerticalStackLayout)ExpensesList.Children[i], items[i]);
            }

            // Compute total in one pass
            decimal total = 0;
            foreach (var item in items) total += item.Amount;
            TotalLabel.Text = $"RM {total:F2}";
        }

        // ── Build a fresh item card (only called when count changes) ──────────

        private VerticalStackLayout BuildItemStack(ExpenseItem item)
        {
            var cardBorder = new Border
            {
                BackgroundColor = CardBg,
                StrokeThickness = 0,
                // Share the shape object — safe because it carries no mutable state
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 20 },
                Padding = new Thickness(16, 12),
                Margin = new Thickness(0, 4)
            };

            var cardGrid = new Grid();
            cardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            cardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var categoryLabel = new Label
            {
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
                VerticalOptions = LayoutOptions.Center
            };
            var noteIcon = new Label
            {
                Text = "📝",
                FontSize = 11,
                TextColor = NoteColor,
                VerticalOptions = LayoutOptions.Center,
                Margin = new Thickness(6, 0, 0, 0)
            };
            var leftStack = new HorizontalStackLayout
            { VerticalOptions = LayoutOptions.Center, Spacing = 0 };
            leftStack.Children.Add(categoryLabel);
            leftStack.Children.Add(noteIcon);

            var amountLabel = new Label
            {
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
                VerticalOptions = LayoutOptions.Center
            };

            Grid.SetColumn(leftStack, 0);
            Grid.SetColumn(amountLabel, 1);
            cardGrid.Children.Add(leftStack);
            cardGrid.Children.Add(amountLabel);
            cardBorder.Content = cardGrid;

            var editLabel = new Label
            {
                Text = "Edit",
                FontSize = 11,
                TextColor = NoteColor,
                HorizontalOptions = LayoutOptions.Center,
                Margin = new Thickness(0, 2, 0, 0)
            };

            var stack = new VerticalStackLayout { Margin = new Thickness(0, 4) };
            stack.Children.Add(cardBorder);
            stack.Children.Add(editLabel);

            // Populate data and wire gestures
            ApplyItemData(stack, item);
            return stack;
        }

        // ── Update an existing card in-place (no new views allocated) ─────────

        private void UpdateItemStack(VerticalStackLayout stack, ExpenseItem item)
        {
            // Detach old gestures before re-wiring to avoid accumulation
            var cardBorder = (Border)stack.Children[0];
            cardBorder.GestureRecognizers.Clear();

            var editLabel = (Label)stack.Children[1];
            editLabel.GestureRecognizers.Clear();

            ApplyItemData(stack, item);
        }

        private void ApplyItemData(VerticalStackLayout stack, ExpenseItem item)
        {
            var cardBorder = (Border)stack.Children[0];
            var cardGrid = (Grid)cardBorder.Content;
            var leftStack = (HorizontalStackLayout)cardGrid.Children[0];
            var categoryLabel = (Label)leftStack.Children[0];
            var noteIcon = (Label)leftStack.Children[1];
            var amountLabel = (Label)cardGrid.Children[1];
            var editLabel = (Label)stack.Children[1];

            categoryLabel.Text = item.Category;
            amountLabel.Text = item.AmountDisplay;
            noteIcon.IsVisible = !string.IsNullOrWhiteSpace(item.Description);

            var capturedItem = item;
            cardBorder.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(() => ShowDetailPopup(capturedItem))
            });
            editLabel.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(async () =>
                    await Navigation.PushAsync(new EditExpenses(capturedItem)))
            });
        }

        // ── Detail popup ──────────────────────────────────────────────────────

        private void ShowDetailPopup(ExpenseItem item)
        {
            _detailItem = item;
            DetailCategoryLabel.Text = item.Category;
            DetailAmountLabel.Text = item.AmountDisplay;
            DetailDateLabel.Text = item.Date.ToString("dddd, MMM d yyyy");
            DetailDescriptionLabel.Text = string.IsNullOrWhiteSpace(item.Description)
                ? "No description added." : item.Description;
            DetailPopupOverlay.IsVisible = true;
        }

        private void OnDetailPopupDismiss(object sender, EventArgs e)
        {
            DetailPopupOverlay.IsVisible = false;
            _detailItem = null;
        }

        private async void OnDetailEditTapped(object sender, EventArgs e)
        {
            if (_detailItem == null) return;
            DetailPopupOverlay.IsVisible = false;
            await Navigation.PushAsync(new EditExpenses(_detailItem));
            _detailItem = null;
        }

        // ── Net Balance popup ─────────────────────────────────────────────────

        private void OnNetBalanceTapped(object sender, EventArgs e)
        {
            decimal income = IncomeStore.Items.Where(i => i.Date.Date == _currentDate.Date).Sum(i => i.Amount);
            decimal expenses = ExpenseStore.Items.Where(i => i.Date.Date == _currentDate.Date).Sum(i => i.Amount);
            decimal net = income - expenses;

            bool isToday = _currentDate.Date == DateTime.Today;
            PopupDateLabel.Text = isToday
                ? $"Today · {_currentDate:M/d/yyyy}"
                : _currentDate.ToString("dddd, MMMM d yyyy");
            PopupIncomeLabel.Text = $"RM {income:F2}";
            PopupExpensesLabel.Text = $"RM {expenses:F2}";
            PopupNetLabel.Text = $"RM {net:F2}";
            PopupNetLabel.TextColor = net >= 0 ? GreenAccent : RedAccent;
            PopupOverlay.IsVisible = true;
        }

        private void OnPopupDismiss(object sender, EventArgs e) => PopupOverlay.IsVisible = false;
        private void OnPopupCardTapped(object sender, EventArgs e) { }

        // ── Page navigation ───────────────────────────────────────────────────

        private async void OnAddExpenseTapped(object sender, EventArgs e) =>
            await Navigation.PushAsync(new AddExpenses(_currentDate));

        private async void OnIncomeTapped(object sender, EventArgs e) =>
            await Navigation.PushAsync(new ViewIncome());
    }

}
