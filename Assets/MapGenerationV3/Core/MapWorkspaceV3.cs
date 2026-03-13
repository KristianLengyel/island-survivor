using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public sealed class MapWorkspaceV3
{
	public int size;
	public int pad;
	public int waterSize;
	public int n;

	public MapDataV3 data;

	public byte[] morphTmp;
	public int[] bfsQueue;

	public int[] labels;
	public int[] componentCounts;
	public byte[] componentTouchesBorder;

	public Vector2[] islandCenters;
	public float[] islandRadii;
	public BiomeType[] islandBiomes;
	public int islandCount;
	public int[] islandGrid;
	public int islandGridW, islandGridH;
	public float islandCellSize;

	public MapChunkV3[] chunks;
	public int chunkCols;
	public int chunkRows;
	public int chunkSize;

	public TileBase[] waterTiles;
	public TileBase[] deepTiles;
	public TileBase[] medTiles;
	public TileBase[] shTiles;
	public TileBase[] landTiles;
	public TileBase[] grassTiles;
	public TileBase[] overlayTiles;

	public TileBase[] paintBuffer;
	public float[] beachWidth;
	public int[] biomeQueue;
	public bool[] biomeVisited;
	public int[] chunkLayerProgress;
	public int[] biomeScratch;

	public List<int> palmCandidates = new List<int>(4096);
	public List<int> rockCandidates = new List<int>(2048);
	public List<Vector2Int> palmAccepted = new List<Vector2Int>(512);
	public List<Vector2Int> rockAccepted = new List<Vector2Int>(512);

	public int decorGridW;
	public int decorGridH;
	public float decorCellSize;
	public int[] palmGrid;
	public int[] rockGrid;

	// Per-chunk decorator positions (built once after generation, read-only during streaming)
	// chunkIndex -> list of flat tile indices where decorators should spawn
	public List<int>[] chunkPalmIndices;
	public List<int>[] chunkRockIndices;

	// Per-chunk active GameObjects (owned by streamer, stored here for easy lookup)
	public List<GameObject>[] chunkActiveDecorators;

	public void Ensure(int size, int pad)
	{
		if (data != null && this.size == size && this.pad == pad) return;

		this.size = size;
		this.pad = pad;
		waterSize = size + pad * 2;
		n = size * size;

		data = new MapDataV3(size, pad);
		morphTmp = new byte[n];
		bfsQueue = new int[n * 2];
		labels = new int[n];

		componentCounts = new int[1024];
		componentTouchesBorder = new byte[1024];

		waterTiles = new TileBase[waterSize * waterSize];
		deepTiles = new TileBase[n];
		medTiles = new TileBase[n];
		shTiles = new TileBase[n];
		landTiles = new TileBase[n];
		grassTiles = new TileBase[n];
		overlayTiles = new TileBase[n];

		paintBuffer = new TileBase[n];
		beachWidth = new float[n];
		biomeQueue = new int[n * 2];
		biomeVisited = new bool[n];
		biomeScratch = new int[System.Enum.GetValues(typeof(BiomeType)).Length];

		islandCenters = new Vector2[256];
		islandRadii = new float[256];
		islandBiomes = new BiomeType[256];
		islandGrid = new int[1];
		islandCount = 0;
		islandGridW = islandGridH = 0;
		islandCellSize = 1f;

		chunks = null;
		chunkCols = chunkRows = chunkSize = 0;
		chunkLayerProgress = null;

		chunkPalmIndices = null;
		chunkRockIndices = null;
		chunkActiveDecorators = null;
	}

	public void EnsureChunkLayerProgress(int chunkCount)
	{
		if (chunkLayerProgress != null && chunkLayerProgress.Length >= chunkCount) return;
		chunkLayerProgress = new int[chunkCount];
	}

	public void EnsureComponentCapacity(int labelMax)
	{
		int need = labelMax + 1;
		if (componentCounts.Length >= need) return;
		int len = componentCounts.Length;
		while (len < need) len <<= 1;
		componentCounts = new int[len];
		componentTouchesBorder = new byte[len];
	}

	public void EnsureIslandCapacity(int count)
	{
		if (islandCenters != null && islandCenters.Length >= count) return;
		int len = islandCenters == null ? 256 : islandCenters.Length;
		while (len < count) len <<= 1;
		islandCenters = new Vector2[len];
		islandRadii = new float[len];
		islandBiomes = new BiomeType[len];
	}

	public void EnsureIslandGrid(int gw, int gh)
	{
		int need = Mathf.Max(1, gw * gh);
		if (islandGrid == null || islandGrid.Length < need)
		{
			int len = islandGrid == null ? 1 : islandGrid.Length;
			while (len < need) len <<= 1;
			islandGrid = new int[len];
		}
		islandGridW = gw;
		islandGridH = gh;
	}

	public void EnsureDecorGrids(int gw, int gh)
	{
		int need = Mathf.Max(1, gw * gh);
		if (palmGrid == null || palmGrid.Length < need)
		{
			palmGrid = new int[need];
			rockGrid = new int[need];
		}
		decorGridW = gw;
		decorGridH = gh;
	}

	/// <summary>
	/// Allocates per-chunk decorator index lists. Called once after chunks are built.
	/// </summary>
	public void EnsureChunkDecoratorStorage(int chunkCount)
	{
		if (chunkPalmIndices != null && chunkPalmIndices.Length >= chunkCount) return;

		chunkPalmIndices = new List<int>[chunkCount];
		chunkRockIndices = new List<int>[chunkCount];
		chunkActiveDecorators = new List<GameObject>[chunkCount];

		for (int i = 0; i < chunkCount; i++)
		{
			chunkPalmIndices[i] = new List<int>();
			chunkRockIndices[i] = new List<int>();
			chunkActiveDecorators[i] = new List<GameObject>();
		}
	}

	public void ClearChunkDecoratorStorage(int chunkCount)
	{
		if (chunkPalmIndices == null) return;
		for (int i = 0; i < chunkCount && i < chunkPalmIndices.Length; i++)
		{
			chunkPalmIndices[i]?.Clear();
			chunkRockIndices[i]?.Clear();
			chunkActiveDecorators[i]?.Clear();
		}
	}

	public MapChunkV3 GetChunkForTile(int x, int y)
	{
		if (chunks == null || chunkSize <= 0) return default;
		int cx = Mathf.Clamp(x / chunkSize, 0, chunkCols - 1);
		int cy = Mathf.Clamp(y / chunkSize, 0, chunkRows - 1);
		return chunks[cy * chunkCols + cx];
	}

	public int GetChunkIndex(int tileX, int tileY)
	{
		if (chunks == null || chunkSize <= 0) return -1;
		int cx = Mathf.Clamp(tileX / chunkSize, 0, chunkCols - 1);
		int cy = Mathf.Clamp(tileY / chunkSize, 0, chunkRows - 1);
		return cy * chunkCols + cx;
	}
}