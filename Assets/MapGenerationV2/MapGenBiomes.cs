using UnityEngine;

public static class MapGenBiomes
{
	public static void BuildBands(MapGenData d, TilemapGenerationSettingsV2 s, ref MapGenRng rng)
	{
		int size = d.size;

		float sea = Mathf.Clamp01(s.landThreshold);
		float invSea = 1f / Mathf.Max(0.0001f, sea);

		int shelfTiles = Mathf.Max(1, s.shelfDistanceTiles);
		float invShelf = 1f / shelfTiles;
		float shelfStrength = Mathf.Clamp01(s.shelfStrength);

		float shCut = Mathf.Clamp01(s.naturalShallowCut);
		float medCut = Mathf.Clamp01(s.naturalMediumCut);
		if (medCut < shCut) medCut = shCut;

		for (int y = 0; y < size; y++)
		{
			for (int x = 0; x < size; x++)
			{
				int i = d.Idx(x, y);

				d.beach[i] = 0;
				d.grass[i] = 0;
				d.oceanBand[i] = 0;
				d.seaweed[i] = 0;

				int cd = d.coastDist[i];

				if (d.land[i] == 1)
				{
					int localBeach = Mathf.RoundToInt(Mathf.Lerp(s.beachWidthMin, s.beachWidthMax, Hash01(x, y)));
					if (cd <= localBeach) d.beach[i] = 1;
					else
					{
						if (cd >= s.grassInset) d.grass[i] = 1;
					}
				}
				else
				{
					if (!s.useNaturalDepthBands)
					{
						int depth = -cd;
						if (depth <= s.shallowDepth) d.oceanBand[i] = 1;
						else if (depth <= s.mediumDepth) d.oceanBand[i] = 2;
						else d.oceanBand[i] = 3;
						continue;
					}

					float h = d.height[i];

					float depth01 = (sea - h) * invSea;
					if (depth01 < 0f) depth01 = 0f;
					if (depth01 > 1f) depth01 = 1f;

					int depthTiles = -cd;
					if (depthTiles < 0) depthTiles = 0;

					float shelfT = depthTiles * invShelf;
					if (shelfT < 0f) shelfT = 0f;
					if (shelfT > 1f) shelfT = 1f;

					float shelfBias = Mathf.Lerp(1f - shelfStrength, 1f, shelfT);
					depth01 *= shelfBias;

					if (depth01 <= shCut) d.oceanBand[i] = 1;
					else if (depth01 <= medCut) d.oceanBand[i] = 2;
					else d.oceanBand[i] = 3;
				}
			}
		}
	}

	public static void PlaceSeaweed(MapGenData d, TilemapGenerationSettingsV2 s, Vector2 seaweedOffset)
	{
		int size = d.size;

		for (int y = 0; y < size; y++)
		{
			for (int x = 0; x < size; x++)
			{
				int i = d.Idx(x, y);
				if (d.land[i] == 1) continue;

				int depth = -d.coastDist[i];
				if (depth < s.seaweedMinDepth) continue;

				float n = MapGenNoise.SeaweedValue(x, y, s, seaweedOffset, size);
				if (n >= s.seaweedThreshold) d.seaweed[i] = 1;
			}
		}
	}

	private static float Hash01(int x, int y)
	{
		unchecked
		{
			uint h = 2166136261u;
			h ^= (uint)x; h *= 16777619u;
			h ^= (uint)y; h *= 16777619u;
			return (h & 0x00FFFFFFu) / 16777216f;
		}
	}
}