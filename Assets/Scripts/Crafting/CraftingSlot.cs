using UnityEngine.UIElements;

public class CraftingSlot
{
	public CraftingRecipe Recipe { get; private set; }
	public VisualElement Element { get; private set; }

	public CraftingSlot(VisualElement element, CraftingRecipe recipe)
	{
		Element = element;
		Recipe = recipe;
	}

	public void Select()
	{
		Element?.AddToClassList("craft-slot--selected");
	}

	public void Deselect()
	{
		Element?.RemoveFromClassList("craft-slot--selected");
	}
}
