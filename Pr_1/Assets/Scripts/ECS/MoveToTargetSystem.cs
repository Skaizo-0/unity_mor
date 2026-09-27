using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;


[BurstCompile]
public partial struct MoveToTargetSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        //  ищет в игре объект с компонентом TargetTag
        if (!SystemAPI.TryGetSingletonEntity<TargetTag>(out Entity targetEntity))
        {
            return;
        }

        // получаем компонент позиции цели
        LocalTransform targetTransform = SystemAPI.GetComponent<LocalTransform>(targetEntity);
        float3 targetPosition = targetTransform.Position;

        
        float deltaTime = SystemAPI.Time.DeltaTime;

        // перебираем entity с определенными компонентами 
        // refro только для чтения а refrw для изменения данных
        foreach (var (transform, moveData, stamina, health) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<MoveToTargetData>, RefRW<StaminaData>, RefRW<HealthData>>())
        {
            float3 currentPos = transform.ValueRO.Position;

            
            float3 toTarget = targetPosition - currentPos;
            toTarget.y = 0; 

            
            float distance = math.length(toTarget);

            //  если дошли до цели 
            if (distance <= moveData.ValueRO.StoppingDistance)
            {
                // от текущего здоровья отнимается урон в секунду
                health.ValueRW.Current -= health.ValueRO.DamagePerSecondNearTarget * deltaTime;

                // восстанавливаем стамину
                stamina.ValueRW.Current = math.min(
                    stamina.ValueRO.Max, 
                    stamina.ValueRO.Current + stamina.ValueRO.RecoveryRate * deltaTime
                );
                continue;
            }

            //  если стамина упала до 0  то состояние отдых
            if (stamina.ValueRO.Current <= 0.01f)
            {
                stamina.ValueRW.IsExhausted = true;
            }

           
            if (stamina.ValueRO.IsExhausted)
            {
                stamina.ValueRW.Current = math.min(
                    stamina.ValueRO.Max, 
                    stamina.ValueRO.Current + stamina.ValueRO.RecoveryRate * deltaTime
                );

                // отдыхает пока стамина не восстановится хотя бы на 50 процентов
                if (stamina.ValueRO.Current >= stamina.ValueRO.Max * 0.5f)
                {
                    stamina.ValueRW.IsExhausted = false;
                }

                
                continue;
            }

            //  если не отдых и не у цели то юнит бежит к цели
            float3 dir = math.normalize(toTarget);
            transform.ValueRW.Position += dir * moveData.ValueRO.Speed * deltaTime;
            transform.ValueRW.Rotation = quaternion.LookRotationSafe(dir, math.up());

            // расходуем стамину при беге
            stamina.ValueRW.Current = math.max(
                0f, 
                stamina.ValueRO.Current - stamina.ValueRO.DrainRate * deltaTime
            );
        }
    }
}
