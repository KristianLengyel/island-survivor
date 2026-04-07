using UnityEngine;

public enum WaterType { None, SaltWater, FreshWater }

public interface IPipeConnectable
{
	Vector2Int GridPosition { get; }
	bool IsPump { get; }
	bool IsActive { get; }
	WaterType OutputWaterType { get; }
	bool IsColorSource { get; }
	bool IsWaterTypeCompatible(WaterType type);
	bool CanReceiveWater(WaterType type);
	void ReceiveWaterUnit(WaterType type);
	bool CanConsumeWater(WaterType type);
	void ConsumeWaterUnit();
}
