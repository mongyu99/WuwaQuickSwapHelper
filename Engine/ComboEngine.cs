using WuwaQuickSwapHelper.Models;

namespace WuwaQuickSwapHelper.Engine;

public class ComboEngine
{
    private readonly List<InputCode> combo;

    private int currentIndex = 0;


    public ComboEngine(List<InputCode> steps)
    {
        combo = steps;
    }


    public IReadOnlyList<InputCode> GetNextInputs(int count)
    {
        return combo
            .Skip(currentIndex)
            .Take(count)
            .ToList();
    }


    public bool Push(InputCode input)
    {
        if (input != combo[currentIndex])
        {
            Reset();
            return false;
        }

        currentIndex++;

        if (currentIndex >= combo.Count)
        {
            Reset();
        }

        return true;
    }


    public void Reset()
    {
        currentIndex = 0;
    }
}