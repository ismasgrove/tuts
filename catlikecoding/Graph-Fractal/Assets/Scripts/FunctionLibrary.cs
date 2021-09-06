using UnityEngine;

using static UnityEngine.Mathf;

public static class FunctionLibrary
{
    static Function[] functions = { Wave, MultiWave, Ripple, Sphere, Torus };
    public delegate Vector3 Function(float u, float v, float t);
    public enum FunctionName { Wave, MultiWave, Ripple, Sphere, Torus }

    public static Function GetFunction(FunctionName name) => functions[(int)name];
    public static Vector3 Wave (float u, float v, float t)
    {
        return new Vector3(u, Sin(PI * (u  + v + t)), v);
    }

    public static Vector3 MultiWave(float u, float v, float t)
    {
        var y = Sin(PI * (u + t * 0.5f)) + Sin(2f * PI * (v + t)) * 0.5f;
        y += Sin(PI * (u + v + 0.25f * t));
        return new Vector3(u, y * (1f / 2.5f), v);
    }

    public static Vector3 Ripple(float u, float v, float t)
    {
        float d = Sqrt(v * v + u * u);
        return new Vector3(u, Sin(PI * (4f * d - t)) / (1f + (10f * d)), v);
    }

    public static Vector3 Sphere(float u, float v, float t)
    {
        float r = 0.9f + 0.1f * Sin(PI * (12f * u + 8f * v + t));
        float s = r * Cos(0.5f * PI * v);
        return new Vector3(
            s * Sin(PI * u)
            , r * Sin(0.5f * PI * v)
            , s * Cos(PI * u)
            );
    }

    public static Vector3 Torus(float u, float v, float t)
    {
        float r1 = (7 + Sin(PI * (8 * u + t * 0.5f))) / 10f
            , r2 = (3 + Sin(PI * (16f * u + 8f * v + 3f * t))) / 20f;
        float s = r1 + r2 * Cos(0.5f * PI * v);
        return new Vector3(
            s * Sin(PI * u)
            , r2 * Sin(PI * v)
            , s * Cos(PI * u)
            );
    }

    public static FunctionName GetNextFunctionName(FunctionName name) =>
        (int)name < functions.Length - 1 ? name + 1 : 0;

    public static FunctionName GetNewRandomFunctionName(FunctionName name)
    {
        var choice = (FunctionName)Random.Range(1, functions.Length);
        return choice == name ? 0 : choice;
    }

    public static int FunctionCount => functions.Length;

    public static Vector3 Morph(float u, float v, float t, Function from, Function to, float progress)
    {
        return Vector3.LerpUnclamped(from(u, v, t), to(u, v, t), SmoothStep(0f, 1f, progress));
    }
}