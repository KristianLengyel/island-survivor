using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public enum TileCategory { Floor, Wall, Door }

[CreateAssetMenu(menuName = "Scriptable object/Tile Resource Requirement")]
public class TileResourceRequirement : ScriptableObject
{
	public string tileName;
	public TileBase tile;
	public TileBase altTile;
	public TileBase pillarTile;
	public Sprite menuIcon;
	public TileCategory tileCategory = TileCategory.Floor;
	public List<ResourceRequirement> resourceRequirements;
	public List<ResourceRequirement> resourcesReturnedOnDestroy;
}

[System.Serializable]
public class ResourceRequirement
{
	public Item resource;
	public int amount;
}
