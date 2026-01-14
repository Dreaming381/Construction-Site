using Latios;
using Latios.Transforms;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

using static Unity.Entities.SystemAPI;

[BurstCompile]
public partial struct QvvsTestSpinSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        new Job
        {
            transformLookup = GetComponentLookup<WorldTransform>(),
            esil            = GetEntityStorageInfoLookup(),
            dt              = Time.DeltaTime,
        }.ScheduleParallel();
    }

    [BurstCompile]
    partial struct Job : IJobEntity
    {
        public TransformsComponentLookup<WorldTransform> transformLookup;
        public EntityStorageInfoLookup                   esil;

        public float dt;

        public void Execute(Entity entity, in DynamicBuffer<EntityInHierarchy> hierarchyBuffer, in Spinner spinner)
        {
            var key      = TransformsKey.CreateFromExclusivelyAccessedRoot(entity, esil);
            var rotation = quaternion.AxisAngle(math.up(), dt * spinner.spinSpeed);
            var root     = hierarchyBuffer.GetRootHandle();
            TransformTools.RotateWorld(root, rotation, key, ref transformLookup, ref esil);
        }
    }
}

public partial class QvvsTestRootSuperSystem : RootSuperSystem
{
    protected override void CreateSystems()
    {
        GetOrCreateAndAddUnmanagedSystem<QvvsTestSpinSystem>();
        GetOrCreateAndAddUnmanagedSystem<QvvsTestParentingSystem>();
    }
}

