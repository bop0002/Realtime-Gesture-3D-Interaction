using System;
using UnityEngine;

public class Line : MonoBehaviour
{
    LineRenderer lineRenderer;
    private Transform _startPoint;
    private Transform _endPoint;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
    }

    public void Init(Transform startPoint,Transform endPoint,String name)
    { 
        _startPoint = startPoint;
        _endPoint = endPoint;
        this.name = name;
    }

    void Update()
    {
        if(_startPoint!=null||_endPoint!=null)
        {
            lineRenderer.SetPosition(0,_startPoint.position);
            lineRenderer.SetPosition(1,_endPoint.position);
        }
        else
        {
            Debug.Log($"{this.name} line points is null");
        }
    }
}
