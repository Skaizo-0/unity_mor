using Unity.Entities; // подключаем базовые типы и интерфейсы сущностей
using Unity.Mathematics; // подключаем библиотеку математики dots

// структура для хранения параметров одного конкретного типа врага в blob
public struct EnemyBlobData
{
    public float Speed; // базовая скорость перемещения врага
    public float Health; // базовый запас здоровья врага
    public float Stamina; // базовый запас стамины врага
}

// структура для описания параметров отдельной волны врагов в blob
public struct WaveBlobData
{
    public int EnemyCount; // общее количество врагов запланированное в этой волне
    public float SpawnInterval; // интервал задержки в секундах между появлением врагов
    public BlobArray<EnemyBlobData> Enemies; // вложенный blob массив с типами врагов для этой волны
}

// корневая структура всего blob дерева для волн
public struct WavesBlobRoot
{
    public BlobArray<WaveBlobData> Waves; // массив всех волн запеченных в неизменяемую память
}

// компонент ссылка на созданный blob asset
// blobassetreference хранит указатель на место в неуправляемой памяти
public struct WavesBlobReference : IComponentData
{
    public BlobAssetReference<WavesBlobRoot> Value; // указатель на физический блок данных blob в памяти
}

// singleton компонент для централизованного хранения ссылок на заготовки префабов
// хранение префабов в одном singleton оптимальнее всего чтобы экономить память
public struct SpawnerPrefabData : IComponentData
{
    public Entity EnemyPrefab; // ссылка на готовый запеченный entity префаб врага
}

// компонент состояния процесса спавна на сцене
public struct SpawnerStateData : IComponentData
{
    public float Timer; // текущий таймер отсчета времени до следующего спавна
    public int CurrentWaveIndex; // порядковый номер текущей активной волны из blob
    public int SpawnedInCurrentWave; // счетчик уже созданных врагов в текущей волне
    public Unity.Mathematics.Random Random; // детерминированный генератор случайных чисел dots
}

// структура элемента динамического списка на entity
// dynamicbuffer это компонент список однотипных данных реализующий ibufferelementdata
// атрибут internalbuffercapacity задает максимальное количество элементов без переаллокации памяти
[InternalBufferCapacity(16)]
public struct SpawnLogBufferElement : IBufferElementData
{
    public float3 SpawnPosition; // трехмерная точка где был создан конкретный враг
    public float SpawnTime; // точное время мира когда произошел спавн этого врага
}
