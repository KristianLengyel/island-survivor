using UnityEngine;

public static class MapGenNoise
{
	public static void GenerateHeight(MapGenData data, TilemapGenerationSettingsV2 s, ref MapGenRng rng, out Vector2 baseOffset, out Vector2 seaweedOffset)
	{
		baseOffset = new Vector2(rng.Range(0f, 10000f), rng.Range(0f, 10000f));
		seaweedOffset = new Vector2(rng.Range(0f, 10000f), rng.Range(0f, 10000f));

		float inv = 1f / Mathf.Max(0.0001f, s.baseScale);
		Vector2 center = new Vector2((data.size - 1) * 0.5f, (data.size - 1) * 0.5f);
		float maxR = center.magnitude;

		for (int y = 0; y < data.size; y++)
		{
			for (int x = 0; x < data.size; x++)
			{
				float amp = 1f;
				float freq = 1f;
				float v = 0f;

				for (int o = 0; o < s.octaves; o++)
				{
					float sx = (x * inv) * freq + baseOffset.x;
					float sy = (y * inv) * freq + baseOffset.y;
					float p = Mathf.PerlinNoise(sx, sy) * 2f - 1f;
					v += p * amp;
					amp *= s.persistence;
					freq *= s.lacunarity;
				}

				float h = Mathf.InverseLerp(-1f, 1f, v);

				Vector2 d = new Vector2(x, y) - center;
				float r = d.magnitude / Mathf.Max(0.0001f, maxR);
				float fall = Mathf.SmoothStep(0f, 1f, r);
				h -= fall * s.radialFalloffStrength;

				float rx = Mathf.PerlinNoise((x * inv) + baseOffset.x + 2000f, (y * inv) + baseOffset.y + 2000f);
				float ry = Mathf.PerlinNoise((x * inv) + baseOffset.x + 4000f, (y * inv) + baseOffset.y + 4000f);
				float ridge = 1f - Mathf.Abs((rx * 2f - 1f) * (ry * 2f - 1f));
				h = Mathf.Clamp01(h + (ridge - 0.5f) * s.ridgeStrength);

				data.height[data.Idx(x, y)] = h;
			}
		}
	}

	public static float SeaweedValue(int x, int y, TilemapGenerationSettingsV2 s, Vector2 seaweedOffset, int size)
	{
		float sx = (float)x / size * s.seaweedScale + seaweedOffset.x;
		float sy = (float)y / size * s.seaweedScale + seaweedOffset.y;
		return Mathf.PerlinNoise(sx, sy);
	}
}
