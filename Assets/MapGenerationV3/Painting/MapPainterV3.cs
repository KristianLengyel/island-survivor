using UnityEngine;
using UnityEngine.Tilemaps;

public static class MapPainterV3
{
	public const int LAYER_OCEAN_DEEP = 0;
	public const int LAYER_OCEAN_MEDIUM = 1;
	public const int LAYER_OCEAN_SHALLOW = 2;
	public const int LAYER_WATER = 3;
	public const int LAYER_LAND = 4;
	public const int LAYER_GRASS = 5;
	public const int LAYER_OVERLAY = 6;
	public const int LAYER_COUNT = 7;

	public static void PaintAll(
		MapDataV3 d, MapSettingsV3 s,
		Tilemap waterTilemap, Tilemap landTilemap, Tilemap grassTilemap,
		Tilemap oceanOverlayTilemap,
		Tilemap oceanFloorShallow, Tilemap oceanFloorMedium, Tilemap oceanFloorDeep,
		MapWorkspaceV3 w)
	{
		if (w.chunks == null) return;
		for (int i = 0; i < w.chunks.Length; i++)
			PaintChunk(i, d, s, waterTilemap, landTilemap, grassTilemap,
				oceanOverlayTilemap, oceanFloorShallow, oceanFloorMedium, oceanFloorDeep, w);
	}

	public static void PaintChunk(
		int chunkIndex,
		MapDataV3 d, MapSettingsV3 s,
		Tilemap waterTilemap, Tilemap landTilemap, Tilemap grassTilemap,
		Tilemap oceanOverlayTilemap,
		Tilemap oceanFloorShallow, Tilemap oceanFloorMedium, Tilemap oceanFloorDeep,
		MapWorkspaceV3 w)
	{
		ref MapChunkV3 chunk = ref w.chunks[chunkIndex];
		if (chunk.isLoaded) return;
		for (int layer = 0; layer < LAYER_COUNT; layer++)
			PaintChunkLayer(chunkIndex, layer, d, s, waterTilemap, landTilemap, grassTilemap,
				oceanOverlayTilemap, oceanFloorShallow, oceanFloorMedium, oceanFloorDeep, w);
		chunk.isLoaded = true;
	}

	public static void PaintChunkLayer(
		int chunkIndex, int layer,
		MapDataV3 d, MapSettingsV3 s,
		Tilemap waterTilemap, Tilemap landTilemap, Tilemap grassTilemap,
		Tilemap oceanOverlayTilemap,
		Tilemap oceanFloorShallow, Tilemap oceanFloorMedium, Tilemap oceanFloorDeep,
		MapWorkspaceV3 w)
	{
		ref MapChunkV3 chunk = ref w.chunks[chunkIndex];

		int size = d.size;
		int tX = chunk.tileX;
		int tY = chunk.tileY;
		int width = Mathf.Min(chunk.size, size - tX);
		int height = Mathf.Min(chunk.size, size - tY);
		int count = width * height;

		// Tiles start at (0,0) in tilemap local space.
		// The Grid GameObject is repositioned to (-size/2, -size/2) in world space
		// by MapGeneratorV3 after generation, so world-space origin stays centred.
		Vector3Int chunkOrigin = new Vector3Int(tX, tY, 0);
		var bounds = new BoundsInt(chunkOrigin, new Vector3Int(width, height, 1));
		var bdef = s.GetBiome(chunk.dominantBiome);
		TileBase[] buf = w.paintBuffer;

		switch (layer)
		{
			case LAYER_OCEAN_DEEP:
				if (oceanFloorDeep == null || s.oceanFloorDeepTile == null) break;
				for (int k = 0; k < count; k++) buf[k] = s.oceanFloorDeepTile;
				oceanFloorDeep.SetTilesBlock(bounds, buf);
				break;

			case LAYER_OCEAN_MEDIUM:
				if (oceanFloorMedium == null || s.oceanFloorMediumTile == null) break;
				for (int dy = 0; dy < height; dy++)
					for (int dx = 0; dx < width; dx++)
					{
						int i = d.Idx(tX + dx, tY + dy);
						buf[dy * width + dx] = (d.land[i] == 0 && d.oceanBand[i] <= 2)
							? s.oceanFloorMediumTile : null;
					}
				oceanFloorMedium.SetTilesBlock(bounds, buf);
				break;

			case LAYER_OCEAN_SHALLOW:
				if (oceanFloorShallow == null || s.oceanFloorShallowTile == null) break;
				for (int dy = 0; dy < height; dy++)
					for (int dx = 0; dx < width; dx++)
					{
						int i = d.Idx(tX + dx, tY + dy);
						buf[dy * width + dx] = (d.land[i] == 1 || (d.land[i] == 0 && d.oceanBand[i] <= 1))
							? s.oceanFloorShallowTile : null;
					}
				oceanFloorShallow.SetTilesBlock(bounds, buf);
				break;

			case LAYER_WATER:
				PaintChunkWater(d, s, waterTilemap, ref chunk, w);
				break;

			case LAYER_LAND:
				if (landTilemap == null) break;
				{
					TileBase landTile = bdef?.landTile ?? s.oceanFloorShallowTile;
					for (int dy = 0; dy < height; dy++)
						for (int dx = 0; dx < width; dx++)
						{
							int i = d.Idx(tX + dx, tY + dy);
							buf[dy * width + dx] = d.land[i] == 1 ? landTile : null;
						}
					landTilemap.SetTilesBlock(bounds, buf);
					if (bdef != null && bdef.landColor != Color.white)
						ApplyColor(landTilemap, d, chunkOrigin, tX, tY, width, height,
							d.land, 1, bdef.landColor);
				}
				break;

			case LAYER_GRASS:
				if (grassTilemap == null) break;
				{
					TileBase grassTile = bdef?.grassTile;
					for (int dy = 0; dy < height; dy++)
						for (int dx = 0; dx < width; dx++)
						{
							int i = d.Idx(tX + dx, tY + dy);
							buf[dy * width + dx] = d.grass[i] == 1 ? grassTile : null;
						}
					grassTilemap.SetTilesBlock(bounds, buf);
					if (bdef != null && bdef.grassColor != Color.white)
						ApplyColor(grassTilemap, d, chunkOrigin, tX, tY, width, height,
							d.grass, 1, bdef.grassColor);
				}
				break;

			case LAYER_OVERLAY:
				if (oceanOverlayTilemap == null) break;
				for (int dy = 0; dy < height; dy++)
					for (int dx = 0; dx < width; dx++)
					{
						int i = d.Idx(tX + dx, tY + dy);
						buf[dy * width + dx] = d.seaweed[i] == 1 ? s.seaweedTile : null;
					}
				oceanOverlayTilemap.SetTilesBlock(bounds, buf);
				break;
		}
	}

	public static void ClearChunk(
		int chunkIndex,
		MapDataV3 d, MapSettingsV3 s,
		Tilemap waterTilemap, Tilemap landTilemap, Tilemap grassTilemap,
		Tilemap oceanOverlayTilemap,
		Tilemap oceanFloorShallow, Tilemap oceanFloorMedium, Tilemap oceanFloorDeep,
		MapWorkspaceV3 w)
	{
		ref MapChunkV3 chunk = ref w.chunks[chunkIndex];
		if (!chunk.isLoaded) return;

		int size = d.size;
		int tX = chunk.tileX;
		int tY = chunk.tileY;
		int width = Mathf.Min(chunk.size, size - tX);
		int height = Mathf.Min(chunk.size, size - tY);
		int count = width * height;

		Vector3Int chunkOrigin = new Vector3Int(tX, tY, 0);
		var bounds = new BoundsInt(chunkOrigin, new Vector3Int(width, height, 1));

		TileBase[] buf = w.paintBuffer;
		for (int k = 0; k < count; k++) buf[k] = null;

		landTilemap?.SetTilesBlock(bounds, buf);
		grassTilemap?.SetTilesBlock(bounds, buf);
		oceanOverlayTilemap?.SetTilesBlock(bounds, buf);
		oceanFloorShallow?.SetTilesBlock(bounds, buf);
		oceanFloorMedium?.SetTilesBlock(bounds, buf);
		oceanFloorDeep?.SetTilesBlock(bounds, buf);
		ClearChunkWater(d, waterTilemap, ref chunk, w);

		chunk.isLoaded = false;
	}

	// -------------------------------------------------------
	// Water layer — includes pad border around edge chunks
	// -------------------------------------------------------
	private static void PaintChunkWater(MapDataV3 d, MapSettingsV3 s,
		Tilemap waterTilemap, ref MapChunkV3 chunk, MapWorkspaceV3 w)
	{
		if (waterTilemap == null || s.waterTile == null) return;

		int size = d.size;
		int pad = d.pad;
		int overlap = Mathf.Max(0, s.waterOverlapLandInland);
		int tX = chunk.tileX;
		int tY = chunk.tileY;

		int waterOffsetLeft = (tX == 0) ? pad : 0;
		int waterOffsetBottom = (tY == 0) ? pad : 0;
		int waterOffsetRight = (tX + chunk.size >= size) ? pad : 0;
		int waterOffsetTop = (tY + chunk.size >= size) ? pad : 0;

		int wX = tX - waterOffsetLeft;
		int wY = tY - waterOffsetBottom;
		int width = Mathf.Min(chunk.size, size - tX) + waterOffsetLeft + waterOffsetRight;
		int height = Mathf.Min(chunk.size, size - tY) + waterOffsetBottom + waterOffsetTop;

		// Water tiles also start at (0,0) tilemap space, with pad extending into negative coords.
		// We subtract pad so the water border sits at (-pad, -pad) to (size+pad, size+pad).
		Vector3Int chunkOrigin = new Vector3Int(wX - pad, wY - pad, 0);
		var bounds = new BoundsInt(chunkOrigin, new Vector3Int(width, height, 1));

		TileBase[] buf = w.paintBuffer;
		TileBase waterTile = s.waterTile;

		for (int dy = 0; dy < height; dy++)
			for (int dx = 0; dx < width; dx++)
			{
				int mapX = wX + dx;
				int mapY = wY + dy;
				if (mapX < 0 || mapY < 0 || mapX >= size || mapY >= size)
				{
					buf[dy * width + dx] = waterTile;
					continue;
				}
				int idx = d.Idx(mapX, mapY);
				if (d.land[idx] != 1)
				{
					buf[dy * width + dx] = waterTile;
					continue;
				}
				bool keep = overlap > 0 && d.coastDist[idx] >= 0 && d.coastDist[idx] < overlap;
				buf[dy * width + dx] = keep ? waterTile : null;
			}

		waterTilemap.SetTilesBlock(bounds, buf);
	}

	private static void ClearChunkWater(MapDataV3 d,
		Tilemap waterTilemap, ref MapChunkV3 chunk, MapWorkspaceV3 w)
	{
		if (waterTilemap == null) return;

		int size = d.size;
		int pad = d.pad;
		int tX = chunk.tileX;
		int tY = chunk.tileY;

		int waterOffsetLeft = (tX == 0) ? pad : 0;
		int waterOffsetBottom = (tY == 0) ? pad : 0;
		int waterOffsetRight = (tX + chunk.size >= size) ? pad : 0;
		int waterOffsetTop = (tY + chunk.size >= size) ? pad : 0;

		int wX = tX - waterOffsetLeft;
		int wY = tY - waterOffsetBottom;
		int width = Mathf.Min(chunk.size, size - tX) + waterOffsetLeft + waterOffsetRight;
		int height = Mathf.Min(chunk.size, size - tY) + waterOffsetBottom + waterOffsetTop;
		int count = width * height;

		Vector3Int chunkOrigin = new Vector3Int(wX - pad, wY - pad, 0);

		TileBase[] buf = w.paintBuffer;
		for (int k = 0; k < count; k++) buf[k] = null;
		waterTilemap.SetTilesBlock(new BoundsInt(chunkOrigin, new Vector3Int(width, height, 1)), buf);
	}

	private static void ApplyColor(Tilemap tm, MapDataV3 d, Vector3Int origin,
		int tX, int tY, int width, int height,
		byte[] mask, byte matchVal, Color color)
	{
		for (int dy = 0; dy < height; dy++)
			for (int dx = 0; dx < width; dx++)
			{
				int i = d.Idx(tX + dx, tY + dy);
				if (mask[i] == matchVal)
					tm.SetColor(origin + new Vector3Int(dx, dy, 0), color);
			}
	}
}