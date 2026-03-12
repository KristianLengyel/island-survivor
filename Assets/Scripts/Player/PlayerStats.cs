using System.Collections.Generic;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
	[Header("Thirst Settings")]
	public float maxThirst = 100f;
	public float thirstDrainRate = 0.2f;
	private float currentThirst;

	[Header("Hunger Settings")]
	public float maxHunger = 100f;
	public float hungerDrainRate = 0.1f;
	private float currentHunger;

	[Header("Stamina Settings")]
	public float maxStamina = 100f;
	public float staminaRegenRate = 8f;
	private float currentStamina;

	[Header("Food Data (Auto)")]
	[SerializeField] private string foodDataResourcesPath = "FoodData";
	private readonly Dictionary<string, float> foodEffects = new Dictionary<string, float>();

	[Header("Critical Threshold")]
	public float criticalThreshold = 0.15f;

	private float updateInterval = 0.1f;
	private float timeSinceLastUpdate;

	void Start()
	{
		if (maxThirst <= 0f) maxThirst = 1f;
		if (maxHunger <= 0f) maxHunger = 1f;
		if (maxStamina <= 0f) maxStamina = 1f;

		if (thirstDrainRate < 0f) thirstDrainRate = 0f;
		if (hungerDrainRate < 0f) hungerDrainRate = 0f;
		if (staminaRegenRate < 0f) staminaRegenRate = 0f;

		if (currentThirst <= 0f) currentThirst = maxThirst;
		if (currentHunger <= 0f) currentHunger = maxHunger;
		if (currentStamina <= 0f) currentStamina = maxStamina;

		LoadFoodDataFromResources();
	}

	private void LoadFoodDataFromResources()
	{
		foodEffects.Clear();

		var foods = Resources.LoadAll<FoodData>(foodDataResourcesPath);
		for (int i = 0; i < foods.Length; i++)
		{
			var food = foods[i];
			if (food == null || food.item == null) continue;

			foodEffects[food.item.name] = food.hungerEffect;
		}
	}

	void Update()
	{
		timeSinceLastUpdate += Time.deltaTime;
		if (timeSinceLastUpdate >= updateInterval)
		{
			float drainAmount = timeSinceLastUpdate * thirstDrainRate;
			if (currentThirst > 0f) currentThirst = Mathf.Max(currentThirst - drainAmount, 0f);

			drainAmount = timeSinceLastUpdate * hungerDrainRate;
			if (currentHunger > 0f) currentHunger = Mathf.Max(currentHunger - drainAmount, 0f);

			float regenAmount = timeSinceLastUpdate * staminaRegenRate;
			if (currentStamina < maxStamina) currentStamina = Mathf.Min(currentStamina + regenAmount, maxStamina);

			timeSinceLastUpdate = 0f;
		}
	}

	public float GetCurrentThirst() => currentThirst;
	public float GetCurrentHunger() => currentHunger;

	public float GetCurrentStamina() => currentStamina;
	public float GetStaminaPercentage() => maxStamina <= 0f ? 0f : Mathf.Clamp01(currentStamina / maxStamina);

	public void SetState(float thirst, float hunger, float stamina = -1f)
	{
		currentThirst = Mathf.Clamp(thirst, 0f, maxThirst);
		currentHunger = Mathf.Clamp(hunger, 0f, maxHunger);
		if (stamina >= 0f) currentStamina = Mathf.Clamp(stamina, 0f, maxStamina);
		timeSinceLastUpdate = 0f;
	}

	public void AddThirst(float amount) => currentThirst = Mathf.Clamp(currentThirst + amount, 0f, maxThirst);

	public void AddHunger(float amount) => currentHunger = Mathf.Clamp(currentHunger + amount, 0f, maxHunger);

	public bool HasStamina(float amount) => currentStamina >= Mathf.Max(0f, amount);

	public bool SpendStamina(float amount)
	{
		amount = Mathf.Max(0f, amount);
		if (currentStamina < amount) return false;
		currentStamina -= amount;
		return true;
	}

	public float GetThirstPercentage() => maxThirst <= 0f ? 0f : Mathf.Clamp01(currentThirst / maxThirst);
	public float GetHungerPercentage() => maxHunger <= 0f ? 0f : Mathf.Clamp01(currentHunger / maxHunger);

	public bool ConsumeFood(string itemName)
	{
		if (string.IsNullOrEmpty(itemName)) return false;
		if (!foodEffects.TryGetValue(itemName, out var effect)) return false;

		AddHunger(effect);
		return true;
	}
}
