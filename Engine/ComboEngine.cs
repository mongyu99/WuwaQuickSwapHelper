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

    // 콤보를 줄 단위로 나눈 것. 스왑 키(1~3)가 한 줄의 끝입니다.
    // 예) Q E 1 Q 🖱 2 E 🖱 1  →  [Q E 1] [Q 🖱 2] [E 🖱 1]
    private List<List<InputCode>> lines = new();

    private int currentLine;

    public IReadOnlyList<List<InputCode>> Lines => lines;

    public int CurrentLine => currentLine;

    // 반복 구간이 시작되는 줄 (반복 없음 = 0번 줄)
    public int LoopLine { get; private set; }

    public void SetCombo(Combo combo) // 사이클을 변경합니다.
    {
        currentCombo = combo;

        lines = SplitLines(combo.Steps);

        LoopLine = 0;

        if (combo.HasLoop)
        {
            // 반복 시작 단계가 들어 있는 줄을 찾습니다.
            int count = 0;

            for (int i = 0; i < lines.Count; i++)
            {
                count += lines[i].Count;

                if (combo.LoopStartIndex < count)
                {
                    LoopLine = i;
                    break;
                }
            }
        }

        Reset();
    }

    public static List<List<InputCode>> SplitLines(List<InputCode> steps)
    {
        var result = new List<List<InputCode>>();
        var line = new List<InputCode>();

        foreach (var step in steps)
        {
            line.Add(step);

            if (step is InputCode.Swap1 or InputCode.Swap2 or InputCode.Swap3)
            {
                result.Add(line);
                line = new List<InputCode>();
            }
        }

        if (line.Count > 0)
        {
            result.Add(line);
        }

        return result;
    }

    public void Reset() // 현재 사이클을 초기화합니다.
    {
        currentIndex = 0;
        currentLine = 0;
    }

    // 스왑 키를 누르면 다음 줄로 이동합니다. 마지막 줄 다음에는 반복 줄로 돌아가며 true를 반환합니다.
    public bool NextLine()
    {
        currentLine++;

        if (currentLine >= lines.Count)
        {
            currentLine = LoopLine;
            return true;
        }

        return false;
    }

    // 현재 줄에서 offset만큼 뒤의 줄 (끝을 넘으면 반복 줄부터 다시 이어집니다)
    public List<InputCode> GetLine(int offset)
    {
        if (lines.Count == 0)
            return new List<InputCode>();

        int index = currentLine + offset;

        if (index >= lines.Count)
        {
            int loopLength = lines.Count - LoopLine;
            index = LoopLine + (index - lines.Count) % loopLength;
        }

        return lines[index];
    }

    public void Restart() // 사이클 완료 후 반복 구간이 있으면 그 위치부터, 없으면 처음부터 다시 시작합니다.
    {
        currentIndex = currentCombo!.HasLoop ? currentCombo.LoopStartIndex : 0;
    }

    public InputCode GetCurrentInput() // 사용자가 눌러야 하는 키를 반환합니다.
    {
        return currentCombo!.Steps[currentIndex];
    }

    public bool IsCompleted() // 사이클이 끝났나요?
    {
        return currentIndex >= currentCombo!.Steps.Count;
    }

    public PushResult Advance() // 어떤 키든 현재 단계를 성공 처리하고 다음 단계로 넘어갑니다.
    {
        return Push(GetCurrentInput());
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