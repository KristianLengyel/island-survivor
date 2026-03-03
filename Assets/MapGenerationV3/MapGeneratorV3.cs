using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Main MonoBehaviour orchestrator.
/// Call Regenerate() from editor button or runtime.
/// Pools palm and rock GameObjects to avoid GC spikes.
/// </summary>
public class MapGeneratorV3 : MonoBehaviour
{
	[Header("Tilemaps")]
	public Tilemap waterTilemap;
	public Tilemap landTilemap;
	public Tilemap grassTilemap;
	public Tilemap oceanOverlayTilemap;
	public Tilemap oceanFloorShallowTilemap;
	public Tilemap oceanFloorMediumTilemap;
	public Tilemap oceanFloorDeepTilemap;
	public Tilemap dockTilemap;

	[Header("Settings")]
	public MapSettingsV3 settings;

	[Header("Decorator Parents")]
	public Transform palmParent;
	public Transform rockParent;

	// ---- Internal ----
	private readonly MapWorkspaceV3 _w = new MapWorkspaceV3();
	private readonly List<GameObject> _activePalms = new List<GameObject>();
	private readonly List<GameObject> _activeRocks = new List<GameObject>();
	private readonly Stack<GameObject> _palmPool = new Stack<GameObject>();
	private readonly Stack<GameObject> _rockPool = new Stack<GameObject>();

	private void Start() => Regenerate();

	// ------------------------------------------------------------------
	public void Regenerate()
	{
		if (settings == null) return;
		uint seed = settings.useRandomSeed
			? (uint)System.DateTime.UtcNow.Ticks
			: MapRngV3.HashSeed(settings.seedInput);
		Regenerate(seed);
	}

	public void Regenerate(uint seed)
	{
		if (settings == null) return;

		_w.Ensure(settings.mapSize, Mathf.Max(0, settings.pad));
		ReturnAllToPool();

		var rng = new MapRngV3(seed);

		// 1. Height generation + island/biome placement
		Vector2 baseOffset, seaweedOffset;
		MapNoiseV3.GenerateHeight(_w.data, settings, ref rng, _w, out baseOffset, out seaweedOffset);

		// 2. Land mask
		MapMasksV3.ThresholdLand(_w.data, settings);

		// 3. Cellular automata — smooth sandy coastlines
		MapMasksV3.CellularAutomata(_w.data, settings, _w.morphTmp);

		// 4. Morphological closing (optional, fills micro-gaps after CA)
		MapMasksV3.MorphologyClosing(_w.data, settings.morphologyClosingIterations, _w.morphTmp);

		// 5. Cleanup
		MapMasksV3.RemoveSmallIslands(_w.data, settings.minIslandTiles, _w);
		MapMasksV3.FillSmallLakes(_w.data, settings.minLakeTiles, _w);

		// 6. Coast distance field
		MapDistanceFieldV3.Compute(_w.data, _w.bfsQueue);

		// 7. Propagate biomes from island seeds to all tiles
		MapMasksV3.PropagateIslandBiomes(_w.data, _w, settings);

		// 8. Biome bands (beach, grass, ocean depth)
		MapBiomesV3.BuildBands(_w.data, settings, ref rng);

		// 9. Decorators
		MapDecoratorsV3.PlaceAll(_w.data, settings, seaweedOffset);

		// 10. Paint tilemaps
		MapPainterV3.PaintAll(
			_w.data, settings,
			waterTilemap, landTilemap, grassTilemap,
			oceanOverlayTilemap,
			oceanFloorShallowTilemap, oceanFloorMediumTilemap, oceanFloorDeepTilemap,
			_w);

		// 11. Paint center dock
		PaintDock();

		// 12. Spawn decorator GameObjects
		SpawnDecorators(ref rng);
	}

	// ------------------------------------------------------------------
	public void ClearAll()
	{
		ReturnAllToPool();
		waterTilemap?.ClearAllTiles();
		landTilemap?.ClearAllTiles();
		grassTilemap?.ClearAllTiles();
		oceanOverlayTilemap?.ClearAllTiles();
		oceanFloorShallowTilemap?.ClearAllTiles();
		oceanFloorMediumTilemap?.ClearAllTiles();
		oceanFloorDeepTilemap?.ClearAllTiles();
		dockTilemap?.ClearAllTiles();
	}

	// ------------------------------------------------------------------
	// CENTER DOCK — always 2x2 (configurable in settings)
	// ------------------------------------------------------------------
	private void PaintDock()
	{
		if (dockTilemap == null || settings.dockTile == null) return;
		dockTilemap.ClearAllTiles();

		int size = settings.mapSize;
		int halfW = settings.dockWidth / 2;
		int halfH = settings.dockHeight / 2;
		var origin = new Vector3Int(-size / 2, -size / 2, 0);

		for (int dy = -halfH; dy <= halfH; dy++)
			for (int dx = -halfW; dx <= halfW; dx++)
			{
				int tx = size / 2 + dx;
				int ty = size / 2 + dy;
				dockTilemap.SetTile(origin + new Vector3Int(tx, ty, 0), settings.dockTile);
			}
	}

	// ------------------------------------------------------------------
	// DECORATOR SPAWNING — biome-aware, pooled
	// ------------------------------------------------------------------
	private void SpawnDecorators(ref MapRngV3 rng)
	{
		MapDataV3 d = _w.data;
		int size = d.size;

		_w.palmCandidates.Clear();
		_w.palmAccepted.Clear();
		_w.rockCandidates.Clear();
		_w.rockAccepted.Clear();

		// Collect candidates
		for (int y = 0; y < size; y++)
		{
			int row = y * size;
			for (int x = 0; x < size; x++)
			{
				int i = row + x;
				if (d.palmTile[i] == 1) _w.palmCandidates.Add(i);
				if (d.rockTile[i] == 1) _w.rockCandidates.Add(i);
			}
		}

		// Shuffle
		Shuffle(_w.palmCandidates, ref rng);
		Shuffle(_w.rockCandidates, ref rng);

		Transform pp = palmParent != null ? palmParent : transform;
		Transform rp = rockParent != null ? rockParent : transform;

		Vector3Int start = new Vector3Int(-size / 2, -size / 2, 0);

		// Palms
		foreach (int idx in _w.palmCandidates)
		{
			var bdef = settings.GetBiome((BiomeType)d.biome[idx]);
			if (bdef == null || bdef.palmPrefab == null) continue;
			if (rng.Next01() > bdef.palmSpawnChance) continue;

			int x = idx % size, y = idx / size;
			if (!CheckMinDistance(_w.palmAccepted, x, y, bdef.palmMinDistance)) continue;

			_w.palmAccepted.Add(new Vector2Int(x, y));
			Vector3 pos = GetWorldPos(grassTilemap ?? landTilemap, start, x, y);
			GetPooled(_palmPool, bdef.palmPrefab, pp).transform.position = pos;
		}

		// Rocks
		foreach (int idx in _w.rockCandidates)
		{
			var bdef = settings.GetBiome((BiomeType)d.biome[idx]);
			if (bdef == null || bdef.rockPrefab == null) continue;
			if (rng.Next01() > bdef.rockSpawnChance) continue;

			int x = idx % size, y = idx / size;
			if (!CheckMinDistance(_w.rockAccepted, x, y, settings.rockMinDistance)) continue;

			_w.rockAccepted.Add(new Vector2Int(x, y));
			Vector3 pos = GetWorldPos(landTilemap, start, x, y);
			GetPooled(_rockPool, bdef.rockPrefab, rp).transform.position = pos;
		}
	}

	// ------------------------------------------------------------------
	// Helpers
	// ------------------------------------------------------------------
	private static Vector3 GetWorldPos(Tilemap tm, Vector3Int start, int x, int y)
	{
		if (tm == null) return new Vector3(x, y, 0);
		Vector3Int cell = start + new Vector3Int(x, y, 0);
		return tm.GetCellCenterWorld(cell) + new Vector3(0f, tm.cellSize.y, 0f);
	}

	private static bool CheckMinDistance(List<Vector2Int> accepted, int x, int y, int minD)
	{
		int minD2 = minD * minD;
		for (int a = 0; a < accepted.Count; a++)
		{
			int dx = accepted[a].x - x, dy = accepted[a].y - y;
			if (dx * dx + dy * dy < minD2) return false;
		}
		return true;
	}

	private GameObject GetPooled(Stack<GameObject> pool, GameObject prefab, Transform parent)
	{
		GameObject go;
		if (pool.Count > 0 && (go = pool.Pop()) != null)
		{
			go.transform.SetParent(parent, false);
			go.SetActive(true);
		}
		else
		{
			go = Instantiate(prefab, parent);
		}
		var list = pool == _palmPool ? _activePalms : _activeRocks;
		list.Add(go);
		return go;
	}

	private void ReturnAllToPool()
	{
		ReturnList(_activePalms, _palmPool);
		ReturnList(_activeRocks, _rockPool);
	}

	private static void ReturnList(List<GameObject> list, Stack<GameObject> pool)
	{
		for (int i = list.Count - 1; i >= 0; i--)
		{
			if (list[i] == null) continue;
			list[i].SetActive(false);
			pool.Push(list[i]);
		}
		list.Clear();
	}

	private static void Shuffle(List<int> list, ref MapRngV3 rng)
	{
		for (int i = list.Count - 1; i > 0; i--)
		{
			int j = rng.NextInt(0, i + 1);
			(list[i], list[j]) = (list[j], list[i]);
		}
	}
}