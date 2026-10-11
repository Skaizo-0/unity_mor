using UnityEngine; // подключаем пространство имен unity engine
using Unity.Entities; // подключаем структуры и интерфейсы ecs
using Unity.Mathematics; // подключаем быструю математику dots
using System.Collections.Generic; // подключаем коллекции для списков в инспекторе

// скрипт для настройки данных спавнера в редакторе и их запекания в blob и entity
public class SpawnerAuthoring : MonoBehaviour
{
    [Header("Префаб для спавна")] // заголовок поля префаба в инспекторе
    public GameObject enemyPrefab; // поле для перетаскивания исходного gameobject префаба

    [System.Serializable] // разрешаем отображение класса в инспекторе unity
    public class EnemyConfig
    {
        public float speed = 3.5f; // скорость врага настраиваемая в инспекторе
        public float health = 100f; // здоровье врага настраиваемое в инспекторе
        public float stamina = 50f; // стамина врага настраиваемая в инспекторе
    }

    [System.Serializable] // разрешаем сериализацию структуры волны для инспектора
    public class WaveConfig
    {
        public int enemyCount = 5; // количество врагов в волне по умолчанию
        public float spawnInterval = 1.5f; // пауза между врагами в секундах
        public List<EnemyConfig> enemies = new List<EnemyConfig>(); // список вариантов врагов
    }

    [Header("Настройки волн для Blob")] // заголовок списка волн в инспекторе
    public List<WaveConfig> waves = new List<WaveConfig>(); // список всех настраиваемых волн

    // вложенный класс baker для запекания authoring компонента в сущность
    public class Baker : Baker<SpawnerAuthoring>
    {
        // переопределяем метод bake который unity вызывает для запекания данных
        public override void Bake(SpawnerAuthoring authoring)
        {
            // объявляем entity в который превратится gameobject в рантайме
            // transformusageflags none подходит если entity вообще не важно находиться в координатной сетке
            Entity spawnerEntity = GetEntity(TransformUsageFlags.None);

            // проверяем назначен ли префаб в инспекторе
            if (authoring.enemyPrefab != null)
            {
                // получаем entity префаба с флагом dynamic так как созданные враги будут перемещаться
                Entity prefabEntity = GetEntity(authoring.enemyPrefab, TransformUsageFlags.Dynamic);

                // добавляем singleton компонент держатель префаба на entity
                AddComponent(spawnerEntity, new SpawnerPrefabData
                {
                    EnemyPrefab = prefabEntity // передаем ссылку на запеченный префаб
                });
            }

            // создаем вспомогательный объект blobbuilder для выделения неуправляемой памяти под blob
            using (var builder = new BlobBuilder(Unity.Collections.Allocator.Temp))
            {
                // создаем корневой объект root типа wavesblobroot внутри blobbuilder
                ref WavesBlobRoot root = ref builder.ConstructRoot<WavesBlobRoot>();

                // определяем число волн для записи
                int waveCount = authoring.waves != null && authoring.waves.Count > 0 ? authoring.waves.Count : 1;

                // для массива blobarray waves внутри выделяем память фиксированной длины
                BlobBuilderArray<WaveBlobData> wavesArray = builder.Allocate(ref root.Waves, waveCount);

                // циклом проходим по всем волнам для заполнения значений
                for (int w = 0; w < waveCount; w++)
                {
                    // берем исходные данные волны из инспектора
                    var waveSrc = (authoring.waves != null && w < authoring.waves.Count) ? authoring.waves[w] : new WaveConfig();

                    // записываем количество врагов в элемент blob
                    wavesArray[w].EnemyCount = waveSrc.enemyCount > 0 ? waveSrc.enemyCount : 5;

                    // записываем интервал между спавнами в элемент blob
                    wavesArray[w].SpawnInterval = waveSrc.spawnInterval > 0 ? waveSrc.spawnInterval : 1.5f;

                    // определяем количество типов врагов внутри этой волны
                    int enemyTypesCount = (waveSrc.enemies != null && waveSrc.enemies.Count > 0) ? waveSrc.enemies.Count : 1;

                    // выделяем память для вложенного blobarray массива врагов внутри каждого элемента waves
                    BlobBuilderArray<EnemyBlobData> enemiesArray = builder.Allocate(ref wavesArray[w].Enemies, enemyTypesCount);

                    // заполняем вложенный blob массив параметров врагов
                    for (int e = 0; e < enemyTypesCount; e++)
                    {
                        // берем параметры врага из списка инспектора
                        var enemySrc = (waveSrc.enemies != null && e < waveSrc.enemies.Count) ? waveSrc.enemies[e] : new EnemyConfig();

                        // записываем скорость во вложенный blob массив
                        enemiesArray[e].Speed = enemySrc.speed;

                        // записываем здоровье во вложенный blob массив
                        enemiesArray[e].Health = enemySrc.health;

                        // записываем стамину во вложенный blob массив
                        enemiesArray[e].Stamina = enemySrc.stamina;
                    }
                }

                // создаем ссылку blobassetreference на заполненный блок памяти
                BlobAssetReference<WavesBlobRoot> blobRef = builder.CreateBlobAssetReference<WavesBlobRoot>(Unity.Collections.Allocator.Persistent);

                // метод addblobasset регистрирует blob и память по окончании жизни мира освободится сама
                AddBlobAsset(ref blobRef, out var hash);

                // добавляем созданный blobassetreference в компонент entity спавнера
                AddComponent(spawnerEntity, new WavesBlobReference
                {
                    Value = blobRef // сохраняем ссылку на созданный blob
                });
            }

            // генерируем случайный сид для unity mathematics random с помощью unityengine random
            uint randomSeed = (uint)UnityEngine.Random.Range(1, int.MaxValue);

            // запекаем компонент состояния спавнера с инициализированным генератором случайных чисел
            AddComponent(spawnerEntity, new SpawnerStateData
            {
                Timer = 0f, // сбрасываем стартовый таймер
                CurrentWaveIndex = 0, // начинаем с первой волны
                SpawnedInCurrentWave = 0, // сбрасываем счетчик заспавненных
                Random = Unity.Mathematics.Random.CreateFromIndex(randomSeed) // создаем генератор из сида
            });

            // чтобы добавить dynamicbuffer на entity в baker скрипте используется метод addbuffer
            AddBuffer<SpawnLogBufferElement>(spawnerEntity);
        }
    }
}
