using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "SaveSystem/Item Database", fileName = "ItemDatabase")]
public class ItemDatabase : ScriptableObject
{
	[SerializeField] private string resourcesFolder = "Items";

	private Dictionary<string, Item> map;

	public void Build()
	{
		map = new Dictionary<string, Item>(256);

		var loaded = Resources.LoadAll<Item>(resourcesFolder);
		for (int i = 0; i < loaded.Length; i++)
		{
			var it = loaded[i];
			if (it == null) continue;

			map[it.name] = it;
		}
	}

	public Item Get(string id)
	{
		if (map == null) Build();
		if (string.IsNullOrEmpty(id)) return null;
		return map.TryGetValue(id, out var it) ? it : null;
	}
}
