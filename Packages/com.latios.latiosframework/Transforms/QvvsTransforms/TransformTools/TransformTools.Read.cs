using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Latios.Transforms
{
    public static partial class TransformTools
    {
        public static partial class Unsafe
        {
            public static TransformQvs LocalTransformFrom(Entity entity, in RootReference rootReference, in WorldTransform worldTransform,
                                                          EntityManager entityManager)
            {
                DynamicBuffer<EntityInHierarchy> hierarchy;
                if (entityManager.HasBuffer<EntityInHierarchy>(rootReference.rootEntity))
                    hierarchy = entityManager.GetBuffer<EntityInHierarchy>(rootReference.rootEntity, true);
                else
                    hierarchy       = entityManager.GetBuffer<EntityInHierarchyCleanup>(rootReference.rootEntity, true).Reinterpret<EntityInHierarchy>();
                var parentIndex     = hierarchy[rootReference.indexInHierarchy].parentIndex;
                var parentEntity    = hierarchy[parentIndex].entity;
                var parentTransform = entityManager.GetComponentData<WorldTransform>(parentEntity);
                return qvvs.inversemul(in parentTransform.worldTransform, in worldTransform.worldTransform);
            }

            public static TransformQvs LocalTransformFrom(Entity entity,
                                                          in RootReference rootReference,
                                                          in WorldTransform worldTransform,
                                                          ref BufferLookup<EntityInHierarchy> entityInHierarchyLookupRO,
                                                          ref BufferLookup<EntityInHierarchyCleanup> entityInHierarchyCleanupLookupRO)
            {
                DynamicBuffer<EntityInHierarchy> hierarchy;
                if (entityInHierarchyLookupRO.HasBuffer(rootReference.rootEntity))
                    hierarchy = entityInHierarchyLookupRO[rootReference.rootEntity];
                else
                    hierarchy       = entityInHierarchyCleanupLookupRO[rootReference.rootEntity].Reinterpret<EntityInHierarchy>();
                var parentIndex     = hierarchy[rootReference.indexInHierarchy].parentIndex;
                var parentEntity    = hierarchy[parentIndex].entity;
                var parentTransform = worldTransformLookupRO[parentEntity];
                return qvvs.inversemul(in parentTransform.worldTransform, in worldTransform.worldTransform);
            }
        }

        public static TransformQvs LocalTransformFrom(Entity entity, EntityManager entityManager)
        {
            var worldTransform = entityManager.GetComponentData<WorldTransform>(entity);
            if (entityManager.HasComponent<RootReference>(entity))
            {
                var                              rootReference = entityManager.GetComponentData<RootReference>(entity);
                DynamicBuffer<EntityInHierarchy> hierarchy;
                if (entityManager.HasBuffer<EntityInHierarchy>(rootReference.rootEntity))
                    hierarchy = entityManager.GetBuffer<EntityInHierarchy>(rootReference.rootEntity, true);
                else
                    hierarchy       = entityManager.GetBuffer<EntityInHierarchyCleanup>(rootReference.rootEntity, true).Reinterpret<EntityInHierarchy>();
                var parentIndex     = hierarchy[rootReference.indexInHierarchy].parentIndex;
                var parentEntity    = hierarchy[parentIndex].entity;
                var parentTransform = entityManager.GetComponentData<WorldTransform>(parentEntity);
                return qvvs.inversemul(in parentTransform.worldTransform, in worldTransform.worldTransform);
            }
            return new TransformQvs(worldTransform.position, worldTransform.rotation, worldTransform.scale);
        }

        public static TransformQvs LocalTransformFrom(Entity entity,
                                                      ref ComponentLookup<RootReference> rootReferenceLookupRO,
                                                      ref ComponentLookup<WorldTransform> worldTransformLookupRO,
                                                      ref BufferLookup<EntityInHierarchy> entityInHierarchyLookupRO,
                                                      ref BufferLookup<EntityInHierarchyCleanup> entityInHierarchyCleanupLookupRO)
        {
            var worldTransform = worldTransformLookupRO[entity];
            if (rootReferenceLookupRO.HasComponent(entity))
            {
                var                              rootReference = rootReferenceLookupRO[entity];
                DynamicBuffer<EntityInHierarchy> hierarchy;
                if (entityInHierarchyLookupRO.HasBuffer(rootReference.rootEntity))
                    hierarchy = entityInHierarchyLookupRO[rootReference.rootEntity];
                else
                    hierarchy       = entityInHierarchyCleanupLookupRO[rootReference.rootEntity].Reinterpret<EntityInHierarchy>();
                var parentIndex     = hierarchy[rootReference.indexInHierarchy].parentIndex;
                var parentEntity    = hierarchy[parentIndex].entity;
                var parentTransform = worldTransformLookupRO[parentEntity];
                return qvvs.inversemul(in parentTransform.worldTransform, in worldTransform.worldTransform);
            }
            return new TransformQvs(worldTransform.position, worldTransform.rotation, worldTransform.scale);
        }
    }
}
