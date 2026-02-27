using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Tooltip : MonoBehaviour
{
	public static Tooltip Instance;

	[Header("Tooltip UI Elements")]
	public GameObject tooltipPanel;
	public TMP_Text nameText;
	public TMP_Text typeText;

	[Header("Behavior")]
	public float margin = 10f;
	public float fadeDuration = 0.08f;
	public bool useFade = true;

	RectTransform tooltipRect;
	RectTransform canvasRect;
	Canvas canvas;
	CanvasGroup canvasGroup;
	Coroutine fadeRoutine;

	void Awake()
	{
		if (Instance == null) Instance = this;
		else
		{
			Destroy(gameObject);
			return;
		}

		tooltipRect = tooltipPanel.GetComponent<RectTransform>();
		canvas = tooltipPanel.GetComponentInParent<Canvas>();
		canvasRect = canvas.GetComponent<RectTransform>();

		var panelImage = tooltipPanel.GetComponent<Image>();
		if (panelImage) panelImage.raycastTarget = false;

		canvasGroup = tooltipPanel.GetComponent<CanvasGroup>();
		if (!canvasGroup) canvasGroup = tooltipPanel.AddComponent<CanvasGroup>();
		canvasGroup.blocksRaycasts = false;
		canvasGroup.interactable = false;

		tooltipPanel.SetActive(false);
	}

	public void ShowTooltip(string itemName, string itemType, RectTransform slotRect)
	{
		if (!tooltipPanel) return;

		tooltipPanel.SetActive(true);

		nameText.text = itemName;
		typeText.text = itemType;

		LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipRect);

		PositionNearSlot(slotRect);

		if (useFade) StartFade(1f);
		else canvasGroup.alpha = 1f;
	}

	void PositionNearSlot(RectTransform slotRect)
	{
		var cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

		Vector2 slotScreen = RectTransformUtility.WorldToScreenPoint(cam, slotRect.position);

		float scale = canvas.scaleFactor;

		float tooltipW = tooltipRect.rect.width * scale;
		float tooltipH = tooltipRect.rect.height * scale;

		float slotH = slotRect.rect.height * scale;
		float slotW = slotRect.rect.width * scale;

		bool canShowAbove = (Screen.height - slotScreen.y) >= (tooltipH + slotH * 0.5f + margin);
		bool canShowBelow = slotScreen.y >= (tooltipH + slotH * 0.5f + margin);

		Vector2 target = slotScreen;

		if (canShowAbove)
			target.y += slotH * 0.5f + tooltipH * 0.5f + margin;
		else if (canShowBelow)
			target.y -= slotH * 0.5f + tooltipH * 0.5f + margin;
		else
			target.y = Mathf.Clamp(target.y, tooltipH * 0.5f + margin, Screen.height - tooltipH * 0.5f - margin);

		RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, target, cam, out Vector2 localPoint);

		Vector2 halfLocal = tooltipRect.rect.size * 0.5f;
		Rect bounds = canvasRect.rect;

		float minX = bounds.xMin + halfLocal.x;
		float maxX = bounds.xMax - halfLocal.x;
		float minY = bounds.yMin + halfLocal.y;
		float maxY = bounds.yMax - halfLocal.y;

		localPoint.x = Mathf.Clamp(localPoint.x, minX, maxX);
		localPoint.y = Mathf.Clamp(localPoint.y, minY, maxY);

		tooltipRect.anchoredPosition = localPoint;
	}

	public void HideTooltip()
	{
		if (!tooltipPanel) return;

		if (useFade) StartFade(0f, deactivateOnEnd: true);
		else tooltipPanel.SetActive(false);
	}

	void StartFade(float targetAlpha, bool deactivateOnEnd = false)
	{
		if (!canvasGroup) return;

		if (fadeRoutine != null) StopCoroutine(fadeRoutine);
		fadeRoutine = StartCoroutine(FadeRoutine(targetAlpha, deactivateOnEnd));
	}

	IEnumerator FadeRoutine(float target, bool deactivateOnEnd)
	{
		float start = canvasGroup.alpha;
		float t = 0f;

		if (!tooltipPanel.activeSelf) tooltipPanel.SetActive(true);

		while (t < fadeDuration)
		{
			t += Time.unscaledDeltaTime;
			float a = Mathf.Lerp(start, target, t / Mathf.Max(0.0001f, fadeDuration));
			canvasGroup.alpha = a;
			yield return null;
		}

		canvasGroup.alpha = target;

		if (deactivateOnEnd && Mathf.Approximately(target, 0f))
			tooltipPanel.SetActive(false);

		fadeRoutine = null;
	}

	void OnDisable()
	{
		if (tooltipPanel) tooltipPanel.SetActive(false);
	}
}
