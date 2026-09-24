namespace MvcApp.Core
{
    public class ExpensesStats
    {
        public Guid Id;
        public float TotalRevenue { get; set; }
        public float TotalExpenses { get; set; }
        public float ExpectedExpenses { get; set; }
        public DateTime LastTimeUpdated { get; set; }

        public ExpensesStats()
        {
            Id = Guid.NewGuid();
        }
    }
}
