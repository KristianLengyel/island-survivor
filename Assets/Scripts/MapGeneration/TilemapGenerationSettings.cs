using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "TilemapGenerationSettings", menuName = "IslandOdyssey/TilemapGenerationSettings")]
public class TilemapGenerationSettings : ScriptableObject
{
	[Header("Map Settings")]
	public int mapSize = 256;
	public float scale = 17f;
	public float offsetX = 100f;
	public float offsetY = 100f;

	[Header("Island Generation")]
	public float islandThreshold = 0.5f;
	public float safeRadius = 50f;
	public int borderSize = 20;
	public int minIslandSize = 10;
	public float maxOceanDepth = 6f;
	[Tooltip("Extra padding of water tiles placed around the map border")]
	public int mapWaterPadding = 1;
	[Tooltip("Expands the sand border outward by this many tiles, covering the shallow ocean floor halo around the island edge")]
	[Min(0)]
	public int landExpansion = 1;

	[Header("Island Cleanup")]
	[Tooltip("Minimum tile count for a lake to be kept (smaller lakes are filled in)")]
	public int minLakeSize = 6;
	[Tooltip("Minimum water neighbors a water tile must have to survive. Tiles below this are converted to land.")]
	[Range(1, 4)]
	public int minWaterNeighbors = 2;

	[Header("Ocean Floor")]
	[Tooltip("Distance from land (in tiles) considered shallow ocean floor")]
	public float shallowOceanThreshold = 2f;
	[Tooltip("Distance from land (in tiles) considered medium ocean floor")]
	public float mediumOceanThreshold = 4f;
	[Tooltip("Distance from land (in tiles) considered deep ocean floor")]
	public float deepOceanThreshold = 6f;
	[Tooltip("Number of blur smoothing passes on the ocean floor distance map")]
	public int oceanFloorBlurIterations = 3;

	[Header("Seaweed Generation")]
	public float seaweedScale = 20f;
	public float seaweedThreshold = 0.6f;
	[Tooltip("Only place seaweed where heightmap is below this value (shallow water)")]
	public float seaweedMaxHeight = 0.4f;

	[Header("Grass Generation")]
	public int minIslandSizeForGrass = 20;
	public int grassBorderOffset = 1;
	[Tooltip("Fraction (0-1) of the island bounding box used for grass coverage")]
	[Range(0.1f, 1f)]
	public float grassCoverageFraction = 0.667f;

	[Header("Central Island / Dock")]
	[Tooltip("Whether to place the central wooden dock structure")]
	public bool placeCentralDock = true;
	[Tooltip("Size of the dock in tiles (e.g. 2 = 2x2)")]
	[Min(1)]
	public int dockSize = 2;

	[Header("Noise Settings")]
	public int numOctaves = 4;
	public float persistence = 0.5f;
	public float lacunarity = 2f;

	[Header("Seed Settings")]
	public string seedInput;
	public bool useRandomSeed;

	[Header("Map Colors")]
	public Color sandColor = Color.yellow;
	public Color waterColor = Color.blue;

	[Header("Tiles")]
	public TileBase waterTile;
	public TileBase sandTile;
	public TileBase grassTile;
	public TileBase seaweedTile;
	public TileBase woodenFloorTile;
	public TileBase woodenPillarTile;
}