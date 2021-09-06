using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

using static Unity.Mathematics.math;
using quaternion = Unity.Mathematics.quaternion;
using Random = UnityEngine.Random;

public class Fractal : MonoBehaviour
{
    private struct FractalPart
    {
        public float3 worldPosition;
        public quaternion rotation, worldRotation;
        public float spinAngle, maxSagAngle, spinVelocity;
    }

    static quaternion[] rotations =
    {
        quaternion.identity,
        quaternion.RotateZ(-0.5f * PI), quaternion.RotateZ(0.5f * PI),
        quaternion.RotateX(0.5f * PI), quaternion.RotateX(-0.5f * PI)
    };

    [SerializeField, Range(3, 8)] int depth = 4;
    [SerializeField] Material material;
    [SerializeField] Mesh mesh, leafMesh;
    [SerializeField] Gradient gradientA, gradientB;
    [SerializeField] Color leafColorA, leafColorB;
    [SerializeField, Range(0, 90f)] float maxSagAngleA = 15f, maxSagAngleB = 25f;
    [SerializeField, Range(0, 90f)] float spinSpeedA = 20f, spinSpeedB = 25f;
    [SerializeField, Range(0f, 1f)] float reverseSpinChance = 0.25f;

    NativeArray<FractalPart>[] parts;
    NativeArray<float3x4>[] matrices;
    ComputeBuffer[] matricesBuffers;

    Vector4[] sequenceNumbers;

    static readonly int matricesId = Shader.PropertyToID("_Matrices")
        , colorAId = Shader.PropertyToID("_ColorA")
        , colorBId = Shader.PropertyToID("_ColorB")
        , sequenceNumbersId = Shader.PropertyToID("_SequenceNumbers");
    static MaterialPropertyBlock propertyBlock;

    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    private struct UpdateFractalLevelJob : IJobFor
    {
        public float deltaTime;
        public float scale;
        [ReadOnly]
        public NativeArray<FractalPart> parents;
        public NativeArray<FractalPart> parts;
        [WriteOnly]
        public NativeArray<float3x4> matrices;
        public void Execute(int i)
        {
            var parent = parents[i / 5];
            var part = parts[i];

            part.spinAngle += part.spinVelocity * deltaTime;

            float3 upAxis = mul(mul(parent.worldRotation, part.rotation), up());
            float3 sagAxis = cross(up(), upAxis);
            float sagMagnitude = length(sagAxis);
            quaternion baseRotation;

            if (sagMagnitude > 0f)
            {
                quaternion sagRotation =
                    quaternion.AxisAngle(normalize(sagAxis), part.maxSagAngle * sagMagnitude);
                baseRotation = mul(sagRotation, parent.worldRotation);
            }
            else
            {
                baseRotation = parent.worldRotation;
            }


            part.worldRotation = mul(baseRotation,
                mul(part.rotation, quaternion.RotateY(part.spinAngle)));

            part.worldPosition =
                parent.worldPosition + mul(part.worldRotation,
                float3(0f, 1.5f * scale, 0f));
            parts[i] = part;

            var r = float3x3(part.worldRotation) * scale;
            matrices[i] = float3x4(r.c0, r.c1, r.c2, part.worldPosition);
        }
    }


    private void OnEnable()
    {
        parts = new NativeArray<FractalPart>[depth];
        matrices = new NativeArray<float3x4>[depth];
        matricesBuffers = new ComputeBuffer[depth];
        sequenceNumbers = new Vector4[depth];

        int stride = 12 * sizeof(float);

        for (int i=0, length = 1; i < parts.Length; i++, length *= 5)
        {
            parts[i] = new NativeArray<FractalPart>(length, Allocator.Persistent);
            matrices[i] = new NativeArray<float3x4>(length, Allocator.Persistent);
            matricesBuffers[i] = new ComputeBuffer(length, stride);
            sequenceNumbers[i] = new Vector4(Random.value, Random.value, Random.value, Random.value);
        }
        parts[0][0] = CreatePart(0);
        for (int li=1; li<parts.Length; li++)
        {
            var levelParts = parts[li];
            for (int fpi = 0, ci = 0; fpi < levelParts.Length; fpi++, ci++)
            {
                if (ci >= 5) ci = 0;
                levelParts[fpi] = CreatePart(ci);
            }
        }

        propertyBlock ??= new MaterialPropertyBlock();
    }

    private void OnDisable()
    {
        for (int i = 0; i < matricesBuffers.Length; i++)
        {
            matricesBuffers[i].Release();
            parts[i].Dispose();
            matrices[i].Dispose();
        }
        parts = null; matrices = null; matricesBuffers = null; sequenceNumbers = null;
    }

    private void OnValidate()
    {
        if (parts != null && enabled)
        {
            OnDisable();
            OnEnable();
        }
    }


    private void Update()
    {
        float deltaTime = Time.deltaTime;
        float objectScale = transform.lossyScale.x;
        var rootPart = parts[0][0];
        rootPart.spinAngle += rootPart.spinVelocity * deltaTime;
        rootPart.worldRotation = mul(transform.rotation,
            mul(rootPart.rotation, quaternion.RotateY(rootPart.spinAngle)));
        rootPart.worldPosition = transform.position;
        parts[0][0] = rootPart;
        var r = float3x3(rootPart.worldRotation) * objectScale;
        matrices[0][0] = float3x4(
            r.c0, r.c1, r.c2, rootPart.worldPosition
            );

        int leafIndex = matricesBuffers.Length - 1;

        float scale = objectScale;

        JobHandle jobHandle = default;

        for (int li = 1; li < parts.Length; li++)
        {
            scale *= 0.5f;

            jobHandle = new UpdateFractalLevelJob
            {
                deltaTime = deltaTime,
                scale = scale,
                parents = parts[li - 1],
                parts = parts[li],
                matrices = matrices[li]
            }.ScheduleParallel(parts[li].Length, 5, jobHandle);
        }

        jobHandle.Complete();

        var bounds = new Bounds(rootPart.worldPosition, objectScale * 3f * Vector3.one);
        for (int i = 0; i < matricesBuffers.Length; i++)
        {
            ComputeBuffer buffer = matricesBuffers[i];
            buffer.SetData(matrices[i]);


            float gradientInterpolatr = i / (matricesBuffers.Length - 2f);

            Color colorA, colorB;
            Mesh instanceMesh;

            if (i == leafIndex)
            {
                colorA = leafColorA;
                colorB = leafColorB;
                instanceMesh = leafMesh;
            }
            else
            {
                colorA = gradientA.Evaluate(gradientInterpolatr);
                colorB = gradientB.Evaluate(gradientInterpolatr);
                instanceMesh = mesh;
            }

            propertyBlock.SetColor(colorAId, colorA);
            propertyBlock.SetColor(colorBId, colorB);

            propertyBlock.SetVector(sequenceNumbersId, sequenceNumbers[i]);
            propertyBlock.SetBuffer(matricesId, buffer);
            Graphics.DrawMeshInstancedProcedural(instanceMesh, 0, material, bounds, buffer.count, propertyBlock);
        }
    }

    private FractalPart CreatePart(int childIndex) => new FractalPart {
        rotation = rotations[childIndex],
        maxSagAngle = radians(Random.Range(maxSagAngleA, maxSagAngleB)),
        spinVelocity = (Random.value < reverseSpinChance ? -1f : 1f) *
            radians(Random.Range(spinSpeedA, spinSpeedB))
    };
    
}
