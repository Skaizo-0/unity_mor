using UnityEngine;
using Unity.Entities;


public class TargetAuthoring : MonoBehaviour
{
    public class Baker : Baker<TargetAuthoring>
    {
        public override void Bake(TargetAuthoring authoring)
        {
            // объявляем entity в который превратится gameobject в рантайме
            // transformusageflags dynamic как параметр метода getentity 
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);

            // методом addcomponent запекаем на entity нужный компонент тег
            AddComponent<TargetTag>(entity);
        }
    }
}
