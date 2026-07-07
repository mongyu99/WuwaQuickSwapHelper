using WuwaQuickSwapHelper.Models;

namespace WuwaQuickSwapHelper.Engine;

public class ComboEngine
{
    private readonly List<InputCode> combo = new()
    {
        InputCode.E,
        InputCode.LeftClick,
        InputCode.LeftClick,
        InputCode.Space,
        InputCode.Q
    };

    private int currentIndex;

    /// <summary>
    /// 현재 화면에 표시해야 하는 입력
    /// </summary>
    public InputCode CurrentInput => combo[currentIndex];

    /// <summary>
    /// 입력 처리
    /// </summary>
    public bool Push(InputCode input)
    {
        // 틀린 입력이면 무시
        if (input != combo[currentIndex])
            return false;

        currentIndex++;

        // 마지막까지 완료
        if (currentIndex >= combo.Count)
        {
            currentIndex = 0;
        }

        return true;
    }

    public void Reset()
    {
        currentIndex = 0;
    }
}