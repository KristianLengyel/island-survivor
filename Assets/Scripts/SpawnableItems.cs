using System.Collections;
using UnityEngine;

public class SpawnableItems : MonoBehaviour
{
	[System.Serializable]
	public class SpawnableObject
	{
		public GameObject prefab;
		public float spawnChance;
		[SerializeField] public int spawnCount;
		public int SpawnCount { get { return spawnCount; } }
	}

	[SerializeField] private SpawnableObject[] spawnObjects;
	[SerializeField] private float timeToSpawn = 1.5f;
	[SerializeField] private float range = 17f;
	[SerializeField] private Transform playerTransform;
	[SerializeField] private GameObject spawnContainerPrefab;

	private Transform spawnContainer;

	void Start()
	{
		SetupSpawnContainer();
		StartCoroutine(ObjectSpawn());
	}

	private void SetupSpawnContainer()
	{
		GameObject existingContainer = GameObject.Find("SpawnedObjectsContainer");
		if (existingContainer != null)
		{
			spawnContainer = existingContainer.transform;
		}
		else
		{
			if (spawnContainerPrefab != null)
			{
				spawnContainer = Instantiate(spawnContainerPrefab, Vector3.zero, Quaternion.identity).transform;
				spawnContainer.name = "SpawnedObjectsContainer";
			}
			else
			{
				spawnContainer = new GameObject("SpawnedObjectsContainer").transform;
			}
		}
	}

	IEnumerator ObjectSpawn()
	{
		while (true)
		{
			yield return new WaitForSeconds(timeToSpawn);

			Vector3 spawnPosition = new Vector3(
				Random.Range(playerTransform.position.x - range, playerTransform.position.x + range),
				playerTransform.position.y + range,
				0
			);

			GameObject selectedObject = SelectObjectToSpawn();
			if (selectedObject != null)
			{
				GameObject spawnedObject = Instantiate(selectedObject, spawnPosition, Quaternion.identity, spawnContainer);

				foreach (var spawnable in spawnObjects)
				{
					if (spawnable.prefab == selectedObject)
					{
						spawnable.spawnCount++;
						break;
					}
				}

				ClickableObject clickableObject = spawnedObject.GetComponent<ClickableObject>();
				if (clickableObject != null)
				{
					StartCoroutine(DestroyIfNotHooked(clickableObject, 20f));
				}
			}
		}
	}

	GameObject SelectObjectToSpawn()
	{
		float totalChance = 0;
		foreach (var spawnableObject in spawnObjects)
		{
			totalChance += spawnableObject.spawnChance;
		}

		float randomValue = Random.Range(0, totalChance);
		float cumulativeChance = 0;

		foreach (var spawnableObject in spawnObjects)
		{
			cumulativeChance += spawnableObject.spawnChance;
			if (randomValue <= cumulativeChance)
			{
				return spawnableObject.prefab;
			}
		}

		return null;
	}

	private IEnumerator DestroyIfNotHooked(ClickableObject clickableObject, float delay)
	{
		yield return new WaitForSeconds(delay);

		if (clickableObject != null && !clickableObject.isCaught)
		{
			Destroy(clickableObject.gameObject);
		}
	}
}