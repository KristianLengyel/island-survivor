using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class MapCleanup
{
	/// <summary>
	/// Removes land islands smaller than minIslandSize tiles.
	/// Uses a cached isLand array to avoid GetTile() calls inside the flood fill.
	/// After removal, updates the isLand/isWater arrays to reflect changes.
	/// </summary>
	public static void RemoveSmallIslands(
		Tilemap landTilemap, Tilemap waterTilemap,
		TileBase sandTile, TileBase waterTile,
		bool[,] isLand, bool[,] isWater,
		int mapSize, int minIslandSize)
	{
		Vector3Int startPosition = new Vector3Int(-mapSize / 2, -mapSize / 2, 0);
		bool[,] visited = new bool[mapSize, mapSize];

		for (int x = 0; x < mapSize; x++)
		{
			for (int y = 0; y < mapSize; y++)
			{
				if (visited[x, y] || !isLand[x, y]) continue;

				Vector3Int tilePos = startPosition + new Vector3Int(x, y, 0);
				List<Vector3Int> islandTiles = new List<Vector3Int>();
				int islandSize = FloodFillLand(x, y, visited, islandTiles, isLand, mapSize, minIslandSize, startPosition);

				if (islandSize < minIslandSize)
				{
					TileBase[] waterTiles = new TileBase[islandTiles.Count];
					for (int i = 0; i < waterTiles.Length; i++) waterTiles[i] = waterTile;

					landTilemap.SetTiles(islandTiles.ToArray(), new TileBase[islandTiles.Count]);
					waterTilemap.SetTiles(islandTiles.ToArray(), waterTiles);

					// Update cached arrays
					foreach (var pos in islandTiles)
					{
						int gx = pos.x - startPosition.x;
						int gy = pos.y - startPosition.y;
						if (gx >= 0 && gx < mapSize && gy >= 0 && gy < mapSize)
						{
							isLand[gx, gy] = false;
							isWater[gx, gy] = true;
						}
					}
				}
			}
		}
	}

	// Uses cached isLand array — no GetTile calls
	private static int FloodFillLand(
		int startX, int startY, bool[,] visited, List<Vector3Int> islandTiles,
		bool[,] isLand, int mapSize, int minIslandSize, Vector3Int startPosition)
	{
		int count = 0;
		Vector3Int[] queue = new Vector3Int[mapSize * mapSize];
		int queueStart = 0, queueEnd = 0;

		queue[queueEnd++] = new Vector3Int(startX, startY, 0);
		visited[startX, startY] = true;

		// 8-directional for island detection
		int[] dx = { 1, -1, 0, 0, 1, 1, -1, -1 };
		int[] dy = { 0, 0, 1, -1, 1, -1, 1, -1 };

		while (queueStart < queueEnd)
		{
			Vector3Int current = queue[queueStart++];
			int x = current.x, y = current.y;

			if (!isLand[x, y]) continue;

			islandTiles.Add(startPosition + new Vector3Int(x, y, 0));
			count++;
			if (count >= minIslandSize) return count;

			for (int d = 0; d < 8; d++)
			{
				int nx = x + dx[d], ny = y + dy[d];
				if (nx >= 0 && nx < mapSize && ny >= 0 && ny < mapSize && !visited[nx, ny])
				{
					visited[nx, ny] = true;
					queue[queueEnd++] = new Vector3Int(nx, ny, 0);
				}
			}
		}

		return count;
	}

	/// <summary>
	/// Removes awkward isolated water tiles nearly surrounded by land.
	/// Uses cached arrays — no GetTile calls.
	/// Updates isLand/isWater arrays after changes.
	/// </summary>
	public static void PerformWaterCleanup(
		Tilemap landTilemap, Tilemap waterTilemap,
		bool[,] originallySand, bool[,] isLand, bool[,] isWater,
		TileBase sandTile, TileBase waterTile, int mapSize)
	{
		Vector3Int startPosition = new Vector3Int(-mapSize / 2, -mapSize / 2, 0);
		bool[,] toRemove = new bool[mapSize, mapSize];

		for (int x = 1; x < mapSize - 1; x++)
		{
			for (int y = 1; y < mapSize - 1; y++)
			{
				if (!isWater[x, y] || !originallySand[x, y]) continue;

				bool leftIsLand = isLand[x - 1, y];
				bool rightIsLand = isLand[x + 1, y];
				bool upIsLand = isLand[x, y + 1];
				bool downIsLand = isLand[x, y - 1];

				int emptyNeighborCount = 0;
				int landCount = 0;

				if (!isWater[x - 1, y] && !isLand[x - 1, y]) emptyNeighborCount++;
				if (!isWater[x + 1, y] && !isLand[x + 1, y]) emptyNeighborCount++;
				if (!isWater[x, y - 1] && !isLand[x, y - 1]) emptyNeighborCount++;
				if (!isWater[x, y + 1] && !isLand[x, y + 1]) emptyNeighborCount++;

				if (leftIsLand) landCount++;
				if (rightIsLand) landCount++;
				if (upIsLand) landCount++;
				if (downIsLand) landCount++;

				if (emptyNeighborCount >= 3 || landCount >= 3 ||
					(leftIsLand && rightIsLand) || (upIsLand && downIsLand))
				{
					toRemove[x, y] = true;
				}
			}
		}

		List<Vector3Int> positionsToRemove = new List<Vector3Int>();
		for (int x = 1; x < mapSize - 1; x++)
		{
			for (int y = 1; y < mapSize - 1; y++)
			{
				if (toRemove[x, y])
				{
					positionsToRemove.Add(startPosition + new Vector3Int(x, y, 0));
					isWater[x, y] = false; // Update cached array
				}
			}
		}

		waterTilemap.SetTiles(positionsToRemove.ToArray(), new TileBase[positionsToRemove.Count]);
	}

	/// <summary>
	/// Removes small lakes not connected to the map border.
	/// Uses cached isWater array — no GetTile calls.
	/// Updates isLand/isWater arrays after changes.
	/// </summary>
	public static void RemoveSmallLakes(
		Tilemap landTilemap, Tilemap waterTilemap,
		TileBase sandTile, TileBase waterTile,
		bool[,] isLand, bool[,] isWater,
		int mapSize, int minLakeSize)
	{
		Vector3Int startPosition = new Vector3Int(-mapSize / 2, -mapSize / 2, 0);
		bool[,] visited = new bool[mapSize, mapSize];

		int[] dx = { 0, 0, 1, -1 };
		int[] dy = { 1, -1, 0, 0 };

		for (int x = 0; x < mapSize; x++)
		{
			for (int y = 0; y < mapSize; y++)
			{
				if (visited[x, y] || !isWater[x, y])
				{
					visited[x, y] = true;
					continue;
				}

				var region = new List<Vector2Int>();
				bool touchesBorder = false;

				Queue<Vector2Int> q = new Queue<Vector2Int>();
				q.Enqueue(new Vector2Int(x, y));
				visited[x, y] = true;

				while (q.Count > 0)
				{
					var p = q.Dequeue();
					if (p.x == 0 || p.y == 0 || p.x == mapSize - 1 || p.y == mapSize - 1)
						touchesBorder = true;

					if (!isWater[p.x, p.y]) continue;

					region.Add(p);

					for (int d = 0; d < 4; d++)
					{
						int nx = p.x + dx[d], ny = p.y + dy[d];
						if (nx < 0 || nx >= mapSize || ny < 0 || ny >= mapSize) continue;
						if (visited[nx, ny]) continue;
						visited[nx, ny] = true;
						q.Enqueue(new Vector2Int(nx, ny));
					}
				}

				if (!touchesBorder && region.Count < minLakeSize)
				{
					Vector3Int[] worldPositions = new Vector3Int[region.Count];
					TileBase[] sandTiles = new TileBase[region.Count];
					TileBase[] clearTiles = new TileBase[region.Count];

					for (int i = 0; i < region.Count; i++)
					{
						worldPositions[i] = startPosition + new Vector3Int(region[i].x, region[i].y, 0);
						sandTiles[i] = sandTile;
						// Update cached arrays
						isWater[region[i].x, region[i].y] = false;
						isLand[region[i].x, region[i].y] = true;
					}

					waterTilemap.SetTiles(worldPositions, clearTiles);
					landTilemap.SetTiles(worldPositions, sandTiles);
				}
			}
		}
	}

	/// <summary>
	/// Converts all inland water (not connected to the map border) to land.
	/// Uses cached isWater array — no GetTile calls.
	/// Updates isLand/isWater arrays after changes.
	/// </summary>
	public static void FillInlandWater(
		Tilemap landTilemap, Tilemap waterTilemap,
		TileBase sandTile, TileBase waterTile,
		bool[,] isLand, bool[,] isWater,
		int mapSize)
	{
		bool[,] isOceanWater = new bool[mapSize, mapSize];
		bool[,] visited = new bool[mapSize, mapSize];
		Vector3Int startPosition = new Vector3Int(-mapSize / 2, -mapSize / 2, 0);

		int[] dx = { 0, 0, 1, -1 };
		int[] dy = { 1, -1, 0, 0 };

		void FloodFromBorder(int bx, int by)
		{
			if (visited[bx, by] || !isWater[bx, by]) return;

			Queue<Vector2Int> queue = new Queue<Vector2Int>();
			queue.Enqueue(new Vector2Int(bx, by));
			visited[bx, by] = true;
			isOceanWater[bx, by] = true;

			while (queue.Count > 0)
			{
				Vector2Int current = queue.Dequeue();
				for (int d = 0; d < 4; d++)
				{
					int nx = current.x + dx[d], ny = current.y + dy[d];
					if (nx < 0 || nx >= mapSize || ny < 0 || ny >= mapSize) continue;
					if (visited[nx, ny] || !isWater[nx, ny]) continue;
					visited[nx, ny] = true;
					isOceanWater[nx, ny] = true;
					queue.Enqueue(new Vector2Int(nx, ny));
				}
			}
		}

		for (int x = 0; x < mapSize; x++)
		{
			FloodFromBorder(x, 0);
			FloodFromBorder(x, mapSize - 1);
		}
		for (int y = 0; y < mapSize; y++)
		{
			FloodFromBorder(0, y);
			FloodFromBorder(mapSize - 1, y);
		}

		List<Vector3Int> waterToSandPositions = new List<Vector3Int>();
		for (int x = 0; x < mapSize; x++)
		{
			for (int y = 0; y < mapSize; y++)
			{
				if (!isOceanWater[x, y] && isWater[x, y])
				{
					waterToSandPositions.Add(startPosition + new Vector3Int(x, y, 0));
					isWater[x, y] = false;
					isLand[x, y] = true;
				}
			}
		}

		TileBase[] sandTiles = new TileBase[waterToSandPositions.Count];
		for (int i = 0; i < sandTiles.Length; i++) sandTiles[i] = sandTile;

		waterTilemap.SetTiles(waterToSandPositions.ToArray(), new TileBase[waterToSandPositions.Count]);
		landTilemap.SetTiles(waterToSandPositions.ToArray(), sandTiles);
	}

	/// <summary>
	/// Removes any water tile that doesn't have at least minWaterNeighbors water neighbors
	/// on its 4 cardinal sides. Repeats until no more tiles are removed, so cascading
	/// isolated tiles (e.g. a thin 1-wide peninsula of water) are fully cleaned up.
	/// Call this last, after all other cleanup passes.
	/// </summary>
	public static void RemoveIsolatedWaterTiles(
		Tilemap landTilemap, Tilemap waterTilemap,
		TileBase sandTile,
		bool[,] isLand, bool[,] isWater,
		int mapSize, int minWaterNeighbors = 2)
	{
		Vector3Int startPosition = new Vector3Int(-mapSize / 2, -mapSize / 2, 0);

		int[] dx = { 0, 0, 1, -1 };
		int[] dy = { 1, -1, 0, 0 };

		bool anyRemoved = true;

		// Repeat until stable — cascading removals (e.g. removing one tile
		// may make its neighbor now have too few water neighbors)
		while (anyRemoved)
		{
			anyRemoved = false;
			List<Vector3Int> toConvert = new List<Vector3Int>();

			for (int x = 0; x < mapSize; x++)
			{
				for (int y = 0; y < mapSize; y++)
				{
					if (!isWater[x, y]) continue;

					int waterNeighbors = 0;
					for (int d = 0; d < 4; d++)
					{
						int nx = x + dx[d], ny = y + dy[d];
						if (nx >= 0 && nx < mapSize && ny >= 0 && ny < mapSize && isWater[nx, ny])
							waterNeighbors++;
					}

					if (waterNeighbors < minWaterNeighbors)
					{
						toConvert.Add(startPosition + new Vector3Int(x, y, 0));
						isWater[x, y] = false;
						isLand[x, y] = true;
						anyRemoved = true;
					}
				}
			}

			if (toConvert.Count == 0) break;

			TileBase[] sandTiles = new TileBase[toConvert.Count];
			for (int i = 0; i < sandTiles.Length; i++) sandTiles[i] = sandTile;

			waterTilemap.SetTiles(toConvert.ToArray(), new TileBase[toConvert.Count]);
			landTilemap.SetTiles(toConvert.ToArray(), sandTiles);
		}
	}
}