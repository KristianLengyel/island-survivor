using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
	public static AudioManager instance;

	[System.Serializable]
	public class Sound
	{
		public string name;
		public AudioClip clip;
		[Range(0f, 1f)]
		public float volume = 1f;
		[Range(0.1f, 3f)]
		public float pitch = 1f;
		public bool loop = false;

		[Header("Fade Settings")]
		public bool useFadeIn = false;
		public bool useFadeOut = false;
		public float fadeDuration = 1f;
	}

	public Sound[] sounds;
	private Dictionary<string, Sound> soundDictionary;
	private Coroutine fadeCoroutine;
	private Coroutine indoorFadeCoroutine;

	[SerializeField] private AudioSource oceanAmbientSource;

	private AudioSource ambientSource;
	public AudioSource effectsSource;

	private float baseAmbientVolume = 1f;
	private float baseOceanVolume;
	private float indoorVolumeMult = 1f;

	private void Awake()
	{
		if (instance == null)
		{
			instance = this;
			DontDestroyOnLoad(gameObject);
		}
		else
		{
			Destroy(gameObject);
			return;
		}

		AudioSource[] existingSources = GetComponents<AudioSource>();

		if (existingSources.Length > 0)
		{
			ambientSource = existingSources[0];
			ambientSource.spatialBlend = 0f;
		}
		else
		{
			ambientSource = gameObject.AddComponent<AudioSource>();
			ambientSource.spatialBlend = 0f;
		}

		if (existingSources.Length > 1)
		{
			effectsSource = existingSources[1];
			effectsSource.spatialBlend = 0f;
		}
		else
		{
			effectsSource = gameObject.AddComponent<AudioSource>();
			effectsSource.spatialBlend = 0f;
		}

		soundDictionary = new Dictionary<string, Sound>();
		foreach (var sound in sounds)
		{
			soundDictionary[sound.name] = sound;
		}

		if (oceanAmbientSource != null)
			baseOceanVolume = oceanAmbientSource.volume;
	}

	public void PlaySound(string name, float customPitch = -1f)
	{
		if (soundDictionary.ContainsKey(name))
		{
			var sound = soundDictionary[name];
			float pitchToUse = customPitch >= 0f ? customPitch : sound.pitch;

			if (sound.loop)
			{
				if (ambientSource.isPlaying && ambientSource.clip == sound.clip)
				{
					return;
				}

				baseAmbientVolume = sound.volume;
				float targetVolume = sound.volume * indoorVolumeMult;

				ambientSource.clip = sound.clip;
				ambientSource.volume = sound.useFadeIn ? 0f : targetVolume;
				ambientSource.pitch = pitchToUse;
				ambientSource.loop = true;
				ambientSource.Play();

				if (sound.useFadeIn)
				{
					StartFade(true, targetVolume, sound.fadeDuration);
				}
			}
			else
			{
				effectsSource.pitch = pitchToUse;
				effectsSource.PlayOneShot(sound.clip, sound.volume);
			}
		}
		else
		{
			Debug.LogWarning($"Sound {name} not found!");
		}
	}

	public void StopSound(string name)
	{
		if (soundDictionary.ContainsKey(name))
		{
			var sound = soundDictionary[name];
			if (sound.loop && ambientSource.clip == sound.clip && ambientSource.isPlaying)
			{
				if (sound.useFadeOut)
				{
					StartFade(false, 0f, sound.fadeDuration);
				}
				else
				{
					ambientSource.Stop();
				}
			}
		}
		else
		{
			Debug.LogWarning($"Sound {name} not found!");
		}
	}

	public void SetIndoorMuffling(bool indoors)
	{
		indoorVolumeMult = indoors ? 0.2f : 1f;
		float target = baseAmbientVolume * indoorVolumeMult;
		float duration = indoors ? 1f : 0.5f;

		if (indoorFadeCoroutine != null)
			StopCoroutine(indoorFadeCoroutine);

		indoorFadeCoroutine = StartCoroutine(FadeIndoorVolume(target, duration));
	}

	private IEnumerator FadeIndoorVolume(float targetVolume, float duration)
	{
		float startVolume = ambientSource.volume;
		float startOceanVolume = oceanAmbientSource != null ? oceanAmbientSource.volume : 0f;
		float targetOceanVolume = baseOceanVolume * indoorVolumeMult;
		float timeElapsed = 0f;

		while (timeElapsed < duration)
		{
			float t = timeElapsed / duration;
			ambientSource.volume = Mathf.Lerp(startVolume, targetVolume, t);
			if (oceanAmbientSource != null)
				oceanAmbientSource.volume = Mathf.Lerp(startOceanVolume, targetOceanVolume, t);
			timeElapsed += Time.deltaTime;
			yield return null;
		}

		ambientSource.volume = targetVolume;
		if (oceanAmbientSource != null)
			oceanAmbientSource.volume = targetOceanVolume;
		indoorFadeCoroutine = null;
	}

	private void StartFade(bool fadeIn, float targetVolume, float duration)
	{
		if (fadeCoroutine != null)
		{
			StopCoroutine(fadeCoroutine);
		}
		fadeCoroutine = StartCoroutine(FadeVolume(fadeIn, targetVolume, duration));
	}

	private IEnumerator FadeVolume(bool fadeIn, float targetVolume, float duration)
	{
		float startVolume = ambientSource.volume;
		float timeElapsed = 0f;

		while (timeElapsed < duration)
		{
			ambientSource.volume = Mathf.Lerp(startVolume, targetVolume, timeElapsed / duration);
			timeElapsed += Time.deltaTime;
			yield return null;
		}

		ambientSource.volume = targetVolume;

		if (!fadeIn && targetVolume == 0f)
		{
			ambientSource.Stop();
		}
	}
}
