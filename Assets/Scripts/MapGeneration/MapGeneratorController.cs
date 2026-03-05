using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapGeneratorController : MonoBehaviour
{
	[Header("Tilemap References")]
	public Tilemap waterTilemap;
	public Tilemap landTilemap;
	public Tilemap grassTilemap;
	public Tilemap buildingTilemap;
	public Tilemap oceanOverlayTilemap;
	public Tilemap betweenOceanFloorAndWaterTilemap;
	public Tilemap oceanFloorShallowTilemap;
	public Tilemap oceanFloorMediumTilemap;
	public Tilemap oceanFloorDeepTilemap;

	[Header("Camera Movement (Optional)")]
	public CameraMovement cameraMovement;

	[Header("Map Settings")]
	public TilemapGenerationSettings settings;

	[Header("Palm Tree Spawning")]
	public GameObject palmTreePrefab;
	[Range(0f, 1f)] public float palmTreeChance = 0.15f;
	[Min(1)] public int palmTreeFootprint = 3;
	[Min(0)] public int palmTreeMinSpacing = 2;
	public Transform palmTreeParent;

	// --- Internal state ---
	private NoiseGenerator noiseGenerator;
	private TilemapBuilder tilemapBuilder;
	private float[,] heightmap;
	private bool[,] originallySand;

	// Cached tile state — shared across all passes to avoid GetTile() calls
	private bool[,] isLand;
	private bool[,] isWater;

	private readonly List<GameObject> spawnedPalmTrees = new List<GameObject>();

	private void Awake()
	{
		noiseGenerator = new NoiseGenerator();
		tilemapBuilder = new TilemapBuilder();
	}

	private void Start()
	{
		GenerateAndBuildMap();
	}

	public void RegenerateMap()
	{
		ClearAllTilemaps();
		GenerateAndBuildMap();
	}

	private void GenerateAndBuildMap()
	{
		InitializeSeed();
		MapManager.Instance.InitializeWorld(settings.mapSize);

		// 1. Generate noise
		var noiseMaps = noiseGenerator.GenerateHeightmap(
			settings.mapSize, settings.scale, settings.offsetX, settings.offsetY,
			settings.numOctaves, settings.persistence, settings.lacunarity,
			settings.seaweedScale, settings.offsetX + 5000f, settings.offsetY + 5000f
		);
		heightmap = noiseMaps.heightmap;

		// 2. Build base tilemap — returns updated heightmap and caches land/water arrays
		heightmap = tilemapBuilder.BuildTilemap(
			waterTilemap, landTilemap, buildingTilemap,
			oceanOverlayTilemap, betweenOceanFloorAndWaterTilemap,
			heightmap, settings
		);

		originallySand = tilemapBuilder.GetOriginallySand();

		// Grab cached arrays — all subsequent passes use these instead of GetTile()
		isLand = tilemapBuilder.GetIsLand();
		isWater = tilemapBuilder.GetIsWater();

		// 3. Save debug heightmap
		HeightmapIO.SaveFloatArray("Heightmaps", "water_heightmap.txt", heightmap);
		HeightmapIO.SaveHeightmapTexture("Heightmaps", "heightmap", heightmap);

		// 4. Cleanup passes — all update isLand/isWater arrays as they go
		MapCleanup.RemoveSmallIslands(
			landTilemap, waterTilemap, settings.sandTile, settings.waterTile,
			isLand, isWater, settings.mapSize, settings.minIslandSize);

		MapCleanup.PerformWaterCleanup(
			landTilemap, waterTilemap, originallySand, isLand, isWater,
			settings.sandTile, settings.waterTile, settings.mapSize);

		MapCleanup.FillInlandWater(
			landTilemap, waterTilemap, settings.sandTile, settings.waterTile,
			isLand, isWater, settings.mapSize);

		MapCleanup.RemoveSmallLakes(
			landTilemap, waterTilemap, settings.sandTile, settings.waterTile,
			isLand, isWater, settings.mapSize, settings.minLakeSize);


		// Final pass — remove water tiles with too few water neighbors (no valid sprite)
		MapCleanup.RemoveIsolatedWaterTiles(
			landTilemap, waterTilemap, settings.sandTile,
			isLand, isWater, settings.mapSize, settings.minWaterNeighbors);

		// Expand sand border AFTER all cleanup passes so cleanup cannot eat it back.
		// Covers the shallow ocean floor halo visible around the island edge.
		if (settings.landExpansion > 0)
			tilemapBuilder.ExpandLandBorder(settings);

		// 5. Decor & structure passes
		PlaceSeaweedPatches(noiseMaps.seaweedMap);

		if (settings.placeCentralDock)
			PlaceCentralIsland();

		BuildMultiLayerOceanFloor();
		PlaceGrassOverlay();
		PlacePalmTreesOnGrass();
		AssignTileColorsToMapManager();

		//if (cameraMovement != null)
		//	cameraMovement.UpdateTilemapBounds();
	}

	// -------------------------------------------------------------------------
	// Seed
	// -------------------------------------------------------------------------

	private void InitializeSeed()
	{
		if (string.IsNullOrEmpty(settings.seedInput) || settings.useRandomSeed)
		{
			int seed = Random.Range(0, int.MaxValue);
			Random.InitState(seed);
		}
		else
		{
			int seed = settings.seedInput.GetHashCode();
			Random.InitState(seed);
		}

		settings.offsetX = Random.Range(0f, 10000f);
		settings.offsetY = Random.Range(0f, 10000f);
	}

	// -------------------------------------------------------------------------
	// Grass overlay
	// Uses HashSet<Vector3Int> for O(1) island membership checks
	// -------------------------------------------------------------------------

	private void PlaceGrassOverlay()
	{
		if (grassTilemap == null || settings.grassTile == null) return;

		int mapSize = settings.mapSize;
		Vector3Int startPosition = new Vector3Int(-mapSize / 2, -mapSize / 2, 0);
		bool[,] visited = new bool[mapSize, mapSize];

		for (int x = 0; x < mapSize; x++)
		{
			for (int y = 0; y < mapSize; y++)
			{
				if (visited[x, y] || !isLand[x, y]) continue;

				Vector3Int tilePos = startPosition + new Vector3Int(x, y, 0);
				List<Vector3Int> islandTiles = new List<Vector3Int>();
				int islandSize = FloodFillIsland(tilePos, visited, islandTiles, mapSize);

				if (islandSize >= settings.minIslandSizeForGrass)
				{
					// Build HashSet once per island — O(1) lookups inside PlaceGrassOnIsland
					HashSet<Vector3Int> islandSet = new HashSet<Vector3Int>(islandTiles);
					PlaceGrassOnIsland(islandTiles, islandSet);
				}
			}
		}
	}

	// Uses cached isLand array
	private int FloodFillIsland(
		Vector3Int startTile, bool[,] visited, List<Vector3Int> islandTiles, int mapSize)
	{
		int count = 0;
		Vector3Int offset = new Vector3Int(-mapSize / 2, -mapSize / 2, 0);
		Queue<Vector3Int> toExplore = new Queue<Vector3Int>();
		toExplore.Enqueue(startTile);

		Vector3Int[] neighbors = { Vector3Int.right, Vector3Int.left, Vector3Int.up, Vector3Int.down };

		while (toExplore.Count > 0)
		{
			Vector3Int current = toExplore.Dequeue();
			int x = current.x - offset.x;
			int y = current.y - offset.y;

			if (x < 0 || x >= mapSize || y < 0 || y >= mapSize || visited[x, y]) continue;

			if (isLand[x, y]) // cached array instead of GetTile
			{
				visited[x, y] = true;
				islandTiles.Add(current);
				count++;

				foreach (var n in neighbors)
				{
					Vector3Int neighbor = current + n;
					int nx = neighbor.x - offset.x;
					int ny = neighbor.y - offset.y;
					if (nx >= 0 && nx < mapSize && ny >= 0 && ny < mapSize && !visited[nx, ny])
						toExplore.Enqueue(neighbor);
				}
			}
		}

		return count;
	}

	// islandSet is a HashSet — Contains() is O(1) instead of O(n)
	private void PlaceGrassOnIsland(List<Vector3Int> islandTiles, HashSet<Vector3Int> islandSet)
	{
		if (grassTilemap == null || settings.grassTile == null) return;

		int mapSize = settings.mapSize;
		Vector3Int offset = new Vector3Int(-mapSize / 2, -mapSize / 2, 0);

		int minX = mapSize, maxX = 0, minY = mapSize, maxY = 0;
		foreach (var tilePos in islandTiles)
		{
			int x = tilePos.x - offset.x;
			int y = tilePos.y - offset.y;
			if (x < minX) minX = x;
			if (x > maxX) maxX = x;
			if (y < minY) minY = y;
			if (y > maxY) maxY = y;
		}

		if (maxX - minX < 2 || maxY - minY < 2) return;

		int centerX = (minX + maxX) / 2;
		int centerY = (minY + maxY) / 2;

		// grassCoverageFraction is now configurable in settings
		float frac = settings.grassCoverageFraction;
		int grassWidth = Mathf.Max(1, Mathf.RoundToInt((maxX - minX) * frac));
		int grassHeight = Mathf.Max(1, Mathf.RoundToInt((maxY - minY) * frac));

		List<Vector3Int> grassPositions = new List<Vector3Int>();

		for (int x = centerX - grassWidth / 2; x <= centerX + grassWidth / 2; x++)
		{
			for (int y = centerY - grassHeight / 2; y <= centerY + grassHeight / 2; y++)
			{
				Vector3Int tilePos = offset + new Vector3Int(x, y, 0);

				// O(1) HashSet lookup instead of O(n) List.Contains
				if (!islandSet.Contains(tilePos)) continue;

				// Use cached isWater instead of GetTile
				int gx = tilePos.x - offset.x;
				int gy = tilePos.y - offset.y;
				if (gx < 0 || gx >= settings.mapSize || gy < 0 || gy >= settings.mapSize) continue;
				if (isWater[gx, gy]) continue;

				if (IsWithinOffsetFromBorder(tilePos, islandSet, settings.grassBorderOffset))
					grassPositions.Add(tilePos);
			}
		}

		TileBase[] grassTiles = new TileBase[grassPositions.Count];
		for (int i = 0; i < grassTiles.Length; i++) grassTiles[i] = settings.grassTile;
		grassTilemap.SetTiles(grassPositions.ToArray(), grassTiles);
	}

	// O(1) per check thanks to HashSet
	private bool IsWithinOffsetFromBorder(
		Vector3Int tilePos, HashSet<Vector3Int> islandSet, int borderOffset)
	{
		Vector3Int[] directions = { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right };
		foreach (var dir in directions)
			for (int i = 1; i <= borderOffset; i++)
				if (!islandSet.Contains(tilePos + dir * i))
					return false;
		return true;
	}

	// -------------------------------------------------------------------------
	// Palm Trees
	// Caches grass tile positions into a bool array first — no GetTile in loops
	// -------------------------------------------------------------------------

	private void PlacePalmTreesOnGrass()
	{
		if (palmTreePrefab == null) return;
		if (grassTilemap == null || settings == null || settings.grassTile == null) return;
		if (palmTreeFootprint <= 0 || palmTreeFootprint % 2 == 0) return;

		int mapSize = settings.mapSize;
		int footprint = palmTreeFootprint;
		int spacing = Mathf.Max(0, palmTreeMinSpacing);
		int centerOffset = footprint / 2;
		Vector3Int start = new Vector3Int(-mapSize / 2, -mapSize / 2, 0);

		// Cache grass positions into a bool array — one pass, then O(1) lookups
		bool[,] isGrass = new bool[mapSize, mapSize];
		for (int x = 0; x < mapSize; x++)
		{
			for (int y = 0; y < mapSize; y++)
			{
				Vector3Int cell = start + new Vector3Int(x, y, 0);
				isGrass[x, y] = grassTilemap.GetTile(cell) == settings.grassTile;
			}
		}

		bool[,] blocked = new bool[mapSize, mapSize];

		for (int x = 0; x <= mapSize - footprint; x++)
		{
			for (int y = 0; y <= mapSize - footprint; y++)
			{
				if (Random.value > palmTreeChance) continue;

				bool canPlace = true;
				for (int dx = 0; dx < footprint && canPlace; dx++)
				{
					for (int dy = 0; dy < footprint; dy++)
					{
						int gx = x + dx, gy = y + dy;
						// Use cached bool arrays — no GetTile calls here
						if (blocked[gx, gy] || !isGrass[gx, gy] || !isLand[gx, gy])
						{
							canPlace = false;
							break;
						}
					}
				}

				if (!canPlace) continue;

				Vector3Int centerCell = start + new Vector3Int(x + centerOffset, y + centerOffset, 0);
				Vector3 worldPos = grassTilemap.GetCellCenterWorld(centerCell)
								 + new Vector3(0f, grassTilemap.cellSize.y, 0f);

				GameObject parent = palmTreeParent != null ? palmTreeParent.gameObject : gameObject;
				GameObject palm = Instantiate(palmTreePrefab, worldPos, Quaternion.identity, parent.transform);
				spawnedPalmTrees.Add(palm);

				int bx0 = Mathf.Max(0, x - spacing);
				int by0 = Mathf.Max(0, y - spacing);
				int bx1 = Mathf.Min(mapSize - 1, x + footprint - 1 + spacing);
				int by1 = Mathf.Min(mapSize - 1, y + footprint - 1 + spacing);

				for (int bx = bx0; bx <= bx1; bx++)
					for (int by = by0; by <= by1; by++)
						blocked[bx, by] = true;
			}
		}
	}

	// -------------------------------------------------------------------------
	// Ocean floor (multi-layer BFS + blur)
	// Uses cached isLand/isWater arrays — no GetTile calls
	// All thresholds and blur iterations come from settings
	// -------------------------------------------------------------------------

	private void BuildMultiLayerOceanFloor()
	{
		int mapSize = settings.mapSize;
		Vector3Int startPos = new Vector3Int(-mapSize / 2, -mapSize / 2, 0);

		float[,] distanceFromLand = new float[mapSize, mapSize];
		bool[,] visited = new bool[mapSize, mapSize];

		Queue<Vector3Int> queue = new Queue<Vector3Int>();

		// Seed BFS from land — use cached isLand array
		for (int x = 0; x < mapSize; x++)
		{
			for (int y = 0; y < mapSize; y++)
			{
				if (isLand[x, y])
				{
					distanceFromLand[x, y] = 0f;
					visited[x, y] = true;
					queue.Enqueue(new Vector3Int(x, y, 0));
				}
				else
				{
					distanceFromLand[x, y] = float.MaxValue;
				}
			}
		}

		int[] dx4 = { 0, 0, 1, -1 };
		int[] dy4 = { 1, -1, 0, 0 };

		while (queue.Count > 0)
		{
			Vector3Int current = queue.Dequeue();
			int cx = current.x, cy = current.y;
			float currentDist = distanceFromLand[cx, cy];

			for (int d = 0; d < 4; d++)
			{
				int nx = cx + dx4[d], ny = cy + dy4[d];
				if (nx < 0 || nx >= mapSize || ny < 0 || ny >= mapSize) continue;
				if (visited[nx, ny]) continue;
				visited[nx, ny] = true;
				distanceFromLand[nx, ny] = currentDist + 1f;
				queue.Enqueue(new Vector3Int(nx, ny, 0));
			}
		}

		// Blur — iterations and kernel size from settings
		float[,] smoothDistance = distanceFromLand;
		for (int iter = 0; iter < settings.oceanFloorBlurIterations; iter++)
		{
			float[,] temp = new float[mapSize, mapSize];
			for (int x = 0; x < mapSize; x++)
			{
				for (int y = 0; y < mapSize; y++)
				{
					float sum = 0f;
					int count = 0;
					for (int kx = -1; kx <= 1; kx++)
					{
						for (int ky = -1; ky <= 1; ky++)
						{
							int nx = x + kx, ny = y + ky;
							if (nx >= 0 && nx < mapSize && ny >= 0 && ny < mapSize)
							{
								sum += smoothDistance[nx, ny];
								count++;
							}
						}
					}
					temp[x, y] = sum / count;
				}
			}
			smoothDistance = temp;
		}

		// Thresholds from settings
		float islandThresh = settings.shallowOceanThreshold;
		float shallowThresh = settings.mediumOceanThreshold;
		float mediumThresh = settings.deepOceanThreshold;

		List<Vector3Int> shallowPositions = new List<Vector3Int>();
		List<Vector3Int> mediumPositions = new List<Vector3Int>();
		List<Vector3Int> deepPositions = new List<Vector3Int>();

		for (int x = 0; x < mapSize; x++)
		{
			for (int y = 0; y < mapSize; y++)
			{
				Vector3Int tilePos = startPos + new Vector3Int(x, y, 0);
				float d = smoothDistance[x, y];

				if (d <= islandThresh)
				{
					// Shallow goes under land tiles intentionally (visual layering).
					// Medium and deep are skipped for land — they render above landTilemap
					// and would cause visible water-on-land artifacts.
					shallowPositions.Add(tilePos);
				}
				else if (d <= shallowThresh)
				{
					shallowPositions.Add(tilePos);
					mediumPositions.Add(tilePos);
					deepPositions.Add(tilePos);
				}
				else if (d <= mediumThresh)
				{
					mediumPositions.Add(tilePos);
					deepPositions.Add(tilePos);
				}
				else
				{
					deepPositions.Add(tilePos);
				}
			}
		}

		SetTilesFromList(oceanFloorDeepTilemap, deepPositions, settings.sandTile);
		SetTilesFromList(oceanFloorMediumTilemap, mediumPositions, settings.sandTile);
		SetTilesFromList(oceanFloorShallowTilemap, shallowPositions, settings.sandTile);
		// landTilemap intentionally omitted — TilemapBuilder already placed all land tiles.
	}

	private void SetTilesFromList(Tilemap tilemap, List<Vector3Int> positions, TileBase tile)
	{
		if (tilemap == null || positions.Count == 0) return;
		TileBase[] tiles = new TileBase[positions.Count];
		for (int i = 0; i < tiles.Length; i++) tiles[i] = tile;
		tilemap.SetTiles(positions.ToArray(), tiles);
	}

	// -------------------------------------------------------------------------
	// Seaweed — uses cached heightmap + isWater + isLand
	// -------------------------------------------------------------------------

	private void PlaceSeaweedPatches(float[,] seaweedMap)
	{
		if (oceanOverlayTilemap == null || settings.seaweedTile == null) return;

		int mapSize = settings.mapSize;
		Vector3Int startPos = new Vector3Int(-mapSize / 2, -mapSize / 2, 0);
		List<Vector3Int> seaweedPositions = new List<Vector3Int>();

		for (int x = 0; x < mapSize; x++)
		{
			for (int y = 0; y < mapSize; y++)
			{
				if (heightmap[x, y] < settings.seaweedMaxHeight
					&& seaweedMap[x, y] > settings.seaweedThreshold
					&& isWater[x, y]
					&& !isLand[x, y])
				{
					seaweedPositions.Add(startPos + new Vector3Int(x, y, 0));
				}
			}
		}

		TileBase[] seaweedTiles = new TileBase[seaweedPositions.Count];
		for (int i = 0; i < seaweedTiles.Length; i++) seaweedTiles[i] = settings.seaweedTile;
		oceanOverlayTilemap.SetTiles(seaweedPositions.ToArray(), seaweedTiles);
	}

	// -------------------------------------------------------------------------
	// Central dock — size and toggle from settings
	// -------------------------------------------------------------------------

	private void PlaceCentralIsland()
	{
		if (settings.woodenFloorTile == null) return;

		int mapSize = settings.mapSize;
		Vector3Int startPosition = new Vector3Int(-mapSize / 2, -mapSize / 2, 0);
		int centerX = mapSize / 2;
		int centerY = mapSize / 2;
		int dockSize = settings.dockSize;
		int half = dockSize / 2;

		List<Vector3Int> floorPositions = new List<Vector3Int>();
		List<Vector3Int> pillarPositions = new List<Vector3Int>();

		for (int x = centerX - half; x < centerX - half + dockSize; x++)
		{
			for (int y = centerY - half; y < centerY - half + dockSize; y++)
			{
				floorPositions.Add(startPosition + new Vector3Int(x, y, 0));
				pillarPositions.Add(startPosition + new Vector3Int(x, y - 1, 0));
			}
		}

		SetTilesFromList(buildingTilemap, floorPositions, settings.woodenFloorTile);

		if (settings.woodenPillarTile != null)
			SetTilesFromList(betweenOceanFloorAndWaterTilemap, pillarPositions, settings.woodenPillarTile);
	}

	// -------------------------------------------------------------------------
	// MapManager color assignment — uses cached isWater array
	// -------------------------------------------------------------------------

	private void AssignTileColorsToMapManager()
	{
		if (MapManager.Instance == null)
		{
			Debug.LogError("MapGeneratorController: MapManager instance not found!");
			return;
		}

		int mapSize = settings.mapSize;
		for (int x = 0; x < mapSize; x++)
		{
			for (int y = 0; y < mapSize; y++)
			{
				MapManager.Instance.tileColors[x, y] = isWater[x, y]
					? settings.waterColor
					: settings.sandColor;
			}
		}
	}

	// -------------------------------------------------------------------------
	// Cleanup
	// -------------------------------------------------------------------------

	private void ClearAllTilemaps()
	{
		ClearSpawnedPalmTrees();
		waterTilemap.ClearAllTiles();
		landTilemap.ClearAllTiles();
		buildingTilemap.ClearAllTiles();
		oceanOverlayTilemap.ClearAllTiles();
		betweenOceanFloorAndWaterTilemap.ClearAllTiles();
		grassTilemap.ClearAllTiles();
		oceanFloorShallowTilemap.ClearAllTiles();
		oceanFloorMediumTilemap.ClearAllTiles();
		oceanFloorDeepTilemap.ClearAllTiles();
	}

	private void ClearSpawnedPalmTrees()
	{
		for (int i = spawnedPalmTrees.Count - 1; i >= 0; i--)
			if (spawnedPalmTrees[i] != null)
				Destroy(spawnedPalmTrees[i]);
		spawnedPalmTrees.Clear();
	}

	// -------------------------------------------------------------------------
	// Editor Gizmos
	// -------------------------------------------------------------------------

	private void OnDrawGizmos()
	{
		if (settings == null) return;

		float halfSize = settings.mapSize / 2f;
		Vector3 pos = transform.position;

		// Map bounds
		Gizmos.color = Color.blue;
		Vector3 bl = pos + new Vector3(-halfSize, -halfSize, 0);
		Vector3 br = pos + new Vector3(+halfSize, -halfSize, 0);
		Vector3 tl = pos + new Vector3(-halfSize, +halfSize, 0);
		Vector3 tr = pos + new Vector3(+halfSize, +halfSize, 0);
		Gizmos.DrawLine(bl, br); Gizmos.DrawLine(br, tr);
		Gizmos.DrawLine(tr, tl); Gizmos.DrawLine(tl, bl);

		// Safe radius
		Gizmos.color = Color.green;
		Gizmos.DrawWireSphere(pos, settings.safeRadius);

		// Border region
		Gizmos.color = Color.red;
		float b = settings.borderSize;
		Vector3 bbl = pos + new Vector3(-halfSize + b, -halfSize + b, 0);
		Vector3 bbr = pos + new Vector3(+halfSize - b, -halfSize + b, 0);
		Vector3 btl = pos + new Vector3(-halfSize + b, +halfSize - b, 0);
		Vector3 btr = pos + new Vector3(+halfSize - b, +halfSize - b, 0);
		Gizmos.DrawLine(bbl, bbr); Gizmos.DrawLine(bbr, btr);
		Gizmos.DrawLine(btr, btl); Gizmos.DrawLine(btl, bbl);
	}
}