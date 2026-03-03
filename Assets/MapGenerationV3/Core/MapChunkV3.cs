public struct MapChunkV3
{
	public int chunkX, chunkY;      // chunk grid position
	public int tileX, tileY;        // world tile origin
	public int size;                 // tiles per side
	public BiomeType dominantBiome; // majority biome in chunk
	public int landTileCount;
}