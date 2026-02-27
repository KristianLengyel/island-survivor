using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable object/Water Container Item")]
public class WaterContainerItem : Item
{
	[Header("Water Container Settings")]
	public Sprite[] fillSprites;
	public Sprite[] saltFillSprites;
	public int maxFillCapacity = 0;
}