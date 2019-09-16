using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RobotManager : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void CalculateTwoServoIK(Servo servo0, Servo servo1, Vector3 target, Vector3 endPoint)
    {
        var targetOffset = servo0.servoBase.InverseTransformPoint(target);
        var angle0 = Math.Atan2(targetOffset.z, targetOffset.y);
        angle0 *= (180 / Math.PI);

        var servo1_0Dist = Vector3.Distance(servo1.transform.position, servo0.transform.position);

        var localPos = servo1.transform.InverseTransformPoint(endPoint);
        localPos.x = 0;
        var globalPos = servo1.transform.TransformPoint(localPos);
        var servo0_GroundPointDist = Vector3.Distance(servo1.transform.position, globalPos);

        var servo1_GroundPointDist = Vector3.Distance(servo0.transform.position, target);

        float angle1 = LawOfCosines(servo1_0Dist, servo0_GroundPointDist, servo1_GroundPointDist);

        //if (servo1.invert)
        //{
        //    angle1 -= (float)angle0;
        //}
        //else
        //{
        //    angle1 += (float)angle0;
        //}
        if (!float.IsNaN(angle1))
        {
            servo0.SetServo(angle1);
        }

        var footOffset = servo0.transform.InverseTransformPoint(endPoint);
       // xOffset = footOffset.z;
       // Vector3 targetOffset;

        targetOffset = servo1.servoBase.InverseTransformPoint(target);

        var angle = Math.Atan2(targetOffset.z, targetOffset.y);
        angle *= (180 / Math.PI);

        servo1.SetServo((float)angle);
    }


    public float LawOfCosines(float a, float b, float c)
    {
        var topEqu = (Math.Pow(c, 2) + Math.Pow(a, 2) - Math.Pow(b, 2));
        var bottomEqu = 2 * a * c;
        var angle = topEqu / bottomEqu;
        angle = (float)Math.Acos(angle);
        angle = (float)(angle * 180 / Math.PI);
        return (float)angle;
    }
}
