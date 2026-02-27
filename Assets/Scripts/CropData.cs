using UnityEngine;

[CreateAssetMenu(fileName = "New Crop Data", menuName = "Scriptable object/Crop Data")]
public class CropData : ScriptableObject
{
	public string cropName;
	public Sprite[] growthSprites = new Sprite[3];
}