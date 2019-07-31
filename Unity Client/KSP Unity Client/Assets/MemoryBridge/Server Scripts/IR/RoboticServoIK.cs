using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoboticServoIK : RoboticServo
{
    public RoboticServoMirror mirrorServo;
    public float rawSetAngle, setAngle;

    PidController speedPID;

    public Vector3 targetAngle = Vector3.zero;

    public float valueP = .1f;
    float valueI = 0;
    float valueD = 0;
    float valueMin;
    float valueMax;

    float offset = 0;

    LimbController limbController;

    public override void CustomStart(string servoName, MemoryBridge memoryBridge, LimbController limbController, int parentID)
    {
        base.CustomStart(servoName, memoryBridge, limbController, parentID);

        this.limbController = limbController;

        //  speedPID = new PidController.PidController(mech.hipRotPIDP, mech.hipRotPIDI, mech.hipRotPIDD, mech.hipRotPIDMax, 0);
        Debug.Log("Create pid");
    }

    public void SetOffset(float newOffset)
    {
        offset = newOffset;
    }

    private void Awake()
    {
        speedPID = new PidController(valueP, valueI, valueD, valueMax, valueMin);
    }

    public void UpdatePIDValue(float valueP, float valueI, float valueD, float valueMin, float valueMax)
    {
        Debug.Log("Update servo PID");
        this.valueP = valueP;
        this.valueI = valueI;
        this.valueD = valueD;
        this.valueMin = valueMin;
        this.valueMax = valueMax;
        speedPID = new PidController(this.valueP, this.valueI, this.valueD, this.valueMax, this.valueMin);
        speedPID.SetPoint = 0;
    }

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
      //  this.groundPoint = mirrorServo.groundPoint;
        setAngle = 0;

        //if (limbControllerPart == RoboticLimb.LimbPart.Wrist)
        //{
        //    //  limbLength = Vector3.Distance(transform.position, memoryBridge.GetVector3(mirrorServo.servoName + "CollisionPoint"));
        //    var limbOffset = limb.trueLimbEnd.localPosition; //transform.InverseTransformPoint(memoryBridge.GetVector3(mirrorServo.servoName + "CollisionPoint"));
        //    targetOffset = (float)(Math.Atan2(limbOffset.z, limbOffset.y));
        //    targetOffset *= (float)(180 / Math.PI);

        //    //  Debug.Log("limb offset " + targetOffset);
        //}

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

        if (this == FindObjectOfType<HexapodRoboticController>().debugServo)
        {
            Debug.Log("pre new pos : " + newPos);
        }
        //limbLength = Vector3.Distance(transform.position, limbController.limbIK.limbEndPoint.position);
        //var limbOffset = transform.InverseTransformPoint(groundPoint.position);
        //targetOffset = (float)(Math.Atan2(limbOffset.z, limbOffset.y));
        //targetOffset *= (float)(180 / Math.PI);

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

        if (this == FindObjectOfType<HexapodRoboticController>().debugServo)
        {
            Debug.Log("new pos : " + newPos + " Set Angle : " + setAngle);
        }

        if (invert)
        {
            setAngle = -setAngle;
        }
        //if (hostPart.kspPartName.Contains("Half"))
        //{
        //    setAngle = -setAngle;
        //}
        SetServo();
    }

    public void LookAt(Transform target)
    {
        
    }
    public void MatchTargetAngle(Transform target)
    {
        // var tempEuler = transform.eulerAngles;
        // tempEuler.x = target.eulerAngles.x;
        // transform.eulerAngles = tempEuler;
        var prevParent = target.parent;
        target.SetParent(transform.parent);

        var rot = target.localEulerAngles.x;
        Debug.Log(rot + " : " + Time.frameCount);
        setAngle = rot;
        target.SetParent(prevParent);
        SetServo();
       // transform.localRotation = Quaternion.Euler(target.eulerAngles.x, 0, 0);
    }

    public bool counteractRot = false;
    public void SetServo()
    {
        if (!disabled)
        {
            //if (limbControllerPart == RoboticLimb.LimbPart.Base && counteractRot)
            //{
            //    Debug.Log("set for hip rot");
            //    setAngle += limbController.baseRotOffset;
            //}

            //  var tempLocalRot = transform.localEulerAngles;
            //  tempLocalRot.x = setAngle;
            transform.localRotation = Quaternion.Euler(setAngle + offset, 0, 0);
            //transform.localRotation = Quaternion.Euler(setAngle, 0, 0);

            if (hostPart.kspPartName == "IR.Pivotron.RangeNinety")
            {
                if (setAngle < 0)
                {
                    setAngle = -setAngle;
                }
            }
            // Debug.Log("set servo pos");
            RunServoPID();

          


            memoryBridge.SetFloat(servoName + "unityServoPos", setAngle + offset);
        }
    }
    public bool debugServoPID;
    public float servoSpeed = 20;
    void RunServoPID()
    {
        //  var timeSinceLastUpdate = Time.deltaTime;// Time.time - prevDeltaTime;
        // var deltaTime = new System.TimeSpan(0,0,0,(int)Time.deltaTime * 1000);

        System.TimeSpan deltaTime = TimeSpan.FromSeconds(Time.fixedDeltaTime);

        //   Debug.Log(deltaTime);

        // float error = 0;
        // if (!Double.IsNaN(ikLeg.hipRotAngle))
        // {
        float servoError = 0;
        // switch (limbAxis)
        //{
        //case LimbController.LimbAxis.X:
        servoError = transform.localEulerAngles.x - mirrorServo.transform.localEulerAngles.x;
        //    break;
        //case LimbController.LimbAxis.Y:
        //    servoError = transform.localEulerAngles.y - mirrorServo.transform.localEulerAngles.y;
        //    break;
        //case LimbController.LimbAxis.Z:
        //    servoError = transform.localEulerAngles.z - mirrorServo.transform.localEulerAngles.z;
        //    break;
        //default:  
        //    break;
        // }


        // hipRotError = -Math.Abs(hipRotError);
        //  Debug.Log("hipRotError " + hipRotError);
        //  if (hipRotError < mech.hipRotErrorThreshHold)
        //  {


        //speedPID.SetPoint = 0;
        // speedPID.ProcessVariable = -Math.Abs(servoError);
        // var servoSpeed = speedPID.ControlVariable(deltaTime);

        //var velError = limbController.velocity - 7;
        //speedPID.ProcessVariable = velError;
        //var servoSpeed = speedPID.ControlVariable(deltaTime);

    

        if (debugServoPID)
        {
            Debug.Log("Servo Pid");
            Debug.Log(speedPID.GainProportional);
            Debug.Log("Servo Error " + servoError);
           // Debug.Log("Servo S[eed " + servoSpeed);

        }

        //  Debug.Log(servoSpeed);
        // }
        //  }

        //if (float.IsNaN((float)servoSpeed))
        //    servoSpeed = 0;

        //if (!useServoSpeedPID)
        //    servoSpeed = 20;

        //  if (limbController.velocity < 5)
        // {
        //  memoryBridge.SetFloat(servoName + "unityServoAcceleration", limbController.servoAcceleration);
        memoryBridge.SetFloat(servoName + "unityServoSpeed", 9999);// (float)limbController.limbIK.servoSpeed);
        //}
        //else
        //{
        //    memoryBridge.SetFloat(servoName + "unityServoAcceleration", limbController.servoAcceleration);
        //    memoryBridge.SetFloat(servoName + "unityServoSpeed", (float)0);
        //}

       
    }

    
    public bool useServoSpeedPID;
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
