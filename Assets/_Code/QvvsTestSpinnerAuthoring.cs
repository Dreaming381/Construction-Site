using Latios.Authoring;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class QvvsTestSpinnerAuthoring : MonoBehaviour
{
    public float spinSpeed = 120f;
}

public class QvvsTestSpinnerAuthoringBaker : Baker<QvvsTestSpinnerAuthoring>
{
    public override void Bake(QvvsTestSpinnerAuthoring authoring)
    {
        var entity                                   = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new Spinner { spinSpeed = math.radians(authoring.spinSpeed) });
    }
}

