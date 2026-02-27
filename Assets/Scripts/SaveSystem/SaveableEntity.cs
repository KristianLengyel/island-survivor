using System;
using UnityEngine;

[DisallowMultipleComponent]
public class SaveableEntity : MonoBehaviour
{
	[SerializeField] private string uniqueId;

	public string UniqueId => uniqueId;

	private void Awake()
	{
		if (string.IsNullOrWhiteSpace(uniqueId))
			uniqueId = Guid.NewGuid().ToString("N");
	}

#if UNITY_EDITOR
	private void OnValidate()
	{
		if (string.IsNullOrWhiteSpace(uniqueId))
			uniqueId = Guid.NewGuid().ToString("N");
	}
#endif
}
