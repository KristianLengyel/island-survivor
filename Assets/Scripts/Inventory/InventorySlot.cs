using UnityEngine;
using UnityEngine.UIElements;

public class InventorySlot
{
	public InventoryItem CurrentItem { get; private set; }
	public bool IsEmpty => CurrentItem == null;
	public bool IsChestSlot { get; set; }
	public VisualElement Element { get; private set; }

	private VisualElement iconElement;
	private Label countLabel;
	private bool isSelected;

	public void Initialize(VisualElement slotElement)
	{
		Element = slotElement;
		iconElement = slotElement.Q<VisualElement>(null, "inv-slot-icon");
		countLabel = slotElement.Q<Label>(null, "inv-slot-count");
		if (isSelected)
			Element.AddToClassList("inv-slot--selected");
		RefreshVisual();
	}

	public void Uninitialize()
	{
		Element = null;
		iconElement = null;
		countLabel = null;
		IsChestSlot = false;
	}

	public void SetItem(InventoryItem item)
	{
		if (CurrentItem != null)
			CurrentItem.slot = null;

		CurrentItem = item;

		if (item != null)
			item.slot = this;

		RefreshVisual();
	}

	public void ClearItem()
	{
		if (CurrentItem != null)
			CurrentItem.slot = null;

		CurrentItem = null;
		RefreshVisual();
	}

	public void RefreshVisual()
	{
		if (Element == null) return;

		if (CurrentItem == null || CurrentItem.item == null)
		{
			if (iconElement != null)
				iconElement.style.backgroundImage = StyleKeyword.None;
			if (countLabel != null)
			{
				countLabel.text = "";
				countLabel.style.display = DisplayStyle.None;
			}
			return;
		}

		Sprite sprite = CurrentItem.GetSprite();
		if (iconElement != null && sprite != null)
			iconElement.style.backgroundImage = new StyleBackground(sprite);

		if (countLabel != null)
		{
			if (CurrentItem.count > 1)
			{
				countLabel.text = CurrentItem.count.ToString();
				countLabel.style.display = DisplayStyle.Flex;
			}
			else
			{
				countLabel.text = "";
				countLabel.style.display = DisplayStyle.None;
			}
		}
	}

	public void Select()
	{
		isSelected = true;
		Element?.AddToClassList("inv-slot--selected");
	}

	public void Deselect()
	{
		isSelected = false;
		Element?.RemoveFromClassList("inv-slot--selected");
	}
}
