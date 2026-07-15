namespace WuwaQuickSwapHelper.Models;

public class PushResult
{
    public PushState State { get; init; }

    public int Index { get; init; }

    public InputCode Input { get; init; }

    public InputCode Expected { get; init; }
}