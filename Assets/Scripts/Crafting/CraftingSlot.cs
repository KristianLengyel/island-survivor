using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CraftingSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
	[Header("UI Elements")]
	[SerializeField] private Image slotImage;
	[SerializeField] private Image itemIcon;
	[SerializeField] private Button craftButton;

	[Header("Sprites")]
	[SerializeField] private Sprite normalSprite;
	[SerializeField] private Sprite highlightedSprite;
	[SerializeField] private Sprite selectedSprite;

	private CraftingRecipe recipe;
	private CraftingMenu craftingMenu;
	private bool isSelected;

	public void Setup(CraftingRecipe craftingRecipe, CraftingMenu menu)
	{
		recipe = craftingRecipe;
		craftingMenu = menu;

		if (recipe != null && recipe.result != null)
		{
			itemIcon.enabled = true;
			itemIcon.sprite = recipe.result.image;
		}
		else
		{
			itemIcon.enabled = false;
			itemIcon.sprite = null;
		}

		isSelected = false;
		slotImage.sprite = normalSprite;
		craftButton.interactable = recipe != null;
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		if (!isSelected)
			slotImage.sprite = highlightedSprite;
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		if (!isSelected)
			slotImage.sprite = normalSprite;
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		if (eventData.button != PointerEventData.InputButton.Left)
			return;

		if (craftingMenu == null || recipe == null)
			return;

		craftingMenu.OnSlotClicked(this, recipe);
	}

	public void Select()
	{
		isSelected = true;
		slotImage.sprite = selectedSprite;
	}

	public void Deselect()
	{
		isSelected = false;
		slotImage.sprite = normalSprite;
	}
}
