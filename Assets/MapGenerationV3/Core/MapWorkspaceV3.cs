using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public struct IslandInfoV3
{
	public float cx, cy, r;
	public IslandInfoV3(float cx, float cy, float r) { this.cx = cx; this.cy = cy; this.r = r; }
}

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
	public IslandInfoV3[] islandData;
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
	public TileChangeData[] tileChangeBuffer;
	public float[] beachWidth;
	public float[] warpDX;
	public float[] warpDY;
	public int[] biomeQueue;
	public bool[] biomeVisited;
	public int[] chunkLayerProgress;
	public int[] biomeScratch;

	public List<int> decoratorCandidates = new List<int>(4096);

	// Per-chunk decorator tile indices (built once after generation, read-only during streaming)
	// chunkIndex -> flat tile indices where decorators should spawn
	public List<int>[] chunkDecoratorIndices;

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
		warpDX = new float[n];
		warpDY = new float[n];
		biomeQueue = new int[n * 2];
		biomeVisited = new bool[n];
		biomeScratch = new int[System.Enum.GetValues(typeof(BiomeType)).Length];

		islandCenters = new Vector2[256];
		islandRadii = new float[256];
		islandBiomes = new BiomeType[256];
		islandData = new IslandInfoV3[256];
		islandGrid = new int[1];
		islandCount = 0;
		islandGridW = islandGridH = 0;
		islandCellSize = 1f;

		chunks = null;
		chunkCols = chunkRows = chunkSize = 0;
		chunkLayerProgress = null;

		chunkDecoratorIndices = null;
	}

	public void EnsureTileChangeBuffer(int minSize)
	{
		if (tileChangeBuffer != null && tileChangeBuffer.Length >= minSize) return;
		int len = tileChangeBuffer == null ? 1024 : tileChangeBuffer.Length;
		while (len < minSize) len <<= 1;
		tileChangeBuffer = new TileChangeData[len];
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
		islandData = new IslandInfoV3[len];
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

	public void EnsureChunkDecoratorStorage(int chunkCount)
	{
		if (chunkDecoratorIndices != null && chunkDecoratorIndices.Length >= chunkCount) return;

		chunkDecoratorIndices = new List<int>[chunkCount];

		for (int i = 0; i < chunkCount; i++)
			chunkDecoratorIndices[i] = new List<int>();
	}

	public void ClearChunkDecoratorStorage(int chunkCount)
	{
		if (chunkDecoratorIndices == null) return;
		for (int i = 0; i < chunkCount && i < chunkDecoratorIndices.Length; i++)
			chunkDecoratorIndices[i]?.Clear();
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
