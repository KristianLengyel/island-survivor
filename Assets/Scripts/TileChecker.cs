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

	/// <summary>
	/// Returns a string describing the tile "type" found at worldPosition.
	/// Possible values:
	///   - "Land"    => if there's a tile in landTilemap OR buildingTilemap OR objectsTilemap
	///   - "Shallow" => purely water + shallow ocean floor
	///   - "Medium"  => purely water + medium ocean floor
	///   - "Deep"    => purely water + deep ocean floor
	///   - "Water"   => purely water, but no shallow/medium/deep found
	///   - "None"    => no tile in any tilemap
	/// </summary>
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

		if (onWater)
		{
			if (onShallow) return "Shallow";
			if (onMedium) return "Medium";
			if (onDeep) return "Deep";
			return "Water";
		}

		return "None";
	}
}
