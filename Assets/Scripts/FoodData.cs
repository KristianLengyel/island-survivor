using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable object/Food Data", fileName = "New Food Data")]
public class FoodData : ScriptableObject
{
	[Tooltip("The inventory item this food corresponds to")]
	public Item item;

	[Tooltip("Amount of hunger this food restores (positive) or drains (negative)")]
	public float hungerEffect = 0f;
}