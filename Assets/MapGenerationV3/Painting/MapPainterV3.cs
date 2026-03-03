using UnityEngine;
using UnityEngine.Tilemaps;

public static class MapPainterV3
{
	public static void PaintAll(
		MapDataV3 d,
		MapSettingsV3 s,
		Tilemap waterTilemap,
		Tilemap landTilemap,
		Tilemap grassTilemap,
		Tilemap oceanOverlayTilemap,
		Tilemap oceanFloorShallow,
		Tilemap oceanFloorMedium,
		Tilemap oceanFloorDeep,
		MapWorkspaceV3 w)
	{
		int size = d.size;
		int pad = d.pad;
		int waterSize = d.waterSize;
		int n = size * size;
		Vector3Int waterStart = new Vector3Int(-size / 2 - pad, -size / 2 - pad, 0);
		Vector3Int landStart = new Vector3Int(-size / 2, -size / 2, 0);
		var landBounds = new BoundsInt(landStart, new Vector3Int(size, size, 1));

		if (oceanFloorDeep != null && s.oceanFloorDeepTile != null)
		{
			for (int i = 0; i < n; i++) w.deepTiles[i] = s.oceanFloorDeepTile;
			oceanFloorDeep.SetTilesBlock(landBounds, w.deepTiles);
		}

		if (oceanFloorMedium != null && s.oceanFloorMediumTile != null)
		{
			for (int i = 0; i < n; i++)
				w.medTiles[i] = (d.land[i] == 0 && d.oceanBand[i] <= 2) ? s.oceanFloorMediumTile : null;
			oceanFloorMedium.SetTilesBlock(landBounds, w.medTiles);
		}

		if (oceanFloorShallow != null && s.oceanFloorShallowTile != null)
		{
			for (int i = 0; i < n; i++)
				w.shTiles[i] = (d.land[i] == 1 || (d.land[i] == 0 && d.oceanBand[i] <= 1))
					? s.oceanFloorShallowTile : null;
			oceanFloorShallow.SetTilesBlock(landBounds, w.shTiles);
		}

		if (landTilemap != null)
		{
			for (int i = 0; i < n; i++)
			{
				if (d.land[i] != 1) { w.landTiles[i] = null; continue; }
				var bdef = s.GetBiome((BiomeType)d.biome[i]);
				w.landTiles[i] = bdef?.landTile ?? s.oceanFloorShallowTile;
			}
			landTilemap.SetTilesBlock(landBounds, w.landTiles);

			for (int i = 0; i < n; i++)
			{
				if (d.land[i] != 1) continue;
				var bdef = s.GetBiome((BiomeType)d.biome[i]);
				if (bdef == null || bdef.landColor == Color.white) continue;
				int x = i % size, y = i / size;
				landTilemap.SetColor(landStart + new Vector3Int(x, y, 0), bdef.landColor);
			}
		}

		if (grassTilemap != null)
		{
			for (int i = 0; i < n; i++)
			{
				if (d.grass[i] != 1) { w.grassTiles[i] = null; continue; }
				var bdef = s.GetBiome((BiomeType)d.biome[i]);
				w.grassTiles[i] = bdef?.grassTile ?? null;
			}
			grassTilemap.SetTilesBlock(landBounds, w.grassTiles);

			for (int i = 0; i < n; i++)
			{
				if (d.grass[i] != 1) continue;
				var bdef = s.GetBiome((BiomeType)d.biome[i]);
				if (bdef == null || bdef.grassColor == Color.white) continue;
				int x = i % size, y = i / size;
				grassTilemap.SetColor(landStart + new Vector3Int(x, y, 0), bdef.grassColor);
			}
		}

		if (oceanOverlayTilemap != null && s.seaweedTile != null)
		{
			for (int i = 0; i < n; i++)
				w.overlayTiles[i] = d.seaweed[i] == 1 ? s.seaweedTile : null;
			oceanOverlayTilemap.SetTilesBlock(landBounds, w.overlayTiles);
		}
		else if (oceanOverlayTilemap != null)
		{
			for (int i = 0; i < n; i++) w.overlayTiles[i] = null;
			oceanOverlayTilemap.SetTilesBlock(landBounds, w.overlayTiles);
		}

		PaintWater(d, s, waterTilemap, waterStart, size, pad, waterSize, w);
	}

	private static void PaintWater(
		MapDataV3 d, MapSettingsV3 s, Tilemap waterTilemap,
		Vector3Int waterStart, int size, int pad, int waterSize, MapWorkspaceV3 w)
	{
		if (waterTilemap == null || s.waterTile == null) return;

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
				bool keepWater = overlap > 0 &&
					d.coastDist[idx] >= 0 &&
					d.coastDist[idx] < overlap;
				if (!keepWater)
					w.waterTiles[wrow + (x + pad)] = null;
			}
		}

		waterTilemap.SetTilesBlock(
			new BoundsInt(waterStart, new Vector3Int(waterSize, waterSize, 1)),
			w.waterTiles);
	}
}