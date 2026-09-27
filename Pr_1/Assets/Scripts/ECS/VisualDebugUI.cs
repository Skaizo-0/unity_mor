using UnityEngine;
using Unity.Entities;


public class VisualDebugUI : MonoBehaviour
{
    private void OnGUI()
    {
        if (World.DefaultGameObjectInjectionWorld == null) return;
        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        // находим entity с компонентами здоровья и стамины
        var query = entityManager.CreateEntityQuery(typeof(HealthData), typeof(StaminaData));
        var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);

        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 15,
            fontStyle = FontStyle.Bold
        };
        style.normal.textColor = Color.white;

        
        GUI.Box(new Rect(10, 10, 260, 100), "Состояние Юнита");

        if (entities.Length == 0)
        {
            style.normal.textColor = Color.red;
            GUI.Label(new Rect(20, 40, 240, 50), "Юнит погиб\n(Здоровье <= 0, entity удален)", style);
            entities.Dispose();
            return;
        }

        var entity = entities[0];
        var health = entityManager.GetComponentData<HealthData>(entity);
        var stamina = entityManager.GetComponentData<StaminaData>(entity);

        
        style.normal.textColor = Color.green;
        GUI.Label(new Rect(20, 35, 230, 25), $"Здоровье: {health.Current:F0} / {health.Max:F0}", style);

        
        string statusText = stamina.IsExhausted ? "ОТДЫХ (до 50%)" : "БЕГ";
        style.normal.textColor = stamina.IsExhausted ? Color.yellow : Color.cyan;
        GUI.Label(new Rect(20, 65, 230, 25), $"Стамина: {stamina.Current:F0} / {stamina.Max:F0} [{statusText}]", style);

        entities.Dispose();
    }
}
