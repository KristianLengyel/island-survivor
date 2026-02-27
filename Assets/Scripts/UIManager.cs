using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
	public GameObject notificationPrefab;
	public Transform notificationParent;

	public float notificationDuration = 3f;
	public float notificationMergeWindow = 0.5f;

	[Header("Player Stats UI")]
	public TextMeshProUGUI thirstText;
	public TextMeshProUGUI hungerText;

	private readonly Dictionary<string, NotificationData> activeNotifications = new Dictionary<string, NotificationData>(64);
	private PlayerStats playerStats;

	private void Start()
	{
		playerStats = FindAnyObjectByType<PlayerStats>();
		if (playerStats == null)
		{
			Debug.LogError("PlayerStats not found in scene!");
		}
	}

	private void Update()
	{
		if (playerStats != null && thirstText != null)
			thirstText.text = $"Thirst: {(int)(playerStats.GetThirstPercentage() * 100)}%";

		if (playerStats != null && hungerText != null)
			hungerText.text = $"Hunger: {(int)(playerStats.GetHungerPercentage() * 100)}%";
	}

	public void ShowItemNotification(Sprite itemSprite, string itemName, int itemCount)
	{
		if (notificationPrefab == null || notificationParent == null) return;
		if (string.IsNullOrEmpty(itemName) || itemCount <= 0) return;

		if (activeNotifications.TryGetValue(itemName, out var existing))
		{
			existing.Count += itemCount;

			if (existing.UI != null)
				existing.UI.Set(existing.IconSprite, existing.ItemName, existing.Count);

			if (existing.MergeCoroutine != null) StopCoroutine(existing.MergeCoroutine);
			existing.MergeCoroutine = StartCoroutine(MergeWindowCooldown(itemName));

			if (existing.RemoveCoroutine != null) StopCoroutine(existing.RemoveCoroutine);
			existing.RemoveCoroutine = StartCoroutine(RemoveNotificationAfterDelay(itemName, notificationDuration));

			activeNotifications[itemName] = existing;
			return;
		}

		GameObject instance = Instantiate(notificationPrefab, notificationParent);
		instance.transform.localScale = Vector3.one;

		var ui = instance.GetComponent<ItemNotificationUI>();
		if (ui == null)
		{
			Destroy(instance);
			return;
		}

		var data = new NotificationData
		{
			Notification = instance,
			UI = ui,
			ItemName = itemName,
			IconSprite = itemSprite,
			Count = itemCount
		};

		ui.Set(itemSprite, itemName, itemCount);

		data.MergeCoroutine = StartCoroutine(MergeWindowCooldown(itemName));
		data.RemoveCoroutine = StartCoroutine(RemoveNotificationAfterDelay(itemName, notificationDuration));

		activeNotifications[itemName] = data;
	}

	private IEnumerator MergeWindowCooldown(string key)
	{
		yield return new WaitForSeconds(notificationMergeWindow);

		if (activeNotifications.TryGetValue(key, out var data))
		{
			data.MergeCoroutine = null;
			activeNotifications[key] = data;
		}
	}

	private IEnumerator RemoveNotificationAfterDelay(string key, float delay)
	{
		yield return new WaitForSeconds(delay);

		if (!activeNotifications.TryGetValue(key, out var data)) yield break;

		if (data.UI != null)
			data.UI.PlayOutAndDestroy();
		else if (data.Notification != null)
			Destroy(data.Notification);

		activeNotifications.Remove(key);
	}

	private struct NotificationData
	{
		public GameObject Notification;
		public ItemNotificationUI UI;
		public string ItemName;
		public Sprite IconSprite;
		public int Count;

		public Coroutine RemoveCoroutine;
		public Coroutine MergeCoroutine;
	}
}
