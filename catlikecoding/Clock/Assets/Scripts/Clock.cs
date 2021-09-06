using System;
using UnityEngine;

public class Clock : MonoBehaviour
{
    [SerializeField]
    Transform hoursPivot, minutesPivot, secondsPivot;

    private void Update()
    {
        var time = DateTime.Now.TimeOfDay;
        hoursPivot.localRotation = Quaternion.Euler(0f, 0f, -30 * (float) time.TotalHours);
        minutesPivot.localRotation = Quaternion.Euler(0f, 0f, -6f * (float) time.TotalMinutes);
        secondsPivot.localRotation = Quaternion.Euler(0f, 0f, -6f * (float) time.TotalSeconds);

    }

    private void Awake()
    {
        
    }

}