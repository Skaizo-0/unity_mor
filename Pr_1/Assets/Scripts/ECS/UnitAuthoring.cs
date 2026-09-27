using UnityEngine;
using Unity.Entities;


public class UnitAuthoring : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 3.5f;
    public float stoppingDistance = 0.8f;

    [Header("Health")]
    public float maxHealth = 100f;
    public float damagePerSecondNearTarget = 30f; 

    [Header("Stamina")]
    public float maxStamina = 50f;
    public float staminaDrainRate = 15f;
    public float staminaRecoveryRate = 12f;

    public class Baker : Baker<UnitAuthoring>
    {
        public override void Bake(UnitAuthoring authoring)
        {
            // запекаем entity с флагом dynamic
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);

            // запекаем компонент движения к цели
            AddComponent(entity, new MoveToTargetData
            {
                Speed = authoring.speed,
                StoppingDistance = authoring.stoppingDistance
            });

            
            AddComponent(entity, new HealthData
            {
                Current = authoring.maxHealth,
                Max = authoring.maxHealth,
                DamagePerSecondNearTarget = authoring.damagePerSecondNearTarget
            });

           
            AddComponent(entity, new StaminaData
            {
                Current = authoring.maxStamina,
                Max = authoring.maxStamina,
                DrainRate = authoring.staminaDrainRate,
                RecoveryRate = authoring.staminaRecoveryRate,
                IsExhausted = false
            });
        }
    }
}
