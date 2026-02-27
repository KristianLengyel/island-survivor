using UnityEngine;
using UnityEngine.Tilemaps;

public static class TilemapSerializer
{
	public static TilemapLayerData Capture(string layerId, Tilemap tilemap)
	{
		var data = new TilemapLayerData { layerId = layerId };

		if (tilemap == null) return data;

		var bounds = tilemap.cellBounds;

		for (int x = bounds.xMin; x < bounds.xMax; x++)
			for (int y = bounds.yMin; y < bounds.yMax; y++)
			{
				var pos = new Vector3Int(x, y, 0);
				var t = tilemap.GetTile(pos);
				if (t == null) continue;

				data.tiles.Add(new TileData
				{
					x = pos.x,
					y = pos.y,
					z = pos.z,
					tileId = t.name
				});
			}

		return data;
	}

	public static void Restore(TilemapLayerData data, Tilemap tilemap, TileDatabase tileDb)
	{
		if (tilemap == null || data == null) return;

		tilemap.ClearAllTiles();

		for (int i = 0; i < data.tiles.Count; i++)
		{
			var td = data.tiles[i];
			var tile = tileDb.Get(td.tileId);
			if (tile == null) continue;
			tilemap.SetTile(new Vector3Int(td.x, td.y, td.z), tile);
		}
	}
}
