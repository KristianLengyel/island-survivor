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
	private TileBase seaweedTile;
	private TileBase woodenFloorTile;
	private TileBase woodenPillarTile;

	private bool[,] originallySand;
	private float[,] heightmap;

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
		this.seaweedTile = settings.seaweedTile;
		this.woodenFloorTile = settings.woodenFloorTile;
		this.woodenPillarTile = settings.woodenPillarTile;

		this.heightmap = noiseMap;

		int mapSize = settings.mapSize;
		originallySand = new bool[mapSize, mapSize];

		FillWaterTilemap(settings);
		PlaceSandTiles(settings);
		ComputeDistanceBasedHeightmap(settings);

		return heightmap;
	}

	private void FillWaterTilemap(TilemapGenerationSettings settings)
	{
		int pad = 1;

		int mapSize = settings.mapSize;
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
				float dynamicThreshold = Mathf.Lerp(settings.islandThreshold, settings.islandThreshold - 0.1f, distFactor);

				if (heightmap[x, y] >= dynamicThreshold)
				{
					Vector3Int pos = startPosition + new Vector3Int(x, y, 0);
					sandPositions.Add(pos);
					originallySand[x, y] = true;
				}
			}
		}

		Vector3Int[] positionsArray = sandPositions.ToArray();
		TileBase[] tilesArray = new TileBase[positionsArray.Length];
		for (int i = 0; i < tilesArray.Length; i++)
			tilesArray[i] = sandTile;
		landTilemap.SetTiles(positionsArray, tilesArray);
	}

	private void ComputeDistanceBasedHeightmap(TilemapGenerationSettings settings)
	{
		int mapSize = settings.mapSize;
		Vector3Int startPosition = new Vector3Int(-mapSize / 2, -mapSize / 2, 0);

		float[,] distanceMap = new float[mapSize, mapSize];
		bool[,] visited = new bool[mapSize, mapSize];
		Queue<Vector3Int> queue = new Queue<Vector3Int>();

		for (int x = 0; x < mapSize; x++)
		{
			for (int y = 0; y < mapSize; y++)
			{
				if (landTilemap.GetTile(startPosition + new Vector3Int(x, y, 0)) == sandTile)
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
				Vector3Int neighbor = current + dir;
				if (neighbor.x < 0 || neighbor.x >= mapSize || neighbor.y < 0 || neighbor.y >= mapSize)
					continue;

				if (!visited[neighbor.x, neighbor.y])
				{
					TileBase wTile = waterTilemap.GetTile(startPosition + neighbor);
					if (wTile == settings.waterTile)
					{
						float currentDistance = distanceMap[current.x, current.y];
						float neighborDistance = currentDistance + 1f;
						if (neighborDistance > settings.maxOceanDepth)
							continue;

						distanceMap[neighbor.x, neighbor.y] = neighborDistance;
						queue.Enqueue(neighbor);
						visited[neighbor.x, neighbor.y] = true;
					}
				}
			}
		}

		for (int x = 0; x < mapSize; x++)
		{
			for (int y = 0; y < mapSize; y++)
			{
				if (landTilemap.GetTile(startPosition + new Vector3Int(x, y, 0)) == sandTile)
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

	public bool[,] GetOriginallySand()
	{
		return originallySand;
	}
}