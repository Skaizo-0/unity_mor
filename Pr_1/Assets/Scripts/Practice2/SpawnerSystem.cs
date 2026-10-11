using Unity.Burst; // подключаем burst компилятор для высокой производительности
using Unity.Entities; // подключаем ядро ecs
using Unity.Mathematics; // подключаем математические типы float3 и функции math
using Unity.Transforms; // подключаем компонент трансформации localtransform

// атрибут burstcompile компилирует систему в быстрый машинный код процессора
[BurstCompile]
// система спавна реализующая интерфейс isystem для работы в общем цикле кадров
public partial struct SpawnerSystem : ISystem
{
    // метод onupdate вызывается каждый кадр в группе simulationsystemgroup
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // чтобы обратиться к держателю префабов из любой системы используем systemapi getsingleton
        if (!SystemAPI.TryGetSingleton<SpawnerPrefabData>(out SpawnerPrefabData prefabData))
        {
            return; // если компонент хранилища префабов еще не создан то выходим из обновления
        }

        // обращаемся к blob через getsingleton так как в мире всего 1 entity с таким компонентом
        if (!SystemAPI.TryGetSingleton<WavesBlobReference>(out WavesBlobReference blobRef))
        {
            return; // если ссылка на blob отсутствует прерываем выполнение системы
        }

        // получаем прямую ссылку на корень данных blob из ссылки blobassetreference
        ref WavesBlobRoot blobData = ref blobRef.Value.Value;

        // проверяем есть ли волны в blob массиве
        if (blobData.Waves.Length == 0) return;

        // вместо привычного time deltatime используем systemapi time deltatime
        float deltaTime = SystemAPI.Time.DeltaTime;

        // для действий со спавном entity в цикле следует использовать entitycommandbuffer
        var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);

        // перебираем спавнеры через query с получением entity через withentityaccess
        foreach (var (spawnerState, spawnerEntity) in 
                 SystemAPI.Query<RefRW<SpawnerStateData>>().WithEntityAccess())
        {
            // считываем индекс текущей обрабатываемой волны
            int waveIndex = spawnerState.ValueRO.CurrentWaveIndex;

            // если номер волны превысил количество волн в blob значит все волны пройдены
            if (waveIndex >= blobData.Waves.Length)
            {
                continue; // пропускаем дальнейшую обработку спавна
            }

            // получаем ссылку на данные текущей волны из blob массива
            ref WaveBlobData currentWave = ref blobData.Waves[waveIndex];

            // увеличиваем таймер спавна на прошедшее время кадра
            spawnerState.ValueRW.Timer += deltaTime;

            // проверяем наступило ли время очередного спавна и не превышен ли лимит врагов в волне
            if (spawnerState.ValueRO.Timer >= currentWave.SpawnInterval && 
                spawnerState.ValueRO.SpawnedInCurrentWave < currentWave.EnemyCount)
            {
                // сбрасываем таймер для следующего врага
                spawnerState.ValueRW.Timer = 0f;

                // увеличиваем счетчик созданных врагов в этой волне
                spawnerState.ValueRW.SpawnedInCurrentWave++;

                // создаем экземпляр entity из заготовленного префаба через entitycommandbuffer
                Entity spawnedEntity = ecb.Instantiate(prefabData.EnemyPrefab);

                // используем unity mathematics random для генерации случайного угла вокруг спавнера
                float angle = spawnerState.ValueRW.Random.NextFloat(0f, math.PI * 2f);

                // используем unity mathematics random для генерации случайного радиуса от 8 до 15 метров
                float radius = spawnerState.ValueRW.Random.NextFloat(8f, 15f);

                // вычисляем случайную трехмерную позицию точки появления
                float3 spawnPos = new float3(math.cos(angle) * radius, 0f, math.sin(angle) * radius);

                // задаем позицию созданного entity через команду localtransform
                ecb.SetComponent(spawnedEntity, LocalTransform.FromPosition(spawnPos));

                // выбираем случайный индекс типа врага из вложенного blob массива через random
                int enemyTypeIdx = spawnerState.ValueRW.Random.NextInt(0, currentWave.Enemies.Length);

                // берем характеристики выбранного врага из вложенного blob массива
                ref EnemyBlobData enemyParams = ref currentWave.Enemies[enemyTypeIdx];

                // генерируем случайный коэффициент разброса скорости от 0.85 до 1.25
                float randomSpeedVariation = spawnerState.ValueRW.Random.NextFloat(0.85f, 1.25f);

                // вычисляем итоговую скорость врага с учетом случайного разброса
                float finalSpeed = enemyParams.Speed * randomSpeedVariation;

                // записываем обновленный компонент движения в созданный entity через ecb
                ecb.SetComponent(spawnedEntity, new MoveToTargetData
                {
                    Speed = finalSpeed, // передаем вычисленную случайную скорость
                    StoppingDistance = 0.8f // задаем дистанцию остановки возле цели
                });

                // записываем параметры здоровья из blob в созданный entity через ecb
                ecb.SetComponent(spawnedEntity, new HealthData
                {
                    Current = enemyParams.Health, // текущее здоровье врага
                    Max = enemyParams.Health, // максимальное здоровье врага
                    DamagePerSecondNearTarget = 30f // урон при контакте с базой
                });

                // записываем параметры выносливости из blob в созданный entity через ecb
                ecb.SetComponent(spawnedEntity, new StaminaData
                {
                    Current = enemyParams.Stamina, // текущая стамина врага
                    Max = enemyParams.Stamina, // максимальная стамина врага
                    DrainRate = 15f, // расход стамины в секунду при беге
                    RecoveryRate = 12f, // восстановление стамины в секунду при отдыхе
                    IsExhausted = false // флаг истощения изначально выключен
                });

                // чтобы использовать dynamicbuffer в системе получаем буфер методом getbuffer
                // dynamicbuffer не требует оберток refro или refrw
                DynamicBuffer<SpawnLogBufferElement> logBuffer = SystemAPI.GetBuffer<SpawnLogBufferElement>(spawnerEntity);

                // добавляем новый элемент в динамический список dynamicbuffer
                logBuffer.Add(new SpawnLogBufferElement
                {
                    SpawnPosition = spawnPos, // сохраняем координаты спавна в буфер
                    SpawnTime = (float)SystemAPI.Time.ElapsedTime // сохраняем время спавна в буфер
                });

                // если в текущей волне заспавнены все запланированные враги
                if (spawnerState.ValueRO.SpawnedInCurrentWave >= currentWave.EnemyCount)
                {
                    // переходим к индексу следующей волны
                    spawnerState.ValueRW.CurrentWaveIndex++;

                    // сбрасываем счетчик заспавненных для новой волны
                    spawnerState.ValueRW.SpawnedInCurrentWave = 0;

                    // делаем задержку в 3 секунды перед стартом следующей волны
                    spawnerState.ValueRW.Timer = -3f;
                }
            }
        }

        // по завершении цикла entitycommandbuffer воспроизводит все команды разом
        ecb.Playback(state.EntityManager);
    }
}
