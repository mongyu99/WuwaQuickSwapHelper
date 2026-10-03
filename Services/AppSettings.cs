using System.IO;
using System.Text.Json;

namespace WuwaQuickSwapHelper.Services;

// exe 옆 settings.json 에 저장되는 설정입니다.
// (Data 폴더는 콤보 파일만 두는 곳이라 따로 둡니다.)
public class AppSettings
{
    // 콤보를 받아올 웹사이트 API 주소 (예: https://example.com/api)
    public string ApiBaseUrl { get; set; } = "";

    // 웹사이트에서 발급받은 개인 API 키
    public string ApiKey { get; set; } = "";

    // 체크리스트: 다음 3줄 한번에 보기
    public bool ShowNextLines { get; set; } = false;

    private static string FilePath =>
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath), Options) ?? new();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
        }

        return new AppSettings();
    }

    public void Save()
    {
        File.WriteAllText(FilePath, JsonSerializer.Serialize(this, Options));
    }
}
