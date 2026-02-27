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
	[SerializeField] private TMP_InputField searchInput;

	[Header("Data")]
	[SerializeField] private string resourcesFolder = "Items";
	[SerializeField] private bool showNotifications = false;

	private readonly List<Item> allItems = new List<Item>(256);
	private readonly List<Item> filteredItems = new List<Item>(256);
	private readonly List<AdminConsoleItemRow> rowPool = new List<AdminConsoleItemRow>(256);

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

		allItems.Sort((a, b) => string.Compare(a != null ? a.name : "", b != null ? b.name : "", StringComparison.OrdinalIgnoreCase));
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

		EnsureRowPoolSize(filteredItems.Count);

		for (int i = 0; i < rowPool.Count; i++)
		{
			bool active = i < filteredItems.Count;
			var row = rowPool[i];
			if (row == null) continue;

			if (active)
			{
				row.gameObject.SetActive(true);
				row.Bind(filteredItems[i], AddItemToInventory);
			}
			else
			{
				row.gameObject.SetActive(false);
			}
		}
	}

	private void EnsureRowPoolSize(int count)
	{
		while (rowPool.Count < count)
		{
			var row = Instantiate(rowPrefab, contentRoot);
			rowPool.Add(row);
		}
	}

	private void AddItemToInventory(Item item)
	{
		if (item == null) return;
		if (GameManager.Instance == null || GameManager.Instance.InventoryManager == null) return;

		GameManager.Instance.InventoryManager.AddItem(item, 1, showNotifications);
	}
}