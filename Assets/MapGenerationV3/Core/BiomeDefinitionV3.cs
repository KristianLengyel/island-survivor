using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// ScriptableObject for a single biome. Add new biomes by creating new assets —
/// no code changes required in the generation pipeline.
/// </summary>
[CreateAssetMenu(fileName = "BiomeDef_Tropical", menuName = "MapGenV3/BiomeDefinition")]
public class BiomeDefinitionV3 : ScriptableObject
{
	[Header("Identity")]
	public BiomeType biomeType = BiomeType.Tropical;
	public string biomeName = "Tropical";

	[Header("Tiles")]
	public TileBase landTile;       // beach / sand tile
	public TileBase grassTile;      // grass / interior tile
	public TileBase[] landVariants; // optional extra variants (randomly chosen)
	public TileBase[] grassVariants;

	[Header("Palms")]
	public GameObject palmPrefab;
	[Range(0f, 1f)] public float palmSpawnChance = 0.65f;
	[Min(1)] public int palmMinDistance = 6;
	[Min(0)] public int palmCoastMin = 3;
	[Min(0)] public int palmCoastMax = 28;

	[Header("Rocks")]
	public GameObject rockPrefab;
	[Range(0f, 1f)] public float rockSpawnChance = 0.2f;

	[Header("Seaweed")]
	[Range(0f, 1f)] public float seaweedDensityMultiplier = 1.0f;

	[Header("Colors (optional tinting)")]
	public Color landColor = Color.white;
	public Color grassColor = Color.white;
}