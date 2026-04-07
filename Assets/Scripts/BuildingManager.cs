using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class BuildingManager : MonoBehaviour
{
	[SerializeField] private List<TileResourceRequirement> tileResourceRequirements;
	[SerializeField] private Tilemap buildingTilemap;
	[SerializeField] private Tilemap wallTilemap;
	[SerializeField] private bool showIndoorDebug;

	private Dictionary<TileBase, TileResourceRequirement> requirementsDict;
	private TileBase selectedTile;

	private bool[] reachableGrid;
	private Vector3Int gridOrigin;
	private int gridWidth;
	private int gridHeight;
	private bool _floodFillDirty;

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

	public void PlacePillarsForFloor(Vector3Int gridPos, TileBase floorTile, Tilemap pillarTilemap, int depth)
	{
		TileResourceRequirement req = GetRequirement(floorTile);
		if (req == null || pillarTilemap == null) return;

		TileBase fallback = req.pillarTile;
		TileBase topBottomTile = req.pillarTopBottomTile != null ? req.pillarTopBottomTile : fallback;
		TileBase topTile = req.pillarTopTile != null ? req.pillarTopTile : (req.pillarMiddleTile != null ? req.pillarMiddleTile : fallback);
		TileBase middleTile = req.pillarMiddleTile != null ? req.pillarMiddleTile : fallback;
		TileBase endTile = req.pillarEndTile != null ? req.pillarEndTile : fallback;

		if (depth <= 0 || (topBottomTile == null && endTile == null)) return;

		if (depth == 1)
		{
			pillarTilemap.SetTile(new Vector3Int(gridPos.x, gridPos.y - 1, gridPos.z), topBottomTile);
			return;
		}

		pillarTilemap.SetTile(new Vector3Int(gridPos.x, gridPos.y - 1, gridPos.z), topTile);
		for (int i = 2; i < depth; i++)
		{
			pillarTilemap.SetTile(new Vector3Int(gridPos.x, gridPos.y - i, gridPos.z), middleTile);
		}
		pillarTilemap.SetTile(new Vector3Int(gridPos.x, gridPos.y - depth, gridPos.z), endTile);
	}

	public void RemovePillarsForFloor(Vector3Int gridPos, TileBase floorTile, Tilemap pillarTilemap)
	{
		TileResourceRequirement req = GetRequirement(floorTile);
		if (req == null || pillarTilemap == null) return;

		TileBase fallback = req.pillarTile;
		var pillarTiles = new System.Collections.Generic.HashSet<TileBase>();
		if (req.pillarTopBottomTile != null) pillarTiles.Add(req.pillarTopBottomTile);
		if (req.pillarTopTile != null) pillarTiles.Add(req.pillarTopTile);
		if (req.pillarMiddleTile != null) pillarTiles.Add(req.pillarMiddleTile);
		if (req.pillarEndTile != null) pillarTiles.Add(req.pillarEndTile);
		if (fallback != null) pillarTiles.Add(fallback);

		const int maxPillarDepth = 5;
		for (int i = 1; i <= maxPillarDepth; i++)
		{
			Vector3Int pos = new Vector3Int(gridPos.x, gridPos.y - i, gridPos.z);
			TileBase t = pillarTilemap.GetTile(pos);
			if (t == null || !pillarTiles.Contains(t)) break;
			pillarTilemap.SetTile(pos, null);
		}
	}

	public List<TileResourceRequirement> GetAllTileRequirements() => tileResourceRequirements;

	public TileResourceRequirement GetRequirement(TileBase tile)
	{
		if (tile == null) return null;
		requirementsDict.TryGetValue(tile, out var req);
		return req;
	}

	public void MarkFloodFillDirty()
	{
		_floodFillDirty = true;
	}

	private void LateUpdate()
	{
		if (!_floodFillDirty) return;
		_floodFillDirty = false;
		RunFloodFill();
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

	private void OnDrawGizmos()
	{
		if (!showIndoorDebug || reachableGrid == null || buildingTilemap == null) return;

		Gizmos.color = new Color(0f, 1f, 0.4f, 0.35f);
		for (int y = 0; y < gridHeight; y++)
		{
			for (int x = 0; x < gridWidth; x++)
			{
				if (reachableGrid[y * gridWidth + x]) continue;
				Vector3Int cell = new Vector3Int(gridOrigin.x + x, gridOrigin.y + y, 0);
				if (buildingTilemap.GetTile(cell) == null) continue;
				Gizmos.DrawCube(buildingTilemap.GetCellCenterWorld(cell), new Vector3(0.9f, 0.9f, 0.01f));
			}
		}
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
