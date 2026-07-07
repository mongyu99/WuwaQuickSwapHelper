namespace WuwaQuickSwapHelper.Models;

public class Combo
{
    public string Id { get; set; } = "";

    public string Character { get; set; } = "";

    public string Name { get; set; } = "";

    public string Description { get; set; } = "";

    public List<ComboStep> Steps { get; set; } = new();
}