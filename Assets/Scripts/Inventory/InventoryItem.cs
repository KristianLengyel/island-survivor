using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryItem : MonoBehaviour,
	IBeginDragHandler, IDragHandler, IEndDragHandler,
	IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
	[Header("UI")]
	public Image image;
	public TMP_Text countText;

	[HideInInspector] public Item item;
	[HideInInspector] public int count = 1;
	[HideInInspector] public Transform parentAfterDrag;

	private bool isSplitting = false;
	private Canvas dragCanvas;
	private CanvasGroup canvasGroup;
	private Transform originalParent;

	private void Awake()
	{
		GameObject dragCanvasObj = GameObject.Find("DragCanvas");
		if (dragCanvasObj != null)
		{
			dragCanvas = dragCanvasObj.GetComponent<Canvas>();
		}
		canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
	}

	public virtual void InitialiseItem(Item newItem)
	{
		item = newItem;
		image.sprite = item.image;
		RefreshCount();
	}

	public void RefreshCount()
	{
		if (count <= 0)
		{
			Destroy(gameObject);
		}
		else
		{
			countText.text = count.ToString();
			bool textActive = count > 1;
			countText.gameObject.SetActive(textActive);
			countText.raycastTarget = false;
		}
	}

	public void OnBeginDrag(PointerEventData eventData)
	{
		if (canvasGroup == null) return;
		canvasGroup.blocksRaycasts = false;
		originalParent = transform.parent;
		parentAfterDrag = transform.parent;

		if (dragCanvas != null) transform.SetParent(dragCanvas.transform, true);
		else transform.SetParent(transform.root, true);

		isSplitting = (eventData.button == PointerEventData.InputButton.Middle);
		image.raycastTarget = false;
	}

	public void OnDrag(PointerEventData eventData)
	{
		transform.position = eventData.position;
	}

	public void OnEndDrag(PointerEventData eventData)
	{
		if (canvasGroup == null) return;
		canvasGroup.blocksRaycasts = true;

		InventorySlot dropSlot = eventData.pointerCurrentRaycast.gameObject?.GetComponent<InventorySlot>();

		if (isSplitting)
		{
			if (dropSlot != null && dropSlot.transform.childCount == 0)
			{
				SplitStack(dropSlot);
			}
			else
			{
				InventoryManager inventoryManager = GameManager.Instance.InventoryManager;
				InventorySlot emptySlot = inventoryManager.GetEmptySlot();
				if (emptySlot != null)
				{
					SplitStack(emptySlot);
				}
			}

			transform.SetParent(originalParent, true);
			transform.localPosition = Vector3.zero;
			isSplitting = false;
		}
		else
		{
			if (dropSlot != null && dropSlot.transform.childCount == 0)
			{
				transform.SetParent(dropSlot.transform, true);
				transform.localPosition = Vector3.zero;
			}
			else if (dropSlot != null && dropSlot.transform.childCount > 0)
			{
				InventoryItem otherItem = dropSlot.GetComponentInChildren<InventoryItem>();
				if (otherItem != null && otherItem != this)
				{
					otherItem.transform.SetParent(parentAfterDrag, true);
					otherItem.transform.localPosition = Vector3.zero;

					transform.SetParent(dropSlot.transform, true);
					transform.localPosition = Vector3.zero;
				}
			}
			else
			{
				transform.SetParent(parentAfterDrag, true);
				transform.localPosition = Vector3.zero;
			}
		}

		image.raycastTarget = true;
	}

	protected virtual void SplitStack(InventorySlot targetSlot)
	{
		if (count <= 1) return;
		if (targetSlot.transform.childCount > 0) return;

		int halfCount = count / 2;
		InventoryManager inventoryManager = GameManager.Instance.InventoryManager;

		count -= halfCount;
		RefreshCount();

		inventoryManager.SpawnNewItem(item, targetSlot, halfCount);
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		bool inventoryOpen = GameManager.Instance.InventoryManager.IsInventoryOpen();

		bool inChestAndOpen = false;
		Chest chest = GetComponentInParent<Chest>();
		if (chest != null)
		{
			inChestAndOpen = chest.IsOpen();
		}

		if (!inventoryOpen && !inChestAndOpen) return;

		RectTransform slotRect = transform.parent.GetComponent<RectTransform>();
		if (slotRect != null)
		{
			Tooltip.Instance.ShowTooltip(item.name, item.type.ToString(), slotRect);
		}
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		Tooltip.Instance.HideTooltip();
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		if (eventData.button != PointerEventData.InputButton.Left) return;

		bool shiftPressed = GameInput.ShiftHeld;
		if (!shiftPressed) return;

		InventoryManager inventoryManager = GameManager.Instance.InventoryManager;

		bool isInChest = IsInChestSlot();
		Chest activeChest = Chest.CurrentOpenChest;

		if (activeChest != null && activeChest.IsOpen())
		{
			if (isInChest)
			{
				int added = inventoryManager.AddItemPartial(item, count, showNotification: false);
				if (added <= 0) return;

				if (added >= count)
				{
					Destroy(gameObject);
					Tooltip.Instance.HideTooltip();
				}
				else
				{
					count -= added;
					RefreshCount();
				}
			}
			else if (inventoryManager.IsInventoryOpen())
			{
				int added = activeChest.AddItemPartial(item, count);
				if (added <= 0) return;

				if (added >= count)
				{
					Destroy(gameObject);
					Tooltip.Instance.HideTooltip();
				}
				else
				{
					count -= added;
					RefreshCount();
				}
			}

			return;
		}

		if (!isInChest && inventoryManager.IsInventoryOpen())
		{
			inventoryManager.ShiftMoveBetweenToolbarAndInventory(this);
		}
	}

	private bool IsInChestSlot()
	{
		return GetComponentInParent<Chest>() != null;
	}
}
