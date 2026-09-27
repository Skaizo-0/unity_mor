using Unity.Burst;
using Unity.Entities;


[BurstCompile]
//  система отрабатывает после движения
[UpdateAfter(typeof(MoveToTargetSystem))]
public partial struct HealthCheckSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
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
    }
}
