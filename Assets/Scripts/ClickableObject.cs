using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClickableObject : MonoBehaviour
{
	[SerializeField] public Item item;
	public float timeToLive = 30f;
	public bool isCaught = false;

	private void Update()
	{
		if (!isCaught)
		{
			timeToLive -= Time.deltaTime;
			if (timeToLive <= 0f)
			{
				Destroy(gameObject);
			}
		}
	}

	public void Initialize(Item item)
	{
		this.item = item;
	}

	public void SetCaught(bool caught)
	{
		isCaught = caught;
		if (caught)
		{
			timeToLive = Mathf.Infinity;
		}
	}

	public void SetHooked(bool hooked)
	{
		isCaught = hooked;
		if (hooked)
		{
			timeToLive = Mathf.Infinity;
		}
	}
}
