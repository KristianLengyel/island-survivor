/// <summary>
/// Future-proof biome enum. Add new biomes here — all downstream
/// systems read from BiomeDefinition assets so no other code changes needed.
/// </summary>
public enum BiomeType : byte
{
	Tropical = 0,
	Temperate = 1,
	Desert = 2,
	Arctic = 3,
	Volcanic = 4,
	// Add new biomes below — existing saves remain valid
	// Swamp   = 5,
	// Mushroom = 6,
}