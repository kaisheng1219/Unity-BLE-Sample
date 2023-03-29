using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

internal class MainThread : MonoBehaviour
{
    internal static MainThread wkr;
    Queue<Action> jobs = new();

    void Awake()
    {
        wkr = this;
        DontDestroyOnLoad(wkr);
    }

    void Update()
    {
        while (jobs.Count > 0)
            jobs.Dequeue().Invoke();
    }

    internal void AddJob(Action newJob)
    {
        jobs.Enqueue(newJob);
    }
}
