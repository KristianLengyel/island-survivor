using UnityEngine;

public static class MapDecoratorsV3
{
	public static void PlaceAll(MapDataV3 d, MapSettingsV3 s, Vector2 seaweedOffset)
	{
		PlaceSeaweed(d, s, seaweedOffset);
		MarkDecoratorCandidates(d, s);
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

				float mult = 1f;
				var bdef = s.GetBiome((BiomeType)d.biome[i]);
				if (bdef != null) mult = bdef.seaweedDensityMultiplier;
				if (mult <= 0f) continue;

				float n = MapNoiseV3.SeaweedValue(x, y, s, seaweedOffset, size);
				float j = MapNoiseV3.SeaweedJitterValue(x, y, s, seaweedOffset);
				if (n + (j - 0.5f) * s.seaweedEdgeJitter >= s.seaweedThreshold / mult) d.seaweed[i] = 1;
			}
		}
	}

	private static void MarkDecoratorCandidates(MapDataV3 d, MapSettingsV3 s)
	{
		int n = d.size * d.size;
		for (int i = 0; i < n; i++) d.decoratorSlot[i] = 0;

		int size = d.size;
		for (int y = 0; y < size; y++)
		{
			for (int x = 0; x < size; x++)
			{
				int i = d.Idx(x, y);
				if (d.land[i] != 1) continue;

				var bdef = s.GetBiome((BiomeType)d.biome[i]);
				if (bdef == null || bdef.decorators == null) continue;

				bool isBeach = d.beach[i] == 1;
				bool isGrass = d.grass[i] == 1;
				int cd = d.coastDist[i];

				int matchCount = 0;
				for (int e = 0; e < bdef.decorators.Length && e < 255; e++)
				{
					var entry = bdef.decorators[e];
					bool zoneMatch = entry.placementZone == DecoratorPlacementZone.Beach ? isBeach : isGrass;
					if (!zoneMatch) continue;
					if (cd < entry.coastDistMin || cd > entry.coastDistMax) continue;
					matchCount++;
				}

				if (matchCount == 0) continue;

				uint h = (uint)(x * 1664525 + y * 1013904223);
				int pick = (int)(h % (uint)matchCount);
				int seen = 0;
				for (int e = 0; e < bdef.decorators.Length && e < 255; e++)
				{
					var entry = bdef.decorators[e];
					bool zoneMatch = entry.placementZone == DecoratorPlacementZone.Beach ? isBeach : isGrass;
					if (!zoneMatch) continue;
					if (cd < entry.coastDistMin || cd > entry.coastDistMax) continue;
					if (seen == pick) { d.decoratorSlot[i] = (byte)(e + 1); break; }
					seen++;
				}
			}
		}
	}
}
