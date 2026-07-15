using WuwaQuickSwapHelper.Models;

namespace WuwaQuickSwapHelper.Engine;

public class ComboEngine
{
    private readonly Combo combo;

    private int currentIndex;

    public Combo CurrentCombo => combo;

    public int CurrentIndex => currentIndex;

    public ComboEngine(Combo combo)
    {
        this.combo = combo;
    }

    public PushResult Push(InputCode input)
    {
        var expected = combo.Steps[currentIndex];

        if (expected != input)
        {
            return new PushResult
            {
                State = PushState.Failed,
                Index = currentIndex,
                Input = input,
                Expected = expected
            };
        }

        int successIndex = currentIndex;

        currentIndex++;

        if (currentIndex >= combo.Steps.Count)
        {
            return new PushResult
            {
                State = PushState.Completed,
                Index = successIndex,
                Input = input,
                Expected = expected
            };
        }

        return new PushResult
        {
            State = PushState.Success,
            Index = successIndex,
            Input = input,
            Expected = expected
        };
    }

    public void Reset()
    {
        currentIndex = 0;
    }
}