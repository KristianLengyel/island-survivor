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

	[Header("Mini-Map Colors")]
	public Color shallowOceanColor = new Color(0.27f, 0.60f, 0.85f);
	public Color mediumOceanColor  = new Color(0.18f, 0.45f, 0.75f);
	public Color deepOceanColor    = new Color(0.10f, 0.28f, 0.60f);
	public Color dockColor         = new Color(0.55f, 0.38f, 0.20f);

	[Header("References")]
	public MapDisplayManager mapDisplayManager;

	public MapSettingsV3 settings => (sizePresets != null && sizePresets.Length > (int)selectedPreset)
		? sizePresets[(int)selectedPreset]
		: null;

	private readonly MapWorkspaceV3 _w = new MapWorkspaceV3();

	private bool _generating = false;
	private uint _lastSeed;
	private uint _forcedSeed;

	public uint LastSeed => _lastSeed;
	public bool IsGenerating => _generating;

	/// <summary>Forces the next Regenerate() call to use this seed instead of a random one. Consumed after one use.</summary>
	public void ForceSeed(uint seed) { _forcedSeed = seed; }

	private void Start() => Regenerate();

	public void Regenerate()
	{
		if (settings == null) return;
		uint seed;
		if (_forcedSeed != 0)
		{
			seed = _forcedSeed;
			_forcedSeed = 0;
		}
		else
		{
			seed = settings.useRandomSeed
				? (uint)System.DateTime.UtcNow.Ticks
				: MapRngV3.HashSeed(settings.seedInput);
		}
		Regenerate(seed);
	}

	public void Regenerate(uint seed)
	{
		if (settings == null || _generating) return;
		_lastSeed = seed;
		StartCoroutine(RegenerateAsync(seed));
	}

	private IEnumerator RegenerateAsync(uint seed)
	{
		_generating = true;

		_w.Ensure(settings.mapSize, Mathf.Max(0, settings.pad));

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

		// Build chunks
		_w.chunks = MapChunkBuilderV3.Build(_w.data, settings, _w);
		_w.chunkSize = Mathf.Max(1, settings.chunkSize);
		_w.chunkCols = Mathf.CeilToInt((float)settings.mapSize / _w.chunkSize);
		_w.chunkRows = Mathf.CeilToInt((float)settings.mapSize / _w.chunkSize);

		// Build per-chunk decorator index lists (fast, no GameObjects yet)
		BuildChunkDecoratorLists(ref rng);

		// Reposition grid
		int half = settings.mapSize / 2;
		int padOff = Mathf.Max(0, settings.pad);
		if (tilemapGrid != null)
			tilemapGrid.transform.position = new Vector3(-half - padOff, -half - padOff, 0f);

		// Clear all tilemaps
		waterTilemap?.ClearAllTiles();
		landTilemap?.ClearAllTiles();
		grassTilemap?.ClearAllTiles();
		oceanOverlayTilemap?.ClearAllTiles();
		oceanFloorShallowTilemap?.ClearAllTiles();
		oceanFloorMediumTilemap?.ClearAllTiles();
		oceanFloorDeepTilemap?.ClearAllTiles();

		PaintDock();

		// Hand off to streamer — it handles all chunk painting and decorator spawning
		var streamer = GetComponent<MapChunkStreamerV3>();
		if (streamer != null)
			streamer.OnMapRegenerated(_w, _w.data, settings);

		PopulateMapColors(_w.data, settings);
		mapDisplayManager?.ResetForNewMap();

		var cam = FindFirstObjectByType<CameraMovement>();
		if (cam != null)
			cam.SetBoundsFromMapData(_w.data, waterTilemap ?? landTilemap);

		_generating = false;
	}

	/// <summary>
	/// Walks all tiles once and bins each palm/rock candidate into its owning chunk's list.
	/// Uses Poisson-disk min-distance filtering (same as old SpawnDecorators) but stores
	/// positions as data only — no GameObjects created here.
	/// </summary>
	private void BuildChunkDecoratorLists(ref MapRngV3 rng)
	{
		int chunkCount = _w.chunks.Length;
		_w.EnsureChunkDecoratorStorage(chunkCount);
		_w.ClearChunkDecoratorStorage(chunkCount);

		MapDataV3 d = _w.data;
		int size = d.size;

		// ---- Collect & shuffle candidates ----
		_w.palmCandidates.Clear();
		_w.rockCandidates.Clear();

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

		// ---- Palm min-distance grid ----
		int palmMinDist = 6;
		if (settings.biomeDefinitions != null && settings.biomeDefinitions.Length > 0)
			palmMinDist = settings.biomeDefinitions[0].palmMinDistance;

		float palmCell = Mathf.Max(1f, palmMinDist / 1.41421356f);
		int palmGW = Mathf.CeilToInt(size / palmCell);
		int palmGH = Mathf.CeilToInt(size / palmCell);

		float rockCell = Mathf.Max(1f, settings.rockMinDistance / 1.41421356f);
		int rockGW = Mathf.CeilToInt(size / rockCell);
		int rockGH = Mathf.CeilToInt(size / rockCell);

		int gridNeed = Mathf.Max(palmGW * palmGH, rockGW * rockGH);
		if (_w.palmGrid == null || _w.palmGrid.Length < gridNeed)
		{
			_w.palmGrid = new int[gridNeed];
			_w.rockGrid = new int[gridNeed];
		}
		for (int k = 0; k < palmGW * palmGH; k++) _w.palmGrid[k] = -1;
		for (int k = 0; k < rockGW * rockGH; k++) _w.rockGrid[k] = -1;

		// ---- Filter palms and bin into chunks ----
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

			int chunkIdx = _w.GetChunkIndex(x, y);
			if (chunkIdx >= 0)
				_w.chunkPalmIndices[chunkIdx].Add(idx);
		}

		// ---- Filter rocks and bin into chunks ----
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

			int chunkIdx = _w.GetChunkIndex(x, y);
			if (chunkIdx >= 0)
				_w.chunkRockIndices[chunkIdx].Add(idx);
		}
	}

	private void PopulateMapColors(MapDataV3 d, MapSettingsV3 s)
	{
		if (MapManager.Instance == null) return;

		int size = d.size;
		MapManager.Instance.InitializeWorld(size);

		for (int y = 0; y < size; y++)
		{
			for (int x = 0; x < size; x++)
			{
				int i = d.Idx(x, y);
				Color c;

				if (d.land[i] == 1)
				{
					var bdef = s.GetBiome((BiomeType)d.biome[i]);
					c = (bdef != null)
						? (d.grass[i] == 1 ? bdef.mapGrassColor : bdef.mapLandColor)
						: Color.green;
				}
				else
				{
					c = d.oceanBand[i] switch
					{
						1 => shallowOceanColor,
						2 => mediumOceanColor,
						_ => deepOceanColor,
					};
				}

				MapManager.Instance.tileColors[x, y] = c;
			}
		}

		// Paint dock over the ocean tiles at map center
		if (s.dockTile != null)
		{
			int half = size / 2;
			int halfW = s.dockWidth / 2;
			int halfH = s.dockHeight / 2;
			int startX = half - halfW;
			int startY = half - halfH;
			int dockW  = halfW * 2 + 1;
			int dockH  = halfH * 2 + 1;

			for (int dy = 0; dy < dockH; dy++)
			{
				for (int dx = 0; dx < dockW; dx++)
				{
					int tx = startX + dx;
					int ty = startY + dy;
					if (tx >= 0 && tx < size && ty >= 0 && ty < size)
						MapManager.Instance.tileColors[tx, ty] = dockColor;
				}
			}
		}
	}

	public void ClearAll()
	{
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

	private bool CheckMinDistanceGrid(int[] grid, int x, int y, int minD,
		float cellSize, int gw, int gh)
	{
		int minD2 = minD * minD;
		int gcx = Mathf.Clamp((int)(x / cellSize), 0, gw - 1);
		int gcy = Mathf.Clamp((int)(y / cellSize), 0, gh - 1);
		int rCells = Mathf.CeilToInt(minD / cellSize) + 1;
		int size = _w.data.size;

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
				int dx = ox - x, dy2 = oy - y;
				if (dx * dx + dy2 * dy2 < minD2) return false;
			}
		}
		return true;
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