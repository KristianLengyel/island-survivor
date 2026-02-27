using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class MenuCoordinator : MonoBehaviour
{
	public static MenuCoordinator Instance { get; private set; }

	private readonly Dictionary<string, IGameMenu> menus = new Dictionary<string, IGameMenu>(32);
	private string currentExclusiveKey;

	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}
		Instance = this;
	}

	public void Register(IGameMenu menu)
	{
		if (menu == null || string.IsNullOrWhiteSpace(menu.Key)) return;
		menus[menu.Key] = menu;
	}

	public void Unregister(IGameMenu menu)
	{
		if (menu == null) return;

		if (menus.TryGetValue(menu.Key, out var existing) && ReferenceEquals(existing, menu))
		{
			menus.Remove(menu.Key);
			if (currentExclusiveKey == menu.Key) currentExclusiveKey = null;
		}
	}

	public void Toggle(string key)
	{
		if (!menus.TryGetValue(key, out var menu)) return;
		if (menu.IsOpen) Close(key);
		else Open(key);
	}

	public void Open(string key)
	{
		if (!menus.TryGetValue(key, out var menu)) return;

		if (!menu.IsOverlay)
		{
			CloseAllExclusiveExcept(key);
			currentExclusiveKey = key;
		}

		menu.Open();
	}

	public void Close(string key)
	{
		if (!menus.TryGetValue(key, out var menu)) return;

		menu.Close();

		if (!menu.IsOverlay && currentExclusiveKey == key)
			currentExclusiveKey = null;
	}

	public bool IsOpen(string key)
	{
		return menus.TryGetValue(key, out var m) && m.IsOpen;
	}

	private void CloseAllExclusiveExcept(string keepKey)
	{
		foreach (var kv in menus)
		{
			if (kv.Key == keepKey) continue;

			var m = kv.Value;
			if (!m.IsOverlay && m.IsOpen)
				m.Close();
		}
	}
}
