using UnityEngine;

public static class MapChunkBuilderV3
{
	public static MapChunkV3[] Build(MapDataV3 d, MapSettingsV3 s, MapWorkspaceV3 w)
	{
		int size = d.size;
		int cs = Mathf.Max(1, s.chunkSize);
		int cols = Mathf.CeilToInt((float)size / cs);
		int rows = Mathf.CeilToInt((float)size / cs);
		var chunks = new MapChunkV3[cols * rows];
		int biomeCount = w.biomeScratch.Length;
		int[] votes = w.biomeScratch;

		for (int cy = 0; cy < rows; cy++)
			for (int cx = 0; cx < cols; cx++)
			{
				int tX = cx * cs, tY = cy * cs;
				int tw = Mathf.Min(cs, size - tX);
				int th = Mathf.Min(cs, size - tY);

				for (int b = 0; b < biomeCount; b++) votes[b] = 0;
				int lc = 0;

				for (int dy = 0; dy < th; dy++)
					for (int dx = 0; dx < tw; dx++)
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