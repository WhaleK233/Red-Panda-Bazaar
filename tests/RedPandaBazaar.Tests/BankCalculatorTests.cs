using Red_Panda_Bazaar_Code.Features.Bank;
using Xunit;

namespace Red_Panda_Bazaar_Code.Tests;

public class BankCalculatorTests
{
    private static LoanAccount Loan(int planType, long principal, long interest = 0, bool repaid = false) =>
        new() { PlanType = planType, Principal = principal, InterestAccrued = interest, Repaid = repaid };

    // 总信用额度 = max(有效收入 × 0.5, 10000)，有效收入 = 总收入 − 历史所有贷款本金（含已还）

    [Fact]
    public void TotalCreditLimit_FloorIs10000_WhenNoEarnings()
    {
        Assert.Equal(10_000, BankCalculator.GetTotalCreditLimit(new List<LoanAccount>(), totalMoneyEarned: 0));
    }

    [Fact]
    public void TotalCreditLimit_HalfOfEarnings()
    {
        Assert.Equal(500_000, BankCalculator.GetTotalCreditLimit(new List<LoanAccount>(), 1_000_000));
    }

    [Fact]
    public void TotalCreditLimit_SubtractsAllLoanPrincipal_EvenRepaid()
    {
        // 有效收入 = 100000 − 60000 = 40000（已还贷款的本金同样计入历史借贷）
        var loans = new List<LoanAccount> { Loan(0, 60_000, repaid: true) };
        Assert.Equal(20_000, BankCalculator.GetTotalCreditLimit(loans, 100_000));
    }

    [Fact]
    public void TotalCreditLimit_NeverBelowFloor_WhenBorrowedExceedsEarnings()
    {
        var loans = new List<LoanAccount> { Loan(0, 500_000) };
        Assert.Equal(10_000, BankCalculator.GetTotalCreditLimit(loans, 100_000));
    }

    // 剩余额度 = 总额度 − 未还贷款的（本金 + 利息）；已还贷款不占额度。
    // 注意总额度在内部重算：贷款本金会先压低有效收入（额度双重收缩），
    // 100 万收入借 10 万 → 总额度 45 万，不是无贷时的 50 万。

    [Fact]
    public void RemainingCredit_SubtractsOutstandingPrincipalAndInterest()
    {
        // 总额度 450000 − 未还（100000 + 5000）= 345000
        var loans = new List<LoanAccount> { Loan(0, 100_000, interest: 5_000) };
        Assert.Equal(345_000, BankCalculator.GetRemainingCredit(loans, 1_000_000));
    }

    [Fact]
    public void RemainingCredit_IgnoresRepaidLoans()
    {
        // 已还贷款不占额度，但本金仍计入历史借贷压低总额度：450000 − 0
        var loans = new List<LoanAccount> { Loan(0, 100_000, repaid: true) };
        Assert.Equal(450_000, BankCalculator.GetRemainingCredit(loans, 1_000_000));
    }

    // 方案可贷 = min(max(总额度 × 方案比例, 方案最低额), max(0, 剩余额度))

    [Theory]
    [InlineData(0, 1_000)]    // 10000 × 0.1 = 1000 ＞ 最低 500
    [InlineData(1, 8_000)]    // 10000 × 0.3 = 3000 ＜ 最低 8000，取最低
    [InlineData(2, 20_000)]   // 10000 × 0.6 = 6000 ＜ 最低 20000，取最低
    public void AvailableLoanAmount_PlanCapAndMinimum(int planType, long expected)
    {
        var amount = BankCalculator.GetAvailableLoanAmount(planType, remainingCredit: long.MaxValue,
            new List<LoanAccount>(), totalMoneyEarned: 0);
        Assert.Equal(expected, amount);
    }

    [Theory]
    [InlineData(0, 1_000)]    // 方案上限 1000 ＜ 剩余 2000，取方案上限
    [InlineData(1, 2_000)]    // 方案上限 8000 ＞ 剩余 2000，被剩余额度压低
    [InlineData(2, 2_000)]
    public void AvailableLoanAmount_TakesMinOfPlanCapAndRemainingCredit(int planType, long expected)
    {
        var amount = BankCalculator.GetAvailableLoanAmount(planType, remainingCredit: 2_000,
            new List<LoanAccount>(), totalMoneyEarned: 0);
        Assert.Equal(expected, amount);
    }

    [Fact]
    public void AvailableLoanAmount_ZeroWhenRemainingNegative()
    {
        var amount = BankCalculator.GetAvailableLoanAmount(0, remainingCredit: -5,
            new List<LoanAccount>(), totalMoneyEarned: 0);
        Assert.Equal(0, amount);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    public void AvailableLoanAmount_ZeroForUnknownPlan(int planType)
    {
        var amount = BankCalculator.GetAvailableLoanAmount(planType, remainingCredit: 100_000,
            new List<LoanAccount>(), totalMoneyEarned: 1_000_000);
        Assert.Equal(0, amount);
    }
}
