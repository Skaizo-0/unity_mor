using Unity.Burst; // подключаем burst компилятор
using Unity.Entities; // подключаем структуры ecs
using Unity.Mathematics; // подключаем математику dots
using Unity.Transforms; // подключаем компоненты трансформации

[BurstCompile]
// система движения и управления стаминой переписанная на параллельные джобы
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

        // получаем время кадра
        float deltaTime = SystemAPI.Time.DeltaTime;

        // создаем экземпляр джобы и передаем необходимые внешние параметры
        var job = new MoveToTargetJob
        {
            TargetPosition = targetPosition,
            DeltaTime = deltaTime
        };

        // ставим джобу в очередь планировщика и передаем цепочку зависимостей state.Dependency
        // следующая система увидит что джоба встала в очередь и будет дожидаться ее завершения
        state.Dependency = job.ScheduleParallel(state.Dependency);

        /*
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
        */
    }
}

// структура описывающая процесс происходящий на каждом рабочем потоке процессора
// интерфейс ijobentity обозначает параллельный процесс для каждого подходящего entity
[BurstCompile]
public partial struct MoveToTargetJob : IJobEntity
{
    public float3 TargetPosition; // позиция цели переданная в джобу
    public float DeltaTime; // дельта времени переданная в джобу

    // в методе execute необходимые компоненты передаются с ключевыми словами ref для записи и in для чтения
    public void Execute(ref LocalTransform transform, in MoveToTargetData moveData, ref StaminaData stamina, ref HealthData health)
    {
        float3 currentPos = transform.Position; // считываем текущее положение entity

        float3 toTarget = TargetPosition - currentPos; // вектор направления к цели
        toTarget.y = 0; // перемещение строго в горизонтальной плоскости

        float distance = math.length(toTarget); // рассчитываем расстояние до цели

        // если дошли до цели то получаем урон и восстанавливаем стамину
        if (distance <= moveData.StoppingDistance)
        {
            health.Current -= health.DamagePerSecondNearTarget * DeltaTime;

            stamina.Current = math.min(
                stamina.Max,
                stamina.Current + stamina.RecoveryRate * DeltaTime
            );
            return; // завершаем выполнение для этого entity в текущем кадре
        }

        // если стамина опустилась до нуля то включаем флаг истощения
        if (stamina.Current <= 0.01f)
        {
            stamina.IsExhausted = true;
        }

        // если юнит истощен то он стоит на месте и копит стамину до пятидесяти процентов
        if (stamina.IsExhausted)
        {
            stamina.Current = math.min(
                stamina.Max,
                stamina.Current + stamina.RecoveryRate * DeltaTime
            );

            if (stamina.Current >= stamina.Max * 0.5f)
            {
                stamina.IsExhausted = false;
            }

            return; // пока истощен прерываем движение
        }

        // рассчитываем нормализованное направление к цели
        float3 dir = math.normalize(toTarget);

        // перемещаем entity и поворачиваем его лицом к цели
        transform.Position += dir * moveData.Speed * DeltaTime;
        transform.Rotation = quaternion.LookRotationSafe(dir, math.up());

        // расходуем стамину при беге
        stamina.Current = math.max(
            0f,
            stamina.Current - stamina.DrainRate * DeltaTime
        );
    }
}
