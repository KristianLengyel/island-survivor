using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AdminConsoleItemRow : MonoBehaviour
{
	[SerializeField] private Image icon;
	[SerializeField] private TextMeshProUGUI nameText;
	[SerializeField] private Button addButton;

	private Item item;
	private Action<Item> onAdd;

	public void Bind(Item item, Action<Item> onAdd)
	{
		this.item = item;
		this.onAdd = onAdd;

		if (icon) icon.sprite = item != null ? item.image : null;
		if (nameText) nameText.text = item != null ? item.name : "-";

		if (addButton)
		{
			addButton.onClick.RemoveAllListeners();
			addButton.onClick.AddListener(() =>
			{
				if (this.item != null)
					this.onAdd?.Invoke(this.item);
			});
		}
	}
}
