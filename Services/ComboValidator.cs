using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using WuwaQuickSwapHelper.Models;

namespace WuwaQuickSwapHelper.Services;

// 외부에서 들어오는 콤보 JSON을 화이트리스트 규칙으로 검사합니다.
// 붙여넣기 / API 받기 / Data 폴더 파일 모두 이 검사를 통과해야 사용됩니다.
public static class ComboValidator
{
    public const int MaxJsonLength = 64 * 1024;
    public const int MaxCombos = 50;
    public const int MaxSteps = 200;
    public const int MaxNameLength = 40;
    public const int MaxDescriptionLength = 200;
    public const int MaxAuthorLength = 30;
    public const int MaxCharacters = 3;
    public const int MaxCharacterNameLength = 20;

    // 단계에 사용할 수 있는 키 (F9, F10 같은 제어 키는 허용하지 않습니다)
    private static readonly HashSet<InputCode> AllowedSteps = new()
    {
        InputCode.Q, InputCode.E, InputCode.R,
        InputCode.Swap1, InputCode.Swap2, InputCode.Swap3,
        InputCode.LeftClick, InputCode.Space,
    };

    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        // 정의되지 않은 필드가 있으면 거부합니다.
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        MaxDepth = 8,
        // 키 값은 정확한 이름("Q", "Swap1" ...)만 허용합니다. 숫자(99, "1")는 거부합니다.
        Converters = { new StrictInputCodeConverter() }
    };

    public static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() }
    };

    // 성공하면 combos에 결과가, 실패하면 error에 이유가 담깁니다.
    public static bool TryParse(string json, out List<Combo> combos, out string error)
    {
        combos = new List<Combo>();
        error = "";

        if (string.IsNullOrWhiteSpace(json))
        {
            error = "내용이 비어 있습니다.";
            return false;
        }

        if (json.Length > MaxJsonLength)
        {
            error = $"데이터가 너무 큽니다. (최대 {MaxJsonLength / 1024}KB)";
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 8 });

            // 콤보 하나({ ... }) 또는 콤보 목록([ ... ]) 둘 다 받습니다.
            switch (document.RootElement.ValueKind)
            {
                case JsonValueKind.Array:
                    combos = document.RootElement.Deserialize<List<Combo>>(ReadOptions) ?? new();
                    break;

                case JsonValueKind.Object:
                    var single = document.RootElement.Deserialize<Combo>(ReadOptions);
                    if (single != null) combos.Add(single);
                    break;

                default:
                    error = "JSON은 콤보 객체 또는 콤보 목록이어야 합니다.";
                    return false;
            }
        }
        catch (JsonException ex)
        {
            error = $"JSON 형식 오류: {ex.Message}";
            return false;
        }

        if (combos.Count == 0 || combos.Count > MaxCombos)
        {
            error = $"콤보는 1개 이상 {MaxCombos}개 이하여야 합니다.";
            return false;
        }

        for (int i = 0; i < combos.Count; i++)
        {
            if (!ValidateCombo(combos[i], out var reason))
            {
                error = $"{i + 1}번째 콤보: {reason}";
                combos = new List<Combo>();
                return false;
            }
        }

        return true;
    }

    private static bool ValidateCombo(Combo combo, out string reason)
    {
        reason = "";

        // null로 들어온 필드는 기본값으로 바꿉니다.
        combo.Name = combo.Name?.Trim() ?? "";
        combo.Description = combo.Description?.Trim() ?? "";
        combo.Author = combo.Author?.Trim() ?? "";
        combo.Characters ??= new();
        combo.Steps ??= new();

        if (!IsValidText(combo.Name, MaxNameLength, required: true))
        {
            reason = $"이름은 1~{MaxNameLength}자이며 제어 문자를 포함할 수 없습니다.";
            return false;
        }

        if (!IsValidText(combo.Description, MaxDescriptionLength, required: false))
        {
            reason = $"설명은 {MaxDescriptionLength}자 이하여야 합니다.";
            return false;
        }

        if (!IsValidText(combo.Author, MaxAuthorLength, required: false))
        {
            reason = $"제작자는 {MaxAuthorLength}자 이하여야 합니다.";
            return false;
        }

        if (combo.Characters.Count > MaxCharacters)
        {
            reason = $"사용 캐릭터는 {MaxCharacters}명 이하여야 합니다.";
            return false;
        }

        for (int i = 0; i < combo.Characters.Count; i++)
        {
            combo.Characters[i] = combo.Characters[i]?.Trim() ?? "";

            if (!IsValidText(combo.Characters[i], MaxCharacterNameLength, required: true))
            {
                reason = $"캐릭터 이름은 1~{MaxCharacterNameLength}자여야 합니다.";
                return false;
            }
        }

        if (combo.Steps.Count == 0 || combo.Steps.Count > MaxSteps)
        {
            reason = $"단계는 1개 이상 {MaxSteps}개 이하여야 합니다.";
            return false;
        }

        foreach (var step in combo.Steps)
        {
            if (!AllowedSteps.Contains(step))
            {
                reason = $"사용할 수 없는 키가 있습니다: {step}";
                return false;
            }
        }

        if (combo.LoopStartIndex < -1 || combo.LoopStartIndex >= combo.Steps.Count)
        {
            reason = "반복 시작 위치(loopStartIndex)가 단계 범위를 벗어났습니다.";
            return false;
        }

        return true;
    }

    private static bool IsValidText(string text, int maxLength, bool required)
    {
        if (required && text.Length == 0) return false;
        if (text.Length > maxLength) return false;

        return !text.Any(char.IsControl);
    }
}

// InputCode를 이름 문자열로만 읽는 변환기. Enum.TryParse는 "1" 같은 숫자 문자열도 받아들이므로 직접 검사합니다.
public class StrictInputCodeConverter : JsonConverter<InputCode>
{
    public override InputCode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException("키 값은 문자열이어야 합니다.");
        }

        var text = reader.GetString() ?? "";

        var match = Enum.GetNames<InputCode>()
            .FirstOrDefault(name => string.Equals(name, text, StringComparison.OrdinalIgnoreCase));

        if (match == null)
        {
            throw new JsonException($"알 수 없는 키: {text}");
        }

        return Enum.Parse<InputCode>(match);
    }

    public override void Write(Utf8JsonWriter writer, InputCode value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
