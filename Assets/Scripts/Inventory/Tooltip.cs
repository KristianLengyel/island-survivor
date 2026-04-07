using UnityEngine;
using UnityEngine.UIElements;

public class Tooltip : MonoBehaviour
{
	public static Tooltip Instance;

	private VisualElement tooltipEl;
	private Label nameLabel;
	private Label typeLabel;
	private VisualElement containerRoot;

	private void Awake()
	{
		if (Instance == null) Instance = this;
		else { Destroy(gameObject); return; }
	}

	public void Initialize(VisualElement root)
	{
		containerRoot = root;

		tooltipEl = new VisualElement();
		tooltipEl.AddToClassList("inv-tooltip");
		tooltipEl.pickingMode = PickingMode.Ignore;

		nameLabel = new Label();
		nameLabel.AddToClassList("inv-tooltip-name");
		tooltipEl.Add(nameLabel);

		typeLabel = new Label();
		typeLabel.AddToClassList("inv-tooltip-type");
		tooltipEl.Add(typeLabel);

		root.Add(tooltipEl);
	}

	public void ShowTooltip(string itemName, string itemType, VisualElement slotEl)
	{
		if (tooltipEl == null || slotEl == null) return;

		if (nameLabel != null) nameLabel.text = itemName;
		if (typeLabel != null) typeLabel.text = itemType;

		tooltipEl.style.display = DisplayStyle.Flex;
		tooltipEl.schedule.Execute(() => PositionNearSlot(slotEl)).StartingIn(0);
	}

	private void PositionNearSlot(VisualElement slotEl)
	{
		if (tooltipEl == null || slotEl == null || containerRoot == null) return;

		var rootBounds = containerRoot.worldBound;
		var slotBounds = slotEl.worldBound;

		float x = slotBounds.x - rootBounds.x;
		float y = slotBounds.y - rootBounds.y - tooltipEl.layout.height - 4f;

		if (y < 0f) y = slotBounds.yMax - rootBounds.y + 4f;

		float maxX = rootBounds.width - tooltipEl.layout.width;
		if (maxX > 0f) x = Mathf.Clamp(x, 0f, maxX);

		tooltipEl.style.left = x;
		tooltipEl.style.top = y;
	}

	public void HideTooltip()
	{
		if (tooltipEl == null) return;
		tooltipEl.style.display = DisplayStyle.None;
	}

	private void OnDisable()
	{
		HideTooltip();
	}
}
