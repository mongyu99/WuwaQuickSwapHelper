namespace WuwaQuickSwapHelper.Models;

public class Combo
{
    public string Name { get; set; } = "";

    public string Description { get; set; } = "";

    // 사용 캐릭터
    public List<string> Characters { get; set; } = new();

    // 제작자
    public string Author { get; set; } = "";

    public List<InputCode> Steps { get; set; } = new();

    // 반복 시작 위치 (-1 = 반복 없음). 사이클 완료 후 이 단계부터 다시 이어집니다.
    public int LoopStartIndex { get; set; } = -1;

    public bool HasLoop => LoopStartIndex >= 0 && LoopStartIndex < Steps.Count;
}