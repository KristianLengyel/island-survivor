using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class AdminConsoleManager : MonoBehaviour, IGameMenu
{
	[SerializeField] private string key = "AdminConsole";
	[SerializeField] private bool isOverlay = false;

	[Header("UI")]
	[SerializeField] private GameObject root;
	[SerializeField] private Transform contentRoot;
	[SerializeField] private AdminConsoleItemRow rowPrefab;
	[SerializeField] private AdminConsoleCategoryHeader headerPrefab;
	[SerializeField] private TMP_InputField searchInput;

	[Header("Data")]
	[SerializeField] private string resourcesFolder = "Items";
	[SerializeField] private bool showNotifications = false;

	private readonly List<Item> allItems = new List<Item>(256);
	private readonly List<Item> filteredItems = new List<Item>(256);
	private readonly List<AdminConsoleItemRow> rowPool = new List<AdminConsoleItemRow>(256);
	private readonly List<AdminConsoleCategoryHeader> headerPool = new List<AdminConsoleCategoryHeader>(16);

	private bool registered;
	private bool itemsLoaded;
	private string lastQuery = "";

	public string Key => key;
	public bool IsOverlay => isOverlay;
	public bool IsOpen => root != null && root.activeSelf;

	private void Awake()
	{
		if (root != null) root.SetActive(false);
	}

	private void Start()
	{
		var coordinator = MenuCoordinator.Instance != null ? MenuCoordinator.Instance : FindAnyObjectByType<MenuCoordinator>();
		if (coordinator != null)
		{
			coordinator.Register(this);
			registered = true;
		}

		EnsureItemsLoaded();
		ApplyFilterAndRender("");

		if (searchInput != null)
		{
			searchInput.onValueChanged.RemoveAllListeners();
			searchInput.onValueChanged.AddListener(q =>
			{
				lastQuery = q ?? "";
				ApplyFilterAndRender(lastQuery);
			});
		}
	}

	private void OnDestroy()
	{
		if (registered && MenuCoordinator.Instance != null)
			MenuCoordinator.Instance.Unregister(this);
	}

	private void Update()
	{
		if (GameInput.AdminConsoleDown)
		{
			var coordinator = MenuCoordinator.Instance != null ? MenuCoordinator.Instance : FindAnyObjectByType<MenuCoordinator>();
			if (coordinator != null) coordinator.Toggle(key);
			else
			{
				if (IsOpen) Close();
				else Open();
			}
		}
	}

	public void Open()
	{
		if (root == null) return;

		EnsureItemsLoaded();

		root.SetActive(true);

		if (searchInput != null)
		{
			searchInput.text = "";
			searchInput.Select();
			searchInput.ActivateInputField();
		}

		ApplyFilterAndRender("");
	}

	public void Close()
	{
		if (root == null) return;
		root.SetActive(false);
	}

	private void EnsureItemsLoaded()
	{
		if (itemsLoaded) return;

		allItems.Clear();
		var loaded = Resources.LoadAll<Item>(resourcesFolder);
		for (int i = 0; i < loaded.Length; i++)
		{
			if (loaded[i] != null)
				allItems.Add(loaded[i]);
		}

		allItems.Sort((a, b) =>
		{
			if (a == null || b == null) return 0;
			int cat = a.category.CompareTo(b.category);
			if (cat != 0) return cat;
			return string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase);
		});
		itemsLoaded = true;
	}

	private void ApplyFilterAndRender(string query)
	{
		if (contentRoot == null || rowPrefab == null) return;

		string q = (query ?? "").Trim();
		bool filter = q.Length > 0;

		filteredItems.Clear();

		if (!filter)
		{
			filteredItems.AddRange(allItems);
		}
		else
		{
			for (int i = 0; i < allItems.Count; i++)
			{
				var it = allItems[i];
				if (it == null || it.name == null) continue;
				if (it.name.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
					filteredItems.Add(it);
			}
		}

		// Hide all pooled objects first
		for (int i = 0; i < rowPool.Count; i++)
			if (rowPool[i] != null) rowPool[i].gameObject.SetActive(false);
		for (int i = 0; i < headerPool.Count; i++)
			if (headerPool[i] != null) headerPool[i].gameObject.SetActive(false);

		int rowIndex = 0;
		int headerIndex = 0;
		ItemCategory? lastCategory = null;

		for (int i = 0; i < filteredItems.Count; i++)
		{
			var item = filteredItems[i];
			if (item == null) continue;

			// Insert category header when category changes (skip headers when searching)
			if (!filter && item.category != lastCategory)
			{
				lastCategory = item.category;

				if (headerPrefab != null)
				{
					var header = GetOrCreateHeader(headerIndex++);
					header.SetLabel(item.category.ToString());
					header.gameObject.SetActive(true);
					header.transform.SetSiblingIndex(rowIndex + headerIndex - 1);
				}
			}

			var row = GetOrCreateRow(rowIndex++);
			row.gameObject.SetActive(true);
			row.Bind(item, AddItemToInventory);
		}
	}

	private AdminConsoleItemRow GetOrCreateRow(int index)
	{
		while (rowPool.Count <= index)
			rowPool.Add(Instantiate(rowPrefab, contentRoot));
		return rowPool[index];
	}

	private AdminConsoleCategoryHeader GetOrCreateHeader(int index)
	{
		while (headerPool.Count <= index)
			headerPool.Add(Instantiate(headerPrefab, contentRoot));
		return headerPool[index];
	}

	private void AddItemToInventory(Item item)
	{
		if (item == null) return;
		if (GameManager.Instance == null || GameManager.Instance.InventoryManager == null) return;

		GameManager.Instance.InventoryManager.AddItem(item, 1, showNotifications);
		AudioManager.instance?.PlaySound("CraftItem");
	}
}