namespace Red_Panda_Bazaar_Code.Features.Bank;

public class BankSaveData
{
    public int CheckingBalance { get; set; }
    public int InterestEarned { get; set; }
    public List<FixedDeposit> FixedDeposits { get; set; } = new();
    public int LastInterestDay { get; set; }
}

public class FixedDeposit
{
    public int Amount { get; set; }
    public int TermDays { get; set; }
    public int StartDay { get; set; }
    public bool Withdrawn { get; set; }
}

/// <summary>多人同步用的包裹，避免直接暴露内部数据类结构。</summary>
public class BankSyncData
{
    public int CheckingBalance { get; set; }
    public int InterestEarned { get; set; }
    public List<FixedDeposit> FixedDeposits { get; set; } = new();
    public int LastInterestDay { get; set; }
}
