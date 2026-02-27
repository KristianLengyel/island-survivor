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

	[Header("Seaweed Generation")]
	public float seaweedScale = 20f;
	public float seaweedThreshold = 0.6f;

	[Header("Grass Generation")]
	public int minIslandSizeForGrass = 20;
	public int grassBorderOffset = 1;

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
