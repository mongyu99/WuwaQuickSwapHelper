using System.Net.Http;
using System.Text.RegularExpressions;

namespace WuwaQuickSwapHelper.Services;

// 웹사이트 API에서 콤보 JSON을 받아옵니다. 받은 내용은 ComboValidator 검사를 거쳐야 사용됩니다.
public class ComboApiService
{
    private static readonly HttpClient Client = new()
    {
        Timeout = TimeSpan.FromSeconds(10),
        // 너무 큰 응답은 읽지 않습니다.
        MaxResponseContentBufferSize = ComboValidator.MaxJsonLength
    };

    // 공유 코드에 허용하는 문자 (영문 / 숫자 / - / _)
    private static readonly Regex CodePattern = new("^[A-Za-z0-9_-]{1,64}$");

    // code가 비어 있으면 API 키 주인의 콤보 목록 전체를, 있으면 해당 코드의 콤보를 받습니다.
    //   GET {ApiBaseUrl}/combos          (X-API-Key 헤더)
    //   GET {ApiBaseUrl}/combos/{code}   (X-API-Key 헤더)
    public async Task<string> FetchAsync(AppSettings settings, string code)
    {
        if (!Uri.TryCreate(settings.ApiBaseUrl.TrimEnd('/'), UriKind.Absolute, out var baseUri))
        {
            throw new InvalidOperationException("settings.json의 apiBaseUrl이 올바르지 않습니다.");
        }

        // API 키가 평문으로 전송되지 않도록 HTTPS만 허용합니다. (개발용 localhost 제외)
        if (baseUri.Scheme != Uri.UriSchemeHttps && !baseUri.IsLoopback)
        {
            throw new InvalidOperationException("API 주소는 https:// 로 시작해야 합니다.");
        }

        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            throw new InvalidOperationException("API 키를 입력하세요.");
        }

        code = code.Trim();

        if (code.Length > 0 && !CodePattern.IsMatch(code))
        {
            throw new InvalidOperationException("코드는 영문, 숫자, -, _ 만 사용할 수 있습니다.");
        }

        var root = baseUri.AbsoluteUri.TrimEnd('/');

        var url = code.Length == 0
            ? $"{root}/combos"
            : $"{root}/combos/{Uri.EscapeDataString(code)}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("X-API-Key", settings.ApiKey);

        using var response = await Client.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"서버 응답 오류: {(int)response.StatusCode} {response.ReasonPhrase}");
        }

        return await response.Content.ReadAsStringAsync();
    }
}
