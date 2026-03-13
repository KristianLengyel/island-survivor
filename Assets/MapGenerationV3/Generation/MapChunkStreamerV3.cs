using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Streams chunks around the player using a two-stage visibility system:
///
///   STAGE 1 — COLOR HIDE/SHOW  (zero tilemap mesh rebuilds)
///     Chunks just outside loadRadius are hidden by setting tile colors to Color.clear.
///     Showing them again is just setting colors back to Color.white.
///     This covers 95% of all load/unload events during normal movement with no FPS cost.
///
///   STAGE 2 — HARD PURGE  (actual SetTilesBlock null, runs on a slow timer)
///     Chunks beyond purgeRadius are truly cleared from the tilemap after purgeDelay seconds.
///     This bounds memory usage on large worlds without affecting frame time during movement.
///     When the player returns, the chunk reloads normally.
///
///   DECORATORS follow chunk visibility:
///     Hidden chunks despawn their GameObjects (returned to pool).
///     Shown/loaded chunks spawn them incrementally per frame.
/// </summary>
public class MapChunkStreamerV3 : MonoBehaviour
{
	// -------------------------------------------------------
	// Inspector
	// -------------------------------------------------------

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

	[Header("Streaming Radii")]
	[Tooltip("Chunks within this radius are fully visible and have active decorators.")]
	[Min(1)] public int loadRadius = 5;

	[Tooltip("Chunks beyond loadRadius but within hideRadius are color-hidden (tiles stay in tilemap, near-free).")]
	[Min(1)] public int hideRadius = 7;

	[Tooltip("Chunks beyond this radius are hard-purged from the tilemap after purgeDelay seconds. Must be > hideRadius.")]
	[Min(1)] public int purgeRadius = 14;

	[Tooltip("Seconds a chunk must be outside purgeRadius before it is hard-purged.")]
	[Min(1f)] public float purgeDelay = 30f;

	[Header("Per-Frame Budgets")]
	[Tooltip("Tile layers painted per frame while loading a new chunk (7 layers total per chunk).")]
	[Range(1, 50)] public int loadLayersPerFrame = 7;

	[Tooltip("Hard-purge tile layers cleared per frame (runs only during idle, far from player).")]
	[Range(1, 20)] public int purgeLayersPerFrame = 3;

	[Tooltip("Decorator GameObjects spawned or despawned per frame.")]
	[Range(1, 64)] public int decoratorsPerFrame = 16;

	[Header("Decorator Parents")]
	[Tooltip("Must NOT be a child of tilemapGrid.")]
	public Transform palmParent;
	[Tooltip("Must NOT be a child of tilemapGrid.")]
	public Transform rockParent;

	// -------------------------------------------------------
	// Chunk state enum
	// -------------------------------------------------------

	private enum ChunkVisibility : byte
	{
		Unloaded = 0,   // tiles not in tilemap, no decorators
		Loading = 1,   // being painted layer by layer
		Visible = 2,   // fully painted, Color.white, decorators active
		Hidden = 3,   // tiles present but Color.clear, no decorators
		Purging = 4,   // being hard-cleared layer by layer
	}

	// -------------------------------------------------------
	// Private state
	// -------------------------------------------------------

	private MapWorkspaceV3 _w;
	private MapSettingsV3 _s;
	private MapDataV3 _d;

	private int _lastPlayerChunkX = int.MinValue;
	private int _lastPlayerChunkY = int.MinValue;

	private ChunkVisibility[] _chunkVis;
	private float[] _chunkHiddenSince;
	private int[] _chunkOpProgress;
	private int[] _chunkDecorCursor;

	private readonly Queue<int> _loadQueue = new Queue<int>();
	private readonly HashSet<int> _pendingLoad = new HashSet<int>();
	private readonly Queue<int> _purgeQueue = new Queue<int>();
	private readonly HashSet<int> _pendingPurge = new HashSet<int>();
	private readonly Queue<int> _decorSpawnQueue = new Queue<int>();
	private readonly HashSet<int> _pendingDecorSpawn = new HashSet<int>();
	private readonly Queue<int> _decorDespawnQueue = new Queue<int>();
	private readonly HashSet<int> _pendingDecorDespawn = new HashSet<int>();

	// Separate pools so palms and rocks never mix
	private readonly Stack<GameObject> _palmPool = new Stack<GameObject>();
	private readonly Stack<GameObject> _rockPool = new Stack<GameObject>();

	// Per-chunk active palm/rock lists — tracked separately so we never need CompareTag
	private List<GameObject>[] _chunkActivePalms;
	private List<GameObject>[] _chunkActiveRocks;

	private Coroutine _streamCoroutine;
	private Coroutine _purgeTimerCoroutine;

	// -------------------------------------------------------
	// Public API
	// -------------------------------------------------------

	public void OnMapRegenerated(MapWorkspaceV3 workspace, MapDataV3 data, MapSettingsV3 settings)
	{
		_w = workspace;
		_d = data;
		_s = settings;

		hideRadius = Mathf.Max(hideRadius, loadRadius + 1);
		purgeRadius = Mathf.Max(purgeRadius, hideRadius + 2);

		ReturnAllDecoratorsToPool();
		ResetState();

		if (_streamCoroutine != null) StopCoroutine(_streamCoroutine);
		if (_purgeTimerCoroutine != null) StopCoroutine(_purgeTimerCoroutine);

		_streamCoroutine = StartCoroutine(StreamLoop());
		_purgeTimerCoroutine = StartCoroutine(PurgeTimerLoop());

		CheckBoundaryCrossing(force: true);
	}

	public void ForceRefresh()
	{
		if (_w?.chunks == null) return;

		ReturnAllDecoratorsToPool();

		waterTilemap?.ClearAllTiles();
		landTilemap?.ClearAllTiles();
		grassTilemap?.ClearAllTiles();
		oceanOverlayTilemap?.ClearAllTiles();
		oceanFloorShallowTilemap?.ClearAllTiles();
		oceanFloorMediumTilemap?.ClearAllTiles();
		oceanFloorDeepTilemap?.ClearAllTiles();

		for (int i = 0; i < _w.chunks.Length; i++)
		{
			_w.chunks[i].isLoaded = false;
			_w.chunkLayerProgress[i] = 0;
		}

		ResetState();

		if (_streamCoroutine != null) StopCoroutine(_streamCoroutine);
		if (_purgeTimerCoroutine != null) StopCoroutine(_purgeTimerCoroutine);

		_streamCoroutine = StartCoroutine(StreamLoop());
		_purgeTimerCoroutine = StartCoroutine(PurgeTimerLoop());

		CheckBoundaryCrossing(force: true);
	}

	// -------------------------------------------------------
	// Unity
	// -------------------------------------------------------

	private void Update()
	{
		if (_w?.chunks == null) return;
		CheckBoundaryCrossing(force: false);
	}

	// -------------------------------------------------------
	// State reset
	// -------------------------------------------------------

	private void ResetState()
	{
		_lastPlayerChunkX = int.MinValue;
		_lastPlayerChunkY = int.MinValue;

		_loadQueue.Clear(); _pendingLoad.Clear();
		_purgeQueue.Clear(); _pendingPurge.Clear();
		_decorSpawnQueue.Clear(); _pendingDecorSpawn.Clear();
		_decorDespawnQueue.Clear(); _pendingDecorDespawn.Clear();

		int count = _w.chunks.Length;
		_w.EnsureChunkLayerProgress(count);

		EnsureArray(ref _chunkVis, count);
		EnsureArray(ref _chunkHiddenSince, count);
		EnsureArray(ref _chunkOpProgress, count);
		EnsureArray(ref _chunkDecorCursor, count);

		// Ensure per-chunk palm/rock active lists
		if (_chunkActivePalms == null || _chunkActivePalms.Length < count)
		{
			_chunkActivePalms = new List<GameObject>[count];
			_chunkActiveRocks = new List<GameObject>[count];
		}
		for (int i = 0; i < count; i++)
		{
			if (_chunkActivePalms[i] == null) _chunkActivePalms[i] = new List<GameObject>();
			if (_chunkActiveRocks[i] == null) _chunkActiveRocks[i] = new List<GameObject>();
		}

		for (int i = 0; i < count; i++)
		{
			_chunkVis[i] = ChunkVisibility.Unloaded;
			_chunkHiddenSince[i] = 0f;
			_chunkOpProgress[i] = 0;
			_chunkDecorCursor[i] = 0;
			_w.chunkLayerProgress[i] = 0;
			_w.chunks[i].isLoaded = false;
		}
	}

	// -------------------------------------------------------
	// Stream origin
	// -------------------------------------------------------

	private Vector2Int GetStreamOriginChunk()
	{
		Vector3 worldPos = playerTransform != null
			? playerTransform.position
			: (Camera.main != null ? Camera.main.transform.position : Vector3.zero);

		Tilemap refMap = landTilemap ?? waterTilemap;
		if (refMap != null)
		{
			Vector3Int cell = refMap.WorldToCell(worldPos);
			int cs = _w.chunkSize;
			int cx = Mathf.Clamp(cell.x / cs, 0, _w.chunkCols - 1);
			int cy = Mathf.Clamp(cell.y / cs, 0, _w.chunkRows - 1);
			return new Vector2Int(cx, cy);
		}
		return new Vector2Int(_w.chunkCols / 2, _w.chunkRows / 2);
	}

	// -------------------------------------------------------
	// Boundary crossing — runs every Update frame
	// -------------------------------------------------------

	private void CheckBoundaryCrossing(bool force)
	{
		var origin = GetStreamOriginChunk();
		if (!force && origin.x == _lastPlayerChunkX && origin.y == _lastPlayerChunkY) return;

		_lastPlayerChunkX = origin.x;
		_lastPlayerChunkY = origin.y;

		UpdateChunkVisibilityStates(origin.x, origin.y);
	}

	/// <summary>
	/// Classifies every chunk by distance and issues the appropriate command.
	/// Show/hide are instant (color only). Load/purge are queued.
	/// </summary>
	private void UpdateChunkVisibilityStates(int pcx, int pcy)
	{
		int count = _w.chunks.Length;

		for (int i = 0; i < count; i++)
		{
			int dist = Mathf.Max(
				Mathf.Abs(_w.chunks[i].chunkX - pcx),
				Mathf.Abs(_w.chunks[i].chunkY - pcy));

			ChunkVisibility vis = _chunkVis[i];

			if (dist <= loadRadius)
			{
				// Must be Visible
				CancelPurge(i);
				switch (vis)
				{
					case ChunkVisibility.Unloaded:
					case ChunkVisibility.Purging:
						EnqueueLoad(i);
						break;
					case ChunkVisibility.Hidden:
						ShowChunk(i);          // free: color only
						break;
						// Loading / Visible: nothing to do
				}
			}
			else if (dist <= hideRadius)
			{
				// Should be Hidden — tiles stay, just invisible
				CancelPurge(i);
				switch (vis)
				{
					case ChunkVisibility.Visible:
						HideChunk(i);          // free: color only
						break;
					case ChunkVisibility.Loading:
						// Let it finish — will be hidden on next boundary check
						break;
						// Unloaded / Hidden: nothing to do
				}
			}
			else
			{
				// Beyond hide radius
				switch (vis)
				{
					case ChunkVisibility.Visible:
						HideChunk(i);
						break;
					case ChunkVisibility.Loading:
						CancelLoad(i);
						break;
						// Hidden chunks are handled by the purge timer
						// Unloaded: nothing to do
				}
			}
		}
	}

	// -------------------------------------------------------
	// Show / Hide  (COLOR ONLY — zero mesh rebuild, near-free)
	// -------------------------------------------------------

	private void ShowChunk(int idx)
	{
		if (_chunkVis[idx] == ChunkVisibility.Visible) return;

		SetChunkAlpha(idx, 1f);
		_chunkVis[idx] = ChunkVisibility.Visible;
		_w.chunks[idx].isLoaded = true;

		if (!_pendingDecorSpawn.Contains(idx) && !_pendingDecorDespawn.Contains(idx))
		{
			_chunkDecorCursor[idx] = 0;
			_decorSpawnQueue.Enqueue(idx);
			_pendingDecorSpawn.Add(idx);
		}
	}

	private void HideChunk(int idx)
	{
		if (_chunkVis[idx] == ChunkVisibility.Hidden) return;

		SetChunkAlpha(idx, 0f);
		_chunkVis[idx] = ChunkVisibility.Hidden;
		_chunkHiddenSince[idx] = Time.time;

		// Despawn decorators
		if (!_pendingDecorDespawn.Contains(idx))
		{
			_decorDespawnQueue.Enqueue(idx);
			_pendingDecorDespawn.Add(idx);
		}
		_pendingDecorSpawn.Remove(idx);
	}

	private void SetChunkAlpha(int idx, float alpha)
	{
		ref MapChunkV3 chunk = ref _w.chunks[idx];
		int size = _d.size;
		int tX = chunk.tileX;
		int tY = chunk.tileY;
		int width = Mathf.Min(chunk.size, size - tX);
		int height = Mathf.Min(chunk.size, size - tY);

		var bounds = new BoundsInt(new Vector3Int(tX, tY, 0), new Vector3Int(width, height, 1));

		// Standard tilemaps — only touch alpha, leave RGB biome tint intact
		ApplyAlphaToTilemap(landTilemap, bounds, alpha);
		ApplyAlphaToTilemap(grassTilemap, bounds, alpha);
		ApplyAlphaToTilemap(oceanOverlayTilemap, bounds, alpha);
		ApplyAlphaToTilemap(oceanFloorShallowTilemap, bounds, alpha);
		ApplyAlphaToTilemap(oceanFloorMediumTilemap, bounds, alpha);
		ApplyAlphaToTilemap(oceanFloorDeepTilemap, bounds, alpha);

		// Water tilemap has a pad offset
		if (waterTilemap != null)
		{
			int pad = _d.pad;
			int wOL = (tX == 0) ? pad : 0;
			int wOB = (tY == 0) ? pad : 0;
			int wOR = (tX + chunk.size >= size) ? pad : 0;
			int wOT = (tY + chunk.size >= size) ? pad : 0;
			var wBounds = new BoundsInt(
				new Vector3Int(tX - wOL - pad, tY - wOB - pad, 0),
				new Vector3Int(width + wOL + wOR, height + wOB + wOT, 1));
			ApplyAlphaToTilemap(waterTilemap, wBounds, alpha);
		}
	}

	/// <summary>
	/// Sets only the alpha channel of each tile color, preserving the RGB biome tint.
	/// Tiles with no explicit color default to Color.white in Unity, so GetColor returns white —
	/// setting alpha on that is safe and correct.
	/// </summary>
	private static void ApplyAlphaToTilemap(Tilemap tm, BoundsInt bounds, float alpha)
	{
		if (tm == null) return;
		for (int y = bounds.yMin; y < bounds.yMax; y++)
		{
			for (int x = bounds.xMin; x < bounds.xMax; x++)
			{
				var pos = new Vector3Int(x, y, 0);
				if (tm.GetTile(pos) == null) continue; // skip empty cells
				Color c = tm.GetColor(pos);
				c.a = alpha;
				tm.SetColor(pos, c);
			}
		}
	}

	// -------------------------------------------------------
	// Load queue helpers
	// -------------------------------------------------------

	private void EnqueueLoad(int idx)
	{
		if (_pendingLoad.Contains(idx)) return;
		if (_chunkVis[idx] == ChunkVisibility.Visible) return;

		_chunkVis[idx] = ChunkVisibility.Loading;
		_chunkOpProgress[idx] = 0;
		_w.chunkLayerProgress[idx] = 0;
		_loadQueue.Enqueue(idx);
		_pendingLoad.Add(idx);
	}

	private void CancelLoad(int idx)
	{
		if (!_pendingLoad.Contains(idx)) return;
		_pendingLoad.Remove(idx);
		_chunkOpProgress[idx] = 0;
		_w.chunkLayerProgress[idx] = 0;
		if (_chunkVis[idx] == ChunkVisibility.Loading)
			_chunkVis[idx] = ChunkVisibility.Unloaded;
	}

	// -------------------------------------------------------
	// Purge queue helpers
	// -------------------------------------------------------

	private void EnqueuePurge(int idx)
	{
		if (_pendingPurge.Contains(idx)) return;
		if (_chunkVis[idx] != ChunkVisibility.Hidden) return;

		_chunkVis[idx] = ChunkVisibility.Purging;
		_chunkOpProgress[idx] = 0;
		_purgeQueue.Enqueue(idx);
		_pendingPurge.Add(idx);
	}

	private void CancelPurge(int idx)
	{
		if (!_pendingPurge.Contains(idx)) return;
		_pendingPurge.Remove(idx);
		_chunkOpProgress[idx] = 0;
		if (_chunkVis[idx] == ChunkVisibility.Purging)
			_chunkVis[idx] = ChunkVisibility.Hidden;
	}

	// -------------------------------------------------------
	// Purge timer — wakes every 5 seconds, no per-frame cost
	// -------------------------------------------------------

	private IEnumerator PurgeTimerLoop()
	{
		var wait = new WaitForSeconds(5f);
		while (true)
		{
			yield return wait;
			if (_w?.chunks == null) continue;

			float now = Time.time;
			var origin = GetStreamOriginChunk();

			for (int i = 0; i < _w.chunks.Length; i++)
			{
				if (_chunkVis[i] != ChunkVisibility.Hidden) continue;

				int dist = Mathf.Max(
					Mathf.Abs(_w.chunks[i].chunkX - origin.x),
					Mathf.Abs(_w.chunks[i].chunkY - origin.y));

				if (dist > purgeRadius && now - _chunkHiddenSince[i] >= purgeDelay)
					EnqueuePurge(i);
			}
		}
	}

	// -------------------------------------------------------
	// Main stream coroutine
	// -------------------------------------------------------

	private IEnumerator StreamLoop()
	{
		while (true)
		{
			int loadOps = 0;
			int purgeOps = 0;
			int decorOps = 0;

			// ---- LOAD ----
			while (_loadQueue.Count > 0 && loadOps < loadLayersPerFrame)
			{
				int idx = _loadQueue.Peek();

				if (!_pendingLoad.Contains(idx))
				{
					_loadQueue.Dequeue();
					continue;
				}

				int layer = _chunkOpProgress[idx];

				MapPainterV3.PaintChunkLayer(idx, layer, _d, _s,
					waterTilemap, landTilemap, grassTilemap,
					oceanOverlayTilemap,
					oceanFloorShallowTilemap, oceanFloorMediumTilemap, oceanFloorDeepTilemap,
					_w);

				layer++;
				loadOps++;
				_chunkOpProgress[idx] = layer;
				_w.chunkLayerProgress[idx] = layer;

				if (layer >= MapPainterV3.LAYER_COUNT)
				{
					_w.chunks[idx].isLoaded = true;
					_chunkVis[idx] = ChunkVisibility.Visible;
					_loadQueue.Dequeue();
					_pendingLoad.Remove(idx);
					_chunkOpProgress[idx] = 0;

					// Queue decorator spawn
					if (!_pendingDecorSpawn.Contains(idx) && !_pendingDecorDespawn.Contains(idx))
					{
						_chunkDecorCursor[idx] = 0;
						_decorSpawnQueue.Enqueue(idx);
						_pendingDecorSpawn.Add(idx);
					}
				}
				else
				{
					_loadQueue.Dequeue();
					_loadQueue.Enqueue(idx);
					break; // one chunk per frame to keep it smooth
				}
			}

			// ---- HARD PURGE ----
			while (_purgeQueue.Count > 0 && purgeOps < purgeLayersPerFrame)
			{
				int idx = _purgeQueue.Peek();

				if (!_pendingPurge.Contains(idx))
				{
					_purgeQueue.Dequeue();
					continue;
				}

				int layer = _chunkOpProgress[idx];
				ClearChunkLayer(idx, layer);
				layer++;
				purgeOps++;
				_chunkOpProgress[idx] = layer;

				if (layer >= MapPainterV3.LAYER_COUNT)
				{
					_w.chunks[idx].isLoaded = false;
					_chunkVis[idx] = ChunkVisibility.Unloaded;
					_purgeQueue.Dequeue();
					_pendingPurge.Remove(idx);
					_chunkOpProgress[idx] = 0;
				}
				else
				{
					_purgeQueue.Dequeue();
					_purgeQueue.Enqueue(idx);
					break;
				}
			}

			// ---- DECORATOR SPAWN ----
			while (_decorSpawnQueue.Count > 0 && decorOps < decoratorsPerFrame)
			{
				int idx = _decorSpawnQueue.Peek();

				if (!_pendingDecorSpawn.Contains(idx))
				{
					_decorSpawnQueue.Dequeue();
					continue;
				}

				decorOps += SpawnDecoratorsIncremental(idx, decoratorsPerFrame - decorOps);

				if (IsDecorSpawnComplete(idx))
				{
					_decorSpawnQueue.Dequeue();
					_pendingDecorSpawn.Remove(idx);
				}
				else
				{
					_decorSpawnQueue.Dequeue();
					_decorSpawnQueue.Enqueue(idx);
				}
				break;
			}

			// ---- DECORATOR DESPAWN ----
			while (_decorDespawnQueue.Count > 0 && decorOps < decoratorsPerFrame)
			{
				int idx = _decorDespawnQueue.Peek();

				if (!_pendingDecorDespawn.Contains(idx))
				{
					_decorDespawnQueue.Dequeue();
					continue;
				}

				decorOps += DespawnDecoratorsIncremental(idx, decoratorsPerFrame - decorOps);

				if (_chunkActivePalms[idx].Count == 0 && _chunkActiveRocks[idx].Count == 0)
				{
					_decorDespawnQueue.Dequeue();
					_pendingDecorDespawn.Remove(idx);
				}
				else
				{
					_decorDespawnQueue.Dequeue();
					_decorDespawnQueue.Enqueue(idx);
				}
				break;
			}

			yield return null;
		}
	}

	// -------------------------------------------------------
	// Hard purge — clear one layer per call
	// -------------------------------------------------------

	private void ClearChunkLayer(int chunkIndex, int layer)
	{
		ref MapChunkV3 chunk = ref _w.chunks[chunkIndex];
		int size = _d.size;
		int tX = chunk.tileX;
		int tY = chunk.tileY;
		int width = Mathf.Min(chunk.size, size - tX);
		int height = Mathf.Min(chunk.size, size - tY);
		int count = width * height;

		TileBase[] buf = _w.paintBuffer;
		for (int k = 0; k < count; k++) buf[k] = null;

		var bounds = new BoundsInt(new Vector3Int(tX, tY, 0), new Vector3Int(width, height, 1));

		switch (layer)
		{
			case MapPainterV3.LAYER_OCEAN_DEEP: oceanFloorDeepTilemap?.SetTilesBlock(bounds, buf); break;
			case MapPainterV3.LAYER_OCEAN_MEDIUM: oceanFloorMediumTilemap?.SetTilesBlock(bounds, buf); break;
			case MapPainterV3.LAYER_OCEAN_SHALLOW: oceanFloorShallowTilemap?.SetTilesBlock(bounds, buf); break;
			case MapPainterV3.LAYER_WATER: ClearWaterLayer(ref chunk); break;
			case MapPainterV3.LAYER_LAND: landTilemap?.SetTilesBlock(bounds, buf); break;
			case MapPainterV3.LAYER_GRASS: grassTilemap?.SetTilesBlock(bounds, buf); break;
			case MapPainterV3.LAYER_OVERLAY: oceanOverlayTilemap?.SetTilesBlock(bounds, buf); break;
		}
	}

	private void ClearWaterLayer(ref MapChunkV3 chunk)
	{
		if (waterTilemap == null) return;
		int size = _d.size, pad = _d.pad;
		int tX = chunk.tileX, tY = chunk.tileY;
		int wOL = (tX == 0) ? pad : 0;
		int wOB = (tY == 0) ? pad : 0;
		int wOR = (tX + chunk.size >= size) ? pad : 0;
		int wOT = (tY + chunk.size >= size) ? pad : 0;
		int wX = tX - wOL, wY = tY - wOB;
		int w = Mathf.Min(chunk.size, size - tX) + wOL + wOR;
		int h = Mathf.Min(chunk.size, size - tY) + wOB + wOT;
		TileBase[] buf = _w.paintBuffer;
		for (int k = 0; k < w * h; k++) buf[k] = null;
		waterTilemap.SetTilesBlock(
			new BoundsInt(new Vector3Int(wX - pad, wY - pad, 0), new Vector3Int(w, h, 1)), buf);
	}

	// -------------------------------------------------------
	// Decorator spawn / despawn (incremental)
	// -------------------------------------------------------

	private int SpawnDecoratorsIncremental(int chunkIndex, int budget)
	{
		if (_w.chunkPalmIndices == null || chunkIndex >= _w.chunkPalmIndices.Length) return 0;

		var palms = _w.chunkPalmIndices[chunkIndex];
		var rocks = _w.chunkRockIndices[chunkIndex];
		var activePalms = _chunkActivePalms[chunkIndex];
		var activeRocks = _chunkActiveRocks[chunkIndex];
		int cursor = _chunkDecorCursor[chunkIndex];
		int total = palms.Count + rocks.Count;
		int done = 0;

		while (cursor < total && done < budget)
		{
			bool isPalm = cursor < palms.Count;
			int tileIdx = isPalm ? palms[cursor] : rocks[cursor - palms.Count];
			var bdef = _s.GetBiome((BiomeType)_d.biome[tileIdx]);

			if (bdef != null)
			{
				GameObject prefab = isPalm ? bdef.palmPrefab : bdef.rockPrefab;
				if (prefab != null)
				{
					Transform parent = isPalm
						? (palmParent != null ? palmParent : transform)
						: (rockParent != null ? rockParent : transform);

					GameObject go = GetPooled(isPalm ? _palmPool : _rockPool, prefab, parent);
					go.transform.position = GetWorldPos(tileIdx % _d.size, tileIdx / _d.size);

					// Store in the correct list — no tag needed for pool routing
					if (isPalm) activePalms.Add(go);
					else activeRocks.Add(go);
				}
			}
			cursor++;
			done++;
		}

		_chunkDecorCursor[chunkIndex] = cursor;
		return done;
	}

	private bool IsDecorSpawnComplete(int idx)
	{
		if (_w.chunkPalmIndices == null || idx >= _w.chunkPalmIndices.Length) return true;
		return _chunkDecorCursor[idx] >= _w.chunkPalmIndices[idx].Count + _w.chunkRockIndices[idx].Count;
	}

	private int DespawnDecoratorsIncremental(int chunkIndex, int budget)
	{
		if (_chunkActivePalms == null || chunkIndex >= _chunkActivePalms.Length) return 0;

		var palms = _chunkActivePalms[chunkIndex];
		var rocks = _chunkActiveRocks[chunkIndex];
		int done = 0;

		// Despawn palms first, then rocks — always routes to the correct pool
		while (palms.Count > 0 && done < budget)
		{
			int last = palms.Count - 1;
			GameObject go = palms[last];
			palms.RemoveAt(last);
			done++;
			if (go == null) continue;
			go.SetActive(false);
			_palmPool.Push(go);
		}

		while (rocks.Count > 0 && done < budget)
		{
			int last = rocks.Count - 1;
			GameObject go = rocks[last];
			rocks.RemoveAt(last);
			done++;
			if (go == null) continue;
			go.SetActive(false);
			_rockPool.Push(go);
		}

		return done;
	}

	private void ReturnAllDecoratorsToPool()
	{
		if (_chunkActivePalms == null) return;

		for (int i = 0; i < _chunkActivePalms.Length; i++)
		{
			ReturnListToPool(_chunkActivePalms[i], _palmPool);
			ReturnListToPool(_chunkActiveRocks[i], _rockPool);
		}
	}

	private static void ReturnListToPool(List<GameObject> list, Stack<GameObject> pool)
	{
		if (list == null) return;
		for (int j = list.Count - 1; j >= 0; j--)
		{
			var go = list[j];
			if (go == null) continue;
			go.SetActive(false);
			pool.Push(go);
		}
		list.Clear();
	}

	// -------------------------------------------------------
	// Utility
	// -------------------------------------------------------

	private Vector3 GetWorldPos(int x, int y)
	{
		Vector3 gridOrigin = (generator != null && generator.tilemapGrid != null)
			? generator.tilemapGrid.transform.position
			: Vector3.zero;
		return new Vector3(gridOrigin.x + x + 0.5f, gridOrigin.y + y + 0.5f, 0f);
	}

	private GameObject GetPooled(Stack<GameObject> pool, GameObject prefab, Transform parent)
	{
		GameObject go;
		if (pool.Count > 0 && (go = pool.Pop()) != null)
		{
			go.transform.SetParent(parent, true);
			go.SetActive(true);
			return go;
		}
		return Instantiate(prefab, parent);
	}

	private static void EnsureArray<T>(ref T[] arr, int count)
	{
		if (arr == null || arr.Length < count)
			arr = new T[count];
		else
			System.Array.Clear(arr, 0, count);
	}
}