using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "SaveSystem/Tile Database", fileName = "TileDatabase")]
public class TileDatabase : ScriptableObject
{
	[SerializeField] private string resourcesRoot = "CustomTiles";

	private Dictionary<string, TileBase> map;

	private void OnEnable()
	{
		Build();
	}

	public void Build()
	{
		var loaded = Resources.LoadAll<TileBase>(resourcesRoot);

		map = new Dictionary<string, TileBase>(loaded.Length);
		for (int i = 0; i < loaded.Length; i++)
		{
			var t = loaded[i];
			if (!t) continue;
			map[t.name] = t;
		}
	}

	public TileBase Get(string id)
	{
		if (map == null) Build();
		if (string.IsNullOrEmpty(id)) return null;
		return map.TryGetValue(id, out var t) ? t : null;
	}
}
