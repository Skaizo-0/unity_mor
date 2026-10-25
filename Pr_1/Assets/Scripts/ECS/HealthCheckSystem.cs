using Unity.Burst; // подключаем burst компилятор
using Unity.Entities; // подключаем структуры ecs

[BurstCompile]
// система отрабатывает после движения
[UpdateAfter(typeof(MoveToTargetSystem))]
public partial struct HealthCheckSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // создаем буфер команд entitycommandbuffer для записи команд на удаление
        var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);

        // джобы используют entitycommandbuffer.parallelwriter вместо обычного ecb
        var job = new HealthCheckJob
        {
            ECB = ecb.AsParallelWriter() // получаем parallelwriter для многопоточной записи команд
        };

        // ставим джобу в очередь планировщика работ с передачей зависимостей state.Dependency
        state.Dependency = job.ScheduleParallel(state.Dependency);

        // принудительно дожидаемся завершения джобы так как playback нельзя вызывать пока джоба работает
        state.Dependency.Complete();

        // воспроизводим накопленные команды удаления entity
        ecb.Playback(state.EntityManager);

        /*
        // используем буфер команд entity command buffer для безопасного удаления entity
        var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);

        // перебираем сущности с компонентом здоровья и получаем доступ к entity через with entity access
        foreach (var (health, entity) in 
                 SystemAPI.Query<RefRO<HealthData>>().WithEntityAccess())
        {
            
            if (health.ValueRO.Current <= 0)
            {
                ecb.DestroyEntity(entity);
            }
        }

        // применяем накопленные команды 
        ecb.Playback(state.EntityManager);
        */
    }
}

// параллельная джоба для проверки здоровья сущностей
[BurstCompile]
public partial struct HealthCheckJob : IJobEntity
{
    public EntityCommandBuffer.ParallelWriter ECB; // многопоточный буфер команд

    // специальный атрибут chunkindexinquery передает индекс чанка для определения порядка команд в playback
    // entity передается простым параметром вместо метода withentityaccess
    public void Execute([ChunkIndexInQuery] int chunkIndex, Entity entity, in HealthData health)
    {
        // если здоровье упало до нуля или ниже записываем команду на уничтожение entity
        if (health.Current <= 0)
        {
            ECB.DestroyEntity(chunkIndex, entity); // удаляем сущность с указанием индекса чанка
        }
    }
}
