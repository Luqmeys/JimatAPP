using SQLite;
using System.Collections.ObjectModel;

namespace JimatAPP_2
{
    // ── Expense Model ─────────────────────────────────────────────────────────
    [Table("Expenses")]
    public class ExpenseItem
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Category { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public string Description { get; set; } = string.Empty;

        [Ignore] // computed property — not stored in DB
        public string AmountDisplay => $"RM {Amount:F2}";
    }

    // ── Income Model ──────────────────────────────────────────────────────────
    [Table("Income")]
    public class IncomeItem
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Category { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public string Description { get; set; } = string.Empty;

        [Ignore]
        public string AmountDisplay => $"RM {Amount:F2}";
    }

    // ── In-Memory Stores (UI source of truth) ─────────────────────────────────
    public static class ExpenseStore
    {
        public static ObservableCollection<ExpenseItem> Items { get; } = new();
    }

    public static class IncomeStore
    {
        public static ObservableCollection<IncomeItem> Items { get; } = new();
    }
}