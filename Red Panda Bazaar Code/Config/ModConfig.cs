namespace Red_Panda_Bazaar_Code.Config;

public class ModConfig
{
    public float CritterMultiplier { get; set; } = 1.0f;

    public float AnimationSpeed_PrizeMenu_Multiplier { get; set; } = 1.0f;

    public bool EnableTax { get; set; }

    public float TaxRate { get; set; } = 0.1f;

    public string DebugToggleKey { get; set; } = "OemTilde";
}