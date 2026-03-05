using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapChunkStreamerV3 : MonoBehaviour
{
	[Header("References")]
	public MapGeneratorV3 generator;
	public Transform playerTransform;

	[Header("Tilemaps")]
	public Tilemap waterTilemap;
	public Tilemap landTilemap;
	public Tilemap grassTilemap;
	public Tilemap oceanOverlayTilemap;
	public Tilemap oceanFloorShallowTilemap;
	public Tilemap oceanFloorMediumTilemap;
	public Tilemap oceanFloorDeepTilemap;

	[Header("Streaming")]
	[Min(1)] public int loadRadius = 5;
	[Range(1, 50)] public int layersPerFrame = 7;

	private MapWorkspaceV3 _w;
	private MapSettingsV3 _s;
	private MapDataV3 _d;

	private int _lastPlayerChunkX = int.MinValue;
	private int _lastPlayerChunkY = int.MinValue;

	private readonly Queue<int> _loadQueue = new Queue<int>();
	private readonly HashSet<int> _pendingLoad = new HashSet<int>();
	private Coroutine _streamCoroutine;

	public void OnMapRegenerated(MapWorkspaceV3 workspace, MapDataV3 data, MapSettingsV3 settings)
	{
		_w = workspace;
		_d = data;
		_s = settings;

		_lastPlayerChunkX = int.MinValue;
		_lastPlayerChunkY = int.MinValue;
		_loadQueue.Clear();
		_pendingLoad.Clear();

		_w.EnsureChunkLayerProgress(_w.chunks.Length);
		for (int i = 0; i < _w.chunks.Length; i++) _w.chunkLayerProgress[i] = 0;

		if (_streamCoroutine != null) StopCoroutine(_streamCoroutine);
		_streamCoroutine = StartCoroutine(StreamLoop());

		if (playerTransform != null)
			CheckBoundaryCrossing();
		else
			EnqueueAll();
	}

	public void ForceRefresh()
	{
		if (_w?.chunks == null) return;

		for (int i = 0; i < _w.chunks.Length; i++)
		{
			_w.chunks[i].isLoaded = false;
			_w.chunkLayerProgress[i] = 0;
		}

		_lastPlayerChunkX = int.MinValue;
		_lastPlayerChunkY = int.MinValue;
		_loadQueue.Clear();
		_pendingLoad.Clear();

		if (_streamCoroutine != null) StopCoroutine(_streamCoroutine);
		_streamCoroutine = StartCoroutine(StreamLoop());

		if (playerTransform != null)
			CheckBoundaryCrossing();
		else
			EnqueueAll();
	}

	private void Update()
	{
		if (_w?.chunks == null || playerTransform == null) return;
		CheckBoundaryCrossing();
	}

	private void CheckBoundaryCrossing()
	{
		Vector3Int cell = landTilemap.WorldToCell(playerTransform.position);
		int tileX = cell.x;
		int tileY = cell.y;
		int cs = _w.chunkSize;
		int chunkX = Mathf.Clamp(tileX / cs, 0, _w.chunkCols - 1);
		int chunkY = Mathf.Clamp(tileY / cs, 0, _w.chunkRows - 1);

		if (chunkX == _lastPlayerChunkX && chunkY == _lastPlayerChunkY) return;

		_lastPlayerChunkX = chunkX;
		_lastPlayerChunkY = chunkY;

		EnqueueRadius(chunkX, chunkY);
	}

	private void EnqueueRadius(int playerCX, int playerCY)
	{
		int r = loadRadius;
		for (int dy = -r; dy <= r; dy++)
			for (int dx = -r; dx <= r; dx++)
			{
				int cx = playerCX + dx;
				int cy = playerCY + dy;
				if (cx < 0 || cy < 0 || cx >= _w.chunkCols || cy >= _w.chunkRows) continue;
				int idx = cy * _w.chunkCols + cx;
				if (!_w.chunks[idx].isLoaded && !_pendingLoad.Contains(idx))
				{
					_loadQueue.Enqueue(idx);
					_pendingLoad.Add(idx);
				}
			}
	}

	private void EnqueueAll()
	{
		int centerCX = _w.chunkCols / 2;
		int centerCY = _w.chunkRows / 2;
		int maxR = Mathf.Max(_w.chunkCols, _w.chunkRows);

		for (int r = 0; r <= maxR; r++)
			for (int dy = -r; dy <= r; dy++)
				for (int dx = -r; dx <= r; dx++)
				{
					if (Mathf.Abs(dx) != r && Mathf.Abs(dy) != r) continue;
					int cx = centerCX + dx;
					int cy = centerCY + dy;
					if (cx < 0 || cy < 0 || cx >= _w.chunkCols || cy >= _w.chunkRows) continue;
					int idx = cy * _w.chunkCols + cx;
					if (!_w.chunks[idx].isLoaded && !_pendingLoad.Contains(idx))
					{
						_loadQueue.Enqueue(idx);
						_pendingLoad.Add(idx);
					}
				}
	}

	private IEnumerator StreamLoop()
	{
		while (true)
		{
			int layersWritten = 0;

			while (_loadQueue.Count > 0 && layersWritten < layersPerFrame)
			{
				int idx = _loadQueue.Peek();

				if (_w.chunks[idx].isLoaded)
				{
					_loadQueue.Dequeue();
					_pendingLoad.Remove(idx);
					_w.chunkLayerProgress[idx] = 0;
					continue;
				}

				int layer = _w.chunkLayerProgress[idx];

				MapPainterV3.PaintChunkLayer(idx, layer, _d, _s,
					waterTilemap, landTilemap, grassTilemap,
					oceanOverlayTilemap,
					oceanFloorShallowTilemap, oceanFloorMediumTilemap, oceanFloorDeepTilemap,
					_w);

				layer++;
				layersWritten++;
				_w.chunkLayerProgress[idx] = layer;

				if (layer >= MapPainterV3.LAYER_COUNT)
				{
					_w.chunks[idx].isLoaded = true;
					_loadQueue.Dequeue();
					_pendingLoad.Remove(idx);
					_w.chunkLayerProgress[idx] = 0;
				}
				else
				{
					_loadQueue.Dequeue();
					_loadQueue.Enqueue(idx);
				}
			}

			yield return null;
		}
	}
}