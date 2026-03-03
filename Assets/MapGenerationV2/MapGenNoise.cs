using UnityEngine;

public static class MapGenNoise
{
	public static void GenerateHeight(
		MapGenData data,
		TilemapGenerationSettingsV2 s,
		ref MapGenRng rng,
		MapGenWorkspace w,
		out Vector2 baseOffset,
		out Vector2 seaweedOffset)
	{
		baseOffset = new Vector2(rng.Range(0f, 10000f), rng.Range(0f, 10000f));
		seaweedOffset = new Vector2(rng.Range(0f, 10000f), rng.Range(0f, 10000f));

		if (s.heightMode == MapGenHeightMode.ArchipelagoSeeds)
		{
			GenerateArchipelago(data, s, ref rng, w, baseOffset);
		}
		else
		{
			GenerateContinentalNoise(data, s, ref rng, baseOffset);
		}
	}

	private static void GenerateContinentalNoise(MapGenData data, TilemapGenerationSettingsV2 s, ref MapGenRng rng, Vector2 baseOffset)
	{
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

	private static void GenerateArchipelago(MapGenData data, TilemapGenerationSettingsV2 s, ref MapGenRng rng, MapGenWorkspace w, Vector2 baseOffset)
	{
		int size = data.size;

		int target = Mathf.Max(1, s.islandCount);
		int minSpacing = Mathf.Max(1, s.islandMinSpacing);
		int minR = Mathf.Max(1, s.islandMinRadius);
		int maxR = Mathf.Max(minR, s.islandMaxRadius);

		w.EnsureIslandCapacity(target);
		w.islandCount = 0;

		float cell = Mathf.Max(1f, minSpacing / 1.41421356f);
		w.islandCellSize = cell;

		int gw = Mathf.CeilToInt(size / cell);
		int gh = Mathf.CeilToInt(size / cell);
		w.EnsureIslandGrid(gw, gh);

		int gridN = gw * gh;
		for (int i = 0; i < gridN; i++) w.islandGrid[i] = -1;

		int attempts = Mathf.Max(1, s.islandPlacementAttemptsPerIsland) * target;
		float minD2 = minSpacing * minSpacing;

		for (int a = 0; a < attempts && w.islandCount < target; a++)
		{
			float px = rng.Range(0f, size - 1f);
			float py = rng.Range(0f, size - 1f);

			int cx = Mathf.Clamp((int)(px / cell), 0, gw - 1);
			int cy = Mathf.Clamp((int)(py / cell), 0, gh - 1);

			bool ok = true;

			int rCells = 2;
			for (int yy = cy - rCells; yy <= cy + rCells && ok; yy++)
			{
				if ((uint)yy >= (uint)gh) continue;
				int row = yy * gw;
				for (int xx = cx - rCells; xx <= cx + rCells; xx++)
				{
					if ((uint)xx >= (uint)gw) continue;
					int idx = w.islandGrid[row + xx];
					if (idx < 0) continue;

					Vector2 c = w.islandCenters[idx];
					float dx = c.x - px;
					float dy = c.y - py;
					if (dx * dx + dy * dy < minD2) { ok = false; break; }
				}
			}

			if (!ok) continue;

			int id = w.islandCount++;
			w.islandCenters[id] = new Vector2(px, py);
			w.islandRadii[id] = rng.Range(minR, maxR + 1);

			w.islandGrid[cy * gw + cx] = id;
		}

		float sharp = Mathf.Max(0.01f, s.islandSharpness);

		// --- Natural shaping controls (derived from your existing settings) ---
		// Domain warp makes blobs non-circular (most important change)
		float warpScaleA = Mathf.Max(6f, s.islandEdgeNoiseScale * 0.45f);
		float warpScaleB = Mathf.Max(6f, s.islandEdgeNoiseScale * 0.85f);
		float warpAmp = Mathf.Clamp01(s.islandEdgeNoiseStrength) * 6.0f;     // tiles

		// Edge roughness (directional) makes coastlines jagged instead of wobbly circles
		float edgeScale = Mathf.Max(0.0001f, s.islandEdgeNoiseScale);
		float edgeAmp = Mathf.Clamp01(s.islandEdgeNoiseStrength) * 0.35f;    // fraction of radius

		// Extra ridged detail helps “rocky” outlines
		float ridgeScale = Mathf.Max(10f, s.islandEdgeNoiseScale * 0.65f);
		float ridgeAmp = Mathf.Clamp01(s.islandEdgeNoiseStrength) * 0.18f;

		int searchCells = Mathf.CeilToInt(maxR / cell) + 2;

		static float Fbm(float x, float y, float ox, float oy, int oct, float lac, float pers)
		{
			float amp = 0.5f, freq = 1f, sum = 0f;
			for (int i = 0; i < oct; i++)
			{
				sum += (Mathf.PerlinNoise(x * freq + ox, y * freq + oy) * 2f - 1f) * amp;
				amp *= pers;
				freq *= lac;
			}
			return sum;
		}

		static float Ridged(float n)
		{
			n = Mathf.Abs(n);
			return 1f - n;
		}

		for (int y = 0; y < size; y++)
		{
			for (int x = 0; x < size; x++)
			{
				// --- Domain warp the sampling position (breaks circles) ---
				float wx1 = Mathf.PerlinNoise((x / warpScaleA) + baseOffset.x + 11.3f, (y / warpScaleA) + baseOffset.y + 91.7f) * 2f - 1f;
				float wy1 = Mathf.PerlinNoise((x / warpScaleA) + baseOffset.x + 71.1f, (y / warpScaleA) + baseOffset.y + 19.9f) * 2f - 1f;

				float wx2 = Mathf.PerlinNoise((x / warpScaleB) + baseOffset.x + 201.2f, (y / warpScaleB) + baseOffset.y + 9.4f) * 2f - 1f;
				float wy2 = Mathf.PerlinNoise((x / warpScaleB) + baseOffset.x + 7.6f, (y / warpScaleB) + baseOffset.y + 155.8f) * 2f - 1f;

				float sx = x + (wx1 + wx2) * warpAmp;
				float sy = y + (wy1 + wy2) * warpAmp;

				int gcx = Mathf.Clamp((int)(sx / cell), 0, gw - 1);
				int gcy = Mathf.Clamp((int)(sy / cell), 0, gh - 1);

				float best = 0f;

				for (int yy = gcy - searchCells; yy <= gcy + searchCells; yy++)
				{
					if ((uint)yy >= (uint)gh) continue;
					int row = yy * gw;

					for (int xx = gcx - searchCells; xx <= gcx + searchCells; xx++)
					{
						if ((uint)xx >= (uint)gw) continue;
						int id = w.islandGrid[row + xx];
						if (id < 0) continue;

						Vector2 c = w.islandCenters[id];
						float r = w.islandRadii[id];

						float dx = sx - c.x;
						float dy = sy - c.y;
						float d2 = dx * dx + dy * dy;

						// --- Directional edge perturbation (key to non-round coastlines) ---
						// Use angle-based noise so “radius” varies with direction, not just with position.
						float ang = Mathf.Atan2(dy, dx);
						float ax = Mathf.Cos(ang) * r;
						float ay = Mathf.Sin(ang) * r;

						float edgeN =
							Fbm(ax / edgeScale, ay / edgeScale, baseOffset.x + 33.7f, baseOffset.y + 88.2f, 3, 2.1f, 0.55f);

						float ridgeN =
							Fbm((sx / ridgeScale), (sy / ridgeScale), baseOffset.x + 401.4f, baseOffset.y + 19.2f, 2, 2.0f, 0.5f);

						float rPerturbed = r * (1f + edgeN * edgeAmp) * (1f - Ridged(ridgeN) * ridgeAmp);

						float rr = rPerturbed * rPerturbed;
						if (d2 >= rr) continue;

						float d = Mathf.Sqrt(d2);
						float t = 1f - (d / Mathf.Max(0.0001f, rPerturbed));
						if (t > best) best = t;
					}
				}

				if (best > 0f) best = Mathf.Pow(best, sharp);

				data.height[data.Idx(x, y)] = Mathf.Clamp01(best);
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