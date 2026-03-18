using UnityEngine;

public static class MapBiomesV3
{
	public static void BuildBands(MapDataV3 d, MapSettingsV3 s, ref MapRngV3 rng, MapWorkspaceV3 w)
	{
		int size = d.size;
		float sea = Mathf.Clamp01(s.landThreshold);
		float invSea = 1f / Mathf.Max(0.0001f, sea);
		int shelfT = Mathf.Max(1, s.shelfDistanceTiles);
		float invShelf = 1f / shelfT;
		float shSt = Mathf.Clamp01(s.shelfStrength);
		float shCut = Mathf.Clamp01(s.naturalShallowCut);
		float medCut = Mathf.Clamp01(s.naturalMediumCut);
		if (medCut < shCut) medCut = shCut;

		float abyssScale = Mathf.Max(0.001f, s.abyssNoiseScale);
		float abyssThresh = s.abyssThreshold;
		float abyssOffX = rng.Next01() * 1000f;
		float abyssOffY = rng.Next01() * 1000f;

		float noiseScale = 0.04f;
		float beachMin = s.beachWidthMin;
		float beachRange = s.beachWidthMax - beachMin;

		for (int y = 0; y < size; y++)
		{
			for (int x = 0; x < size; x++)
			{
				int i = d.Idx(x, y);
				d.beach[i] = 0;
				d.grass[i] = 0;
				d.oceanBand[i] = 0;

				int cd = d.coastDist[i];

				if (d.land[i] == 1)
				{
					float n = Mathf.PerlinNoise(x * noiseScale + 91.3f, y * noiseScale + 17.7f);
					float localBeach = beachMin + n * beachRange;
					if (cd <= localBeach) d.beach[i] = 1;
					else if (cd >= s.grassInset) d.grass[i] = 1;
				}
				else
				{
					float h = d.height[i];
					float dep01 = Mathf.Clamp01((sea - h) * invSea);
					int depT = Mathf.Max(0, -cd);
					float shelfB = Mathf.Lerp(1f - shSt, 1f, Mathf.Clamp01(depT * invShelf));
					dep01 *= shelfB;

					byte band;
					if (dep01 <= shCut) band = 1;
					else if (dep01 <= medCut) band = 2;
					else band = 3;

					if (band == 3)
					{
						float abyssNoise = Mathf.PerlinNoise(
							x / abyssScale + abyssOffX,
							y / abyssScale + abyssOffY);
						if (abyssNoise >= abyssThresh) band = 4;
					}

					d.oceanBand[i] = band;
				}
			}
		}
	}
}
