using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PipeNetwork : MonoBehaviour
{
	public static PipeNetwork Instance { get; private set; }

	[SerializeField] private Tilemap pipeTilemap;
	[SerializeField] private TileBase pipeTile;
	[SerializeField] private Tilemap waterTilemap;
	[SerializeField] private Tilemap buildingTilemap;
	[SerializeField] private Tilemap pipeMaskTilemap;
	[SerializeField] private TileBase pipeMaskTile;

	public Tilemap WaterTilemap => waterTilemap;
	public Tilemap BuildingTilemap => buildingTilemap;

	private readonly Dictionary<Vector2Int, IPipeConnectable> _registry = new Dictionary<Vector2Int, IPipeConnectable>();
	private readonly Dictionary<Vector2Int, int> _roundRobinIndex = new Dictionary<Vector2Int, int>();
	private readonly HashSet<Vector3Int> _pipeCells = new HashSet<Vector3Int>();
	private bool _colorsDirty = true;
	private bool _pipeSetNeedsRebuild = true;

	public event System.Action OnColorsDirty;

	private static readonly Vector2Int[] _dirs =
	{
		Vector2Int.up,
		Vector2Int.down,
		Vector2Int.left,
		Vector2Int.right
	};

	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}
		Instance = this;
	}

	private void OnDestroy()
	{
		if (Instance == this)
			Instance = null;
	}

	public bool HasStationAt(Vector2Int pos)
	{
		return _registry.ContainsKey(pos);
	}

	public void Register(IPipeConnectable node)
	{
		_registry[node.GridPosition] = node;
		_pipeSetNeedsRebuild = true;
		MarkColorsDirty();
		RefreshNeighborTiles(node.GridPosition);
	}

	public void Deregister(IPipeConnectable node)
	{
		_registry.Remove(node.GridPosition);
		_pipeSetNeedsRebuild = true;
		MarkColorsDirty();
		RefreshNeighborTiles(node.GridPosition);
	}

	private void RefreshNeighborTiles(Vector2Int pos)
	{
		if (pipeTilemap == null) return;
		for (int i = 0; i < _dirs.Length; i++)
		{
			var n = pos + _dirs[i];
			pipeTilemap.RefreshTile(new Vector3Int(n.x, n.y, 0));
		}
	}

	public void MarkColorsDirty()
	{
		_colorsDirty = true;
		OnColorsDirty?.Invoke();
	}

	public void NotifyPipePlaced(Vector3Int cell)
	{
		_pipeCells.Add(cell);
		if (pipeMaskTilemap != null && pipeMaskTile != null)
			pipeMaskTilemap.SetTile(cell, pipeMaskTile);
		MarkColorsDirty();
	}

	public void NotifyPipeRemoved(Vector3Int cell)
	{
		_pipeCells.Remove(cell);
		if (pipeMaskTilemap != null)
			pipeMaskTilemap.SetTile(cell, null);
		MarkColorsDirty();
	}

	public WaterType GetWaterType(Vector2Int startPos, IPipeConnectable exclude = null, WaterType requiredType = WaterType.None)
	{
		if (pipeTilemap == null || pipeTile == null)
			return WaterType.None;

		var visitedPipes = new HashSet<Vector2Int>();
		var queue = new Queue<Vector2Int>();

		for (int i = 0; i < _dirs.Length; i++)
		{
			var neighbor = startPos + _dirs[i];

			if (_registry.TryGetValue(neighbor, out var adj) && adj != exclude && adj.IsPump && adj.IsActive)
			{
				if (requiredType == WaterType.None || adj.OutputWaterType == requiredType)
					return adj.OutputWaterType;
			}

			if (IsPipeTile(neighbor) && visitedPipes.Add(neighbor))
				queue.Enqueue(neighbor);
		}

		while (queue.Count > 0)
		{
			var pos = queue.Dequeue();

			for (int i = 0; i < _dirs.Length; i++)
			{
				var neighbor = pos + _dirs[i];

				if (_registry.TryGetValue(neighbor, out var node) && node != exclude && node.IsPump && node.IsActive)
				{
					if (requiredType == WaterType.None || node.OutputWaterType == requiredType)
						return node.OutputWaterType;
				}

				if (IsPipeTile(neighbor) && visitedPipes.Add(neighbor))
					queue.Enqueue(neighbor);
			}
		}

		return WaterType.None;
	}

	private bool IsTraversable(Vector2Int pos)
	{
		return IsPipeTile(pos) || _registry.ContainsKey(pos);
	}

	public bool HasConsumableWater(Vector2Int fromPos, WaterType type, IPipeConnectable exclude = null)
	{
		if (pipeTilemap == null || pipeTile == null) return false;

		var visited = new HashSet<Vector2Int>();
		var queue = new Queue<Vector2Int>();

		visited.Add(fromPos);
		for (int i = 0; i < _dirs.Length; i++)
		{
			var n = fromPos + _dirs[i];
			if (visited.Add(n) && IsTraversable(n))
				queue.Enqueue(n);
		}

		while (queue.Count > 0)
		{
			var pos = queue.Dequeue();

			if (_registry.TryGetValue(pos, out var node) && node != exclude)
			{
				if (node.CanConsumeWater(type)) return true;
				if (!node.IsWaterTypeCompatible(type)) continue;
			}

			for (int i = 0; i < _dirs.Length; i++)
			{
				var n = pos + _dirs[i];
				if (visited.Add(n) && IsTraversable(n))
					queue.Enqueue(n);
			}
		}

		return false;
	}

	public bool TryPushWaterUnit(Vector2Int fromPos, WaterType type, IPipeConnectable exclude = null)
	{
		return TryPushWaterUnitCore(fromPos, type, null, exclude);
	}

	public bool TryPushWaterUnit(Vector2Int fromPos, WaterType type, Vector2Int inputSideNode, IPipeConnectable exclude = null)
	{
		return TryPushWaterUnitCore(fromPos, type, inputSideNode, exclude);
	}

	private bool TryPushWaterUnitCore(Vector2Int fromPos, WaterType type, Vector2Int? inputSideNode, IPipeConnectable exclude)
	{
		if (pipeTilemap == null || pipeTile == null) return false;

		var candidates = new List<IPipeConnectable>();
		var globalVisited = new HashSet<Vector2Int>();
		globalVisited.Add(fromPos);

		for (int i = 0; i < _dirs.Length; i++)
		{
			var start = fromPos + _dirs[i];
			if (!IsTraversable(start)) continue;
			if (inputSideNode.HasValue && IsOnInputSide(start, inputSideNode.Value, fromPos)) continue;
			if (!globalVisited.Add(start)) continue;

			var branchQueue = new Queue<Vector2Int>();
			branchQueue.Enqueue(start);
			IPipeConnectable branchFarthest = null;

			while (branchQueue.Count > 0)
			{
				var pos = branchQueue.Dequeue();

				if (_registry.TryGetValue(pos, out var node) && node != exclude)
				{
					if (node.CanReceiveWater(type))
						branchFarthest = node;
					else if (!node.IsWaterTypeCompatible(type))
						continue;
				}

				for (int j = 0; j < _dirs.Length; j++)
				{
					var n = pos + _dirs[j];
					if (IsTraversable(n) && globalVisited.Add(n))
						branchQueue.Enqueue(n);
				}
			}

			if (branchFarthest != null && !candidates.Contains(branchFarthest))
				candidates.Add(branchFarthest);
		}

		if (candidates.Count == 0) return false;

		if (!_roundRobinIndex.TryGetValue(fromPos, out int idx))
			idx = 0;
		idx = idx % candidates.Count;
		candidates[idx].ReceiveWaterUnit(type);
		_roundRobinIndex[fromPos] = (idx + 1) % candidates.Count;
		return true;
	}

	public bool TryConsumeWaterUnit(Vector2Int fromPos, WaterType type, IPipeConnectable exclude = null)
	{
		return TryConsumeWaterUnit(fromPos, type, out _, exclude);
	}

	public bool TryConsumeWaterUnit(Vector2Int fromPos, WaterType type, out Vector2Int consumedFrom, IPipeConnectable exclude = null)
	{
		consumedFrom = default;
		if (pipeTilemap == null || pipeTile == null) return false;

		var visited = new HashSet<Vector2Int>();
		var queue = new Queue<Vector2Int>();

		visited.Add(fromPos);
		for (int i = 0; i < _dirs.Length; i++)
		{
			var n = fromPos + _dirs[i];
			if (visited.Add(n) && IsTraversable(n))
				queue.Enqueue(n);
		}

		while (queue.Count > 0)
		{
			var pos = queue.Dequeue();

			if (_registry.TryGetValue(pos, out var node) && node != exclude)
			{
				if (node.CanConsumeWater(type))
				{
					consumedFrom = pos;
					node.ConsumeWaterUnit();
					return true;
				}
				if (!node.IsWaterTypeCompatible(type)) continue;
			}

			for (int i = 0; i < _dirs.Length; i++)
			{
				var n = pos + _dirs[i];
				if (visited.Add(n) && IsTraversable(n))
					queue.Enqueue(n);
			}
		}

		return false;
	}

	private bool IsOnInputSide(Vector2Int neighborPos, Vector2Int inputNodePos, Vector2Int barrierPos)
	{
		if (neighborPos == inputNodePos) return true;
		if (!IsPipeTile(neighborPos) && !_registry.ContainsKey(neighborPos)) return false;

		var visited = new HashSet<Vector2Int>();
		var queue = new Queue<Vector2Int>();
		visited.Add(barrierPos);
		visited.Add(neighborPos);
		queue.Enqueue(neighborPos);

		while (queue.Count > 0)
		{
			var pos = queue.Dequeue();
			for (int i = 0; i < _dirs.Length; i++)
			{
				var n = pos + _dirs[i];
				if (n == inputNodePos) return true;
				if ((IsPipeTile(n) || _registry.ContainsKey(n)) && visited.Add(n))
					queue.Enqueue(n);
			}
		}
		return false;
	}

	public void ColorizePipes(Color noWaterColor, Color saltWaterColor, Color freshWaterColor)
	{
		if (!_colorsDirty) return;
		if (pipeTilemap == null || pipeTile == null) return;
		if (pipeMaskTilemap == null || pipeMaskTile == null) return;

		_colorsDirty = false;

		if (_pipeSetNeedsRebuild)
		{
			_pipeCells.Clear();
			var bounds = pipeTilemap.cellBounds;
			for (int y = bounds.yMin; y < bounds.yMax; y++)
			{
				for (int x = bounds.xMin; x < bounds.xMax; x++)
				{
					var cell = new Vector3Int(x, y, 0);
					if (pipeTilemap.GetTile(cell) == pipeTile)
						_pipeCells.Add(cell);
				}
			}

			pipeMaskTilemap.ClearAllTiles();
			foreach (var cell in _pipeCells)
				pipeMaskTilemap.SetTile(cell, pipeMaskTile);

			_pipeSetNeedsRebuild = false;
		}

		foreach (var cell in _pipeCells)
		{
			pipeMaskTilemap.SetTileFlags(cell, TileFlags.None);
			pipeMaskTilemap.SetColor(cell, noWaterColor);
		}

		var visited = new HashSet<Vector2Int>();
		foreach (var kvp in _registry)
		{
			var node = kvp.Value;
			if (!node.IsColorSource) continue;
			Color c = node.OutputWaterType == WaterType.SaltWater ? saltWaterColor : freshWaterColor;
			BfsColorFrom(node.GridPosition, c, node.OutputWaterType, visited);
		}
	}

	private void BfsColorFrom(Vector2Int startPos, Color color, WaterType colorType, HashSet<Vector2Int> visited)
	{
		var queue = new Queue<Vector2Int>();

		for (int i = 0; i < _dirs.Length; i++)
		{
			var n = startPos + _dirs[i];
			if (!visited.Add(n)) continue;
			if (IsPipeTile(n) || IsTransparentStation(n, colorType))
				queue.Enqueue(n);
		}

		while (queue.Count > 0)
		{
			var pos = queue.Dequeue();

			if (IsPipeTile(pos))
			{
				var pos3 = new Vector3Int(pos.x, pos.y, 0);
				pipeMaskTilemap.SetTileFlags(pos3, TileFlags.None);
				pipeMaskTilemap.SetColor(pos3, color);
			}

			for (int i = 0; i < _dirs.Length; i++)
			{
				var n = pos + _dirs[i];
				if (!visited.Add(n)) continue;
				if (IsPipeTile(n) || IsTransparentStation(n, colorType))
					queue.Enqueue(n);
			}
		}
	}

	private bool IsTransparentStation(Vector2Int pos, WaterType colorType)
	{
		return _registry.TryGetValue(pos, out var node)
			&& !node.IsColorSource
			&& node.IsWaterTypeCompatible(colorType);
	}

	public Vector2Int WorldToCell(Vector3 worldPos)
	{
		if (pipeTilemap == null)
			return Vector2Int.RoundToInt((Vector2)worldPos);

		return (Vector2Int)pipeTilemap.WorldToCell(worldPos);
	}

	private bool IsPipeTile(Vector2Int pos)
	{
		return pipeTilemap.GetTile(new Vector3Int(pos.x, pos.y, 0)) == pipeTile;
	}
}
