namespace WuwaQuickSwapHelper.Models;

public static class InputCodeExtensions
{
    public static string DisplayName(this InputCode input)
    {
        return input switch
        {
            InputCode.LeftClick => "🖱",

            InputCode.Space => "SPACE",

            InputCode.Swap1 => "1",
            InputCode.Swap2 => "2",
            InputCode.Swap3 => "3",

            InputCode.Q => "Q",
            InputCode.E => "E",
            InputCode.R => "R",

            InputCode.F10 => "F10",

            _ => input.ToString()
        };
    }
}