using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Tilemaps;

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

	[Tooltip("The Grid GameObject that is the parent of all Tilemaps.")]
	public Grid tilemapGrid;

	public enum MapSizePreset { _256 = 0, _512 = 1, _1024 = 2, _2048 = 3 }

	[Header("Settings")]
	public MapSettingsV3[] sizePresets;
	public MapSizePreset selectedPreset = MapSizePreset._512;

	public MapSettingsV3 settings => (sizePresets != null && sizePresets.Length > (int)selectedPreset)
		? sizePresets[(int)selectedPreset]
		: null;

	[Header("Decorator Parents")]
	[Tooltip("Must NOT be a child of tilemapGrid.")]
	public Transform palmParent;
	[Tooltip("Must NOT be a child of tilemapGrid.")]
	public Transform rockParent;

	private readonly MapWorkspaceV3 _w = new MapWorkspaceV3();
	private readonly List<GameObject> _activePalms = new List<GameObject>();
	private readonly List<GameObject> _activeRocks = new List<GameObject>();
	private readonly Stack<GameObject> _palmPool = new Stack<GameObject>();
	private readonly Stack<GameObject> _rockPool = new Stack<GameObject>();

	private bool _generating = false;

	private void Start() => Regenerate();

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
		if (settings == null || _generating) return;
		StartCoroutine(RegenerateAsync(seed));
	}

	private IEnumerator RegenerateAsync(uint seed)
	{
		_generating = true;

		_w.Ensure(settings.mapSize, Mathf.Max(0, settings.pad));
		ReturnAllToPool();

		var w = _w;
		var s = settings;
		var rng = new MapRngV3(seed);
		bool threadDone = false;
		System.Exception threadException = null;
		Vector2 seaweedOffset = Vector2.zero;

		Thread genThread = new Thread(() =>
		{
			try
			{
				Vector2 baseOffset;
				MapNoiseV3.GenerateHeight(w.data, s, ref rng, w, out baseOffset, out seaweedOffset);
				MapMasksV3.ThresholdLand(w.data, s);
				MapMasksV3.CellularAutomata(w.data, s, w.morphTmp);
				MapMasksV3.MorphologyClosing(w.data, s.morphologyClosingIterations, w.morphTmp);
				MapMasksV3.RemoveSmallIslands(w.data, s.minIslandTiles, w);
				MapMasksV3.FillSmallLakes(w.data, s.minLakeTiles, w);
				MapDistanceFieldV3.Compute(w.data, w.bfsQueue);
				MapMasksV3.PropagateIslandBiomes(w.data, w, s);
				MapBiomesV3.BuildBands(w.data, s, ref rng, w);
				MapDecoratorsV3.PlaceAll(w.data, s, seaweedOffset);
			}
			catch (System.Exception e) { threadException = e; }
			finally { threadDone = true; }
		}, 64 * 1024 * 1024);

		genThread.IsBackground = true;
		genThread.Start();

		while (!threadDone)
			yield return null;

		if (threadException != null)
		{
			Debug.LogError($"MapGeneratorV3: generation thread failed: {threadException.GetType().Name}: {threadException.Message}\n{threadException.StackTrace}");
			_generating = false;
			yield break;
		}

		_w.chunks = MapChunkBuilderV3.Build(_w.data, settings, _w);
		_w.chunkSize = Mathf.Max(1, settings.chunkSize);
		_w.chunkCols = Mathf.CeilToInt((float)settings.mapSize / _w.chunkSize);
		_w.chunkRows = Mathf.CeilToInt((float)settings.mapSize / _w.chunkSize);

		int half = settings.mapSize / 2;
		int padOff = Mathf.Max(0, settings.pad);
		if (tilemapGrid != null)
			tilemapGrid.transform.position = new Vector3(-half - padOff, -half - padOff, 0f);

		waterTilemap?.ClearAllTiles();
		landTilemap?.ClearAllTiles();
		grassTilemap?.ClearAllTiles();
		oceanOverlayTilemap?.ClearAllTiles();
		oceanFloorShallowTilemap?.ClearAllTiles();
		oceanFloorMediumTilemap?.ClearAllTiles();
		oceanFloorDeepTilemap?.ClearAllTiles();

		PaintDock();
		SpawnDecorators(ref rng);

		var streamer = GetComponent<MapChunkStreamerV3>();
		if (streamer != null)
			streamer.OnMapRegenerated(_w, _w.data, settings);

		var cam = FindFirstObjectByType<CameraMovement>();
		if (cam != null)
			cam.SetBoundsFromMapData(_w.data, waterTilemap ?? landTilemap);

		_generating = false;
	}

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

	private void PaintDock()
	{
		if (dockTilemap == null || settings.dockTile == null) return;
		dockTilemap.ClearAllTiles();

		int size = settings.mapSize;
		int halfW = settings.dockWidth / 2;
		int halfH = settings.dockHeight / 2;
		int dockW = halfW * 2 + 1;
		int dockH = halfH * 2 + 1;
		int count = dockW * dockH;

		TileBase[] buf = _w.paintBuffer;
		for (int k = 0; k < count; k++) buf[k] = settings.dockTile;

		int startX = size / 2 - halfW;
		int startY = size / 2 - halfH;
		var bounds = new BoundsInt(new Vector3Int(startX, startY, 0), new Vector3Int(dockW, dockH, 1));
		dockTilemap.SetTilesBlock(bounds, buf);
	}

	private void SpawnDecorators(ref MapRngV3 rng)
	{
		MapDataV3 d = _w.data;
		int size = d.size;

		_w.palmCandidates.Clear();
		_w.palmAccepted.Clear();
		_w.rockCandidates.Clear();
		_w.rockAccepted.Clear();

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

		Shuffle(_w.palmCandidates, ref rng);
		Shuffle(_w.rockCandidates, ref rng);

		int palmMinDist = 0;
		if (settings.biomeDefinitions != null && settings.biomeDefinitions.Length > 0)
			palmMinDist = settings.biomeDefinitions[0].palmMinDistance;

		float palmCell = Mathf.Max(1f, palmMinDist / 1.41421356f);
		float rockCell = Mathf.Max(1f, settings.rockMinDistance / 1.41421356f);
		int palmGW = Mathf.CeilToInt(size / palmCell);
		int palmGH = Mathf.CeilToInt(size / palmCell);
		int rockGW = Mathf.CeilToInt(size / rockCell);
		int rockGH = Mathf.CeilToInt(size / rockCell);

		int palmGridSize = Mathf.Max(palmGW * palmGH, rockGW * rockGH);
		if (_w.palmGrid == null || _w.palmGrid.Length < palmGridSize)
		{
			_w.palmGrid = new int[palmGridSize];
			_w.rockGrid = new int[palmGridSize];
		}
		for (int k = 0; k < palmGW * palmGH; k++) _w.palmGrid[k] = -1;
		for (int k = 0; k < rockGW * rockGH; k++) _w.rockGrid[k] = -1;

		Transform pp = palmParent != null ? palmParent : transform;
		Transform rp = rockParent != null ? rockParent : transform;

		foreach (int idx in _w.palmCandidates)
		{
			var bdef = settings.GetBiome((BiomeType)d.biome[idx]);
			if (bdef == null || bdef.palmPrefab == null) continue;
			if (rng.Next01() > bdef.palmSpawnChance) continue;

			int x = idx % size;
			int y = idx / size;
			int minD = bdef.palmMinDistance;
			if (!CheckMinDistanceGrid(_w.palmGrid, x, y, minD, palmCell, palmGW, palmGH)) continue;

			int gcx = Mathf.Clamp((int)(x / palmCell), 0, palmGW - 1);
			int gcy = Mathf.Clamp((int)(y / palmCell), 0, palmGH - 1);
			_w.palmGrid[gcy * palmGW + gcx] = idx;
			_w.palmAccepted.Add(new Vector2Int(x, y));

			GetPooled(_palmPool, bdef.palmPrefab, pp).transform.position = GetWorldPos(x, y);
		}

		foreach (int idx in _w.rockCandidates)
		{
			var bdef = settings.GetBiome((BiomeType)d.biome[idx]);
			if (bdef == null || bdef.rockPrefab == null) continue;
			if (rng.Next01() > bdef.rockSpawnChance) continue;

			int x = idx % size;
			int y = idx / size;
			int minD = settings.rockMinDistance;
			if (!CheckMinDistanceGrid(_w.rockGrid, x, y, minD, rockCell, rockGW, rockGH)) continue;

			int gcx = Mathf.Clamp((int)(x / rockCell), 0, rockGW - 1);
			int gcy = Mathf.Clamp((int)(y / rockCell), 0, rockGH - 1);
			_w.rockGrid[gcy * rockGW + gcx] = idx;
			_w.rockAccepted.Add(new Vector2Int(x, y));

			GetPooled(_rockPool, bdef.rockPrefab, rp).transform.position = GetWorldPos(x, y);
		}
	}

	private bool CheckMinDistanceGrid(int[] grid, int x, int y, int minD,
		float cellSize, int gw, int gh)
	{
		int minD2 = minD * minD;
		int gcx = Mathf.Clamp((int)(x / cellSize), 0, gw - 1);
		int gcy = Mathf.Clamp((int)(y / cellSize), 0, gh - 1);
		int rCells = Mathf.CeilToInt(minD / cellSize) + 1;
		MapDataV3 d = _w.data;
		int size = d.size;

		for (int gy = gcy - rCells; gy <= gcy + rCells; gy++)
		{
			if ((uint)gy >= (uint)gh) continue;
			int grow = gy * gw;
			for (int gx = gcx - rCells; gx <= gcx + rCells; gx++)
			{
				if ((uint)gx >= (uint)gw) continue;
				int stored = grid[grow + gx];
				if (stored < 0) continue;
				int ox = stored % size, oy = stored / size;
				int dx = ox - x, dy = oy - y;
				if (dx * dx + dy * dy < minD2) return false;
			}
		}
		return true;
	}

	private Vector3 GetWorldPos(int x, int y)
	{
		Vector3 gridOrigin = tilemapGrid != null
			? tilemapGrid.transform.position
			: Vector3.zero;
		return new Vector3(gridOrigin.x + x + 0.5f, gridOrigin.y + y + 0.5f, 0f);
	}

	private GameObject GetPooled(Stack<GameObject> pool, GameObject prefab, Transform parent)
	{
		GameObject go;
		if (pool.Count > 0 && (go = pool.Pop()) != null)
		{
			go.transform.SetParent(parent, true);
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