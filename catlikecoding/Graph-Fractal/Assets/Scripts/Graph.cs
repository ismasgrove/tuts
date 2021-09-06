using UnityEngine;

public class Graph : MonoBehaviour
{
    [SerializeField]
    Transform pointPrefab;

    [SerializeField, Min(0f)]
    float functionDuration = 1f, transitionDuration = 1f;

    [SerializeField, Range(10, 100)]
    int resolution = 10;


    public enum TransitionMode { Cycle, Random };

    [SerializeField] TransitionMode transitionMode;

    Transform[] points;
    float duration;
    bool transitioning;
    FunctionLibrary.FunctionName transitionFunction;

    FunctionLibrary.FunctionName function;


    private void Awake()
    {
        float step = (float)(2f / resolution);
        var scale = Vector3.one * step;
        points = new Transform[resolution * resolution];
        for (int i = 0; i < points.Length; i++)
        {
            var point = points[i] = Instantiate(pointPrefab);
            point.SetParent(transform, false);
            point.localScale = scale;

        }
    }
    private void Update()
    {
        duration += Time.deltaTime;
        if (transitioning)
        {
            if (duration >= transitionDuration)
            {
                duration -= transitionDuration;
                transitioning = false;
            }
        }
        else if (duration >= functionDuration)
        {
            duration -= functionDuration;
            transitioning = true;
            transitionFunction = function;
            PickNextFunction();
        }

        if (transitioning)
            UpdateFunctionTransition();
        else
            UpdateFunction();

    }

    private void PickNextFunction()
    {
        function = transitionMode == TransitionMode.Cycle ?
                FunctionLibrary.GetNextFunctionName(function)
                : FunctionLibrary.GetNewRandomFunctionName(function);
    }

    private void UpdateFunction ()
    {
        float time = Time.time;
        float step = (float)2f / resolution;
        float v = (0.5f) * step - 1f;
        FunctionLibrary.Function f = FunctionLibrary.GetFunction(function);
        for (int i = 0, x = 0, z = 0; i < points.Length; i++, x++)
        {
            if (x == resolution) { x = 0; z++; v = (z + 0.5f) * step - 1f; }
            float u = (x + 0.5f) * step - 1f;
            points[i].localPosition = f(u, v, time);
        }
    }
    private void UpdateFunctionTransition()
    {
        float time = Time.time;
        float step = 2f / resolution;
        float v = (0.5f) * step - 1f;
        float progress = duration / transitionDuration;
        FunctionLibrary.Function
            from = FunctionLibrary.GetFunction(transitionFunction)
            , to = FunctionLibrary.GetFunction(function);


        for (int i = 0, x = 0, z = 0; i < points.Length; i++, x++)
        {
            if (x == resolution) { x = 0; z++; v = (z + 0.5f) * step - 1f; }
            float u = (x + 0.5f) * step - 1f;
            points[i].localPosition = FunctionLibrary.Morph(u, v, time, from, to, progress);
        }
    }
}