using UnityEngine; // подключаем движок unity для интерфейса ongui
using Unity.Entities; // подключаем доступ к миру ecs

// обычный monobehaviour скрипт для наглядного вывода состояния практики на экран игры
public class Practice2DebugUI : MonoBehaviour
{
    // метод ongui вызывается для отрисовки графического интерфейса
    private void OnGUI()
    {
        // проверяем существует ли главный мир сущностей ecs
        if (World.DefaultGameObjectInjectionWorld == null) return;

        // получаем доступ к entitymanager из мира по умолчанию
        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        // формируем запрос для поиска entity спавнера с компонентами состояния и blob ссылки
        var spawnerQuery = entityManager.CreateEntityQuery(typeof(SpawnerStateData), typeof(WavesBlobReference));

        // если спавнер еще не создан на сцене прерываем отрисовку
        if (spawnerQuery.CalculateEntityCount() == 0) return;

        // получаем единственный entity спавнера через getsingletonentity
        var spawnerEntity = spawnerQuery.GetSingletonEntity();

        // считываем текущие данные компонента состояния спавнера
        var spawnerState = entityManager.GetComponentData<SpawnerStateData>(spawnerEntity);

        // считываем ссылку на blob asset из компонента спавнера
        var blobRef = entityManager.GetComponentData<WavesBlobReference>(spawnerEntity);

        // создаем стиль для заголовка плашки
        GUIStyle headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 15, // задаем размер шрифта заголовка
            fontStyle = FontStyle.Bold // делаем шрифт жирным
        };
        headerStyle.normal.textColor = Color.white; // белый цвет текста заголовка

        // создаем стиль для обычных информационных строк
        GUIStyle textStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13 // задаем размер шрифта для строк
        };
        textStyle.normal.textColor = Color.yellow; // желтый цвет информационного текста

        // формируем запрос для подсчета живых юнитов на сцене
        var unitQuery = entityManager.CreateEntityQuery(typeof(MoveToTargetData), typeof(HealthData));

        // подсчитываем количество живых врагов в мире
        int livingUnits = unitQuery.CalculateEntityCount();

        // переменная для подсчета числа записей в dynamicbuffer
        int bufferCount = 0;

        // проверяем есть ли dynamicbuffer на entity спавнера
        if (entityManager.HasBuffer<SpawnLogBufferElement>(spawnerEntity))
        {
            // получаем dynamicbuffer как список элементов
            var buffer = entityManager.GetBuffer<SpawnLogBufferElement>(spawnerEntity);

            // считываем текущую длину динамического списка
            bufferCount = buffer.Length;
        }

        // обращаемся к данным blob дерева через разыменование ссылки
        ref WavesBlobRoot blobData = ref blobRef.Value.Value;

        // получаем общее количество волн запеченных в blob массиве
        int totalWaves = blobData.Waves.Length;

        // рисуем прямоугольную рамку окна интерфейса в углу экрана
        GUI.Box(new Rect(10, 10, 320, 160), "Практика 2: Спавнер Волн (ECS DOTS)");

        // выводим строку с номером текущей активной волны из blob
        GUI.Label(new Rect(20, 35, 300, 20), $"Волна: {spawnerState.CurrentWaveIndex + 1} из {totalWaves}", headerStyle);

        // проверяем идет ли еще процесс спавна волн
        if (spawnerState.CurrentWaveIndex < totalWaves)
        {
            // получаем ссылку на параметры текущей волны из blob массива
            ref WaveBlobData currentWave = ref blobData.Waves[spawnerState.CurrentWaveIndex];

            // выводим количество заспавненных врагов относительно лимита волны
            GUI.Label(new Rect(20, 55, 300, 20), $"Заспавнено в волне: {spawnerState.SpawnedInCurrentWave} / {currentWave.EnemyCount}", textStyle);
        }
        else
        {
            // если все волны пройдены меняем цвет на зеленый
            headerStyle.normal.textColor = Color.green;

            // выводим сообщение об успешном завершении всех волн
            GUI.Label(new Rect(20, 55, 300, 20), "ВСЕ ВОЛНЫ ЗАВЕРШЕНЫ!", headerStyle);
        }

        // настраиваем бирюзовый цвет для строки живых врагов
        textStyle.normal.textColor = Color.cyan;

        // выводим число активных сущностей врагов на сцене
        GUI.Label(new Rect(20, 80, 300, 20), $"Живых юнитов на сцене: {livingUnits}", textStyle);

        // настраиваем белый цвет для строки с информацией о dynamicbuffer
        textStyle.normal.textColor = Color.white;

        // выводим текущее число элементов сохраненных в dynamicbuffer
        GUI.Label(new Rect(20, 105, 300, 20), $"DynamicBuffer (всего записей): {bufferCount}", textStyle);

        // выводим краткую подсказку об используемых технологиях задания
        GUI.Label(new Rect(20, 125, 300, 25), "Спавн: ECB + Random | Данные: Blob", textStyle);
    }
}
