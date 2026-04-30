using SQLite;

namespace JimatAPP_2
{
    public static class DatabaseService
    {
        private static SQLiteAsyncConnection? _db;

        private static string DbPath => Path.Combine(
            FileSystem.AppDataDirectory, "jimat.db3");

        // ── Synchronous init — safe to call on the main thread at startup ─────
        // Uses a plain SQLiteConnection just long enough to create tables,
        // then closes it. The async connection is opened lazily on first use.
        public static void InitSync()
        {
            // Create tables with the synchronous connection (no thread-pool needed)
            using var syncDb = new SQLiteConnection(DbPath,
                SQLiteOpenFlags.ReadWrite |
                SQLiteOpenFlags.Create |
                SQLiteOpenFlags.SharedCache);

            syncDb.CreateTable<ExpenseItem>();
            syncDb.CreateTable<IncomeItem>();
        }

        // ── Synchronous bulk read — used only once at startup ─────────────────
        public static List<ExpenseItem> GetExpensesSync()
        {
            using var syncDb = new SQLiteConnection(DbPath,
                SQLiteOpenFlags.ReadOnly |
                SQLiteOpenFlags.SharedCache);
            return syncDb.Table<ExpenseItem>().ToList();
        }

        public static List<IncomeItem> GetIncomeSync()
        {
            using var syncDb = new SQLiteConnection(DbPath,
                SQLiteOpenFlags.ReadOnly |
                SQLiteOpenFlags.SharedCache);
            return syncDb.Table<IncomeItem>().ToList();
        }

        // ── Lazy async connection (used for all saves/deletes after startup) ──
        private static async Task<SQLiteAsyncConnection> GetDbAsync()
        {
            if (_db == null)
            {
                _db = new SQLiteAsyncConnection(DbPath,
                    SQLiteOpenFlags.ReadWrite |
                    SQLiteOpenFlags.Create |
                    SQLiteOpenFlags.SharedCache);

                await _db.CreateTableAsync<ExpenseItem>();
                await _db.CreateTableAsync<IncomeItem>();
            }
            return _db;
        }

        // ── EXPENSES ──────────────────────────────────────────────────────────

        public static async Task<int> SaveExpenseAsync(ExpenseItem item)
        {
            var db = await GetDbAsync();
            return item.Id == 0
                ? await db.InsertAsync(item)
                : await db.UpdateAsync(item);
        }

        public static async Task<int> DeleteExpenseAsync(ExpenseItem item)
        {
            var db = await GetDbAsync();
            return await db.DeleteAsync(item);
        }

        // ── INCOME ────────────────────────────────────────────────────────────

        public static async Task<int> SaveIncomeAsync(IncomeItem item)
        {
            var db = await GetDbAsync();
            return item.Id == 0
                ? await db.InsertAsync(item)
                : await db.UpdateAsync(item);
        }

        public static async Task<int> DeleteIncomeAsync(IncomeItem item)
        {
            var db = await GetDbAsync();
            return await db.DeleteAsync(item);
        }
    }
}
