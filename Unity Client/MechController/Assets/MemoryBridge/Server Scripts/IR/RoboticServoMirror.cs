using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoboticServoMirror : RoboticServo {
    float _kspAngle;
    public float kspStartAngle = 400;

    public Vector3 kspLocalEuler;
    public Quaternion kspLocalRot, localRot;

    public RoboticServoIK IKservo;

    public float currentServoPos;

    public float kspAngle
    {
        get
        {
            if (transform.localPosition.x > 0 & _kspAngle < 0)
                return -_kspAngle;
            else
            {
                return _kspAngle;
            }
        }
        set
        {
            if (kspStartAngle == 400)
                kspStartAngle = value;
            if (transform.localPosition.x > 0)
                _kspAngle = value;
            else
            {
                _kspAngle = -value;
            }
            _kspAngle = value;
        }
    }

    public override void CustomStart(string servoName, MemoryBridge memoryBridge, LimbController limbController,  int parentID)
    {
        base.CustomStart(servoName, memoryBridge, limbController, parentID);
        currentServoPos = memoryBridge.GetFloat(servoName + "servoPos");
    }

    public override void SetStartAngle()
    {
        base.SetStartAngle();
        MirrorServoPos();
    }

    public override void MirrorServoPos()
    {
        currentServoPos = memoryBridge.GetFloat(servoName + "servoPos");
        kspAngle = memoryBridge.GetFloat(servoName + "servoPos");
        // memoryBridge.SetFloat(servoName + "unityServoPos", servoAngleSet);
        kspLocalEuler = memoryBridge.GetVector3(servoName + "servoLocalEuler");
        //  kspLocalRot = memoryBridge.GetQuaternion(servoName + "servoLocalRot");
      

        base.MirrorServoPos();
        kspLocalRot = memoryBridge.GetQuaternion(servoName + "servoLocalRot");
        transform.localRotation = kspLocalRot;

        localRot = transform.localRotation;
    }

    public override void CustomUpdate()
    {
        base.CustomUpdate();
        Debug.Log("mirror update");
       
        transform.localRotation = kspLocalRot;
    }
}
