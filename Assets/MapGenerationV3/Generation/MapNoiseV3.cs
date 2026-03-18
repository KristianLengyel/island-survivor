using UnityEngine;

/// <summary>
/// Archipelago Seeds height generation with:
/// - Poisson-disk island placement
/// - Per-island biome assignment (weighted by enabled biomes)
/// - Domain warp (breaks circular shapes)
/// - Directional edge noise (natural coastlines)
/// - Biome stored per island in workspace for later propagation
/// 
/// NOTE: islandCount in MapSettingsV3 assets is already authored per map size
/// (256→28, 512→110, 1024→440, etc.) so no area-scaling is applied here.
/// </summary>
public static class MapNoiseV3
{
	public static void GenerateHeight(
		MapDataV3 d,
		MapSettingsV3 s,
		ref MapRngV3 rng,
		MapWorkspaceV3 w,
		out Vector2 baseOffset,
		out Vector2 seaweedOffset)
	{
		baseOffset = new Vector2(rng.Range(0f, 10000f), rng.Range(0f, 10000f));
		seaweedOffset = new Vector2(rng.Range(0f, 10000f), rng.Range(0f, 10000f));

		PlaceIslands(d, s, ref rng, w, baseOffset);
		PrecomputeWarp(d, s, w, baseOffset);
		RasterizeIslands(d, s, w, baseOffset);
	}

	// ------------------------------------------------------------------
	// STEP 1 — Poisson-disk island placement + biome assignment
	// ------------------------------------------------------------------
	private static void PlaceIslands(
		MapDataV3 d, MapSettingsV3 s, ref MapRngV3 rng, MapWorkspaceV3 w, Vector2 baseOffset)
	{
		int size = d.size;

		// Use islandCount directly from settings — the assets are already
		// authored with the correct count for each map size (no area scaling needed).
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

		for (int i = 0; i < gw * gh; i++) w.islandGrid[i] = -1;

		float minD2 = (float)minSpacing * minSpacing;
		int attempts = Mathf.Max(1, s.islandPlacementAttemptsPerIsland) * target;
		int border = s.borderWater;
		int cr = s.centerNoIslandRadius;
		int cr2 = cr * cr;
		float cx = (size - 1) * 0.5f;
		float cy = (size - 1) * 0.5f;

		// Build enabled biome list from definitions
		int biomeCount = s.biomeDefinitions != null ? s.biomeDefinitions.Length : 1;

		for (int a = 0; a < attempts && w.islandCount < target; a++)
		{
			float px = rng.Range(border, size - 1f - border);
			float py = rng.Range(border, size - 1f - border);

			// Exclude center dock area
			float ddx = px - cx, ddy = py - cy;
			if (cr > 0 && ddx * ddx + ddy * ddy < cr2) continue;

			int gcx = Mathf.Clamp((int)(px / cell), 0, gw - 1);
			int gcy = Mathf.Clamp((int)(py / cell), 0, gh - 1);

			bool ok = true;
			int rCells = 2;
			for (int yy = gcy - rCells; yy <= gcy + rCells && ok; yy++)
			{
				if ((uint)yy >= (uint)gh) continue;
				int row = yy * gw;
				for (int xx = gcx - rCells; xx <= gcx + rCells; xx++)
				{
					if ((uint)xx >= (uint)gw) continue;
					int id = w.islandGrid[row + xx];
					if (id < 0) continue;
					float dx = w.islandCenters[id].x - px;
					float dy = w.islandCenters[id].y - py;
					if (dx * dx + dy * dy < minD2) { ok = false; break; }
				}
			}
			if (!ok) continue;

			int islandId = w.islandCount++;
			w.islandCenters[islandId] = new Vector2(px, py);
			w.islandRadii[islandId] = rng.Range(minR, maxR + 1);
			w.islandData[islandId] = new IslandInfoV3(px, py, w.islandRadii[islandId]);

			w.islandBiomes[islandId] = (BiomeType)(rng.NextInt(0, biomeCount));

			w.islandGrid[gcy * gw + gcx] = islandId;
		}
	}

	// ------------------------------------------------------------------
	// STEP 1b — Precompute domain warp displacements into flat arrays
	// Moves 4 Perlin calls out of the per-island inner loop into a
	// single sequential pass that is cache-friendly. Output is identical.
	// ------------------------------------------------------------------
	private static void PrecomputeWarp(MapDataV3 d, MapSettingsV3 s, MapWorkspaceV3 w, Vector2 baseOffset)
	{
		int size = d.size;
		float warpScaleA = Mathf.Max(1f, s.warpScaleA);
		float warpScaleB = Mathf.Max(1f, s.warpScaleB);
		float warpAmp = s.warpAmplitude;

		for (int y = 0; y < size; y++)
		{
			int row = y * size;
			for (int x = 0; x < size; x++)
			{
				float wx1 = Mathf.PerlinNoise((x / warpScaleA) + baseOffset.x + 11.3f, (y / warpScaleA) + baseOffset.y + 91.7f) * 2f - 1f;
				float wy1 = Mathf.PerlinNoise((x / warpScaleA) + baseOffset.x + 71.1f, (y / warpScaleA) + baseOffset.y + 19.9f) * 2f - 1f;
				float wx2 = Mathf.PerlinNoise((x / warpScaleB) + baseOffset.x + 201.2f, (y / warpScaleB) + baseOffset.y + 9.4f) * 2f - 1f;
				float wy2 = Mathf.PerlinNoise((x / warpScaleB) + baseOffset.x + 7.6f, (y / warpScaleB) + baseOffset.y + 155.8f) * 2f - 1f;
				w.warpDX[row + x] = (wx1 + wx2) * warpAmp;
				w.warpDY[row + x] = (wy1 + wy2) * warpAmp;
			}
		}
	}

	// ------------------------------------------------------------------
	// STEP 2 — Rasterize islands onto height map
	// Domain warp + directional edge noise for organic shapes
	// ------------------------------------------------------------------
	private static void RasterizeIslands(
		MapDataV3 d, MapSettingsV3 s, MapWorkspaceV3 w, Vector2 baseOffset)
	{
		int size = d.size;
		float cell = w.islandCellSize;
		int gw = w.islandGridW;
		int gh = w.islandGridH;
		float sharp = Mathf.Max(0.01f, s.islandSharpness);

		float warpAmp = s.warpAmplitude;
		float edgeScale = Mathf.Max(0.0001f, s.edgeNoiseScale);
		float edgeAmp = Mathf.Clamp01(s.edgeNoiseStrength);
		float ridgeAmp = Mathf.Clamp01(s.ridgeNoiseStrength);

		int maxR = s.islandMaxRadius;

		// Include warp amplitude in the search distance so that tiles
		// displaced by domain warp can still find their nearest island center.
		float maxSearchDist = maxR + warpAmp * 2f;
		int searchCells = Mathf.CeilToInt(maxSearchDist / cell) + 2;

		// Reset islandId map
		int n = size * size;
		for (int i = 0; i < n; i++) d.islandId[i] = -1;

		float[] warpDX = w.warpDX;
		float[] warpDY = w.warpDY;

		for (int y = 0; y < size; y++)
		{
			int row = y * size;
			for (int x = 0; x < size; x++)
			{
				int ti = row + x;
				float sx = x + warpDX[ti];
				float sy = y + warpDY[ti];

				int gcx = Mathf.Clamp((int)(sx / cell), 0, gw - 1);
				int gcy = Mathf.Clamp((int)(sy / cell), 0, gh - 1);

				float best = 0f;
				int bestId = -1;

				for (int yy = gcy - searchCells; yy <= gcy + searchCells; yy++)
				{
					if ((uint)yy >= (uint)gh) continue;
					int gRow = yy * gw;
					for (int xx = gcx - searchCells; xx <= gcx + searchCells; xx++)
					{
						if ((uint)xx >= (uint)gw) continue;
						int id = w.islandGrid[gRow + xx];
						if (id < 0) continue;

						IslandInfoV3 isl = w.islandData[id];
						float r = isl.r;

						float dx = sx - isl.cx;
						float dy = sy - isl.cy;
						float d2 = dx * dx + dy * dy;

						// Directional edge perturbation (non-round coastlines)
						float ang = Mathf.Atan2(dy, dx);
						float ax = Mathf.Cos(ang) * r;
						float ay = Mathf.Sin(ang) * r;

						float edgeN = Fbm(
							ax / edgeScale, ay / edgeScale,
							baseOffset.x + 33.7f, baseOffset.y + 88.2f, 3, 2.1f, 0.55f);

						float ridgeN = Fbm(
							sx / Mathf.Max(1f, edgeScale * 0.65f), sy / Mathf.Max(1f, edgeScale * 0.65f),
							baseOffset.x + 401.4f, baseOffset.y + 19.2f, 2, 2.0f, 0.5f);

						float rPerturbed = r * (1f + edgeN * edgeAmp) * (1f - Mathf.Abs(ridgeN) * ridgeAmp);
						float rr = rPerturbed * rPerturbed;
						if (d2 >= rr) continue;

						float dist = Mathf.Sqrt(d2);
						float t = 1f - (dist / Mathf.Max(0.0001f, rPerturbed));
						if (t > best) { best = t; bestId = id; }
					}
				}

				if (best > 0f)
				{
					d.height[row + x] = Mathf.Clamp01(Mathf.Pow(best, sharp));
					d.islandId[row + x] = bestId;
				}
				else
				{
					d.height[row + x] = 0f;
				}
			}
		}
	}

	private static float Fbm(float x, float y, float ox, float oy, int oct, float lac, float pers)
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

	public static float SeaweedValue(int x, int y, MapSettingsV3 s, Vector2 seaweedOffset, int size)
	{
		float sx = (float)x / size * s.seaweedScale + seaweedOffset.x;
		float sy = (float)y / size * s.seaweedScale + seaweedOffset.y;
		return Mathf.PerlinNoise(sx, sy);
	}

	public static float SeaweedJitterValue(int x, int y, MapSettingsV3 s, Vector2 seaweedOffset)
	{
		float sx = x / s.seaweedJitterScale + seaweedOffset.x + 3141.59f;
		float sy = y / s.seaweedJitterScale + seaweedOffset.y + 2718.28f;
		return Mathf.PerlinNoise(sx, sy);
	}
}