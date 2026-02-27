using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class MapGenPalmSpawner
{
	public static void Spawn(
		MapGenData d,
		TilemapGenerationSettingsV2 s,
		Tilemap grassTilemap,
		GameObject palmPrefab,
		Transform parent,
		ref MapGenRng rng,
		List<GameObject> spawned)
	{
		if (palmPrefab == null) return;
		if (grassTilemap == null) return;

		int size = d.size;

		List<int> candidates = new List<int>(size * size / 4);
		for (int y = 0; y < size; y++)
		{
			for (int x = 0; x < size; x++)
			{
				int i = d.Idx(x, y);
				if (d.grass[i] != 1) continue;

				int cd = d.coastDist[i];
				if (cd < s.palmCoastMin || cd > s.palmCoastMax) continue;

				candidates.Add(i);
			}
		}

		Shuffle(candidates, ref rng);

		int minD = Mathf.Max(1, s.palmMinDistance);
		int minD2 = minD * minD;

		List<Vector2Int> accepted = new List<Vector2Int>();

		Vector3Int start = new Vector3Int(-size / 2, -size / 2, 0);

		for (int c = 0; c < candidates.Count; c++)
		{
			if (rng.Next01() > s.palmSpawnChance) continue;

			int idx = candidates[c];
			int x = idx % size;
			int y = idx / size;

			bool ok = true;
			for (int a = 0; a < accepted.Count; a++)
			{
				int dx = accepted[a].x - x;
				int dy = accepted[a].y - y;
				if (dx * dx + dy * dy < minD2) { ok = false; break; }
			}
			if (!ok) continue;

			accepted.Add(new Vector2Int(x, y));

			Vector3Int cell = start + new Vector3Int(x, y, 0);
			Vector3 pos = grassTilemap.GetCellCenterWorld(cell);
			pos += new Vector3(0f, grassTilemap.cellSize.y, 0f);

			GameObject go = Object.Instantiate(palmPrefab, pos, Quaternion.identity, parent);
			spawned.Add(go);
		}
	}

	private static void Shuffle(List<int> list, ref MapGenRng rng)
	{
		for (int i = list.Count - 1; i > 0; i--)
		{
			int j = rng.NextInt(0, i + 1);
			int t = list[i];
			list[i] = list[j];
			list[j] = t;
		}
	}
}
