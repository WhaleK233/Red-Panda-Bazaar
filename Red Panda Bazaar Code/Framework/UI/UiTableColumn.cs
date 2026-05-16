namespace Red_Panda_Bazaar_Code.Framework.UI;

public class UiTableColumn
{
    public string Header { get; set; } = "";
    public UiAlign Align { get; set; } = UiAlign.Left;
    public int Width { get; set; }
    public int MinWidth { get; set; }
    public bool Bold { get; set; }
}
