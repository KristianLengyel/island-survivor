using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RecipeDetailsUI : MonoBehaviour
{
	[SerializeField] private Image resultIcon;
	[SerializeField] private TMP_Text resultName;
	[SerializeField] private Transform resourcesContainer;
	[SerializeField] private Button craftButton;
	[SerializeField] private GameObject resourceEntryPrefab;

	private CraftingRecipe recipe;
	private CraftingManager craftingManager;
	private bool crafting;

	private void OnDisable()
	{
		craftButton.onClick.RemoveListener(CraftItem);
		crafting = false;
	}

	public void Setup(CraftingRecipe recipe, CraftingManager manager)
	{
		craftButton.onClick.RemoveListener(CraftItem);

		this.recipe = recipe;
		craftingManager = manager;

		resultIcon.sprite = recipe.result.image;
		resultName.text = recipe.result.name;

		for (int i = resourcesContainer.childCount - 1; i >= 0; i--)
			Destroy(resourcesContainer.GetChild(i).gameObject);

		foreach (var req in recipe.requirements)
		{
			foreach (var resource in req.resources)
			{
				var entryObj = Instantiate(resourceEntryPrefab, resourcesContainer);
				var entry = entryObj.GetComponent<ResourceEntryUI>();
				if (entry != null)
					entry.Setup(resource, req.amount, craftingManager.inventoryManager);
			}
		}

		craftButton.onClick.AddListener(CraftItem);
		UpdateButtonInteractable();
	}

	private void CraftItem()
	{
		if (crafting) return;
		if (recipe == null || craftingManager == null) return;

		crafting = true;

		craftButton.interactable = false;

		craftingManager.Craft(recipe);

		UpdateButtonInteractable();

		crafting = false;
	}

	public void UpdateButtonInteractable()
	{
		if (recipe == null || craftingManager == null)
		{
			craftButton.interactable = false;
			return;
		}

		craftButton.interactable = craftingManager.CanCraft(recipe);
	}
}
