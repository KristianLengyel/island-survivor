using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public sealed class MapGenWorkspace
{
	public MapGenData data;

	public byte[] morphTmp;

	public TileBase[] waterTiles;
	public TileBase[] deep;
	public TileBase[] med;
	public TileBase[] sh;
	public TileBase[] land;
	public TileBase[] gr;
	public TileBase[] ov;

	public int[] bfsQueue;
	public int[] labels;
	public int[] componentCounts;
	public byte[] componentTouchesBorder;

	public List<int> tmpCellsA;
	public List<int> tmpCellsB;

	public Vector2[] islandCenters;
	public float[] islandRadii;
	public int islandCount;

	public int[] islandGrid;
	public int islandGridW;
	public int islandGridH;
	public float islandCellSize;

	public int size;
	public int pad;
	public int waterSize;
	public int n;

	public void Ensure(int size, int pad)
	{
		if (data != null && this.size == size && this.pad == pad) return;

		this.size = size;
		this.pad = pad;
		waterSize = size + pad * 2;
		n = size * size;

		data = new MapGenData(size, pad);

		morphTmp = new byte[n];

		waterTiles = new TileBase[waterSize * waterSize];

		deep = new TileBase[n];
		med = new TileBase[n];
		sh = new TileBase[n];
		land = new TileBase[n];
		gr = new TileBase[n];
		ov = new TileBase[n];

		bfsQueue = new int[n];
		labels = new int[n];

		componentCounts = new int[256];
		componentTouchesBorder = new byte[256];

		tmpCellsA = new List<int>(n / 8);
		tmpCellsB = new List<int>(n / 8);

		islandCenters = new Vector2[256];
		islandRadii = new float[256];
		islandGrid = new int[1];
		islandCount = 0;
		islandGridW = 0;
		islandGridH = 0;
		islandCellSize = 1f;
	}

	public void EnsureComponentCapacity(int labelMaxInclusive)
	{
		int need = labelMaxInclusive + 1;
		if (componentCounts.Length < need)
		{
			int newLen = componentCounts.Length;
			while (newLen < need) newLen <<= 1;
			componentCounts = new int[newLen];
			componentTouchesBorder = new byte[newLen];
		}
	}

	public void EnsureIslandCapacity(int count)
	{
		if (islandCenters == null || islandCenters.Length < count)
		{
			int newLen = islandCenters == null ? 256 : islandCenters.Length;
			while (newLen < count) newLen <<= 1;
			islandCenters = new Vector2[newLen];
			islandRadii = new float[newLen];
		}
	}

	public void EnsureIslandGrid(int gridW, int gridH)
	{
		int need = Mathf.Max(1, gridW * gridH);
		if (islandGrid == null || islandGrid.Length < need)
		{
			int newLen = islandGrid == null ? 1 : islandGrid.Length;
			while (newLen < need) newLen <<= 1;
			islandGrid = new int[newLen];
		}
		islandGridW = gridW;
		islandGridH = gridH;
	}
}