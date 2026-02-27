public static class MapGenMasks
{
	public static void ThresholdLand(MapGenData d, TilemapGenerationSettingsV2 s)
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
			float dy = y - cy;
			float dy2 = dy * dy;

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

	public static void MorphologyClosing(MapGenData d, int iterations, byte[] tmp)
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

				byte any = 0;

				int y0 = y - 1;
				int y1 = y;
				int y2 = y + 1;

				if ((uint)y0 < (uint)size)
				{
					int r = y0 * size;
					int x0 = x - 1;
					int x1 = x;
					int x2 = x + 1;

					if ((uint)x0 < (uint)size && src[r + x0] == 1) any = 1;
					else if (src[r + x1] == 1) any = 1;
					else if ((uint)x2 < (uint)size && src[r + x2] == 1) any = 1;
				}

				if (any == 0)
				{
					int r = y1 * size;
					int x0 = x - 1;
					int x2 = x + 1;

					if ((uint)x0 < (uint)size && src[r + x0] == 1) any = 1;
					else if ((uint)x2 < (uint)size && src[r + x2] == 1) any = 1;
				}

				if (any == 0 && (uint)y2 < (uint)size)
				{
					int r = y2 * size;
					int x0 = x - 1;
					int x1 = x;
					int x2 = x + 1;

					if ((uint)x0 < (uint)size && src[r + x0] == 1) any = 1;
					else if (src[r + x1] == 1) any = 1;
					else if ((uint)x2 < (uint)size && src[r + x2] == 1) any = 1;
				}

				dst[i] = any;
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

				if (y == 0 || x == 0 || y == size - 1 || x == size - 1)
				{
					dst[i] = 0;
					continue;
				}

				int r0 = (y - 1) * size;
				int r1 = y * size;
				int r2 = (y + 1) * size;

				int x0 = x - 1;
				int x1 = x;
				int x2 = x + 1;

				byte all = 1;

				if (src[r0 + x0] == 0) all = 0;
				else if (src[r0 + x1] == 0) all = 0;
				else if (src[r0 + x2] == 0) all = 0;
				else if (src[r1 + x0] == 0) all = 0;
				else if (src[r1 + x2] == 0) all = 0;
				else if (src[r2 + x0] == 0) all = 0;
				else if (src[r2 + x1] == 0) all = 0;
				else if (src[r2 + x2] == 0) all = 0;

				dst[i] = all;
			}
		}
	}

	public static void RemoveSmallIslands(MapGenData d, int minTiles, MapGenWorkspace w)
	{
		if (minTiles <= 1) return;

		int size = d.size;
		int n = size * size;

		int[] labels = w.labels;
		for (int i = 0; i < n; i++) labels[i] = 0;

		int[] q = w.bfsQueue;

		int label = 0;

		w.EnsureComponentCapacity(8);
		int[] counts = w.componentCounts;

		for (int i = 0; i < counts.Length; i++) counts[i] = 0;

		for (int start = 0; start < n; start++)
		{
			if (d.land[start] == 0) continue;
			if (labels[start] != 0) continue;

			label++;
			w.EnsureComponentCapacity(label);
			int count = FloodLabelLand(d.land, labels, size, start, label, q);
			w.componentCounts[label] = count;
		}

		for (int i = 0; i < n; i++)
		{
			int l = labels[i];
			if (l == 0) continue;
			if (w.componentCounts[l] < minTiles) d.land[i] = 0;
		}
	}

	public static void FillSmallLakes(MapGenData d, int minTiles, MapGenWorkspace w)
	{
		if (minTiles <= 1) return;

		int size = d.size;
		int n = size * size;

		int[] labels = w.labels;
		for (int i = 0; i < n; i++) labels[i] = 0;

		int[] q = w.bfsQueue;

		int label = 0;

		w.EnsureComponentCapacity(8);
		int[] counts = w.componentCounts;
		byte[] touchesBorder = w.componentTouchesBorder;

		for (int i = 0; i < counts.Length; i++) counts[i] = 0;
		for (int i = 0; i < touchesBorder.Length; i++) touchesBorder[i] = 0;

		for (int start = 0; start < n; start++)
		{
			if (d.land[start] == 1) continue;
			if (labels[start] != 0) continue;

			label++;
			w.EnsureComponentCapacity(label);

			int count;
			byte border;
			FloodLabelWater(d.land, labels, size, start, label, q, out count, out border);

			w.componentCounts[label] = count;
			w.componentTouchesBorder[label] = border;
		}

		for (int i = 0; i < n; i++)
		{
			int l = labels[i];
			if (l == 0) continue;
			if (w.componentTouchesBorder[l] == 0 && w.componentCounts[l] < minTiles) d.land[i] = 1;
		}
	}

	private static int FloodLabelLand(byte[] land, int[] labels, int size, int start, int label, int[] q)
	{
		int qs = 0;
		int qe = 0;

		q[qe++] = start;
		labels[start] = label;

		int count = 0;

		while (qs < qe)
		{
			int idx = q[qs++];
			count++;

			int x = idx % size;
			int y = idx / size;

			int up = y + 1;
			int dn = y - 1;
			int rt = x + 1;
			int lf = x - 1;

			if (up < size) TryEnqueueLand(land, labels, idx + size, label, q, ref qe);
			if (dn >= 0) TryEnqueueLand(land, labels, idx - size, label, q, ref qe);
			if (rt < size) TryEnqueueLand(land, labels, idx + 1, label, q, ref qe);
			if (lf >= 0) TryEnqueueLand(land, labels, idx - 1, label, q, ref qe);
		}

		return count;
	}

	private static void TryEnqueueLand(byte[] land, int[] labels, int idx, int label, int[] q, ref int qe)
	{
		if (labels[idx] != 0) return;
		if (land[idx] == 0) return;
		labels[idx] = label;
		q[qe++] = idx;
	}

	private static void FloodLabelWater(byte[] land, int[] labels, int size, int start, int label, int[] q, out int count, out byte border)
	{
		int qs = 0;
		int qe = 0;

		q[qe++] = start;
		labels[start] = label;

		count = 0;
		border = 0;

		while (qs < qe)
		{
			int idx = q[qs++];
			count++;

			int x = idx % size;
			int y = idx / size;

			if (x == 0 || y == 0 || x == size - 1 || y == size - 1) border = 1;

			int up = y + 1;
			int dn = y - 1;
			int rt = x + 1;
			int lf = x - 1;

			if (up < size) TryEnqueueWater(land, labels, idx + size, label, q, ref qe);
			if (dn >= 0) TryEnqueueWater(land, labels, idx - size, label, q, ref qe);
			if (rt < size) TryEnqueueWater(land, labels, idx + 1, label, q, ref qe);
			if (lf >= 0) TryEnqueueWater(land, labels, idx - 1, label, q, ref qe);
		}
	}

	private static void TryEnqueueWater(byte[] land, int[] labels, int idx, int label, int[] q, ref int qe)
	{
		if (labels[idx] != 0) return;
		if (land[idx] == 1) return;
		labels[idx] = label;
		q[qe++] = idx;
	}
}