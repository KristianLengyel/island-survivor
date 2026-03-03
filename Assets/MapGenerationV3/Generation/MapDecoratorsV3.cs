using UnityEngine;

/// <summary>
/// Places seaweed, palm candidates, and rock candidates.
/// Per-biome density is read from BiomeDefinitionV3 assets.
/// Actual GameObject spawning is handled by the controller.
/// </summary>
public static class MapDecoratorsV3
{
	public static void PlaceAll(MapDataV3 d, MapSettingsV3 s, Vector2 seaweedOffset)
	{
		PlaceSeaweed(d, s, seaweedOffset);
		MarkPalmCandidates(d, s);
		MarkRockCandidates(d, s);
	}

	private static void PlaceSeaweed(MapDataV3 d, MapSettingsV3 s, Vector2 seaweedOffset)
	{
		int size = d.size;
		for (int y = 0; y < size; y++)
		{
			for (int x = 0; x < size; x++)
			{
				int i = d.Idx(x, y);
				d.seaweed[i] = 0;
				if (d.land[i] == 1) continue;

				int depth = -d.coastDist[i];
				if (depth < s.seaweedMinDepth) continue;

				// Per-biome density multiplier
				float mult = 1f;
				var bdef = s.GetBiome((BiomeType)d.biome[i]);
				if (bdef != null) mult = bdef.seaweedDensityMultiplier;
				if (mult <= 0f) continue;

				float n = MapNoiseV3.SeaweedValue(x, y, s, seaweedOffset, size);
				if (n >= s.seaweedThreshold / mult) d.seaweed[i] = 1;
			}
		}
	}

	private static void MarkPalmCandidates(MapDataV3 d, MapSettingsV3 s)
	{
		int size = d.size;
		for (int i = 0; i < size * size; i++) d.palmTile[i] = 0;

		for (int y = 0; y < size; y++)
		{
			for (int x = 0; x < size; x++)
			{
				int i = d.Idx(x, y);
				if (d.grass[i] != 1) continue;

				var bdef = s.GetBiome((BiomeType)d.biome[i]);
				int cMin = bdef != null ? bdef.palmCoastMin : s.rockCoastMin;
				int cMax = bdef != null ? bdef.palmCoastMax : s.rockCoastMax;

				int cd = d.coastDist[i];
				if (cd < cMin || cd > cMax) continue;

				d.palmTile[i] = 1;
			}
		}
	}

	private static void MarkRockCandidates(MapDataV3 d, MapSettingsV3 s)
	{
		int size = d.size;
		for (int i = 0; i < size * size; i++) d.rockTile[i] = 0;

		for (int y = 0; y < size; y++)
		{
			for (int x = 0; x < size; x++)
			{
				int i = d.Idx(x, y);
				if (d.beach[i] != 1) continue;

				int cd = d.coastDist[i];
				if (cd < s.rockCoastMin || cd > s.rockCoastMax) continue;

				d.rockTile[i] = 1;
			}
		}
	}
}