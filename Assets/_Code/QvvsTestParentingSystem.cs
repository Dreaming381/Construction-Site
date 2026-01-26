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

    struct CheckParent : IComponentData
    {
        public Entity parent;
    }

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        latiosWorld = state.GetLatiosWorldUnmanaged();

        var entity = state.EntityManager.CreateEntity();
        state.EntityManager.AddComponent<CheckParent>(entity);
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

            // Check we don't try to assign child's parent as one of child's own descendants
            if (state.EntityManager.HasComponent<RootReference>(parent))
            {
                var parentHandle = state.EntityManager.GetComponentData<RootReference>(parent).ToHandle(state.EntityManager);
                if (parentHandle.root.entity == child)
                    continue;
                if (state.EntityManager.HasComponent<RootReference>(child))
                {
                    var childRootRef = state.EntityManager.GetComponentData<RootReference>(child);
                    if (childRootRef.rootEntity == parentHandle.root.entity && childRootRef.indexInHierarchy < parentHandle.indexInHierarchy)
                    {
                        bool fail = false;
                        for (var h = parentHandle.bloodParent; !h.isRoot; h = h.bloodParent)
                        {
                            if (h.indexInHierarchy == childRootRef.indexInHierarchy)
                            {
                                fail = true;
                                break;
                            }
                        }
                        if (fail)
                            continue;
                    }
                }
            }

            state.EntityManager.AddChild(parent, child);
            state.EntityManager.SetComponentData(child, new CheckParent { parent = parent });
            var rr                                                               = state.EntityManager.GetComponentData<RootReference>(child);
            var handle                                                           = rr.ToHandle(state.EntityManager);
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

            if (state.EntityManager.GetComponentData<CheckParent>(handle.root.entity).parent != Entity.Null)
                UnityEngine.Debug.LogError("A root was supposed to have a parent.");
            for (int j = 1; j < handle.totalInHierarchy; j++)
            {
                var h = handle.GetFromIndexInHierarchy(j);
                var p = h.bloodParent;
                if (state.EntityManager.GetComponentData<CheckParent>(h.entity).parent != p.entity)
                    UnityEngine.Debug.LogError("A child has the wrong parent index in the hierarchy.");
                var firstChildIndex = p.bloodChildren[0].indexInHierarchy;
                if (h.indexInHierarchy < firstChildIndex || h.indexInHierarchy >= firstChildIndex + p.bloodChildren.length)
                    UnityEngine.Debug.LogError("A child is not contained within its parent's child span.");
            }
        }

        /*for (int i = 0; i < 5; i++)
           {
            var child = entities[rng.NextInt(0, entities.Length)];
            if (state.EntityManager.GetComponentData<CheckParent>(child).parent != Entity.Null)
            {
                var  handle = state.EntityManager.GetComponentData<RootReference>(child).ToHandle(state.EntityManager);
                var  root   = handle.root.entity;
                bool detach = rng.NextBool();
                if (detach)
                {
                    var newParent = handle.bloodParent.entity;
                    foreach (var orphan in handle.bloodChildren)
                    {
                        state.EntityManager.SetComponentData(orphan.entity, new CheckParent { parent = newParent});
                    }
                }
                state.EntityManager.RemoveFromHierarchy(handle, detach);
                state.EntityManager.SetComponentData(child, new CheckParent { parent = Entity.Null });

                if (state.EntityManager.HasBuffer<EntityInHierarchy>(root))
                {
                    handle = state.EntityManager.GetBuffer<EntityInHierarchy>(root).GetRootHandle();
                    for (int j = 1; j < handle.totalInHierarchy; j++)
                    {
                        var h = handle.GetFromIndexInHierarchy(j);
                        var p = h.bloodParent;
                        if (state.EntityManager.GetComponentData<CheckParent>(h.entity).parent != p.entity)
                            UnityEngine.Debug.LogError("A child has the wrong parent index in the hierarchy.");
                        var firstChildIndex = p.bloodChildren[0].indexInHierarchy;
                        if (h.indexInHierarchy < firstChildIndex || h.indexInHierarchy >= firstChildIndex + p.bloodChildren.length)
                            UnityEngine.Debug.LogError("A child is not contained within its parent's child span.");
                    }
                }
            }
           }*/
    }
}

