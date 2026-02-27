public sealed class MapGenData
{
	public readonly int size;
	public readonly int pad;
	public readonly int waterSize;

	public readonly float[] height;
	public readonly byte[] land;
	public readonly int[] coastDist;
	public readonly byte[] beach;
	public readonly byte[] grass;
	public readonly byte[] oceanBand;
	public readonly byte[] seaweed;

	public MapGenData(int size, int pad)
	{
		this.size = size;
		this.pad = pad;
		waterSize = size + pad * 2;

		int n = size * size;
		height = new float[n];
		land = new byte[n];
		coastDist = new int[n];
		beach = new byte[n];
		grass = new byte[n];
		oceanBand = new byte[n];
		seaweed = new byte[n];
	}

	public int Idx(int x, int y) => y * size + x;
}
