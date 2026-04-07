using UnityEngine;
using UnityEngine.Tilemaps;

public class TileChecker : MonoBehaviour
{
	[Header("Basic Tilemaps")]
	public Tilemap waterTilemap;
	public Tilemap landTilemap;
	public Tilemap buildingTilemap;
	public Tilemap objectsTilemap;

	[Header("Ocean Floor Tilemaps")]
	public Tilemap oceanFloorShallowTilemap;
	public Tilemap oceanFloorMediumTilemap;
	public Tilemap oceanFloorDeepTilemap;
	public Tilemap oceanFloorAbyssTilemap;

	public int GetWaterDepth(Vector3Int cellPosition)
	{
		if (oceanFloorAbyssTilemap != null && oceanFloorAbyssTilemap.HasTile(cellPosition)) return 4;
		if (oceanFloorDeepTilemap != null && oceanFloorDeepTilemap.HasTile(cellPosition)) return 3;
		if (oceanFloorMediumTilemap != null && oceanFloorMediumTilemap.HasTile(cellPosition)) return 2;
		return 1;
	}

	public string CheckTileType(Vector3 worldPosition)
	{
		Vector3Int cellPosition = waterTilemap.WorldToCell(worldPosition);
		bool onLand =
			landTilemap.HasTile(cellPosition) ||
			buildingTilemap.HasTile(cellPosition) ||
			objectsTilemap.HasTile(cellPosition);

		if (onLand)
		{
			return "Land";
		}

		bool onWater = waterTilemap.HasTile(cellPosition);
		bool onShallow = (oceanFloorShallowTilemap != null)
						 && oceanFloorShallowTilemap.HasTile(cellPosition);
		bool onMedium = (oceanFloorMediumTilemap != null)
						&& oceanFloorMediumTilemap.HasTile(cellPosition);
		bool onDeep = (oceanFloorDeepTilemap != null)
					  && oceanFloorDeepTilemap.HasTile(cellPosition);
		bool onAbyss = (oceanFloorAbyssTilemap != null)
					   && oceanFloorAbyssTilemap.HasTile(cellPosition);

		if (onWater)
		{
			if (onAbyss) return "Abyss";
			if (onShallow) return "Shallow";
			if (onMedium) return "Medium";
			if (onDeep) return "Deep";
			return "Water";
		}

		return "None";
	}
}
