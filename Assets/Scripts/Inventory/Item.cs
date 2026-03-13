using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "Scriptable object/Item")]
public class Item : ScriptableObject
{
	[Header("Only gameplay")]
	public TileBase tile;
	public ItemType type;
	public ItemCategory category;
	public ActionType actionType;
	public PlacementCondition placementCondition;
	public Vector2Int range = new Vector2Int(5, 4);

	[Header("Only UI")]
	public bool stackable = true;

	[Header("Tooltip / Info")]
	public Sprite image;

	[Header("Prefab")]
	public GameObject prefab;
}

public enum ItemCategory
{
	Tool,
	Food,
	Drink,
	Material,
	Building,
}

public enum ItemType
{
	Tool,
	Consumable,
	BuildableObject,
	Material,
	PlaceableObject,
	PlaceableObjectWalkableOver,
}

public enum ActionType
{
	Throw,
	Pickup,
	Mine,
	Consume,
	Place
}

public enum PlacementCondition
{
	Anywhere,
	WaterOnly,
	FloorOnly
}