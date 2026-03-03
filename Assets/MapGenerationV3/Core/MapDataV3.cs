/// <summary>
/// All map data as flat arrays indexed by [y * size + x].
/// Single source of truth passed between every generation stage.
/// </summary>
public sealed class MapDataV3
{
	public readonly int size;
	public readonly int pad;
	public readonly int waterSize;

	// Height & Land
	public readonly float[] height;     // [0..1] raw noise
	public readonly byte[] land;       // 1 = land, 0 = water

	// Signed BFS coast distance
	// Positive = inland tiles from coast, Negative = ocean tiles from coast, 0 = coast edge
	public readonly int[] coastDist;

	// Biome per tile (cast from BiomeType enum)
	public readonly byte[] biome;

	// Land bands
	public readonly byte[] beach;       // 1 = beach strip
	public readonly byte[] grass;       // 1 = grass interior

	// Ocean bands: 1 = shallow, 2 = medium, 3 = deep
	public readonly byte[] oceanBand;

	// Decorators
	public readonly byte[] seaweed;
	public readonly byte[] palmTile;    // spawn candidate
	public readonly byte[] rockTile;    // spawn candidate

	// Which island seed owns this tile (-1 = ocean/unowned)
	public readonly int[] islandId;

	public MapDataV3(int size, int pad)
	{
		this.size = size;
		this.pad = pad;
		waterSize = size + pad * 2;

		int n = size * size;
		height = new float[n];
		land = new byte[n];
		coastDist = new int[n];
		biome = new byte[n];
		beach = new byte[n];
		grass = new byte[n];
		oceanBand = new byte[n];
		seaweed = new byte[n];
		palmTile = new byte[n];
		rockTile = new byte[n];
		islandId = new int[n];
	}

	public int Idx(int x, int y) => y * size + x;

	public int IdxSafe(int x, int y)
	{
		if ((uint)x >= (uint)size || (uint)y >= (uint)size) return -1;
		return y * size + x;
	}
}