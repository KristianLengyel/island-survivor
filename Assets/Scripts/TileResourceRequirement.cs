using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "Scriptable object/Tile Resource Requirement")]
public class TileResourceRequirement : ScriptableObject
{
	public TileBase tile;
	public TileBase pillarTile;
	public List<ResourceRequirement> resourceRequirements;
	public List<ResourceRequirement> resourcesReturnedOnDestroy;
}

[System.Serializable]
public class ResourceRequirement
{
	public Item resource;
	public int amount;
}