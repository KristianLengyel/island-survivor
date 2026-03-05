public static class MapDistanceFieldV3
{
	public static void Compute(MapDataV3 d, int[] q)
	{
		int size = d.size;
		int n = size * size;
		int[] dist = d.coastDist;

		for (int i = 0; i < n; i++) dist[i] = int.MaxValue;

		int qs = 0, qe = 0;

		for (int y = 0; y < size; y++)
		{
			int row = y * size;
			for (int x = 0; x < size; x++)
			{
				int i = row + x;
				byte v = d.land[i];
				bool isCoast =
					(x > 0 && d.land[i - 1] != v) ||
					(x < size - 1 && d.land[i + 1] != v) ||
					(y > 0 && d.land[i - size] != v) ||
					(y < size - 1 && d.land[i + size] != v);

				if (isCoast)
				{
					dist[i] = 0;
					q[qe++] = i;
				}
			}
		}

		while (qs < qe)
		{
			int idx = q[qs++];
			int x = idx % size;
			int y = idx / size;
			int nd = dist[idx] + 1;

			if (y + 1 < size) TryEnqueue(idx + size, nd, dist, q, ref qe);
			if (y - 1 >= 0) TryEnqueue(idx - size, nd, dist, q, ref qe);
			if (x + 1 < size) TryEnqueue(idx + 1, nd, dist, q, ref qe);
			if (x - 1 >= 0) TryEnqueue(idx - 1, nd, dist, q, ref qe);
		}

		for (int i = 0; i < n; i++)
			if (d.land[i] == 0) dist[i] = -dist[i];
	}

	private static void TryEnqueue(int nIdx, int nd, int[] dist, int[] q, ref int qe)
	{
		if (dist[nIdx] != int.MaxValue) return;
		dist[nIdx] = nd;
		q[qe++] = nIdx;
	}
}