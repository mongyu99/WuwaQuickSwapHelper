namespace WuwaQuickSwapHelper.Models;

public class Combo
{
    public string Name { get; set; } = "";

    public string Description { get; set; } = "";

    public List<InputCode> Steps { get; set; } = new();
}