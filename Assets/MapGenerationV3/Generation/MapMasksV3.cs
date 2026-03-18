using UnityEngine;

public static class MapMasksV3
{
	public static void ThresholdLand(MapDataV3 d, MapSettingsV3 s)
	{
		int size = d.size;
		int border = s.borderWater;
		float thr = s.landThreshold;

		float cx = (size - 1) * 0.5f;
		float cy = (size - 1) * 0.5f;
		int r = s.centerNoIslandRadius;
		int r2 = r * r;

		for (int y = 0; y < size; y++)
		{
			int row = y * size;
			bool yBorder = y < border || y >= size - border;
			float dy2 = (y - cy) * (y - cy);

			for (int x = 0; x < size; x++)
			{
				int i = row + x;
				byte isLand = d.height[i] >= thr ? (byte)1 : (byte)0;

				if (yBorder || x < border || x >= size - border) isLand = 0;

				if (r > 0)
				{
					float dx = x - cx;
					if (dx * dx + dy2 < r2) isLand = 0;
				}

				d.land[i] = isLand;
			}
		}
	}

	public static void CellularAutomata(MapDataV3 d, MapSettingsV3 s, byte[] tmp)
	{
		if (s.caIterations <= 0) return;

		int size = d.size;
		int border = s.borderWater;
		int birth = s.caBirthThreshold;
		int surv = s.caSurvivalThreshold;
		int xMin = Mathf.Max(1, border);
		int xMax = Mathf.Min(size - 1, size - border);
		int yMin = Mathf.Max(1, border);
		int yMax = Mathf.Min(size - 1, size - border);

		for (int it = 0; it < s.caIterations; it++)
		{
			byte[] land = d.land;

			for (int y = 0; y < size; y++)
			{
				int row = y * size;
				if (y < yMin || y >= yMax)
				{
					for (int x = 0; x < size; x++) tmp[row + x] = 0;
					continue;
				}
				for (int x = 0; x < xMin; x++) tmp[row + x] = 0;
				for (int x = xMax; x < size; x++) tmp[row + x] = 0;

				int prev = row - size;
				int next = row + size;
				for (int x = xMin; x < xMax; x++)
				{
					int n =
						land[prev + x - 1] + land[prev + x] + land[prev + x + 1] +
						land[row  + x - 1]                  + land[row  + x + 1] +
						land[next + x - 1] + land[next + x] + land[next + x + 1];
					tmp[row + x] = (byte)(land[row + x] == 1
						? (n >= surv  ? 1 : 0)
						: (n >= birth ? 1 : 0));
				}
			}

			System.Array.Copy(tmp, d.land, size * size);
		}
	}

	public static void MorphologyClosing(MapDataV3 d, int iterations, byte[] tmp)
	{
		if (iterations <= 0) return;
		int n = d.size * d.size;
		if (tmp == null || tmp.Length < n) return;
		for (int it = 0; it < iterations; it++)
		{
			Dilate(d.land, tmp, d.size);
			Erode(tmp, d.land, d.size);
		}
	}

	private static void Dilate(byte[] src, byte[] dst, int size)
	{
		for (int y = 0; y < size; y++)
		{
			int row = y * size;
			for (int x = 0; x < size; x++)
			{
				int i = row + x;
				if (src[i] == 1) { dst[i] = 1; continue; }
				dst[i] = HasLandNeighbour4(src, x, y, size) ? (byte)1 : (byte)0;
			}
		}
	}

	private static void Erode(byte[] src, byte[] dst, int size)
	{
		for (int y = 0; y < size; y++)
		{
			int row = y * size;
			for (int x = 0; x < size; x++)
			{
				int i = row + x;
				if (src[i] == 0) { dst[i] = 0; continue; }
				if (x == 0 || y == 0 || x == size - 1 || y == size - 1) { dst[i] = 0; continue; }
				dst[i] = AllNeighboursLand8(src, x, y, size) ? (byte)1 : (byte)0;
			}
		}
	}

	private static bool HasLandNeighbour4(byte[] src, int x, int y, int size)
	{
		if (y > 0 && src[(y - 1) * size + x] == 1) return true;
		if (y < size - 1 && src[(y + 1) * size + x] == 1) return true;
		if (x > 0 && src[y * size + x - 1] == 1) return true;
		if (x < size - 1 && src[y * size + x + 1] == 1) return true;
		return false;
	}

	private static bool AllNeighboursLand8(byte[] src, int x, int y, int size)
	{
		for (int dy = -1; dy <= 1; dy++)
			for (int dx = -1; dx <= 1; dx++)
			{
				if (dx == 0 && dy == 0) continue;
				if (src[(y + dy) * size + (x + dx)] == 0) return false;
			}
		return true;
	}

	public static void RemoveSmallIslands(MapDataV3 d, int minTiles, MapWorkspaceV3 w)
	{
		if (minTiles <= 1) return;
		int n = d.size * d.size;

		for (int i = 0; i < n; i++) w.labels[i] = 0;
		w.EnsureComponentCapacity(8);
		for (int i = 0; i < w.componentCounts.Length; i++) w.componentCounts[i] = 0;

		int label = 0;
		for (int start = 0; start < n; start++)
		{
			if (d.land[start] == 0 || w.labels[start] != 0) continue;
			label++;
			w.EnsureComponentCapacity(label);
			w.componentCounts[label] = FloodLabel(
				d.land, w.labels, d.size, start, label, w.bfsQueue, true);
		}

		for (int i = 0; i < n; i++)
		{
			int l = w.labels[i];
			if (l != 0 && w.componentCounts[l] < minTiles) d.land[i] = 0;
		}
	}

	public static void FillSmallLakes(MapDataV3 d, int minTiles, MapWorkspaceV3 w)
	{
		if (minTiles <= 1) return;
		int n = d.size * d.size;

		for (int i = 0; i < n; i++) w.labels[i] = 0;
		w.EnsureComponentCapacity(8);
		for (int i = 0; i < w.componentCounts.Length; i++) w.componentCounts[i] = 0;
		for (int i = 0; i < w.componentTouchesBorder.Length; i++) w.componentTouchesBorder[i] = 0;

		int label = 0;
		for (int start = 0; start < n; start++)
		{
			if (d.land[start] == 1 || w.labels[start] != 0) continue;
			label++;
			w.EnsureComponentCapacity(label);
			w.componentCounts[label] = FloodLabel(
				d.land, w.labels, d.size, start, label, w.bfsQueue, false,
				out w.componentTouchesBorder[label]);
		}

		for (int i = 0; i < n; i++)
		{
			int l = w.labels[i];
			if (l != 0 && w.componentTouchesBorder[l] == 0 && w.componentCounts[l] < minTiles)
				d.land[i] = 1;
		}
	}

	public static void PropagateIslandBiomes(MapDataV3 d, MapWorkspaceV3 w, MapSettingsV3 s)
	{
		int size = d.size;
		int n = size * size;

		for (int i = 0; i < n; i++)
		{
			if (d.land[i] == 1 && d.islandId[i] >= 0)
				d.biome[i] = (byte)w.islandBiomes[d.islandId[i]];
			else
				d.biome[i] = 0;
		}

		int[] q = w.biomeQueue;
		bool[] visited = w.biomeVisited;

		for (int i = 0; i < n; i++) visited[i] = false;

		int qs = 0, qe = 0;

		for (int i = 0; i < n; i++)
		{
			if (d.land[i] == 1)
			{
				visited[i] = true;
				q[qe++] = i;
			}
		}

		while (qs < qe)
		{
			int idx = q[qs++];
			int x = idx % size;
			int y = idx / size;

			TrySpread(idx, idx - size, d, q, visited, ref qe, y > 0);
			TrySpread(idx, idx + size, d, q, visited, ref qe, y < size - 1);
			TrySpread(idx, idx - 1, d, q, visited, ref qe, x > 0);
			TrySpread(idx, idx + 1, d, q, visited, ref qe, x < size - 1);
		}
	}

	private static void TrySpread(int from, int to, MapDataV3 d,
		int[] q, bool[] visited, ref int qe, bool inBounds)
	{
		if (!inBounds || visited[to]) return;
		visited[to] = true;
		if (d.biome[to] == 0) d.biome[to] = d.biome[from];
		q[qe++] = to;
	}

	private static int FloodLabel(byte[] land, int[] labels, int size, int start,
		int label, int[] q, bool forLand)
	{
		byte b;
		return FloodLabel(land, labels, size, start, label, q, forLand, out b);
	}

	private static int FloodLabel(byte[] land, int[] labels, int size, int start,
		int label, int[] q, bool forLand, out byte touchesBorder)
	{
		int qs = 0, qe = 0;
		q[qe++] = start;
		labels[start] = label;
		touchesBorder = 0;
		int count = 0;

		while (qs < qe)
		{
			int idx = q[qs++];
			count++;
			int x = idx % size, y = idx / size;
			if (x == 0 || y == 0 || x == size - 1 || y == size - 1) touchesBorder = 1;

			TryEnqueue(land, labels, size, x, y + 1, label, q, ref qe, forLand);
			TryEnqueue(land, labels, size, x, y - 1, label, q, ref qe, forLand);
			TryEnqueue(land, labels, size, x + 1, y, label, q, ref qe, forLand);
			TryEnqueue(land, labels, size, x - 1, y, label, q, ref qe, forLand);
		}
		return count;
	}

	private static void TryEnqueue(byte[] land, int[] labels, int size,
		int nx, int ny, int label, int[] q, ref int qe, bool forLand)
	{
		if ((uint)nx >= (uint)size || (uint)ny >= (uint)size) return;
		int nIdx = ny * size + nx;
		if (labels[nIdx] != 0) return;
		if (forLand && land[nIdx] == 0) return;
		if (!forLand && land[nIdx] == 1) return;
		labels[nIdx] = label;
		q[qe++] = nIdx;
	}
}