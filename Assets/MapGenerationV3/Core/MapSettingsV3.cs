using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "MapSettingsV3", menuName = "MapGenV3/MapSettingsV3")]
public class MapSettingsV3 : ScriptableObject
{
	[Header("Map Size")]
	[Min(32)] public int mapSize = 256;
	[Min(0)] public int pad = 2;

	[Header("Chunk")]
	[Tooltip("Chunk size in tiles. Biome is assigned per chunk.")]
	[Min(8)] public int chunkSize = 32;

	[Header("Seed")]
	public bool useRandomSeed = true;
	public string seedInput = "island";

	// -------------------------------------------------------
	// ISLAND PLACEMENT
	// -------------------------------------------------------
	[Header("Island Placement")]
	[Min(1)] public int islandCount = 80;
	[Min(1)] public int islandMinSpacing = 12;
	[Min(1)] public int islandMinRadius = 6;
	[Min(1)] public int islandMaxRadius = 22;
	[Range(0.6f, 3.0f)] public float islandSharpness = 1.4f;
	[Min(1)] public int islandPlacementAttemptsPerIsland = 30;

	// -------------------------------------------------------
	// DOMAIN WARP  (breaks circular shapes)
	// -------------------------------------------------------
	[Header("Domain Warp")]
	[Tooltip("Scale of the large warp (lower = more dramatic blobs)")]
	public float warpScaleA = 14f;
	[Tooltip("Scale of the fine warp (lower = more fine crinkle)")]
	public float warpScaleB = 28f;
	[Tooltip("How many tiles the warp displaces the sample point")]
	[Range(0f, 12f)] public float warpAmplitude = 5f;

	// -------------------------------------------------------
	// EDGE NOISE  (directional coastline roughness)
	// -------------------------------------------------------
	[Header("Edge Noise")]
	public float edgeNoiseScale = 30f;
	[Range(0f, 0.5f)] public float edgeNoiseStrength = 0.18f;
	[Range(0f, 0.3f)] public float ridgeNoiseStrength = 0.12f;

	// -------------------------------------------------------
	// CELLULAR AUTOMATA  (smooth, sandy coastlines)
	// -------------------------------------------------------
	[Header("Cellular Automata")]
	[Tooltip("More iterations = smoother, rounder coasts. 3-5 recommended for sandy feel.")]
	[Range(0, 8)] public int caIterations = 4;
	[Tooltip("Neighbour land threshold to survive (out of 8). 4-5 = sandy smooth.")]
	[Range(3, 6)] public int caBirthThreshold = 5;
	[Range(3, 6)] public int caSurvivalThreshold = 4;

	// -------------------------------------------------------
	// LAND MASK
	// -------------------------------------------------------
	[Header("Land Mask")]
	[Range(0.2f, 0.8f)] public float landThreshold = 0.50f;
	[Min(0)] public int borderWater = 20;
	[Min(0)] public int centerNoIslandRadius = 20;

	// -------------------------------------------------------
	// CLEANUP
	// -------------------------------------------------------
	[Header("Cleanup")]
	[Min(1)] public int minIslandTiles = 16;
	[Min(1)] public int minLakeTiles = 8;
	[Range(0, 3)] public int morphologyClosingIterations = 0;

	// -------------------------------------------------------
	// COAST BANDS
	// -------------------------------------------------------
	[Header("Coast Bands")]
	[Min(0)] public int beachWidthMin = 2;
	[Min(0)] public int beachWidthMax = 5;
	[Min(0)] public int grassInset = 1;

	// -------------------------------------------------------
	// OCEAN DEPTH BANDS
	// -------------------------------------------------------
	[Header("Ocean Depth (Natural)")]
	[Range(0.01f, 1f)] public float naturalShallowCut = 0.28f;
	[Range(0.01f, 1f)] public float naturalMediumCut = 0.62f;
	[Min(1)] public int shelfDistanceTiles = 10;
	[Range(0f, 1f)] public float shelfStrength = 0.7f;

	[Header("Abyss Patches")]
	public float abyssNoiseScale = 18f;
	[Range(0f, 1f)] public float abyssThreshold = 0.72f;

	// -------------------------------------------------------
	// DECORATORS
	// -------------------------------------------------------
	[Header("Seaweed")]
	public float seaweedScale = 20f;
	[Range(0f, 1f)] public float seaweedThreshold = 0.62f;
	[Min(0)] public int seaweedMinDepth = 2;
	public float seaweedJitterScale = 3f;
	[Range(0f, 0.3f)] public float seaweedEdgeJitter = 0.15f;

	// -------------------------------------------------------
	// CENTER DOCK
	// -------------------------------------------------------
	[Header("Center Dock")]
	[Min(1)] public int dockWidth = 2;
	[Min(1)] public int dockHeight = 2;
	public TileBase dockTile;
	public TileBase pillarTile;

	// -------------------------------------------------------
	// GLOBAL TILES  (ocean — shared across biomes)
	// -------------------------------------------------------
	[Header("Global Ocean Tiles")]
	public TileBase waterTile;
	public TileBase oceanFloorShallowTile;
	public TileBase oceanFloorMediumTile;
	public TileBase oceanFloorDeepTile;
	public TileBase oceanFloorAbyssTile;
	public TileBase seaweedTile;

	// -------------------------------------------------------
	// BIOME DEFINITIONS
	// -------------------------------------------------------
	[Header("Biome Definitions")]
	public BiomeDefinitionV3[] biomeDefinitions;

	/// <summary>Safely look up a biome definition. Falls back to index 0 if missing.</summary>
	public BiomeDefinitionV3 GetBiome(BiomeType type)
	{
		if (biomeDefinitions == null || biomeDefinitions.Length == 0) return null;
		int idx = (int)type;
		return idx < biomeDefinitions.Length ? biomeDefinitions[idx] : biomeDefinitions[0];
	}

	// -------------------------------------------------------
	// WATER VISUAL
	// -------------------------------------------------------
	[Header("Water Visual")]
	[Min(0)] public int waterOverlapLandInland = 0;
}