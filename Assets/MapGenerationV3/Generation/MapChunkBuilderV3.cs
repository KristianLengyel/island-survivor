using UnityEngine;

public static class MapChunkBuilderV3
{
	public static MapChunkV3[] Build(MapDataV3 d, MapSettingsV3 s)
	{
		int size = d.size;
		int cs = Mathf.Max(1, s.chunkSize);
		int cols = Mathf.CeilToInt((float)size / cs);
		int rows = Mathf.CeilToInt((float)size / cs);
		var chunks = new MapChunkV3[cols * rows];
		int biomeCount = System.Enum.GetValues(typeof(BiomeType)).Length;

		for (int cy = 0; cy < rows; cy++)
			for (int cx = 0; cx < cols; cx++)
			{
				int tX = cx * cs, tY = cy * cs;
				int w = Mathf.Min(cs, size - tX);
				int h = Mathf.Min(cs, size - tY);
				int[] votes = new int[biomeCount];
				int lc = 0;

				for (int dy = 0; dy < h; dy++)
					for (int dx = 0; dx < w; dx++)
					{
						int i = d.Idx(tX + dx, tY + dy);
						if (d.land[i] == 1) { votes[d.biome[i]]++; lc++; }
					}

				int best = 0;
				for (int b = 1; b < biomeCount; b++)
					if (votes[b] > votes[best]) best = b;

				chunks[cy * cols + cx] = new MapChunkV3
				{
					chunkX = cx,
					chunkY = cy,
					tileX = tX,
					tileY = tY,
					size = cs,
					dominantBiome = (BiomeType)best,
					landTileCount = lc
				};
			}
		return chunks;
	}
}