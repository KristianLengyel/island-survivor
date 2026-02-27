using System.Linq;
using UnityEngine;

public class CraftingMenu : MonoBehaviour
{
	[Header("UI Elements")]
	[SerializeField] private Transform craftingSlotsPanel;
	[SerializeField] private GameObject craftingSlotPrefab;

	[SerializeField] private GameObject recipeDetailsPanel;
	[SerializeField] private RecipeDetailsUI recipeDetailsUI;

	[Header("References")]
	[SerializeField] private CraftingManager craftingManager;

	private CraftingRecipe[] allRecipes;
	private CraftingSlot selectedSlot;

	private void Awake()
	{
		if (craftingManager == null)
			craftingManager = GameManager.Instance.GetComponent<CraftingManager>();

		allRecipes = Resources.LoadAll<CraftingRecipe>("CraftingRecipes");
		if (allRecipes != null && allRecipes.Length > 0)
		{
			allRecipes = allRecipes
				.OrderBy(r => r.category)
				.ThenBy(r => r.result.name)
				.ToArray();
		}

		PopulateCraftingSlots();
		ResetUIState();
	}

	private void OnEnable()
	{
		ResetUIState();
	}

	private void OnDisable()
	{
		ResetUIState();
	}

	private void PopulateCraftingSlots()
	{
		foreach (Transform child in craftingSlotsPanel)
			Destroy(child.gameObject);

		if (allRecipes == null) return;

		foreach (var recipe in allRecipes)
		{
			var slotObj = Instantiate(craftingSlotPrefab, craftingSlotsPanel);
			var slot = slotObj.GetComponent<CraftingSlot>();
			slot.Setup(recipe, this);
		}
	}

	public void ShowRecipeDetails(CraftingRecipe recipe)
	{
		recipeDetailsPanel.SetActive(true);
		recipeDetailsUI.Setup(recipe, craftingManager);
	}

	public void HideRecipeDetails()
	{
		recipeDetailsPanel.SetActive(false);

		if (selectedSlot != null)
		{
			selectedSlot.Deselect();
			selectedSlot = null;
		}
	}

	public void ResetUIState()
	{
		HideRecipeDetails();
	}

	public void SelectSlot(CraftingSlot slot)
	{
		if (selectedSlot != null && selectedSlot != slot)
			selectedSlot.Deselect();

		selectedSlot = slot;
	}

	public void OnSlotClicked(CraftingSlot slot, CraftingRecipe recipe)
	{
		SelectSlot(slot);
		slot.Select();
		ShowRecipeDetails(recipe);
	}
}
