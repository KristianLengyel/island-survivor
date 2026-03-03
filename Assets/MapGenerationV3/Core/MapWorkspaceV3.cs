using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Reusable scratch buffers. Call Ensure() before each generation to
/// avoid re-allocating when map size hasn't changed.
/// </summary>
public sealed class MapWorkspaceV3
{
	// ---- sizing ----
	public int size;
	public int pad;
	public int waterSize;
	public int n;

	// ---- core data ----
	public MapDataV3 data;

	// ---- morphology ----
	public byte[] morphTmp;

	// ---- BFS ----
	public int[] bfsQueue;

	// ---- flood fill ----
	public int[] labels;
	public int[] componentCounts;
	public byte[] componentTouchesBorder;

	// ---- island placement ----
	public Vector2[] islandCenters;
	public float[] islandRadii;
	public BiomeType[] islandBiomes;
	public int islandCount;
	public int[] islandGrid;
	public int islandGridW, islandGridH;
	public float islandCellSize;

	// ---- tilemap paint buffers ----
	public TileBase[] waterTiles;
	public TileBase[] deepTiles;
	public TileBase[] medTiles;
	public TileBase[] shTiles;
	public TileBase[] landTiles;
	public TileBase[] grassTiles;
	public TileBase[] overlayTiles;

	// ---- decorator lists (reused per regen) ----
	public List<int> palmCandidates = new List<int>(4096);
	public List<int> rockCandidates = new List<int>(2048);
	public List<Vector2Int> palmAccepted = new List<Vector2Int>(512);
	public List<Vector2Int> rockAccepted = new List<Vector2Int>(512);

	public void Ensure(int size, int pad)
	{
		if (data != null && this.size == size && this.pad == pad) return;

		this.size = size;
		this.pad = pad;
		waterSize = size + pad * 2;
		n = size * size;

		data = new MapDataV3(size, pad);
		morphTmp = new byte[n];
		bfsQueue = new int[n];
		labels = new int[n];

		componentCounts = new int[256];
		componentTouchesBorder = new byte[256];

		waterTiles = new TileBase[waterSize * waterSize];
		deepTiles = new TileBase[n];
		medTiles = new TileBase[n];
		shTiles = new TileBase[n];
		landTiles = new TileBase[n];
		grassTiles = new TileBase[n];
		overlayTiles = new TileBase[n];

		islandCenters = new Vector2[256];
		islandRadii = new float[256];
		islandBiomes = new BiomeType[256];
		islandGrid = new int[1];
		islandCount = 0;
		islandGridW = islandGridH = 0;
		islandCellSize = 1f;
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
}