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

		float noiseScale = 0.04f;
		float[] beachWidth = w.beachWidth;
		for (int y = 0; y < size; y++)
			for (int x = 0; x < size; x++)
			{
				float n = Mathf.PerlinNoise(x * noiseScale + 91.3f, y * noiseScale + 17.7f);
				beachWidth[y * size + x] = Mathf.Lerp(s.beachWidthMin, s.beachWidthMax, n);
			}

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
					float localBeach = beachWidth[i];
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

					if (dep01 <= shCut) d.oceanBand[i] = 1;
					else if (dep01 <= medCut) d.oceanBand[i] = 2;
					else d.oceanBand[i] = 3;
				}
			}
		}
	}
}