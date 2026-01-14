using Latios;
using Latios.Calci;
using Latios.Transforms;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public partial struct QvvsTestParentingSystem : ISystem
{
    LatiosWorldUnmanaged latiosWorld;
    NativeArray<Entity>  entities;

    struct Tag : IComponentData { }

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        latiosWorld = state.GetLatiosWorldUnmanaged();

        var entity = state.EntityManager.CreateEntity();
        state.EntityManager.AddComponent<Tag>(entity);
        entities = state.EntityManager.Instantiate(entity, 1000, Allocator.Persistent);
        state.InitSystemRng((FixedString128Bytes)"QvvsTestParentingSystem");
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        entities.Dispose();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var rng = state.GetMainThreadRng();

        for (int i = 0; i < 50; i++)
        {
            var childI  = rng.NextInt(0, entities.Length);
            var parentI = rng.NextInt(0, entities.Length);
            if (parentI == childI)
                continue;

            var child  = entities[childI];
            var parent = entities[parentI];
            state.EntityManager.AddChild(parent, child);
            var rr     = state.EntityManager.GetComponentData<RootReference>(child);
            var handle = rr.ToHandle(state.EntityManager);
            if (state.EntityManager.HasBuffer<EntityInHierarchy>(parent))
            {
                var eih = state.EntityManager.GetBuffer<EntityInHierarchy>(parent);
                if (eih[eih.Length - 1].firstChildIndex != eih.Length)
                {
                    UnityEngine.Debug.LogError($"EntityInHierarchy last child's firstChildIndex is wrong! Culprit: {parent.ToFixedString()}");
                }
                var leg = state.EntityManager.GetBuffer<LinkedEntityGroup>(parent);
                if (eih.Length != leg.Length)
                {
                    UnityEngine.Debug.LogError($"EntityInHierarchy and LinkedEntityGroup do not match! Culprit: {parent.ToFixedString()}");
                }
            }
            if (state.EntityManager.HasBuffer<EntityInHierarchyCleanup>(parent))
            {
                UnityEngine.Debug.LogError($"Nothing should have EntityInHierarchyCleanup! Culprit: {parent.ToFixedString()}");
            }
            bool success  = handle.entity == child;
            success      &= handle.bloodParent.entity == parent;
            if (success)
                UnityEngine.Debug.Log("Parenting successful");
            else
                UnityEngine.Debug.LogError($"Things went bad! Child: {child.ToFixedString()}, Parent: {parent.ToFixedString()}");
        }
    }
}

