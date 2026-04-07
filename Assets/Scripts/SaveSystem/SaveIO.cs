using System.IO;
using UnityEngine;

public static class SaveIO
{
	private const string FolderName = "SaveData";
	public const int MaxSlots = 5;

	public static string GetPath(int slot)
	{
		var dir = Path.Combine(Application.persistentDataPath, FolderName);
		if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
		return Path.Combine(dir, $"save_{slot}.json");
	}

	public static void Write(int slot, SaveGameData data)
	{
		var json = JsonUtility.ToJson(data, true);
		File.WriteAllText(GetPath(slot), json);
	}

	public static SaveGameData Read(int slot)
	{
		var path = GetPath(slot);
		if (!File.Exists(path)) return null;
		var json = File.ReadAllText(path);
		return JsonUtility.FromJson<SaveGameData>(json);
	}

	public static bool Exists(int slot) => File.Exists(GetPath(slot));

	public static void Delete(int slot)
	{
		var path = GetPath(slot);
		if (File.Exists(path)) File.Delete(path);
	}

	public struct SlotMeta
	{
		public bool exists;
		public string worldName;
		public int dayCount;
		public int mapSizePreset;
	}

	public static SlotMeta[] GetAllSlotMeta()
	{
		var meta = new SlotMeta[MaxSlots];
		for (int i = 0; i < MaxSlots; i++)
		{
			if (!Exists(i)) { meta[i] = new SlotMeta { exists = false }; continue; }
			var data = Read(i);
			if (data == null) { meta[i] = new SlotMeta { exists = false }; continue; }
			meta[i] = new SlotMeta
			{
				exists = true,
				worldName = string.IsNullOrEmpty(data.worldName) ? $"World {i + 1}" : data.worldName,
				dayCount = data.dayNight.dayCount,
				mapSizePreset = data.mapSizePreset
			};
		}
		return meta;
	}
}
