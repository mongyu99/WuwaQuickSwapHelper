using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using WuwaQuickSwapHelper.Models;

namespace WuwaQuickSwapHelper.Services;

public class JsonComboLoader
{
	public List<Combo> Load(string path)
	{
		if (!File.Exists(path))
		{
			// 파일이 없어도 앱이 종료되지 않도록 빈 목록을 반환합니다.
			return new List<Combo>();
		}

		var json = File.ReadAllText(path);

		var options = new JsonSerializerOptions
		{
			PropertyNameCaseInsensitive = true
		};

		options.Converters.Add(
			new JsonStringEnumConverter()
		);

		var result = JsonSerializer.Deserialize<List<Combo>>(json, options);

		return result ?? new List<Combo>();
	}

	// 폴더 안의 모든 사이클 JSON 파일 정보를 읽습니다. (이름 / 사용 캐릭터 / 제작자)
	public List<CycleFileInfo> LoadFileInfos(string directory)
	{
		var infos = new List<CycleFileInfo>();

		if (!Directory.Exists(directory))
		{
			return infos;
		}

		foreach (var file in Directory.GetFiles(directory, "*.json").OrderBy(f => f))
		{
			var info = new CycleFileInfo
			{
				FilePath = file,
				Name = Path.GetFileNameWithoutExtension(file),
				Characters = "-",
				Author = "-"
			};

			try
			{
				var first = Load(file).FirstOrDefault();

				if (first != null)
				{
					if (!string.IsNullOrWhiteSpace(first.Name)) info.Name = first.Name;
					if (first.Characters.Count > 0) info.Characters = string.Join(", ", first.Characters);
					if (!string.IsNullOrWhiteSpace(first.Author)) info.Author = first.Author;
				}
			}
			catch (Exception)
			{
				// 형식이 잘못된 파일은 파일 이름만 표시합니다.
			}

			infos.Add(info);
		}

		return infos;
	}
}