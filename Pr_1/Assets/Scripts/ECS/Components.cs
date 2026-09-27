using Unity.Entities;
using Unity.Mathematics;

public struct TargetTag : IComponentData
{
}


public struct MoveToTargetData : IComponentData
{
    public float Speed;
    public float StoppingDistance;
}

public struct HealthData : IComponentData
{
    public float Current;
    public float Max;
    public float DamagePerSecondNearTarget; // урон в секунду 
}

public struct StaminaData : IComponentData
{
    public float Current;
    public float Max;
    public float DrainRate;     // расход стамины 
    public float RecoveryRate;  // восстановление стамины в секунду 
    public bool IsExhausted;    
}
