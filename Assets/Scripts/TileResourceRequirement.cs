using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public enum TileCategory { Floor, Wall, Door, Pipe }

[CreateAssetMenu(menuName = "Scriptable object/Tile Resource Requirement")]
public class TileResourceRequirement : ScriptableObject
{
	public string tileName;
	public TileBase tile;
	public TileBase altTile;
	public TileBase pillarTile;
	public TileBase pillarTopBottomTile;
	public TileBase pillarTopTile;
	public TileBase pillarMiddleTile;
	public TileBase pillarEndTile;
	public Sprite menuIcon;
	public TileCategory tileCategory = TileCategory.Floor;
	public PlacementCondition placementCondition = PlacementCondition.Anywhere;
	public List<ResourceRequirement> resourceRequirements;
	public List<ResourceRequirement> resourcesReturnedOnDestroy;
}

[System.Serializable]
public class ResourceRequirement
{
	public Item resource;
	public int amount;
}
