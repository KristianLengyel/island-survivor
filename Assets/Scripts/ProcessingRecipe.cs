using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable object/Processing Recipe")]
public class ProcessingRecipe : ScriptableObject
{
	public enum ProcessorType
	{
		Grill,
		CookingPot,
		Furnace,
		Smelter,
		Recycler,
		Workbench
	}

	public ProcessorType processor;

	public List<IngredientRequirement> inputs;
	public Item output;

	public float processTime = 10f;

	public Sprite inputOverlaySprite;
	public Sprite outputOverlaySprite;
}

[System.Serializable]
public class IngredientRequirement
{
	public Item item;
	public int amount;
}
