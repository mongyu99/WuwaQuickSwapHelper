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
			throw new FileNotFoundException(path);
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
}