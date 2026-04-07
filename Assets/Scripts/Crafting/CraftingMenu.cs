using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class CraftingMenu : MonoBehaviour
{
	[SerializeField] private CraftingManager craftingManager;
	[SerializeField] private Sprite[] categorySprites;

	private VisualElement craftPanelWrapper;
	private VisualElement craftDetailsWrapper;
	private VisualElement craftSlotContainer;
	private VisualElement craftCategoryBar;
	private VisualElement previewInner;
	private Label resultNameLabel;
	private VisualElement ingredientsContainer;
	private Button craftButton;

	private CraftingRecipe[] allRecipes;
	private CraftingSlot[] slots;
	private readonly Dictionary<int, VisualElement> categoryButtons = new Dictionary<int, VisualElement>();

	private CraftingSlot selectedSlot;
	private CraftingRecipe selectedRecipe;
	private int activeCategory = -1;

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
	}

	public void Initialize(VisualElement root)
	{
		craftPanelWrapper = root.Q<VisualElement>("craft-panel-wrapper");
		craftDetailsWrapper = root.Q<VisualElement>("craft-details-wrapper");
		craftSlotContainer = root.Q<VisualElement>("craft-slot-container");
		craftCategoryBar = root.Q<VisualElement>("craft-category-bar");
		previewInner = root.Q<VisualElement>("craft-preview-inner");
		resultNameLabel = root.Q<Label>("craft-result-name");
		ingredientsContainer = root.Q<VisualElement>("craft-ingredients-container");
		craftButton = root.Q<Button>("craft-button");

		if (craftButton != null)
			craftButton.clicked += OnCraftClicked;

		BuildCategoryBar();
		PopulateSlots();
		HideRecipeDetails();
	}

	private void BuildCategoryBar()
	{
		if (craftCategoryBar == null || allRecipes == null) return;

		craftCategoryBar.Clear();
		categoryButtons.Clear();

		var allBtn = new VisualElement();
		allBtn.AddToClassList("craft-category-btn");
		allBtn.AddToClassList("craft-category-btn--selected");
		var allLabel = new Label("all");
		allLabel.AddToClassList("craft-category-btn-label");
		allBtn.Add(allLabel);
		allBtn.RegisterCallback<PointerDownEvent>(evt => { if (evt.button == 0) SetActiveCategory(-1); });
		craftCategoryBar.Add(allBtn);
		categoryButtons[-1] = allBtn;

		var usedCategories = allRecipes.Select(r => (int)r.category).Distinct().OrderBy(c => c);
		foreach (int cat in usedCategories)
		{
			var btn = new VisualElement();
			btn.AddToClassList("craft-category-btn");

			Sprite sprite = (categorySprites != null && cat < categorySprites.Length) ? categorySprites[cat] : null;
			if (sprite != null)
			{
				var icon = new VisualElement();
				icon.AddToClassList("craft-category-btn-icon");
				icon.style.backgroundImage = new StyleBackground(sprite);
				btn.Add(icon);
			}
			else
			{
				string enumName = ((CraftingRecipe.RecipeCategory)cat).ToString();
				var lbl = new Label(enumName.Length > 3 ? enumName.Substring(0, 3) : enumName);
				lbl.AddToClassList("craft-category-btn-label");
				btn.Add(lbl);
			}

			int capturedCat = cat;
			btn.RegisterCallback<PointerDownEvent>(evt => { if (evt.button == 0) SetActiveCategory(capturedCat); });
			craftCategoryBar.Add(btn);
			categoryButtons[cat] = btn;
		}
	}

	private void SetActiveCategory(int category)
	{
		if (categoryButtons.TryGetValue(activeCategory, out var prevBtn))
			prevBtn.RemoveFromClassList("craft-category-btn--selected");

		activeCategory = category;

		if (categoryButtons.TryGetValue(activeCategory, out var newBtn))
			newBtn.AddToClassList("craft-category-btn--selected");

		HideRecipeDetails();
		PopulateSlots();
	}

	private void PopulateSlots()
	{
		if (craftSlotContainer == null || allRecipes == null) return;

		craftSlotContainer.Clear();

		CraftingRecipe[] filtered = activeCategory == -1
			? allRecipes
			: allRecipes.Where(r => (int)r.category == activeCategory).ToArray();

		slots = new CraftingSlot[filtered.Length];

		int slotsPerRow = 7;
		VisualElement craftRow = null;

		for (int i = 0; i < filtered.Length; i++)
		{
			if (i % slotsPerRow == 0)
			{
				craftRow = new VisualElement();
				craftRow.style.flexDirection = FlexDirection.Row;
				craftSlotContainer.Add(craftRow);
			}

			var el = new VisualElement();
			el.AddToClassList("craft-slot");

			var icon = new VisualElement();
			icon.AddToClassList("craft-slot-icon");
			if (filtered[i]?.result?.image != null)
				icon.style.backgroundImage = new StyleBackground(filtered[i].result.image);
			el.Add(icon);

			if (i % slotsPerRow == slotsPerRow - 1) el.style.marginRight = 0;

			craftRow?.Add(el);
			slots[i] = new CraftingSlot(el, filtered[i]);

			var capturedSlot = slots[i];
			el.RegisterCallback<PointerDownEvent>(evt =>
			{
				if (evt.button != 0) return;
				OnSlotClicked(capturedSlot);
			});
		}

		if (craftSlotContainer.childCount > 0)
		{
			var lastRow = craftSlotContainer.ElementAt(craftSlotContainer.childCount - 1);
			if (filtered.Length % slotsPerRow != 0)
				lastRow.ElementAt(lastRow.childCount - 1).style.marginRight = 0;
			foreach (var child in lastRow.Children())
				child.style.marginBottom = 0;
		}
	}

	private void OnSlotClicked(CraftingSlot slot)
	{
		if (slot == null) return;
		if (selectedSlot != null && selectedSlot != slot) selectedSlot.Deselect();
		selectedSlot = slot;
		slot.Select();
		ShowRecipeDetails(slot.Recipe);
	}

	private void ShowRecipeDetails(CraftingRecipe recipe)
	{
		if (recipe == null) { HideRecipeDetails(); return; }

		selectedRecipe = recipe;

		if (previewInner != null && recipe.result?.image != null)
			previewInner.style.backgroundImage = new StyleBackground(recipe.result.image);

		if (resultNameLabel != null)
			resultNameLabel.text = recipe.result != null ? recipe.result.name : "";

		BuildIngredients(recipe);
		UpdateCraftButton();

		if (craftDetailsWrapper != null)
			craftDetailsWrapper.style.display = DisplayStyle.Flex;
	}

	public void HideRecipeDetails()
	{
		selectedRecipe = null;

		if (selectedSlot != null)
		{
			selectedSlot.Deselect();
			selectedSlot = null;
		}

		if (craftDetailsWrapper != null)
			craftDetailsWrapper.style.display = DisplayStyle.None;
	}

	private void BuildIngredients(CraftingRecipe recipe)
	{
		if (ingredientsContainer == null) return;
		ingredientsContainer.Clear();
		if (recipe?.requirements == null) return;

		foreach (var req in recipe.requirements)
		{
			Item firstItem = null;
			if (req.resources != null)
				foreach (var resource in req.resources)
					if (resource != null) { firstItem = resource; break; }

			if (firstItem == null) continue;

			var row = new VisualElement();
			row.AddToClassList("craft-ingredient-row");

			var icon = new VisualElement();
			icon.AddToClassList("craft-ingredient-icon");
			if (firstItem.image != null)
				icon.style.backgroundImage = new StyleBackground(firstItem.image);
			row.Add(icon);

			var nameLabel = new Label(firstItem.name);
			nameLabel.AddToClassList("craft-ingredient-name");
			row.Add(nameLabel);

			var countLabel = new Label();
			countLabel.AddToClassList("craft-ingredient-count");
			int have = craftingManager?.inventoryManager?.GetItemCount(firstItem.name) ?? 0;
			bool affordable = have >= req.amount;
			countLabel.text = $"{have}/{req.amount}";
			countLabel.AddToClassList(affordable ? "craft-ingredient-count--affordable" : "craft-ingredient-count--unaffordable");
			row.Add(countLabel);

			ingredientsContainer.Add(row);
		}
	}

	public void UpdateCraftButton()
	{
		if (craftButton == null) return;
		bool canCraft = selectedRecipe != null && craftingManager != null && craftingManager.CanCraft(selectedRecipe);
		craftButton.SetEnabled(canCraft);
		if (canCraft)
			craftButton.RemoveFromClassList("craft-button--unavailable");
		else
			craftButton.AddToClassList("craft-button--unavailable");
	}

	private void OnCraftClicked()
	{
		if (selectedRecipe == null || craftingManager == null) return;
		craftingManager.Craft(selectedRecipe);
		if (selectedRecipe != null)
		{
			BuildIngredients(selectedRecipe);
			UpdateCraftButton();
		}
	}

	public void ResetUIState()
	{
		HideRecipeDetails();
	}

	public void SetVisible(bool visible)
	{
		if (craftPanelWrapper != null)
			craftPanelWrapper.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
		if (!visible && craftDetailsWrapper != null)
			craftDetailsWrapper.style.display = DisplayStyle.None;
	}
}
