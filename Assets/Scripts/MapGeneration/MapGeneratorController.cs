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

	private NoiseGenerator noiseGenerator;
	private TilemapBuilder tilemapBuilder;
	private float[,] heightmap;
	private bool[,] originallySand;

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

		var noiseMaps = noiseGenerator.GenerateHeightmap(
			settings.mapSize, settings.scale, settings.offsetX, settings.offsetY,
			settings.numOctaves, settings.persistence, settings.lacunarity,
			settings.seaweedScale, settings.offsetX + 5000f, settings.offsetY + 5000f
		);
		heightmap = noiseMaps.heightmap;

		heightmap = tilemapBuilder.BuildTilemap(
			waterTilemap, landTilemap, buildingTilemap, oceanOverlayTilemap,
			betweenOceanFloorAndWaterTilemap, heightmap, settings
		);

		originallySand = tilemapBuilder.GetOriginallySand();

		HeightmapIO.SaveFloatArray("Heightmaps", "water_heightmap.txt", heightmap);
		HeightmapIO.SaveHeightmapTexture("Heightmaps", "heightmap", heightmap);

		RemoveSmallIslands();
		MapCleanup.PerformWaterCleanup(landTilemap, waterTilemap, originallySand, settings.sandTile, settings.waterTile, settings.mapSize);
		MapCleanup.FillInlandWater(landTilemap, waterTilemap, settings.sandTile, settings.waterTile, settings.mapSize);
		MapCleanup.RemoveSmallLakes(landTilemap, waterTilemap, settings.sandTile, settings.waterTile, settings.mapSize, 6);
		PlaceSeaweedPatches(noiseMaps.seaweedMap);
		PlaceCentralIsland();
		BuildMultiLayerOceanFloor(heightmap);
		PlaceGrassOverlay();
		PlacePalmTreesOnGrass();
		AssignTileColorsToMapManager();

		if (cameraMovement != null)
			cameraMovement.UpdateTilemapBounds();
	}

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
				if (visited[x, y]) continue;

				Vector3Int tilePos = startPosition + new Vector3Int(x, y, 0);
				TileBase currentTile = landTilemap.GetTile(tilePos);
				if (currentTile == settings.sandTile)
				{
					List<Vector3Int> islandTiles = new List<Vector3Int>();
					int islandSize = FloodFillIsland(tilePos, visited, islandTiles, mapSize);

					if (islandSize >= settings.minIslandSizeForGrass)
					{
						PlaceGrassOnIsland(islandTiles);
					}
				}
			}
		}
	}

	private int FloodFillIsland(Vector3Int startTile, bool[,] visited, List<Vector3Int> islandTiles, int mapSize)
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

			TileBase tile = landTilemap.GetTile(current);
			if (tile == settings.sandTile)
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
					{
						toExplore.Enqueue(neighbor);
					}
				}
			}
		}
		return count;
	}

	private void PlaceGrassOnIsland(List<Vector3Int> islandTiles)
	{
		if (grassTilemap == null || settings.grassTile == null) return;

		int borderOffset = settings.grassBorderOffset;
		int mapSize = settings.mapSize;
		Vector3Int offset = new Vector3Int(-mapSize / 2, -mapSize / 2, 0);

		int minX = mapSize, maxX = 0, minY = mapSize, maxY = 0;
		foreach (var tilePos in islandTiles)
		{
			int x = tilePos.x - offset.x;
			int y = tilePos.y - offset.y;
			minX = Mathf.Min(minX, x);
			maxX = Mathf.Max(maxX, x);
			minY = Mathf.Min(minY, y);
			maxY = Mathf.Max(maxY, y);
		}

		if (maxX - minX < 2 || maxY - minY < 2) return;

		int centerX = (minX + maxX) / 2;
		int centerY = (minY + maxY) / 2;

		int grassWidth = Mathf.Max(1, (maxX - minX) * 2 / 3);
		int grassHeight = Mathf.Max(1, (maxY - minY) * 2 / 3);

		List<Vector3Int> grassPositions = new List<Vector3Int>();
		for (int x = centerX - grassWidth / 2; x <= centerX + grassWidth / 2; x++)
		{
			for (int y = centerY - grassHeight / 2; y <= centerY + grassHeight / 2; y++)
			{
				Vector3Int tilePos = offset + new Vector3Int(x, y, 0);
				if (!islandTiles.Contains(tilePos) || waterTilemap.GetTile(tilePos) == settings.waterTile)
					continue;

				if (IsWithinOffsetFromBorder(tilePos, islandTiles, borderOffset))
				{
					grassPositions.Add(tilePos);
				}
			}
		}

		TileBase[] grassTiles = new TileBase[grassPositions.Count];
		for (int i = 0; i < grassTiles.Length; i++) grassTiles[i] = settings.grassTile;
		grassTilemap.SetTiles(grassPositions.ToArray(), grassTiles);
	}

	private bool IsWithinOffsetFromBorder(Vector3Int tilePos, List<Vector3Int> islandTiles, int borderOffset)
	{
		Vector3Int[] directions = { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right };
		foreach (var dir in directions)
		{
			for (int i = 1; i <= borderOffset; i++)
			{
				Vector3Int checkPos = tilePos + dir * i;
				if (!islandTiles.Contains(checkPos))
					return false;
			}
		}
		return true;
	}

	private void PlacePalmTreesOnGrass()
	{
		if (palmTreePrefab == null) return;
		if (grassTilemap == null || settings == null || settings.grassTile == null) return;
		if (palmTreeFootprint <= 0) return;
		if (palmTreeFootprint % 2 == 0) return;

		int mapSize = settings.mapSize;
		int footprint = palmTreeFootprint;
		int spacing = Mathf.Max(0, palmTreeMinSpacing);
		int centerOffset = footprint / 2;

		Vector3Int start = new Vector3Int(-mapSize / 2, -mapSize / 2, 0);

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
						int gx = x + dx;
						int gy = y + dy;

						if (blocked[gx, gy]) { canPlace = false; break; }

						Vector3Int cell = start + new Vector3Int(gx, gy, 0);
						if (grassTilemap.GetTile(cell) != settings.grassTile) { canPlace = false; break; }
						if (landTilemap.GetTile(cell) != settings.sandTile) { canPlace = false; break; }
					}
				}

				if (!canPlace) continue;

				Vector3Int centerCell = start + new Vector3Int(x + centerOffset, y + centerOffset, 0);
				Vector3 worldPos = grassTilemap.GetCellCenterWorld(centerCell);

				worldPos += new Vector3(0f, grassTilemap.cellSize.y, 0f);

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

	private void BuildMultiLayerOceanFloor(float[,] finalHeightmap)
	{
		int mapSize = settings.mapSize;
		Vector3Int startPos = new Vector3Int(-mapSize / 2, -mapSize / 2, 0);
		float[,] distanceFromLand = new float[mapSize, mapSize];
		bool[,] visited = new bool[mapSize, mapSize];

		List<Vector3Int> landPositions = new List<Vector3Int>();
		List<Vector3Int> shallowPositions = new List<Vector3Int>();
		List<Vector3Int> mediumPositions = new List<Vector3Int>();
		List<Vector3Int> deepPositions = new List<Vector3Int>();

		Queue<Vector3Int> queue = new Queue<Vector3Int>();
		for (int x = 0; x < mapSize; x++)
		{
			for (int y = 0; y < mapSize; y++)
			{
				Vector3Int tilePos = startPos + new Vector3Int(x, y, 0);
				if (landTilemap.GetTile(tilePos) != null)
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

		Vector3Int[] directions = { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right };
		while (queue.Count > 0)
		{
			Vector3Int current = queue.Dequeue();
			int cx = current.x, cy = current.y;
			float currentDist = distanceFromLand[cx, cy];
			foreach (var dir in directions)
			{
				int nx = cx + dir.x, ny = cy + dir.y;
				if (nx >= 0 && nx < mapSize && ny >= 0 && ny < mapSize && !visited[nx, ny])
				{
					visited[nx, ny] = true;
					distanceFromLand[nx, ny] = currentDist + 1f;
					queue.Enqueue(new Vector3Int(nx, ny, 0));
				}
			}
		}

		float[,] smoothDistance = new float[mapSize, mapSize];
		for (int x = 0; x < mapSize; x++)
			for (int y = 0; y < mapSize; y++)
				smoothDistance[x, y] = distanceFromLand[x, y];

		int blurIterations = 3;
		for (int iter = 0; iter < blurIterations; iter++)
		{
			float[,] temp = new float[mapSize, mapSize];
			for (int x = 0; x < mapSize; x++)
			{
				for (int y = 0; y < mapSize; y++)
				{
					float sum = 0;
					int count = 0;
					for (int dx = -1; dx <= 1; dx++)
					{
						for (int dy = -1; dy <= 1; dy++)
						{
							int nx = x + dx, ny = y + dy;
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

		float islandThreshold = 2f, shallowThreshold = 4f, mediumThreshold = 6f;
		for (int x = 0; x < mapSize; x++)
		{
			for (int y = 0; y < mapSize; y++)
			{
				Vector3Int tilePos = startPos + new Vector3Int(x, y, 0);
				float d = smoothDistance[x, y];

				if (d <= islandThreshold)
				{
					landPositions.Add(tilePos);
					shallowPositions.Add(tilePos);
					mediumPositions.Add(tilePos);
					deepPositions.Add(tilePos);
				}
				else if (d <= shallowThreshold)
				{
					shallowPositions.Add(tilePos);
					mediumPositions.Add(tilePos);
					deepPositions.Add(tilePos);
				}
				else if (d <= mediumThreshold)
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

		TileBase[] sandTiles = new TileBase[deepPositions.Count];
		for (int i = 0; i < sandTiles.Length; i++) sandTiles[i] = settings.sandTile;
		oceanFloorDeepTilemap.SetTiles(deepPositions.ToArray(), sandTiles);

		sandTiles = new TileBase[mediumPositions.Count];
		for (int i = 0; i < sandTiles.Length; i++) sandTiles[i] = settings.sandTile;
		oceanFloorMediumTilemap.SetTiles(mediumPositions.ToArray(), sandTiles);

		sandTiles = new TileBase[shallowPositions.Count];
		for (int i = 0; i < sandTiles.Length; i++) sandTiles[i] = settings.sandTile;
		oceanFloorShallowTilemap.SetTiles(shallowPositions.ToArray(), sandTiles);

		sandTiles = new TileBase[landPositions.Count];
		for (int i = 0; i < sandTiles.Length; i++) sandTiles[i] = settings.sandTile;
		landTilemap.SetTiles(landPositions.ToArray(), sandTiles);
	}

	private void RemoveSmallIslands()
	{
		MapCleanup.RemoveSmallIslands(
			landTilemap, waterTilemap, settings.sandTile, settings.waterTile,
			settings.mapSize, settings.minIslandSize
		);
	}

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
				if (heightmap[x, y] < 0.4f && seaweedMap[x, y] > settings.seaweedThreshold)
				{
					Vector3Int tilePosition = startPos + new Vector3Int(x, y, 0);
					TileBase wTile = waterTilemap.GetTile(tilePosition);
					TileBase landCheck = landTilemap.GetTile(tilePosition);
					if (wTile == settings.waterTile && landCheck == null)
					{
						seaweedPositions.Add(tilePosition);
					}
				}
			}
		}

		TileBase[] seaweedTiles = new TileBase[seaweedPositions.Count];
		for (int i = 0; i < seaweedTiles.Length; i++) seaweedTiles[i] = settings.seaweedTile;
		oceanOverlayTilemap.SetTiles(seaweedPositions.ToArray(), seaweedTiles);
	}

	private void PlaceCentralIsland()
	{
		if (settings.woodenFloorTile == null) return;

		int mapSize = settings.mapSize;
		Vector3Int startPosition = new Vector3Int(-mapSize / 2, -mapSize / 2, 0);
		int centerX = mapSize / 2;
		int centerY = mapSize / 2;

		Vector3Int[] floorPositions = new Vector3Int[4];
		Vector3Int[] pillarPositions = new Vector3Int[4];
		int index = 0;
		for (int x = centerX - 1; x <= centerX; x++)
		{
			for (int y = centerY - 1; y <= centerY; y++)
			{
				floorPositions[index] = startPosition + new Vector3Int(x, y, 0);
				pillarPositions[index] = startPosition + new Vector3Int(x, y - 1, 0);
				index++;
			}
		}

		TileBase[] floorTiles = new TileBase[4];
		TileBase[] pillarTiles = new TileBase[4];
		for (int i = 0; i < 4; i++)
		{
			floorTiles[i] = settings.woodenFloorTile;
			pillarTiles[i] = settings.woodenPillarTile ?? null;
		}

		buildingTilemap.SetTiles(floorPositions, floorTiles);
		if (settings.woodenPillarTile != null)
			betweenOceanFloorAndWaterTilemap.SetTiles(pillarPositions, pillarTiles);
	}

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
				Vector3Int pos = new Vector3Int(x - mapSize / 2, y - mapSize / 2, 0);
				TileBase wTile = waterTilemap.GetTile(pos);
				Color c = (wTile != null) ? settings.waterColor : settings.sandColor;
				MapManager.Instance.tileColors[x, y] = c;
			}
		}
	}

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
		{
			if (spawnedPalmTrees[i] != null)
				Destroy(spawnedPalmTrees[i]);
		}
		spawnedPalmTrees.Clear();
	}

	private void OnDrawGizmos()
	{
		if (!Application.isPlaying) { }

		if (settings == null) return;

		float halfSize = settings.mapSize / 2f;
		Gizmos.color = Color.blue;

		Vector3 bottomLeft = transform.position + new Vector3(-halfSize, -halfSize, 0);
		Vector3 bottomRight = transform.position + new Vector3(+halfSize, -halfSize, 0);
		Vector3 topLeft = transform.position + new Vector3(-halfSize, +halfSize, 0);
		Vector3 topRight = transform.position + new Vector3(+halfSize, +halfSize, 0);

		Gizmos.DrawLine(bottomLeft, bottomRight);
		Gizmos.DrawLine(bottomRight, topRight);
		Gizmos.DrawLine(topRight, topLeft);
		Gizmos.DrawLine(topLeft, bottomLeft);

		Gizmos.color = Color.green;
		Gizmos.DrawWireSphere(transform.position, settings.safeRadius);

		Gizmos.color = Color.red;
		Vector3 borderBL = transform.position + new Vector3(-halfSize + settings.borderSize, -halfSize + settings.borderSize, 0);
		Vector3 borderBR = transform.position + new Vector3(+halfSize - settings.borderSize, -halfSize + settings.borderSize, 0);
		Vector3 borderTL = transform.position + new Vector3(-halfSize + settings.borderSize, +halfSize - settings.borderSize, 0);
		Vector3 borderTR = transform.position + new Vector3(+halfSize - settings.borderSize, +halfSize - settings.borderSize, 0);

		Gizmos.DrawLine(borderBL, borderBR);
		Gizmos.DrawLine(borderBR, borderTR);
		Gizmos.DrawLine(borderTR, borderTL);
		Gizmos.DrawLine(borderTL, borderBL);
	}
}
