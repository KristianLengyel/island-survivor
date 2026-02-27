using UnityEngine;

[System.Serializable]
public class ResourceDrop
{
	public Item item;
	public int minAmount = 1;
	public int maxAmount = 1;

	public int RollAmount()
	{
		if (minAmount < 0) minAmount = 0;
		if (maxAmount < minAmount) maxAmount = minAmount;
		return Random.Range(minAmount, maxAmount + 1);
	}
}