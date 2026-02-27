using System.IO;
using UnityEngine;

public static class SaveIO
{
	private const string FolderName = "SaveData";
	private const string FileName = "save_0.json";

	public static string GetPath()
	{
		var dir = Path.Combine(Application.persistentDataPath, FolderName);
		if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
		return Path.Combine(dir, FileName);
	}

	public static void Write(SaveGameData data)
	{
		var json = JsonUtility.ToJson(data, true);
		File.WriteAllText(GetPath(), json);
	}

	public static SaveGameData Read()
	{
		var path = GetPath();
		if (!File.Exists(path)) return null;
		var json = File.ReadAllText(path);
		return JsonUtility.FromJson<SaveGameData>(json);
	}

	public static bool Exists() => File.Exists(GetPath());
}
