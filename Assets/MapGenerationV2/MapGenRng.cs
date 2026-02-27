public struct MapGenRng
{
	private uint state;

	public MapGenRng(uint seed)
	{
		state = seed == 0 ? 0x6D2B79F5u : seed;
	}

	public uint NextU()
	{
		uint x = state;
		x ^= x << 13;
		x ^= x >> 17;
		x ^= x << 5;
		state = x;
		return x;
	}

	public int NextInt(int minInclusive, int maxExclusive)
	{
		if (maxExclusive <= minInclusive) return minInclusive;
		uint r = NextU();
		uint range = (uint)(maxExclusive - minInclusive);
		return (int)(r % range) + minInclusive;
	}

	public float Next01()
	{
		return (NextU() & 0x00FFFFFFu) / 16777216f;
	}

	public float Range(float min, float max)
	{
		return min + (max - min) * Next01();
	}

	public static uint HashSeed(string s)
	{
		if (string.IsNullOrEmpty(s)) return 0x9E3779B9u;
		unchecked
		{
			uint h = 2166136261u;
			for (int i = 0; i < s.Length; i++)
			{
				h ^= s[i];
				h *= 16777619u;
			}
			return h;
		}
	}
}
