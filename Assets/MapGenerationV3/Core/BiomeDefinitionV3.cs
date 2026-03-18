using UnityEngine;
using UnityEngine.Tilemaps;

public enum DecoratorPlacementZone { Beach, Grass }

[System.Serializable]
public class BiomeDecoratorEntry
{
	public GameObject prefab;
	public DecoratorPlacementZone placementZone;
	[Min(0)] public int coastDistMin;
	[Min(0)] public int coastDistMax = 28;
	[Range(0f, 1f)] public float spawnChance = 0.5f;
	[Min(1)] public int minSpacing = 4;
	public Vector2 spawnOffset;
}

[CreateAssetMenu(fileName = "BiomeDef_Tropical", menuName = "MapGenV3/BiomeDefinition")]
public class BiomeDefinitionV3 : ScriptableObject
{
	[Header("Identity")]
	public BiomeType biomeType = BiomeType.Tropical;
	public string biomeName = "Tropical";

	[Header("Tiles")]
	public TileBase landTile;
	public TileBase grassTile;
	public TileBase[] landVariants;
	public TileBase[] grassVariants;

	[Header("Decorators")]
	public BiomeDecoratorEntry[] decorators;

	[Header("Seaweed")]
	[Range(0f, 1f)] public float seaweedDensityMultiplier = 1.0f;

	[Header("Colors (optional tinting)")]
	public Color landColor = Color.white;
	public Color grassColor = Color.white;

	[Header("Mini-Map Colors")]
	public Color mapLandColor  = new Color(0.85f, 0.78f, 0.50f);
	public Color mapGrassColor = new Color(0.27f, 0.60f, 0.25f);
}
