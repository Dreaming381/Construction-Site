using Latios;
using Latios.Transforms;
using Unity.Burst;
using Unity.Burst.Intrinsics;
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
            transformAspectHandle = new TransformAspectRootHandle(SystemAPI.GetComponentLookup<WorldTransform>(false),
                                                                  SystemAPI.GetBufferTypeHandle<EntityInHierarchy>(true),
                                                                  SystemAPI.GetBufferTypeHandle<EntityInHierarchyCleanup>(true),
                                                                  SystemAPI.GetEntityStorageInfoLookup()),
            dt = Time.DeltaTime,
        }.ScheduleParallel();
    }

    [WithAll(typeof(WorldTransform), typeof(EntityInHierarchy))]
    [BurstCompile]
    partial struct Job : IJobEntity, IJobEntityChunkBeginEnd
    {
        public TransformAspectRootHandle transformAspectHandle;

        public float dt;

        public void Execute([EntityIndexInChunk] int indexInChunk, in Spinner spinner)
        {
            var transform = transformAspectHandle[indexInChunk];
            var rotation  = quaternion.AxisAngle(math.up(), dt * spinner.spinSpeed);
            transform.RotateWorld(rotation);
        }

        public bool OnChunkBegin(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            transformAspectHandle.SetupChunk(in chunk);
            return true;
        }

        public void OnChunkEnd(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask, bool chunkWasExecuted)
        {
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

