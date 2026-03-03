using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapGeneratorControllerV2 : MonoBehaviour
{
	[Header("Tilemaps")]
	public Tilemap waterTilemap;
	public Tilemap landTilemap;
	public Tilemap grassTilemap;
	public Tilemap oceanOverlayTilemap;
	public Tilemap oceanFloorShallowTilemap;
	public Tilemap oceanFloorMediumTilemap;
	public Tilemap oceanFloorDeepTilemap;

	[Header("Start Platform Tilemap")]
	public Tilemap startPlatformTilemap;

	[Header("Settings")]
	public TilemapGenerationSettingsV2 settings;

	[Header("Palm Trees")]
	public GameObject palmTreePrefab;
	public Transform palmTreeParent;

	private readonly List<GameObject> activePalms = new List<GameObject>();
	private readonly Stack<GameObject> palmPool = new Stack<GameObject>();

	private readonly List<int> palmCandidates = new List<int>(8192);
	private readonly List<Vector2Int> palmAccepted = new List<Vector2Int>(256);

	private readonly MapGenWorkspace w = new MapGenWorkspace();

	private void Start()
	{
		Regenerate();
	}

	public void Regenerate()
	{
		if (settings == null) return;

		uint seed = settings.useRandomSeed
			? (uint)System.DateTime.UtcNow.Ticks
			: MapGenRng.HashSeed(settings.seedInput);

		Regenerate(seed);
	}

	public void Regenerate(uint seed)
	{
		if (settings == null) return;

		w.Ensure(settings.mapSize, Mathf.Max(0, settings.pad));

		DeactivateAllPalms();

		MapGenRng rng = new MapGenRng(seed);

		Vector2 baseOffset, seaweedOffset;
		MapGenNoise.GenerateHeight(w.data, settings, ref rng, w, out baseOffset, out seaweedOffset);

		MapGenMasks.ThresholdLand(w.data, settings);
		MapGenMasks.MorphologyClosing(w.data, settings.morphologyClosingIterations, w.morphTmp);
		MapGenMasks.RemoveSmallIslands(w.data, settings.minIslandTiles, w);
		MapGenMasks.FillSmallLakes(w.data, settings.minLakeTiles, w);

		MapGenDistanceField.ComputeCoastDistance(w.data, w.bfsQueue);

		MapGenBiomes.BuildBands(w.data, settings, ref rng);
		MapGenBiomes.PlaceSeaweed(w.data, settings, seaweedOffset);

		MapGenPainter.PaintAll(
			w.data, settings,
			waterTilemap, landTilemap, grassTilemap,
			oceanOverlayTilemap,
			oceanFloorShallowTilemap, oceanFloorMediumTilemap, oceanFloorDeepTilemap,
			w
		);

		PaintStartPlatform();

		Transform parent = palmTreeParent != null ? palmTreeParent : transform;
		SpawnPalms(w.data, settings, grassTilemap, palmTreePrefab, parent, ref rng);
	}

	public void ClearAll()
	{
		DeactivateAllPalms();

		if (waterTilemap != null) waterTilemap.ClearAllTiles();
		if (landTilemap != null) landTilemap.ClearAllTiles();
		if (grassTilemap != null) grassTilemap.ClearAllTiles();
		if (oceanOverlayTilemap != null) oceanOverlayTilemap.ClearAllTiles();
		if (oceanFloorShallowTilemap != null) oceanFloorShallowTilemap.ClearAllTiles();
		if (oceanFloorMediumTilemap != null) oceanFloorMediumTilemap.ClearAllTiles();
		if (oceanFloorDeepTilemap != null) oceanFloorDeepTilemap.ClearAllTiles();
		if (startPlatformTilemap != null) startPlatformTilemap.ClearAllTiles();
	}

	private void PaintStartPlatform()
	{
		if (startPlatformTilemap == null) return;
		if (settings == null) return;
		if (settings.startPlatformTile == null) return;

		startPlatformTilemap.ClearAllTiles();

		int size = settings.mapSize;

		int w = Mathf.Max(1, settings.startPlatformWidth);
		int h = Mathf.Max(1, settings.startPlatformHeight);

		int halfW = w / 2;
		int halfH = h / 2;

		int cx = size / 2;
		int cy = size / 2;

		Vector3Int origin = new Vector3Int(-size / 2, -size / 2, 0);

		for (int y = -halfH; y <= halfH; y++)
		{
			for (int x = -halfW; x <= halfW; x++)
			{
				int tx = cx + x;
				int ty = cy + y;
				Vector3Int cell = origin + new Vector3Int(tx, ty, 0);
				startPlatformTilemap.SetTile(cell, settings.startPlatformTile);
			}
		}
	}

	private void SpawnPalms(
		MapGenData d,
		TilemapGenerationSettingsV2 s,
		Tilemap grass,
		GameObject prefab,
		Transform parent,
		ref MapGenRng rng)
	{
		if (prefab == null) return;
		if (grass == null) return;

		int size = d.size;

		palmCandidates.Clear();
		palmAccepted.Clear();

		for (int y = 0; y < size; y++)
		{
			int row = y * size;
			for (int x = 0; x < size; x++)
			{
				int i = row + x;
				if (d.grass[i] != 1) continue;

				int cd = d.coastDist[i];
				if (cd < s.palmCoastMin || cd > s.palmCoastMax) continue;

				palmCandidates.Add(i);
			}
		}

		for (int i = palmCandidates.Count - 1; i > 0; i--)
		{
			int j = rng.NextInt(0, i + 1);
			int t = palmCandidates[i];
			palmCandidates[i] = palmCandidates[j];
			palmCandidates[j] = t;
		}

		int minD = Mathf.Max(1, s.palmMinDistance);
		int minD2 = minD * minD;

		Vector3Int start = new Vector3Int(-size / 2, -size / 2, 0);

		for (int c = 0; c < palmCandidates.Count; c++)
		{
			if (rng.Next01() > s.palmSpawnChance) continue;

			int idx = palmCandidates[c];
			int x = idx % size;
			int y = idx / size;

			bool ok = true;
			for (int a = 0; a < palmAccepted.Count; a++)
			{
				int dx = palmAccepted[a].x - x;
				int dy = palmAccepted[a].y - y;
				if (dx * dx + dy * dy < minD2) { ok = false; break; }
			}
			if (!ok) continue;

			palmAccepted.Add(new Vector2Int(x, y));

			Vector3Int cell = start + new Vector3Int(x, y, 0);
			Vector3 pos = grass.GetCellCenterWorld(cell);
			pos += new Vector3(0f, grass.cellSize.y, 0f);

			GameObject go = GetPalm(prefab, parent);
			go.transform.position = pos;
			go.transform.rotation = Quaternion.identity;
			activePalms.Add(go);
		}
	}

	private GameObject GetPalm(GameObject prefab, Transform parent)
	{
		if (palmPool.Count > 0)
		{
			GameObject go = palmPool.Pop();
			if (go == null) return Instantiate(prefab, parent);
			go.transform.SetParent(parent, false);
			go.SetActive(true);
			return go;
		}
		return Instantiate(prefab, parent);
	}

	private void DeactivateAllPalms()
	{
		for (int i = activePalms.Count - 1; i >= 0; i--)
		{
			GameObject go = activePalms[i];
			if (go == null) continue;
			go.SetActive(false);
			palmPool.Push(go);
		}
		activePalms.Clear();
	}
}