using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RobotManager : MonoBehaviour
{
    protected List<ServoLimb> legs = new List<ServoLimb>();
    protected List<ServoLimb> arms = new List<ServoLimb>();

    protected MemoryBridge memoryBridge;
    protected ServoLimb[] limbs;

    public AnimationCurve gaitCurve = AnimationCurve.EaseInOut(-1, 0, 1, 0);
    public enum RobotStatus { Deactivated, Idle, AdjustingGaitPosition, Walking }
    public RobotStatus robotStatus = RobotStatus.Deactivated;

    public Transform baseTargets { get; protected set; }

    public VesselControl vessel { get; protected set; }
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
   protected virtual void Update()
    {
        memoryBridge.StartUpdate();

        foreach (var limb in limbs)
        {
            limb.MirrorServos();
        }

        if (Time.frameCount > 20 && robotStatus == RobotStatus.Deactivated)
        {
            ActivateIK();
        }
    }

    protected virtual void ActivateIK()
    {
        if(legs.Count > 0)
        {
            robotStatus = RobotStatus.Idle;
            //create base target parent for all limb base targets
            baseTargets = new GameObject("Base Targets").transform;
            baseTargets.SetParent(GameObject.Find("Vessel Offset").transform);
            baseTargets.localEulerAngles = Vector3.zero;
            //set the base height to the average height of all limb bases
            Vector3 baseOffset = Vector3.zero;
            foreach (var leg in legs)
            {
                baseOffset += memoryBridge.vesselControl.adjustedGimbal.InverseTransformPoint(leg.servos[0].transform.position);
            }
            baseOffset /= legs.Count;
            baseTargets.transform.position = baseOffset;
            SetBaseHeights(baseTargets.position.y - vessel.ground.position.y, false);
            baseTargets.SetParent(null);

            //activate Ik and create leg base targets
            foreach (var limb in legs)
            {
                limb.ActivateIK();
                limb.CreateBaseTarget();
                limb.CreateGait(true, gaitCurve);
            }
        }
    }

    protected float targetBaseHeight;
    protected float baseHeight;
    public void SetBaseHeights(float newHeight, bool lerp = true)
    {
        Debug.Log("______________Adjust base height");
        targetBaseHeight = newHeight;
        if (!lerp)
        {
            baseHeight = newHeight;
        }
    }

    public void CalculateTwoServoIK(Servo servo0, Servo servo1, Vector3 target, Transform endPoint)
    {
        var targetOffset = servo0.servoBase.InverseTransformPoint(target);
        var angle0 = Math.Atan2(targetOffset.z, targetOffset.y);
        angle0 *= (180 / Math.PI);

        var servo1_0Dist = Vector3.Distance(servo1.transform.position, servo0.transform.position);

       // var localPos = servo1.transform.InverseTransformPoint(endPoint.position);
       // localPos.x = 0;
       // var globalPos = servo1.transform.TransformPoint(localPos);
        var servo0_GroundPointDist = Vector3.Distance(servo0.transform.position, target);// endPoint.position);

        var servo1_GroundPointDist = Vector3.Distance(servo1.transform.position, endPoint.position);// target);

        float angle1 = LawOfCosines(servo1_0Dist, servo1_GroundPointDist, servo0_GroundPointDist);

        //if (servo1.invert)
        //{
        //    angle1 -= (float)angle0;
        //}
        //else
        //{
            angle1 += (float)angle0;
        //}
        if (!float.IsNaN(angle1))
        {
            //Debug.Log(servo0.groupOffsets[servo1.gameObject]);
            servo0.SetServo(angle1 - servo0.groupOffsets[servo1.gameObject]);
        }
        else
        {
            CalculateSingleServoIK(servo0, target, endPoint);
        }
        var footOffset = servo0.transform.InverseTransformPoint(endPoint.position);
        // xOffset = footOffset.z;
        // Vector3 targetOffset;

        //  targetOffset = servo1.servoBase.InverseTransformPoint(target);

        // var angle = Math.Atan2(targetOffset.z, targetOffset.y);
        // angle *= (180 / Math.PI);

        CalculateSingleServoIK(servo1, target, endPoint);

       //// Debug.Log(servo1.groupOffsets[endPoint.gameObject]);
       // servo1.SetServo((float)angle - servo1.groupOffsets[endPoint.gameObject]);
    }

    public void CalculateSingleServoIK(Servo servo, Vector3 target, Transform endPoint)
    {
        var targetOffset = servo.servoBase.InverseTransformPoint(target);
        var angle = Math.Atan2(targetOffset.z, targetOffset.y);
        angle *= (180 / Math.PI);
        // Debug.Log(servo1.groupOffsets[endPoint.gameObject]);
        servo.SetServo((float)angle - servo.groupOffsets[endPoint.gameObject]);
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
