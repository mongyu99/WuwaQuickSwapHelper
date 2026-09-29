using WuwaQuickSwapHelper.Models;

namespace WuwaQuickSwapHelper.Engine;

public class ComboEngine
{
    private Combo? currentCombo;

    private int currentIndex;

    public Combo CurrentCombo => currentCombo!;

    public int CurrentIndex => currentIndex;

    public ComboEngine(Combo combo) // 첫번째 사이클을 지정합니다.
    {
        SetCombo(combo);
    }

    public void SetCombo(Combo combo) // 사이클을 변경합니다.
    {
        currentCombo = combo;

        Reset();
    }

    public void Reset() // 현재 사이클을 초기화합니다.
    {
        currentIndex = 0;
    }

    public InputCode GetCurrentInput() // 사용자가 눌러야 하는 키를 반환합니다.
    {
        return currentCombo!.Steps[currentIndex];
    }

    public bool IsCompleted() // 사이클이 끝났나요?
    {
        return currentIndex >= currentCombo!.Steps.Count;
    }

    public PushResult Push(InputCode input)
    {
        if (currentCombo == null)
        {
            throw new InvalidOperationException("현재 콤보가 설정되지 않았습니다.");
        }

        var expected = currentCombo.Steps[currentIndex];

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

        if (currentIndex >= currentCombo.Steps.Count)
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
}