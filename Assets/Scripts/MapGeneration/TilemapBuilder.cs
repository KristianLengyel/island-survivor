using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TilemapBuilder
{
	private Tilemap waterTilemap;
	private Tilemap landTilemap;
	private Tilemap buildingTilemap;
	private Tilemap oceanOverlayTilemap;
	private Tilemap betweenOceanFloorAndWaterTilemap;

	private TileBase waterTile;
	private TileBase sandTile;

	private bool[,] originallySand;
	private float[,] heightmap;

	// Cached tile state arrays  avoids repeated GetTile() calls downstream
	private bool[,] isLand;
	private bool[,] isWater;

	public float[,] BuildTilemap(
		Tilemap waterTilemap, Tilemap landTilemap, Tilemap buildingTilemap,
		Tilemap oceanOverlayTilemap, Tilemap betweenOceanFloorAndWaterTilemap,
		float[,] noiseMap, TilemapGenerationSettings settings)
	{
		this.waterTilemap = waterTilemap;
		this.landTilemap = landTilemap;
		this.buildingTilemap = buildingTilemap;
		this.oceanOverlayTilemap = oceanOverlayTilemap;
		this.betweenOceanFloorAndWaterTilemap = betweenOceanFloorAndWaterTilemap;

		this.waterTile = settings.waterTile;
		this.sandTile = settings.sandTile;
		this.heightmap = noiseMap;

		int mapSize = settings.mapSize;
		originallySand = new bool[mapSize, mapSize];
		isLand = new bool[mapSize, mapSize];
		isWater = new bool[mapSize, mapSize];

		FillWaterTilemap(settings);
		PlaceSandTiles(settings);
		ComputeDistanceBasedHeightmap(settings);

		return heightmap;
	}

	private void FillWaterTilemap(TilemapGenerationSettings settings)
	{
		int mapSize = settings.mapSize;
		int pad = settings.mapWaterPadding;
		int waterSize = mapSize + pad * 2;

		Vector3Int startPosition = new Vector3Int(-mapSize / 2 - pad, -mapSize / 2 - pad, 0);

		TileBase[] waterTilesArray = new TileBase[waterSize * waterSize];
		for (int i = 0; i < waterTilesArray.Length; i++)
			waterTilesArray[i] = waterTile;

		for (int x = 0; x < waterSize; x++)
		{
			for (int y = 0; y < waterSize; y++)
			{
				int innerX = x - pad;
				int innerY = y - pad;
				bool insideInner = innerX >= 0 && innerX < mapSize && innerY >= 0 && innerY < mapSize;
				if (!insideInner) continue;

				int index = y * waterSize + x;

				bool inBorder =
					(innerX < settings.borderSize || innerX >= mapSize - settings.borderSize ||
					 innerY < settings.borderSize || innerY >= mapSize - settings.borderSize);

				Vector2 posFromCenter = new Vector2(innerX - mapSize / 2f, innerY - mapSize / 2f);
				float dist = posFromCenter.magnitude;

				if (inBorder || dist < settings.safeRadius)
				{
					waterTilesArray[index] = waterTile;
					heightmap[innerX, innerY] = 0f;
				}
			}
		}

		BoundsInt bounds = new BoundsInt(startPosition, new Vector3Int(waterSize, waterSize, 1));
		waterTilemap.ClearAllTiles();
		waterTilemap.SetTilesBlock(bounds, waterTilesArray);

		// Cache water state
		for (int x = 0; x < mapSize; x++)
			for (int y = 0; y < mapSize; y++)
				isWater[x, y] = true; // everything starts as water
	}

	private void PlaceSandTiles(TilemapGenerationSettings settings)
	{
		int mapSize = settings.mapSize;
		Vector3Int startPosition = new Vector3Int(-mapSize / 2, -mapSize / 2, 0);
		List<Vector3Int> sandPositions = new List<Vector3Int>();

		for (int x = 0; x < mapSize; x++)
		{
			for (int y = 0; y < mapSize; y++)
			{
				float distFromCenter = Vector2.Distance(
					new Vector2(x, y), new Vector2(mapSize / 2f, mapSize / 2f));

				float distFactor = distFromCenter / (mapSize / 2f);
				float dynamicThreshold = Mathf.Lerp(
					settings.islandThreshold, settings.islandThreshold - 0.1f, distFactor);

				if (heightmap[x, y] >= dynamicThreshold)
				{
					Vector3Int pos = startPosition + new Vector3Int(x, y, 0);
					sandPositions.Add(pos);
					originallySand[x, y] = true;
					isLand[x, y] = true;
					isWater[x, y] = false;
				}
			}
		}

		Vector3Int[] positionsArray = sandPositions.ToArray();

		TileBase[] tilesArray = new TileBase[positionsArray.Length];
		for (int i = 0; i < tilesArray.Length; i++)
			tilesArray[i] = sandTile;
		landTilemap.SetTiles(positionsArray, tilesArray);

		// Clear water tiles underneath land so no overlap exists
		waterTilemap.SetTiles(positionsArray, new TileBase[positionsArray.Length]);
		// Note: land expansion (ExpandLandBorder) is called by MapGeneratorController
		// AFTER all cleanup passes, so cleanup doesn't eat back the expansion.
	}

	// Grows the sand border outward by landExpansion tiles using BFS from existing land.
	// New sand tiles are added to landTilemap, water is cleared at those positions,
	// and isLand/isWater arrays are updated so all subsequent passes stay consistent.
	// Called by MapGeneratorController AFTER all cleanup passes.
	public void ExpandLandBorder(TilemapGenerationSettings settings)
	{
		int mapSize = settings.mapSize;
		int expansion = settings.landExpansion;
		Vector3Int startPosition = new Vector3Int(-mapSize / 2, -mapSize / 2, 0);

		int[] dx = { 0, 0, 1, -1 };
		int[] dy = { 1, -1, 0, 0 };

		// BFS from all existing land tiles outward into water
		int[,] dist = new int[mapSize, mapSize];
		for (int x = 0; x < mapSize; x++)
			for (int y = 0; y < mapSize; y++)
				dist[x, y] = -1;

		Queue<Vector2Int> queue = new Queue<Vector2Int>();

		for (int x = 0; x < mapSize; x++)
			for (int y = 0; y < mapSize; y++)
				if (isLand[x, y])
				{
					dist[x, y] = 0;
					queue.Enqueue(new Vector2Int(x, y));
				}

		List<Vector3Int> newSandPositions = new List<Vector3Int>();

		while (queue.Count > 0)
		{
			Vector2Int current = queue.Dequeue();
			int currentDist = dist[current.x, current.y];
			if (currentDist >= expansion) continue;

			for (int d = 0; d < 4; d++)
			{
				int nx = current.x + dx[d], ny = current.y + dy[d];
				if (nx < 0 || nx >= mapSize || ny < 0 || ny >= mapSize) continue;
				if (dist[nx, ny] != -1) continue;

				dist[nx, ny] = currentDist + 1;

				if (isWater[nx, ny])
				{
					newSandPositions.Add(startPosition + new Vector3Int(nx, ny, 0));
					// Update cached arrays immediately so BFS neighbours are correct
					isLand[nx, ny] = true;
					isWater[nx, ny] = false;
				}

				queue.Enqueue(new Vector2Int(nx, ny));
			}
		}

		if (newSandPositions.Count == 0) return;

		TileBase[] sandTilesArr = new TileBase[newSandPositions.Count];
		TileBase[] clearTilesArr = new TileBase[newSandPositions.Count];
		for (int i = 0; i < sandTilesArr.Length; i++) sandTilesArr[i] = sandTile;

		waterTilemap.SetTiles(newSandPositions.ToArray(), clearTilesArr);
		landTilemap.SetTiles(newSandPositions.ToArray(), sandTilesArr);
	}

	private void ComputeDistanceBasedHeightmap(TilemapGenerationSettings settings)
	{
		int mapSize = settings.mapSize;
		Vector3Int startPosition = new Vector3Int(-mapSize / 2, -mapSize / 2, 0);

		float[,] distanceMap = new float[mapSize, mapSize];
		bool[,] visited = new bool[mapSize, mapSize];
		Queue<Vector3Int> queue = new Queue<Vector3Int>();

		// Seed BFS from land tiles  use cached isLand array, no GetTile calls
		for (int x = 0; x < mapSize; x++)
		{
			for (int y = 0; y < mapSize; y++)
			{
				if (isLand[x, y])
				{
					queue.Enqueue(new Vector3Int(x, y, 0));
					visited[x, y] = true;
					distanceMap[x, y] = 0f;
				}
			}
		}

		Vector3Int[] directions = { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right };

		while (queue.Count > 0)
		{
			Vector3Int current = queue.Dequeue();
			foreach (var dir in directions)
			{
				int nx = current.x + dir.x;
				int ny = current.y + dir.y;
				if (nx < 0 || nx >= mapSize || ny < 0 || ny >= mapSize) continue;
				if (visited[nx, ny]) continue;

				// Use cached isWater array instead of GetTile
				if (isWater[nx, ny])
				{
					float neighborDistance = distanceMap[current.x, current.y] + 1f;
					if (neighborDistance > settings.maxOceanDepth) continue;

					distanceMap[nx, ny] = neighborDistance;
					queue.Enqueue(new Vector3Int(nx, ny, 0));
					visited[nx, ny] = true;
				}
			}
		}

		for (int x = 0; x < mapSize; x++)
		{
			for (int y = 0; y < mapSize; y++)
			{
				if (isLand[x, y])
				{
					heightmap[x, y] = 1f;
				}
				else if (distanceMap[x, y] >= 1f && distanceMap[x, y] <= settings.maxOceanDepth)
				{
					heightmap[x, y] = 1f - (distanceMap[x, y] / settings.maxOceanDepth);
				}
				else
				{
					heightmap[x, y] = 0f;
				}
			}
		}
	}

	public bool[,] GetOriginallySand() => originallySand;

	/// <summary>
	/// Returns a cached bool array of land tiles (true = land/sand).
	/// Use this instead of calling landTilemap.GetTile() in loops.
	/// </summary>
	public bool[,] GetIsLand() => isLand;

	/// <summary>
	/// Returns a cached bool array of water tiles (true = water).
	/// Use this instead of calling waterTilemap.GetTile() in loops.
	/// </summary>
	public bool[,] GetIsWater() => isWater;
}