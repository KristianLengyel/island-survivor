using System.Collections.Generic;
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
}