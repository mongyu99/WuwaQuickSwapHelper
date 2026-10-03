using System.IO;
using System.Text.Json;
using WuwaQuickSwapHelper.Models;

namespace WuwaQuickSwapHelper.Services;

public class JsonComboLoader
{
	public static string DataDirectory =>
		Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");

	public List<Combo> Load(string path)
	{
		// 파일이 없거나 검사를 통과하지 못하면 앱이 종료되지 않도록 빈 목록을 반환합니다.
		if (!File.Exists(path) || new FileInfo(path).Length > ComboValidator.MaxJsonLength)
		{
			return new List<Combo>();
		}

		var json = File.ReadAllText(path);

		if (!ComboValidator.TryParse(json, out var combos, out var error))
		{
			System.Diagnostics.Debug.WriteLine($"{path} : {error}");
			return new List<Combo>();
		}

		return combos;
	}

	// Data 폴더의 모든 파일에서 검사를 통과한 콤보를 모읍니다.
	public List<Combo> LoadAll(string directory)
	{
		if (!Directory.Exists(directory))
		{
			return new List<Combo>();
		}

		return Directory.GetFiles(directory, "*.json")
			.OrderBy(f => f)
			.SelectMany(Load)
			.ToList();
	}

	// 검사를 통과한 콤보를 Data 폴더에 새 파일로 저장합니다.
	// 파일 이름은 JSON 내용이 아니라 앱이 만든 안전한 이름만 사용합니다.
	public string Save(string directory, List<Combo> combos)
	{
		Directory.CreateDirectory(directory);

		var safeName = new string(combos[0].Name
			.Where(c => char.IsLetterOrDigit(c) || c is '-' or '_')
			.Take(30)
			.ToArray());

		if (safeName.Length == 0)
		{
			safeName = "combo";
		}

		var path = Path.Combine(directory, $"{safeName}_{DateTime.Now:yyyyMMddHHmmss}.json");

		File.WriteAllText(path, JsonSerializer.Serialize(combos, ComboValidator.WriteOptions));

		return path;
	}

	// Data 폴더 안의 기존 파일을 덮어씁니다. Data 폴더 밖의 경로는 거부합니다.
	public void Overwrite(string path, List<Combo> combos)
	{
		var dataDir = Path.GetFullPath(DataDirectory) + Path.DirectorySeparatorChar;

		if (!Path.GetFullPath(path).StartsWith(dataDir, StringComparison.OrdinalIgnoreCase))
		{
			throw new InvalidOperationException("Data 폴더 밖의 파일은 저장할 수 없습니다.");
		}

		File.WriteAllText(path, JsonSerializer.Serialize(combos, ComboValidator.WriteOptions));
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

			var first = Load(file).FirstOrDefault();

			if (first == null)
			{
				// 검사를 통과하지 못한 파일은 표시만 하고 사용하지 않습니다.
				info.Characters = "(잘못된 파일)";
			}
			else
			{
				if (!string.IsNullOrWhiteSpace(first.Name)) info.Name = first.Name;
				if (first.Characters.Count > 0) info.Characters = string.Join(", ", first.Characters);
				if (!string.IsNullOrWhiteSpace(first.Author)) info.Author = first.Author;
			}

			infos.Add(info);
		}

		return infos;
	}
}
