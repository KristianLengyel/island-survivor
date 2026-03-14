using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class BuildingManager : MonoBehaviour
{
	[SerializeField] private List<TileResourceRequirement> tileResourceRequirements;
	[SerializeField] private Tilemap buildingTilemap;
	[SerializeField] private Tilemap wallTilemap;

	private Dictionary<TileBase, TileResourceRequirement> requirementsDict;
	private TileBase selectedTile;

	private bool[] reachableGrid;
	private Vector3Int gridOrigin;
	private int gridWidth;
	private int gridHeight;

	private void Awake()
	{
		requirementsDict = new Dictionary<TileBase, TileResourceRequirement>();
		foreach (var req in tileResourceRequirements)
		{
			if (req.tile != null && !requirementsDict.ContainsKey(req.tile))
				requirementsDict.Add(req.tile, req);
			if (req.altTile != null && !requirementsDict.ContainsKey(req.altTile))
				requirementsDict.Add(req.altTile, req);
		}
	}

	public void SetSelectedTile(TileBase tile)
	{
		selectedTile = tile;
	}

	public TileBase GetSelectedTile()
	{
		return selectedTile;
	}

	public TileCategory GetTileCategory(TileBase tile)
	{
		if (tile == null) return TileCategory.Floor;
		requirementsDict.TryGetValue(tile, out var req);
		return req != null ? req.tileCategory : TileCategory.Floor;
	}

	public TileBase ResolveDoorTile(TileBase selected, Vector3Int gridPos)
	{
		var req = GetRequirement(selected);
		if (req == null || req.tileCategory != TileCategory.Door || req.altTile == null)
			return selected;

		bool hasLeft  = IsWallTileAt(new Vector3Int(gridPos.x - 1, gridPos.y, gridPos.z));
		bool hasRight = IsWallTileAt(new Vector3Int(gridPos.x + 1, gridPos.y, gridPos.z));
		bool hasUp    = IsWallTileAt(new Vector3Int(gridPos.x, gridPos.y + 1, gridPos.z));
		bool hasDown  = IsWallTileAt(new Vector3Int(gridPos.x, gridPos.y - 1, gridPos.z));

		bool horizontalWall = (hasLeft || hasRight) && !hasUp && !hasDown;

		return horizontalWall ? req.altTile : req.tile;
	}

	private bool IsWallTileAt(Vector3Int pos)
	{
		if (wallTilemap == null) return false;
		TileBase tile = wallTilemap.GetTile(pos);
		if (tile == null) return false;
		requirementsDict.TryGetValue(tile, out var req);
		return req != null && req.tileCategory == TileCategory.Wall;
	}

	public bool HasEnoughResources(TileBase tile)
	{
		TileResourceRequirement req = GetRequirement(tile);
		if (req == null) return true;

		var inv = GameManager.Instance.InventoryManager;

		foreach (var resource in req.resourceRequirements)
		{
			if (inv.GetItemCount(resource.resource.name) < resource.amount)
			{
				return false;
			}
		}
		return true;
	}

	public void UseResources(TileBase tile)
	{
		TileResourceRequirement req = GetRequirement(tile);
		if (req != null)
		{
			GameManager.Instance.InventoryManager.UseResources(req.resourceRequirements);
		}
	}

	public List<ResourceRequirement> GetResourcesReturnedOnDestroy(TileBase tile)
	{
		TileResourceRequirement req = GetRequirement(tile);
		return req?.resourcesReturnedOnDestroy;
	}

	public TileBase GetPillarTileForFloor(TileBase tile)
	{
		TileResourceRequirement req = GetRequirement(tile);
		return req?.pillarTile;
	}

	private TileResourceRequirement GetRequirement(TileBase tile)
	{
		if (tile == null) return null;
		requirementsDict.TryGetValue(tile, out var req);
		return req;
	}

	public void RunFloodFill()
	{
		if (buildingTilemap == null)
		{
			reachableGrid = null;
			return;
		}

		buildingTilemap.CompressBounds();
		if (wallTilemap != null) wallTilemap.CompressBounds();

		BoundsInt floorBounds = buildingTilemap.cellBounds;
		BoundsInt wallBounds = wallTilemap != null ? wallTilemap.cellBounds : new BoundsInt();

		int minX = floorBounds.size.x > 0 ? floorBounds.xMin : 0;
		int minY = floorBounds.size.y > 0 ? floorBounds.yMin : 0;
		int maxX = floorBounds.size.x > 0 ? floorBounds.xMax : 0;
		int maxY = floorBounds.size.y > 0 ? floorBounds.yMax : 0;

		if (wallBounds.size.x > 0)
		{
			minX = Mathf.Min(minX, wallBounds.xMin);
			minY = Mathf.Min(minY, wallBounds.yMin);
			maxX = Mathf.Max(maxX, wallBounds.xMax);
			maxY = Mathf.Max(maxY, wallBounds.yMax);
		}

		if (maxX <= minX || maxY <= minY)
		{
			reachableGrid = null;
			return;
		}

		minX -= 1; minY -= 1;
		maxX += 1; maxY += 1;

		int w = maxX - minX;
		int h = maxY - minY;

		gridOrigin = new Vector3Int(minX, minY, 0);
		gridWidth = w;
		gridHeight = h;

		var reachable = new bool[w * h];
		var queue = new Queue<Vector3Int>();

		for (int x = minX; x < maxX; x++)
		{
			TryEnqueue(new Vector3Int(x, minY, 0), minX, minY, w, h, reachable, queue);
			TryEnqueue(new Vector3Int(x, maxY - 1, 0), minX, minY, w, h, reachable, queue);
		}
		for (int y = minY + 1; y < maxY - 1; y++)
		{
			TryEnqueue(new Vector3Int(minX, y, 0), minX, minY, w, h, reachable, queue);
			TryEnqueue(new Vector3Int(maxX - 1, y, 0), minX, minY, w, h, reachable, queue);
		}

		while (queue.Count > 0)
		{
			Vector3Int cell = queue.Dequeue();
			TryEnqueue(new Vector3Int(cell.x + 1, cell.y, cell.z), minX, minY, w, h, reachable, queue);
			TryEnqueue(new Vector3Int(cell.x - 1, cell.y, cell.z), minX, minY, w, h, reachable, queue);
			TryEnqueue(new Vector3Int(cell.x, cell.y + 1, cell.z), minX, minY, w, h, reachable, queue);
			TryEnqueue(new Vector3Int(cell.x, cell.y - 1, cell.z), minX, minY, w, h, reachable, queue);
		}

		reachableGrid = reachable;
	}

	private void TryEnqueue(Vector3Int cell, int minX, int minY, int w, int h, bool[] reachable, Queue<Vector3Int> queue)
	{
		int lx = cell.x - minX;
		int ly = cell.y - minY;
		if (lx < 0 || ly < 0 || lx >= w || ly >= h) return;
		int idx = ly * w + lx;
		if (reachable[idx]) return;

		if (wallTilemap != null && wallTilemap.GetTile(cell) != null)
			return;

		reachable[idx] = true;
		queue.Enqueue(cell);
	}

	public bool IsIndoors(Vector3 worldPos)
	{
		if (reachableGrid == null || buildingTilemap == null) return false;

		Vector3Int cell = buildingTilemap.WorldToCell(worldPos);
		int lx = cell.x - gridOrigin.x;
		int ly = cell.y - gridOrigin.y;

		if (lx < 0 || ly < 0 || lx >= gridWidth || ly >= gridHeight) return false;
		if (reachableGrid[ly * gridWidth + lx]) return false;

		return buildingTilemap.GetTile(cell) != null;
	}
}
