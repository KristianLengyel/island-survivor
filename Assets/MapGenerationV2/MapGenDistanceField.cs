public static class MapGenDistanceField
{
	public static void ComputeCoastDistance(MapGenData d, int[] q)
	{
		int size = d.size;
		int n = size * size;

		int[] dist = d.coastDist;
		for (int i = 0; i < n; i++) dist[i] = int.MaxValue;

		int qs = 0;
		int qe = 0;

		for (int y = 0; y < size; y++)
		{
			int row = y * size;
			for (int x = 0; x < size; x++)
			{
				int i = row + x;
				byte v = d.land[i];

				bool isCoast = false;
				if (x > 0 && d.land[i - 1] != v) isCoast = true;
				else if (x < size - 1 && d.land[i + 1] != v) isCoast = true;
				else if (y > 0 && d.land[i - size] != v) isCoast = true;
				else if (y < size - 1 && d.land[i + size] != v) isCoast = true;

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

			int baseD = dist[idx];

			int up = y + 1;
			int dn = y - 1;
			int rt = x + 1;
			int lf = x - 1;

			if (up < size) Relax(idx + size, baseD);
			if (dn >= 0) Relax(idx - size, baseD);
			if (rt < size) Relax(idx + 1, baseD);
			if (lf >= 0) Relax(idx - 1, baseD);
		}

		void Relax(int nIdx, int baseD)
		{
			int nd = baseD + 1;
			if (nd >= dist[nIdx]) return;
			dist[nIdx] = nd;
			q[qe++] = nIdx;
		}

		for (int i = 0; i < n; i++)
		{
			if (d.land[i] == 0) dist[i] = -dist[i];
		}
	}
}