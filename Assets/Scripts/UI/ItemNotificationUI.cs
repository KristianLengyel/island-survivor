using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemNotificationUI : MonoBehaviour
{
	[SerializeField] private Image icon;
	[SerializeField] private TextMeshProUGUI nameText;
	[SerializeField] private TextMeshProUGUI countText;

	[SerializeField] private float fadeIn = 0.08f;
	[SerializeField] private float fadeOut = 0.12f;
	[SerializeField] private float slideDistance = 18f;

	private CanvasGroup cg;
	private RectTransform rt;
	private Coroutine anim;
	private Vector2 basePos;

	private void Awake()
	{
		cg = GetComponent<CanvasGroup>();
		if (!cg) cg = gameObject.AddComponent<CanvasGroup>();
		cg.interactable = false;
		cg.blocksRaycasts = false;

		rt = GetComponent<RectTransform>();
		basePos = rt.anchoredPosition;

		cg.alpha = 0f;
		rt.anchoredPosition = basePos + new Vector2(slideDistance, 0f);
	}

	public void Set(Sprite sprite, string itemName, int count)
	{
		if (icon) icon.sprite = sprite;
		if (nameText) nameText.text = itemName;
		if (countText) countText.text = $"+{Mathf.Max(0, count)}";

		PlayIn();
	}

	public void PlayIn()
	{
		if (anim != null) StopCoroutine(anim);
		anim = StartCoroutine(FadeSlide(1f, basePos, fadeIn));
	}

	public void PlayOutAndDestroy()
	{
		if (anim != null) StopCoroutine(anim);
		anim = StartCoroutine(OutThenDestroy());
	}

	private IEnumerator OutThenDestroy()
	{
		yield return FadeSlide(0f, basePos + new Vector2(slideDistance, 0f), fadeOut);
		Destroy(gameObject);
	}

	private IEnumerator FadeSlide(float targetA, Vector2 targetPos, float dur)
	{
		float startA = cg.alpha;
		Vector2 startPos = rt.anchoredPosition;

		float t = 0f;
		while (t < dur)
		{
			t += Time.unscaledDeltaTime;
			float k = t / Mathf.Max(0.0001f, dur);
			cg.alpha = Mathf.Lerp(startA, targetA, k);
			rt.anchoredPosition = Vector2.Lerp(startPos, targetPos, k);
			yield return null;
		}

		cg.alpha = targetA;
		rt.anchoredPosition = targetPos;
		anim = null;
	}
}
