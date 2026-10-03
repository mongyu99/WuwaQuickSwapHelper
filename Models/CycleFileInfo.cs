namespace WuwaQuickSwapHelper.Models;

// 목록에 표시할 사이클 JSON 파일 정보
public class CycleFileInfo
{
    public string FilePath { get; set; } = "";

    public string Name { get; set; } = "";

    public string Characters { get; set; } = "";

    public string Author { get; set; } = "";

    // 지금 사용 중인 사이클인지 (목록에서 강조)
    public bool IsActive { get; set; }
}
