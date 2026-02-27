using UnityEngine;

public class CraftingManager : MonoBehaviour
{
	[SerializeField] public InventoryManager inventoryManager;
	private RecipeDetailsUI currentRecipeDetailsUI;

	private void Awake()
	{
		if (inventoryManager == null)
		{
			inventoryManager = GameManager.Instance.InventoryManager;
		}
	}

	public bool CanCraft(CraftingRecipe recipe)
	{
		if (recipe == null || recipe.requirements == null) return false;

		foreach (var requirement in recipe.requirements)
		{
			bool satisfied = false;

			foreach (var resource in requirement.resources)
			{
				if (resource == null) continue;

				if (inventoryManager.GetItemCount(resource.name) >= requirement.amount)
				{
					satisfied = true;
					break;
				}
			}

			if (!satisfied) return false;
		}

		return true;
	}

	public void Craft(CraftingRecipe recipe)
	{
		if (!CanCraft(recipe)) return;

		foreach (var requirement in recipe.requirements)
		{
			string chosenResource = null;

			foreach (var resource in requirement.resources)
			{
				if (resource == null) continue;

				if (inventoryManager.GetItemCount(resource.name) >= requirement.amount)
				{
					chosenResource = resource.name;
					break;
				}
			}

			if (!string.IsNullOrEmpty(chosenResource))
			{
				inventoryManager.RemoveItem(chosenResource, requirement.amount);
			}
		}

		inventoryManager.AddItem(recipe.result);
		AudioManager.instance.PlaySound("CraftItem");

		if (currentRecipeDetailsUI != null)
		{
			currentRecipeDetailsUI.UpdateButtonInteractable();
		}
	}

	public void RegisterRecipeDetailsUI(RecipeDetailsUI ui)
	{
		currentRecipeDetailsUI = ui;
	}
}
