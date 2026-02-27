using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "TilemapGenerationSettingsV2", menuName = "IslandOdyssey/TilemapGenerationSettingsV2")]
public class TilemapGenerationSettingsV2 : ScriptableObject
{
	[Header("Map")]
	public int mapSize = 256;
	public int pad = 1;

	[Header("Seed")]
	public bool useRandomSeed = true;
	public string seedInput = "seed";

	[Header("Noise")]
	public float baseScale = 90f;
	[Range(1, 8)] public int octaves = 4;
	[Range(0.1f, 0.95f)] public float persistence = 0.5f;
	[Range(1.2f, 4.0f)] public float lacunarity = 2.0f;

	[Header("Island Shape")]
	[Range(0.2f, 0.8f)] public float landThreshold = 0.52f;
	[Range(0.0f, 0.7f)] public float radialFalloffStrength = 0.45f;
	[Range(0.0f, 1.0f)] public float ridgeStrength = 0.15f;
	public int borderWater = 18;

	[Header("Center Gameplay")]
	[Min(0)] public int centerNoIslandRadius = 18;
	[Min(1)] public int startPlatformWidth = 9;
	[Min(1)] public int startPlatformHeight = 9;
	public TileBase startPlatformTile;

	[Header("Cleanup")]
	[Min(1)] public int minIslandTiles = 20;
	[Min(1)] public int minLakeTiles = 10;
	[Range(0, 4)] public int morphologyClosingIterations = 1;

	[Header("Coast Bands")]
	[Min(0)] public int beachWidthMin = 2;
	[Min(0)] public int beachWidthMax = 5;
	[Min(0)] public int grassInset = 1;

	[Header("Ocean Depth Bands (Legacy)")]
	[Min(1)] public int shallowDepth = 3;
	[Min(1)] public int mediumDepth = 6;

	[Header("Ocean Depth Bands (Natural)")]
	public bool useNaturalDepthBands = true;
	[Range(0.01f, 1f)] public float naturalShallowCut = 0.28f;
	[Range(0.01f, 1f)] public float naturalMediumCut = 0.62f;
	[Min(1)] public int shelfDistanceTiles = 10;
	[Range(0f, 1f)] public float shelfStrength = 0.7f;

	[Header("Seaweed")]
	public float seaweedScale = 20f;
	[Range(0f, 1f)] public float seaweedThreshold = 0.62f;
	[Min(0)] public int seaweedMinDepth = 2;

	[Header("Palm Trees")]
	[Range(0f, 1f)] public float palmSpawnChance = 0.7f;
	[Min(1)] public int palmMinDistance = 6;
	[Min(0)] public int palmCoastMin = 4;
	[Min(0)] public int palmCoastMax = 30;

	[Header("Water / Shore Visual")]
	[Min(0)] public int waterOverlapLandInland = 0;

	[Header("Tiles")]
	public TileBase waterTile;
	public TileBase sandTile;
	public TileBase grassTile;
	public TileBase seaweedTile;

	[Header("Ocean Floor Tiles")]
	public TileBase oceanFloorShallowTile;
	public TileBase oceanFloorMediumTile;
	public TileBase oceanFloorDeepTile;

	[Header("Colors")]
	public Color sandColor = Color.yellow;
	public Color waterColor = Color.blue;
}