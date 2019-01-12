using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoboticServoIK : RoboticServo
{
    public RoboticServoMirror mirrorServo;
    public float rawSetAngle, setAngle;

    public Vector3 targetAngle = Vector3.zero;

    public override void CustomUpdate()
    {
        base.CustomUpdate();
    }

    public void SetMirrorServo(RoboticServoMirror mirrorServo)
    {
        this.mirrorServo = mirrorServo;
        this.mirrorServo.IKservo = this;
        this.invert = mirrorServo.invert;
        this.limbController = mirrorServo.limbController;
        servoBase = transform.parent;
        memoryBridge = mirrorServo.memoryBridge;
        this.groundPoint = mirrorServo.groundPoint;
        setAngle = 0;

        if (limbControllerPart == RoboticLimb.LimbPart.Wrist)
        {
            limbLength = Vector3.Distance(transform.position, groundPoint.position);
            var limbOffset = transform.InverseTransformPoint(groundPoint.position);
            targetOffset = (float)(Math.Atan2(limbOffset.z, limbOffset.y));
            targetOffset *= (float)(180 / Math.PI);

            Debug.Log("limb offset " + targetOffset);
        }

        if (hostPart.name.ToLower().Contains("down"))
        {
            targetAngle = Vector3.down;
        }
    }

    public void SetServo(float newPos)
    {
        if (hostPart.kspPartName == "IR.Pivotron.RangeNinety")
        {
            limitMax = 0;
            limitMin = -90;
            //targetOffset = 0;
        }

        var tempPos = newPos -= targetOffset;

        rawSetAngle = tempPos;

        if (tempPos >= limitMin & tempPos <= limitMax)
        {
            setAngle = newPos;
        }
        else
        {
            if (newPos < limitMin)
            {
                setAngle = limitMin;
            }
            else if (newPos > limitMax)
            {
                setAngle = limitMax;
            }
        }

        if (invert)
        {
            setAngle = -setAngle;
        }
        //if (hostPart.kspPartName.Contains("Half"))
        //{
        //    setAngle = -setAngle;
        //}
    }

    public void SetServo()
    {
        if (!disabled)
        {
            //  var tempLocalRot = transform.localEulerAngles;
            //  tempLocalRot.x = setAngle;
            transform.localRotation = Quaternion.Euler(setAngle, 0, 0);
            //transform.localRotation = Quaternion.Euler(setAngle, 0, 0);

            if (hostPart.kspPartName == "IR.Pivotron.RangeNinety")
            {
                if (setAngle <0)
                {
                    setAngle = -setAngle;
                }
            }
            memoryBridge.SetFloat(servoName + "unityServoPos", setAngle);
        }
    }

    public override void CreateBaseAnchor()
    {
        base.CreateBaseAnchor();
        if (hostPart.kspPartName == "IR.Pivotron.RangeNinety")
        {
            limitMax = -90;
            //var tempChildren = gameObject.GetComponentsInChildren<Transform>();
            //foreach (var child in tempChildren)
            //{
            //    if (child != transform)
            //        child.SetParent(null);
            //}

            //servoBase.localEulerAngles += new Vector3(0, 0, 180);
            //foreach (var child in tempChildren)
            //{
            //    if (child != transform)
            //        child.SetParent(transform);

            //}
        }
    }

    public void LookDown()
    {
        //transform.rotation = Quaternion.LookRotation(-limbController.transform.forward, -limbController.transform.up);

        // setAngle = transform.localEulerAngles.x;
    }

    public void SetServoRot(float newRot)
    {
        var tempLocalRot = transform.localRotation;
        tempLocalRot.x = newRot;
        transform.localRotation = tempLocalRot;
        Debug.Log(newRot);
    }

    public override void SetStartAngle()
    {
        Debug.Log("IK mirror angle");
        base.SetStartAngle();
        transform.localRotation = mirrorServo.kspLocalRot;
    }
}
