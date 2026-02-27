using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class BuildingManager : MonoBehaviour
{
	[SerializeField] private List<TileResourceRequirement> tileResourceRequirements;

	private Dictionary<TileBase, TileResourceRequirement> requirementsDict;
	private TileBase selectedTile;

	private void Awake()
	{
		requirementsDict = new Dictionary<TileBase, TileResourceRequirement>();
		foreach (var req in tileResourceRequirements)
		{
			if (req.tile != null && !requirementsDict.ContainsKey(req.tile))
			{
				requirementsDict.Add(req.tile, req);
			}
		}
	}

	public void SetSelectedTile(TileBase tile)
	{
		selectedTile = tile;
	}

	public TileBase GetSelectedTile()
	{
		return selectedTile;
	}

	public bool HasEnoughResources(TileBase tile)
	{
		TileResourceRequirement req = GetRequirement(tile);
		if (req == null) return true;

		var inv = GameManager.Instance.InventoryManager;

		foreach (var resource in req.resourceRequirements)
		{
			if (inv.GetItemCount(resource.resource.name) < resource.amount)
			{
				return false;
			}
		}
		return true;
	}

	public void UseResources(TileBase tile)
	{
		TileResourceRequirement req = GetRequirement(tile);
		if (req != null)
		{
			GameManager.Instance.InventoryManager.UseResources(req.resourceRequirements);
		}
	}

	public List<ResourceRequirement> GetResourcesReturnedOnDestroy(TileBase tile)
	{
		TileResourceRequirement req = GetRequirement(tile);
		return req?.resourcesReturnedOnDestroy;
	}

	public TileBase GetPillarTileForFloor(TileBase tile)
	{
		TileResourceRequirement req = GetRequirement(tile);
		return req?.pillarTile;
	}

	private TileResourceRequirement GetRequirement(TileBase tile)
	{
		if (tile == null) return null;
		requirementsDict.TryGetValue(tile, out var req);
		return req;
	}
}
