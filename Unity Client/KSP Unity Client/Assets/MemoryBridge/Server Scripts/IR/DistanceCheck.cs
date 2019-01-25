using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[ExecuteInEditMode]
public class DistanceCheck : MonoBehaviour
{
    public Transform point0, point1;
    float distance;
    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (point1 & point0)
            distance = Vector3.Distance(point0.position, point1.position);
    }
}
