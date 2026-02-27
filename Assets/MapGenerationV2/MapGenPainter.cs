using UnityEngine;
using UnityEngine.Tilemaps;

public static class MapGenPainter
{
	public static void PaintAll(
		MapGenData d,
		TilemapGenerationSettingsV2 s,
		Tilemap waterTilemap,
		Tilemap landTilemap,
		Tilemap grassTilemap,
		Tilemap oceanOverlayTilemap,
		Tilemap oceanFloorShallow,
		Tilemap oceanFloorMedium,
		Tilemap oceanFloorDeep,
		MapGenWorkspace w)
	{
		int size = d.size;
		int pad = d.pad;
		int waterSize = d.waterSize;
		int n = size * size;

		Vector3Int waterStart = new Vector3Int(-size / 2 - pad, -size / 2 - pad, 0);
		Vector3Int landStart = new Vector3Int(-size / 2, -size / 2, 0);

		PaintWaterWithLandOverlap(d, s, waterTilemap, waterStart, size, pad, waterSize, w);

		if (oceanFloorDeep != null)
		{
			TileBase t = s.oceanFloorDeepTile;
			if (t != null)
			{
				for (int i = 0; i < n; i++) w.deep[i] = t;
				oceanFloorDeep.SetTilesBlock(new BoundsInt(landStart, new Vector3Int(size, size, 1)), w.deep);
			}
		}

		if (oceanFloorMedium != null)
		{
			TileBase t = s.oceanFloorMediumTile;
			if (t != null)
			{
				for (int i = 0; i < n; i++) w.med[i] = (d.land[i] == 0 && d.oceanBand[i] <= 2) ? t : null;
				oceanFloorMedium.SetTilesBlock(new BoundsInt(landStart, new Vector3Int(size, size, 1)), w.med);
			}
		}

		if (oceanFloorShallow != null)
		{
			TileBase t = s.oceanFloorShallowTile;
			if (t != null)
			{
				for (int i = 0; i < n; i++)
					w.sh[i] = (d.land[i] == 1 || (d.land[i] == 0 && d.oceanBand[i] <= 1)) ? t : null;
				oceanFloorShallow.SetTilesBlock(new BoundsInt(landStart, new Vector3Int(size, size, 1)), w.sh);
			}
		}

		if (landTilemap != null)
		{
			TileBase t = s.sandTile;
			if (t != null)
			{
				for (int i = 0; i < n; i++) w.land[i] = d.land[i] == 1 ? t : null;
				landTilemap.SetTilesBlock(new BoundsInt(landStart, new Vector3Int(size, size, 1)), w.land);
			}
		}

		if (grassTilemap != null)
		{
			TileBase t = s.grassTile;
			if (t != null)
			{
				for (int i = 0; i < n; i++) w.gr[i] = d.grass[i] == 1 ? t : null;
				grassTilemap.SetTilesBlock(new BoundsInt(landStart, new Vector3Int(size, size, 1)), w.gr);
			}
		}

		if (oceanOverlayTilemap != null)
		{
			TileBase t = s.seaweedTile;
			if (t != null)
			{
				for (int i = 0; i < n; i++) w.ov[i] = d.seaweed[i] == 1 ? t : null;
				oceanOverlayTilemap.SetTilesBlock(new BoundsInt(landStart, new Vector3Int(size, size, 1)), w.ov);
			}
		}
	}

	private static void PaintWaterWithLandOverlap(
		MapGenData d,
		TilemapGenerationSettingsV2 s,
		Tilemap waterTilemap,
		Vector3Int waterStart,
		int size,
		int pad,
		int waterSize,
		MapGenWorkspace w)
	{
		if (waterTilemap == null) return;
		if (s.waterTile == null) return;

		int wn = waterSize * waterSize;
		for (int i = 0; i < wn; i++) w.waterTiles[i] = s.waterTile;

		int overlap = Mathf.Max(0, s.waterOverlapLandInland);

		for (int y = 0; y < size; y++)
		{
			int wy = y + pad;
			int wrow = wy * waterSize;
			int drow = y * size;

			for (int x = 0; x < size; x++)
			{
				int idx = drow + x;

				if (d.land[idx] != 1) continue;

				bool keepWater = false;
				if (overlap > 0)
				{
					int inland = d.coastDist[idx];
					if (inland >= 0 && inland < overlap) keepWater = true;
				}

				if (!keepWater)
				{
					int wx = x + pad;
					w.waterTiles[wrow + wx] = null;
				}
			}
		}

		waterTilemap.SetTilesBlock(new BoundsInt(waterStart, new Vector3Int(waterSize, waterSize, 1)), w.waterTiles);
	}
}