namespace JimatAPP_2
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            // Use fully synchronous DB calls — no deadlock possible on the main thread.
            // InitSync() creates the tables if they don't exist yet.
            // GetExpensesSync() / GetIncomeSync() read rows with a plain SQLiteConnection.
            DatabaseService.InitSync();

            var expenses = DatabaseService.GetExpensesSync();
            var incomes = DatabaseService.GetIncomeSync();

            foreach (var e in expenses) ExpenseStore.Items.Add(e);
            foreach (var i in incomes) IncomeStore.Items.Add(i);

            return new Window(new AppShell());
        }
    }
}
