using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable object/Crafting Recipe")]
public class CraftingRecipe : ScriptableObject
{
	public enum RecipeCategory
	{
		FoodAndWater,
		Materials,
		Resources,
		Stations,
		Tools,
		Equipment,
		Decorations
	}

	public RecipeCategory category;
	public Item result;
	public List<CraftingResourceRequirement> requirements;
}

[System.Serializable]
public class CraftingResourceRequirement
{
	public List<Item> resources;
	public int amount;
}