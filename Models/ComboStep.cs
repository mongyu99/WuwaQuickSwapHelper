namespace WuwaQuickSwapHelper.Models;

public class ComboStep
{
    public InputCode Input { get; set; }

    public List<ComboStep> Steps { get; set; } = new();
}