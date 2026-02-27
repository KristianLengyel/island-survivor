using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class MapCleanup
{
	public static void RemoveSmallIslands(
		Tilemap landTilemap, Tilemap waterTilemap, TileBase sandTile, TileBase waterTile,
		int mapSize, int minIslandSize)
	{
		Vector3Int startPosition = new Vector3Int(-mapSize / 2, -mapSize / 2, 0);
		bool[,] visited = new bool[mapSize, mapSize];

		for (int x = 0; x < mapSize; x++)
		{
			for (int y = 0; y < mapSize; y++)
			{
				if (visited[x, y]) continue;

				Vector3Int tilePos = startPosition + new Vector3Int(x, y, 0);
				TileBase currentTile = landTilemap.GetTile(tilePos);
				if (currentTile == sandTile)
				{
					List<Vector3Int> islandTiles = new List<Vector3Int>();
					int islandSize = FloodFillIsland(tilePos, visited, islandTiles, landTilemap, sandTile, mapSize, minIslandSize);

					if (islandSize < minIslandSize)
					{
						TileBase[] waterTiles = new TileBase[islandTiles.Count];
						for (int i = 0; i < waterTiles.Length; i++) waterTiles[i] = waterTile;
						landTilemap.SetTiles(islandTiles.ToArray(), new TileBase[islandTiles.Count]);
						waterTilemap.SetTiles(islandTiles.ToArray(), waterTiles);
					}
				}
			}
		}
	}

	private static int FloodFillIsland(Vector3Int startTile, bool[,] visited, List<Vector3Int> islandTiles,
		Tilemap landTilemap, TileBase sandTile, int mapSize, int minIslandSize)
	{
		int count = 0;
		Vector3Int offset = new Vector3Int(-mapSize / 2, -mapSize / 2, 0);
		Vector3Int[] queue = new Vector3Int[mapSize * mapSize];
		int queueStart = 0, queueEnd = 0;

		queue[queueEnd++] = startTile;
		visited[startTile.x - offset.x, startTile.y - offset.y] = true;

		Vector3Int[] neighbors = {
			Vector3Int.right, Vector3Int.left, Vector3Int.up, Vector3Int.down,
			Vector3Int.right + Vector3Int.up, Vector3Int.right + Vector3Int.down,
			Vector3Int.left + Vector3Int.up, Vector3Int.left + Vector3Int.down
		};

		while (queueStart < queueEnd)
		{
			Vector3Int current = queue[queueStart++];
			int x = current.x - offset.x;
			int y = current.y - offset.y;

			TileBase tile = landTilemap.GetTile(current);
			if (tile == sandTile)
			{
				islandTiles.Add(current);
				count++;
				if (count >= minIslandSize) return count;

				foreach (var n in neighbors)
				{
					Vector3Int neighbor = current + n;
					int nx = neighbor.x - offset.x;
					int ny = neighbor.y - offset.y;
					if (nx >= 0 && nx < mapSize && ny >= 0 && ny < mapSize && !visited[nx, ny])
					{
						visited[nx, ny] = true;
						queue[queueEnd++] = neighbor;
					}
				}
			}
		}
		return count;
	}

	public static void PerformWaterCleanup(
		Tilemap landTilemap, Tilemap waterTilemap, bool[,] originallySand,
		TileBase sandTile, TileBase waterTile, int mapSize)
	{
		Vector3Int startPosition = new Vector3Int(-mapSize / 2, -mapSize / 2, 0);
		Vector3Int[] directions = { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right };
		bool[,] toRemove = new bool[mapSize, mapSize];

		for (int x = 1; x < mapSize - 1; x++)
		{
			for (int y = 1; y < mapSize - 1; y++)
			{
				Vector3Int tilePos = startPosition + new Vector3Int(x, y, 0);
				TileBase wTile = waterTilemap.GetTile(tilePos);
				if (wTile == waterTile && originallySand[x, y])
				{
					int emptyNeighborCount = 0;
					int landCount = 0;
					bool leftIsLand = landTilemap.GetTile(tilePos + Vector3Int.left) == sandTile;
					bool rightIsLand = landTilemap.GetTile(tilePos + Vector3Int.right) == sandTile;
					bool upIsLand = landTilemap.GetTile(tilePos + Vector3Int.up) == sandTile;
					bool downIsLand = landTilemap.GetTile(tilePos + Vector3Int.down) == sandTile;

					foreach (var dir in directions)
					{
						TileBase neighborTile = waterTilemap.GetTile(tilePos + dir);
						if (neighborTile == null) emptyNeighborCount++;
						if (landTilemap.GetTile(tilePos + dir) == sandTile) landCount++;
					}

					if (emptyNeighborCount >= 3 || landCount >= 3 || (leftIsLand && rightIsLand) || (upIsLand && downIsLand))
					{
						toRemove[x, y] = true;
					}
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
				}
			}
		}
		waterTilemap.SetTiles(positionsToRemove.ToArray(), new TileBase[positionsToRemove.Count]);
	}

	public static void RemoveSmallLakes(
	Tilemap landTilemap, Tilemap waterTilemap,
	TileBase sandTile, TileBase waterTile,
	int mapSize, int minLakeSize)
	{
		Vector3Int startPosition = new Vector3Int(-mapSize / 2, -mapSize / 2, 0);
		bool[,] visited = new bool[mapSize, mapSize];

		Vector3Int[] neighbors =
		{
		Vector3Int.right, Vector3Int.left, Vector3Int.up, Vector3Int.down
	};

		for (int x = 0; x < mapSize; x++)
		{
			for (int y = 0; y < mapSize; y++)
			{
				if (visited[x, y]) continue;

				Vector3Int tilePos = startPosition + new Vector3Int(x, y, 0);
				if (waterTilemap.GetTile(tilePos) != waterTile)
				{
					visited[x, y] = true;
					continue;
				}

				var region = new List<Vector3Int>();
				bool touchesBorder = false;

				Queue<Vector2Int> q = new Queue<Vector2Int>();
				q.Enqueue(new Vector2Int(x, y));
				visited[x, y] = true;

				while (q.Count > 0)
				{
					var p = q.Dequeue();
					if (p.x == 0 || p.y == 0 || p.x == mapSize - 1 || p.y == mapSize - 1)
						touchesBorder = true;

					Vector3Int wp = startPosition + new Vector3Int(p.x, p.y, 0);
					if (waterTilemap.GetTile(wp) != waterTile) continue;

					region.Add(wp);
					if (region.Count >= minLakeSize && !touchesBorder) { }

					foreach (var dir in neighbors)
					{
						int nx = p.x + dir.x;
						int ny = p.y + dir.y;
						if (nx < 0 || nx >= mapSize || ny < 0 || ny >= mapSize) continue;
						if (visited[nx, ny]) continue;
						visited[nx, ny] = true;
						q.Enqueue(new Vector2Int(nx, ny));
					}
				}

				if (!touchesBorder && region.Count < minLakeSize)
				{
					var sandTiles = new TileBase[region.Count];
					var clearWater = new TileBase[region.Count];
					for (int i = 0; i < region.Count; i++) sandTiles[i] = sandTile;

					waterTilemap.SetTiles(region.ToArray(), clearWater);
					landTilemap.SetTiles(region.ToArray(), sandTiles);
				}
			}
		}
	}

	public static void FillInlandWater(
		Tilemap landTilemap, Tilemap waterTilemap, TileBase sandTile, TileBase waterTile, int mapSize)
	{
		bool[,] isOceanWater = new bool[mapSize, mapSize];
		bool[,] visited = new bool[mapSize, mapSize];
		Vector3Int startPosition = new Vector3Int(-mapSize / 2, -mapSize / 2, 0);
		Vector3Int[] directions = { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right };

		for (int x = 0; x < mapSize; x++)
		{
			CheckAndMarkOceanWater(new Vector2Int(x, 0));
			CheckAndMarkOceanWater(new Vector2Int(x, mapSize - 1));
		}
		for (int y = 0; y < mapSize; y++)
		{
			CheckAndMarkOceanWater(new Vector2Int(0, y));
			CheckAndMarkOceanWater(new Vector2Int(mapSize - 1, y));
		}

		void CheckAndMarkOceanWater(Vector2Int gridPos)
		{
			Vector3Int tilePos = startPosition + new Vector3Int(gridPos.x, gridPos.y, 0);
			if (!visited[gridPos.x, gridPos.y] && waterTilemap.GetTile(tilePos) == waterTile)
			{
				Queue<Vector2Int> queue = new Queue<Vector2Int>();
				queue.Enqueue(gridPos);
				visited[gridPos.x, gridPos.y] = true;
				isOceanWater[gridPos.x, gridPos.y] = true;

				while (queue.Count > 0)
				{
					Vector2Int current = queue.Dequeue();
					foreach (var dir in directions)
					{
						int nx = current.x + dir.x;
						int ny = current.y + dir.y;
						if (nx >= 0 && nx < mapSize && ny >= 0 && ny < mapSize && !visited[nx, ny])
						{
							Vector3Int neighborPos = startPosition + new Vector3Int(nx, ny, 0);
							if (waterTilemap.GetTile(neighborPos) == waterTile)
							{
								visited[nx, ny] = true;
								isOceanWater[nx, ny] = true;
								queue.Enqueue(new Vector2Int(nx, ny));
							}
						}
					}
				}
			}
		}

		List<Vector3Int> waterToSandPositions = new List<Vector3Int>();
		for (int x = 0; x < mapSize; x++)
		{
			for (int y = 0; y < mapSize; y++)
			{
				if (!isOceanWater[x, y])
				{
					Vector3Int tilePos = startPosition + new Vector3Int(x, y, 0);
					if (waterTilemap.GetTile(tilePos) == waterTile)
					{
						waterToSandPositions.Add(tilePos);
					}
				}
			}
		}

		TileBase[] sandTiles = new TileBase[waterToSandPositions.Count];
		for (int i = 0; i < sandTiles.Length; i++) sandTiles[i] = sandTile;
		waterTilemap.SetTiles(waterToSandPositions.ToArray(), new TileBase[waterToSandPositions.Count]);
		landTilemap.SetTiles(waterToSandPositions.ToArray(), sandTiles);
	}
}