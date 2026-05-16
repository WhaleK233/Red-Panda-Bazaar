namespace Red_Panda_Bazaar_Code.Features.Bank;

public class BankSaveData
{
    public long CheckingBalance { get; set; }
    public List<FixedDeposit> FixedDeposits { get; set; } = new();
    public List<LoanAccount> Loans { get; set; } = new();
    public int LastInterestDay { get; set; }
}

public class FixedDeposit
{
    public long Amount { get; set; }
    public int TermDays { get; set; }
    public int StartDay { get; set; }
    public bool Withdrawn { get; set; }
}

public class LoanAccount
{
    public int PlanType { get; set; }
    public long Principal { get; set; }
    public int StartDay { get; set; }
    public long InterestAccrued { get; set; }
    public bool Repaid { get; set; }
}

/// <summary>多人同步用的包裹，避免直接暴露内部数据类结构。</summary>
public class BankSyncData
{
    public long CheckingBalance { get; set; }
    public List<FixedDeposit> FixedDeposits { get; set; } = new();
    public List<LoanAccount> Loans { get; set; } = new();
    public int LastInterestDay { get; set; }
}
