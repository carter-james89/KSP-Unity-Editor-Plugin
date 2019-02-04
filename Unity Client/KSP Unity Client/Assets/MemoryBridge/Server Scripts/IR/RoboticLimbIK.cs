using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoboticLimbIK : RoboticLimb
{
    public struct IKAxis { public List<RoboticServoIK> servoGroup; public RoboticServoIK fixedAngleServo; public RoboticServoIK servo0; public RoboticServoIK servo1; }
    public IKAxis IKAxisX, IKAxisY, IKAxisZ;
    public List<RoboticServoIK> servosIK;//, servoGroup0, servoGroup1, servoGroup2;
    public Transform limbEndPoint;
    Transform targetTransform, rotAxis;
    public enum GaitTarget { MidPoint, FrontPoint, BackPoint}
    bool IKactive = false;

    public Transform gait { get; private set; }
    public Transform pointMid { get; private set; }
    public Transform pointFront { get; private set; }
    public Transform pointBack { get; private set; }

    public enum LegDirection { Forward, Reverse}
    public LegDirection legdDir = LegDirection.Forward;
    public enum LegMode { Translate, Rotate}
    public LegMode legMode;

    public Transform currentTarget;
    bool atTarget = true;

    Quaternion targetRot;
    Vector3 targetPos;

    public float strideLength = 2f;

    public List<Transform> gaitSequence;

    public void AddGaitTarget(Transform newTarget)
    {
        if (gaitSequence == null)
        {
            gaitSequence = new List<Transform>();
        }
        gaitSequence.Add(newTarget);
    }

    public override void CustomStart(LimbController limbController)
    {
        base.CustomStart(limbController);
    }


    public void StartGaitSequence()
    {
        UpdateTarget(gaitSequence[0]);
    }
    public void MoveToNextTarget()
    {
        int currentInt = 0;
        for (int i = 0; i < gaitSequence.Count; i++)
        {
            if(gaitSequence[i] == currentTarget)
            {
                currentInt = i;
            }
        }
        if(currentInt == gaitSequence.Count - 1)
        {
            UpdateTarget(gaitSequence[0]);
        }
        else
        {
            UpdateTarget(gaitSequence[currentInt + 1]);
        }
    }

    void UpdateTarget(Transform newTransform)
    {
        //var currentTargetPos = currentTarget.position;
        rotAxis.localEulerAngles = Vector3.zero;

        // var newTargetOffset = targetTransform.InverseTransformPoint(newTransform.position);

       

        rotAxis.localPosition = new Vector3(0, 0, (newTransform.localPosition.z + currentTarget.localPosition.z) / 2);

        targetTransform.position = currentTarget.position;
        var tempPos = targetTransform.localPosition;
        tempPos.x = 0;
        tempPos.y = 0;
        targetTransform.localPosition = tempPos;
    //    Debug.Log("set " + name + " to current target pos " + currentTarget.name);
        if (newTransform.localPosition.z > rotAxis.localPosition.z)//newTargetOffset.z > 0)// & legdDir == LegDirection.Forward)
        {
            //rotAxis.position = targetTransform.position;
            //var dist = Vector3.Distance(rotAxis.position, newTransform.position);
            //Debug.Log("New target dist from target transform " + dist);
            //rotAxis.localPosition += new Vector3(0, 0, dist / 2);

          //  Debug.Log("Set rot mode");

            // targetRot = new Vector3(180, 0, 0);
            targetRot = Quaternion.Euler(new Vector3(-180, 0, 0));
           // rotAxis.localPosition = Vector3.zero;
            legMode = LegMode.Rotate;
        }
        else
        {
            legMode = LegMode.Translate;
            //rotAxis.localPosition = Vector3.zero;
        }
       
        currentTarget = newTransform;
        atTarget = false;
    }
    //public void SetTarget(GaitTarget newTarget)
    //{
    //    Transform newTransform = null;
    //    switch (newTarget)
    //    {
    //        case GaitTarget.MidPoint:
    //            newTransform = pointMid;
    //            break;
    //        case GaitTarget.FrontPoint:
    //            newTransform = pointFront;
    //            break;
    //        case GaitTarget.BackPoint:
    //            newTransform = pointBack;
    //            break;
    //        default:
    //            break;
    //    }

    //    Debug.Log("new point set");
       

    //    var currentTargetPos = targetTransform.position;
    //    rotAxis.localEulerAngles = Vector3.zero;

    //    var newTargetOffset = targetTransform.InverseTransformPoint(newTransform.position);

    //    if (newTargetOffset.z > 0)// & legdDir == LegDirection.Forward)
    //    {
    //        rotAxis.position = targetTransform.position;
    //        var dist = Vector3.Distance(rotAxis.position, newTransform.position);
    //        Debug.Log("New target dist from target transform " + dist);
    //        rotAxis.localPosition += new Vector3(0, 0, dist / 2);
            
    //       // targetRot = new Vector3(180, 0, 0);
    //        targetRot = Quaternion.Euler(new Vector3(-180, 0, 0));
    //        legMode = LegMode.Rotate;                  
    //    }
    //    else
    //    {
    //        legMode = LegMode.Translate;
    //        rotAxis.localPosition = Vector3.zero;
    //    }
    //    targetTransform.position = currentTargetPos;
    //    currentTarget = newTransform;
    //    atTarget = false;
    //}

    public void RunGait()
    {    
        if (!atTarget)
        {
     
            var dist = Vector3.Distance(targetTransform.position, currentTarget.position);
            if (dist < .05f)
            {
                targetTransform.position = currentTarget.position;
                atTarget = true;
                Debug.Log("at target");
            }
            else
            {
                switch (legMode)
                {
                    case LegMode.Translate:
                        targetTransform.localPosition = Vector3.Lerp(targetTransform.localPosition, currentTarget.localPosition,Time.deltaTime*limbController.roboticController.walkSpeed);
                        break;
                    case LegMode.Rotate:
                        rotAxis.localRotation = Quaternion.Lerp(rotAxis.localRotation, targetRot, Time.deltaTime * limbController.roboticController.walkSpeed);
                        break;
                    default:
                        break;
                }
            }      
        }
    }

    //private void Update()
    //{  
    //    RunGait();
    //}
    public void RunIK()
    {
        CalculateIK(IKAxisX);
        CalculateIK(IKAxisY);
       // IKAxisZ.servo0.SetServo(1);
        //   CalculateIK(IKAxisZ);
    }
    public Transform testTarget;
    public int limbDistCheckCount = 0;
    float servo1_0Dist;
    float servo0_GroundPointDist;
    float servo1_GroundPointDist;
    bool firstFrame = true;
    void CalculateIK(IKAxis IKAxis)
    {
        var servoGroup = IKAxis.servoGroup;

        if (IKAxis.fixedAngleServo)
        {

            var targetOffset = IKAxis.fixedAngleServo.servoBase.InverseTransformPoint(IKAxis.fixedAngleServo.servoBase.transform.position + new Vector3(0, -100, 0));
            var angle = Math.Atan2(targetOffset.z, targetOffset.y);
            angle *= (180 / Math.PI);
            IKAxis.fixedAngleServo.SetServo((float)angle);
            //IKAxis.fixedAngleServo.LookDown();
        }

        if (IKAxis.servo0)
        {
            if (IKAxis.fixedAngleServo)
            {
                Debug.Log("run servo 0 fixed angle");
                var yDif = IKAxis.servo0.groundPoint.position.y - targetTransform.position.y;
                Debug.Log(yDif);
                var limbDist = Vector3.Distance(IKAxis.fixedAngleServo.transform.position, IKAxis.fixedAngleServo.groundPoint.position);
                var targetPos = targetTransform.position;// + new Vector3(0, limbDist, 0);
                if (testTarget)
                    testTarget.position = targetPos;

                var targetOffset = IKAxis.servo0.servoBase.InverseTransformPoint(targetPos);
                var angle = Math.Atan2(targetOffset.z, targetOffset.y);
                angle *= (180 / Math.PI);

                // angle += IKAxis.fixedAngleServo.setAngle;
                // angle = -IKAxis.fixedAngleServo.setAngle;

                //   IKAxis.servo0.SetServo((float)angle);// + IKAxis.fixedAngleServo.setAngle);
            }
            else
            {
                //   Debug.Log("run servo 0");
                var targetOffset = IKAxis.servo0.servoBase.InverseTransformPoint(targetTransform.position);
                var angle = Math.Atan2(targetOffset.z, targetOffset.y);
                angle *= (180 / Math.PI);

                // angle -= IKAxis.servo0.targetOffset;
                IKAxis.servo0.SetServo((float)angle);
            }
        }

        if (IKAxis.servo1)
        {
            // Debug.Log("run servo 2");
            var targetOffset = IKAxis.servo1.servoBase.InverseTransformPoint(targetTransform.position);
            var angle0 = Math.Atan2(targetOffset.z, targetOffset.y);
            angle0 *= (180 / Math.PI);
            // float angle1 = LawOfCosines(IKAxis.servo1.limbLength, IKAxis.servo0.limbLength, Vector3.Distance(IKAxis.servo1.transform.position, targetTransform.position));

            // if (limbDistCheckCount == 0)
            //  var dist = Vector3.Distance(IKAxis.servo0.groundPoint.position, targetTransform.position);
            //  if(dist > .5f)
            // {
            // var 8 = IKAxis.servo1.limbLength

            if (firstFrame)
            {
                var children = GetComponentsInChildren<Transform>();
                foreach (var child in children)
                {
                    if (child.name == "Foot(Clone)")
                    {
                        limbEndPoint = child;
                    }

                }
                // Debug.Log("servo 1 offset " + IKAxis.servo1.targetOffset);
                firstFrame = false;
                var axis = IKAxisZ;
                if (axis.servo0)
                    DebugVector.DrawVector(axis.servo0.transform, DebugVector.Direction.all, .5f, .1f, Color.red, Color.white, Color.blue);
                if (axis.servo1)
                    DebugVector.DrawVector(IKAxis.servo1.transform, DebugVector.Direction.all, .5f, .1f, Color.red, Color.white, Color.blue);
                //DebugVector.DrawVector(IKAxis.servo1.transform, DebugVector.Direction.all, .5f, .1f, Color.red, Color.white, Color.blue);
            }
            servo1_0Dist = Vector3.Distance(IKAxis.servo1.transform.position, IKAxis.servo0.transform.position);
            servo0_GroundPointDist = Vector3.Distance(IKAxis.servo0.transform.position, limbEndPoint.position);
            servo1_GroundPointDist = Vector3.Distance(IKAxis.servo1.transform.position, targetTransform.position);
           // limbDistCheckCount = 10;
            //   }
            //  limbDistCheckCount--;
            float angle1 = LawOfCosines(servo1_0Dist, servo0_GroundPointDist, servo1_GroundPointDist);
            //float angle1 = LawOfCosines(IKAxis.servo1.limbLength, Vector3.Distance(IKAxis.servo0.transform.position,targetTransform.position), Vector3.Distance(IKAxis.servo1.transform.position, targetTransform.position));

            if (IKAxis.servo1.invert)
            {
                angle1 -= (float)angle0;
            }
            else
            {
                angle1 += (float)angle0;
            }

            // var angle = (float)(angle1 + angle0);// - IKAxis.servo1.targetOffset);
            if (!float.IsNaN(angle1))
            {
                IKAxis.servo1.SetServo(angle1);
            }


            //   if (limbController.name == "ik Test")
            //    {
            //Debug.Log(Time.frameCount);

            //// Debug.Log(" limblength " + IKAxis.servo0.limbLength);
            //Debug.Log(" servo1_0Dist " + servo1_0Dist);
            //Debug.Log(" servo0_groundPoint " + servo0_GroundPointDist);
            //Debug.Log(" servo1_ground point " + servo1_GroundPointDist);
            //Debug.Log("Angle 1 " + angle0);
            //Debug.Log("Angle 2 " + angle1);
            //  }//

        }





        // }

        // // for (int i = 0; i < servoGroup.Count; i++)
        ////  {
        //      var servo = servoGroup[i];
        //      if (i == 0)
        //      {
        //          if (servo.targetAngle != Vector3.down)
        //          {
        //              var targetOffset = servoGroup[0].servoBase.InverseTransformPoint(targetTransform.position);
        //              var angle = Math.Atan2(targetOffset.z, targetOffset.y);
        //              angle *= (180 / Math.PI);
        //              servoGroup[0].SetServo((float)angle);
        //          }
        //          else
        //          {
        //              var targetOffset = servo.servoBase.InverseTransformPoint(Vector3.down);
        //              var angle = Math.Atan2(targetOffset.z, targetOffset.y);
        //              angle *= (180 / Math.PI);
        //              servoGroup[0].SetServo(-(float)angle);
        //          }
        //      }

        //      if (i == 1 || i == 2)
        //      {
        //          Debug.Log("run servo 2");
        //          var targetOffset = servoGroup[i].servoBase.InverseTransformPoint(targetTransform.position);
        //          var angle0 = Math.Atan2(targetOffset.z, targetOffset.y);
        //          angle0 *= (180 / Math.PI);
        //          float angle1 = LawOfCosines(servoGroup[i].limbLength, servoGroup[i - 1].limbLength, Vector3.Distance(servoGroup[i].transform.position, targetTransform.position));
        //          //float angle1 = LawOfCosines(Vector3.Distance(servoGroup[i].transform.position, servoGroup[0].transform.position), servoGroup[0].limbLength, Vector3.Distance(servoGroup[i].transform.position, targetTransform.position));
        //          if (servo.invert)
        //          {
        //              angle1 -= (float)angle0;
        //          }
        //          else
        //          {
        //              angle1 += (float)angle0;
        //          }



        //          if (!float.IsNaN((float)angle1))
        //          {
        //              servoGroup[i].SetServo((float)angle1);
        //          }
        //          else
        //          {
        //              //  servoGroup[i].SetServo((float)angle0);
        //          }
        //      }
        // // }
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
    public void SetServos()
    {
        if (IKactive)
        {
            foreach (var servo in servosIK)
            {
                servo.SetServo();
            }
        }
    }

    public Vector3 gaitStartPos;
    public void ActivateIK()
    {
        foreach (var servo in servosIK)
        {
            servo.SetServo(servo.mirrorServo.kspStartAngle);
        }
        var newObject = Instantiate(Resources.Load("Gait", typeof(GameObject))) as GameObject;
        gait = newObject.transform;
        rotAxis = gait.Find("Axis Point");
        targetTransform = rotAxis.Find("Limb Target");
        pointMid = gait.Find("Point Mid");
        pointFront = gait.Find("Point Front");
        pointBack = gait.Find("Point Back");
        gait.position = limbController.limbMirror.limbEnd.position;
        gait.SetParent(transform);

        gaitStartPos = gait.localPosition;

        var tempEuler = gait.eulerAngles;
        tempEuler.y = limbController.vesselControl.vessel.transform.eulerAngles.y;
        gait.eulerAngles = tempEuler;

        pointFront.localPosition = new Vector3(0,0,strideLength / 2);
        pointBack.localPosition = new Vector3(0,0,strideLength / -2);
        IKactive = true;

        currentTarget = pointMid;
    }

    public void StoreGroupedServos()
    {
        //  Debug.Log("store grouped servos");
        IKAxisX = new IKAxis();
        IKAxisX.servoGroup = new List<RoboticServoIK>();
        IKAxisY = new IKAxis();
        IKAxisY.servoGroup = new List<RoboticServoIK>();
        IKAxisZ = new IKAxis();
        IKAxisZ.servoGroup = new List<RoboticServoIK>();
        foreach (var servo in servosIK)
        {
            if (!servo.disabled)
            {
                switch (servo.limbAxis)
                {
                    case LimbController.LimbAxis.X:
                        IKAxisX.servoGroup.Add(servo);
                        break;
                    case LimbController.LimbAxis.Y:
                        IKAxisY.servoGroup.Add(servo);
                        break;
                    case LimbController.LimbAxis.Z:
                        IKAxisZ.servoGroup.Add(servo);
                        break;
                }
            }
            else
            {
                Debug.Log("Did not add to servo list " + servo.name);
            }
        }

        IKAxisX.servoGroup.Reverse();
        IKAxisY.servoGroup.Reverse();
        IKAxisZ.servoGroup.Reverse();

        CalculateServoGroupOffset(ref IKAxisX);
        CalculateServoGroupOffset(ref IKAxisY);
        CalculateServoGroupOffset(ref IKAxisZ);

        foreach (var item in IKAxisX.servoGroup)
        {
            // Debug.Log(item.name + "dddddddd");
        }
    }

    void CalculateServoGroupOffset(ref IKAxis IKAxis)
    {
        // Debug.Log("servo axis count " + IKAxis.servoGroup.Count + " ");
        var servoGroup = IKAxis.servoGroup;
        for (int i = 0; i < servoGroup.Count; i++)
        {
            if (i == 0)
            {
                servoGroup[i].limbLength = Vector3.Distance(servoGroup[i].transform.position, servoGroup[i].groundPoint.position);
            }
            else
            {
                //  Debug.Log("Set servo limb length to ");
                servoGroup[i].limbLength = Vector3.Distance(servoGroup[i].transform.position, servoGroup[i - 1].transform.position);
                var limbOffset = servoGroup[i].servoBase.InverseTransformPoint(servoGroup[i - 1].transform.position);
                var tempAngle = (float)(Math.Atan2(limbOffset.z, limbOffset.y));
                tempAngle = (float)(tempAngle * 180 / Math.PI);
                servoGroup[i].targetOffset = tempAngle;
            }
        }

        if (servoGroup.Count > 0)
        {
            if (servoGroup[0].targetAngle != Vector3.zero)
            {
                Debug.Log("0 joint has set angle");
                IKAxis.fixedAngleServo = servoGroup[0];
                if (servoGroup.Count > 1)
                {
                    IKAxis.servo0 = servoGroup[1];
                }
                if (servoGroup.Count > 2)
                {
                    IKAxis.servo1 = servoGroup[2];
                }
            }
            else
            {
                if (servoGroup.Count > 0)
                {
                    IKAxis.servo0 = servoGroup[0];
                    //  Debug.Log("set servo0");
                }
                if (servoGroup.Count > 1)
                {
                    IKAxis.servo1 = servoGroup[1];
                }
            }
        }

    }
    public void ConvertToIKLimb(RoboticLimbMirror mirrorLimb)
    {
        //  Debug.Log("create ik leg");
        var servosMirror = servos;
        servosIK = new List<RoboticServoIK>();
        var newServos = new List<RoboticServo>();
        //Convert the mirror servos to Ik servos
        foreach (var servo in servosMirror)
        {
            var newServoIK = servo.gameObject.AddComponent(typeof(RoboticServoIK)) as RoboticServoIK;
            servo.Clone(newServoIK);
            servosIK.Add(newServoIK);
            newServos.Add(newServoIK);
        }
        for (int i = 0; i < servosMirror.Length; i++)
        {
            if (servosMirror[i].servoParent)
            {
                servosIK[i].servoParent = servosMirror[i].servoParent.gameObject.GetComponent<RoboticServoIK>();
            }
            if (servosMirror[i].servoChild)
            {
                servosIK[i].servoChild = servosMirror[i].servoChild.gameObject.GetComponent<RoboticServoIK>();
            }
        }
        foreach (var servo in servosMirror)
        {
            Destroy(servo);
        }
        //Link the RoboticServoMirror and RoboticServoIk components
        foreach (var servo in servosIK)
        {
            var mirrorPart = mirrorLimb.FindServo(servo.hostPart.ID);
            servo.SetMirrorServo(mirrorPart.GetComponent<RoboticServoMirror>());
        }
        servosMirror = new List<RoboticServoMirror>().ToArray();
        servos = newServos.ToArray();
        DisableParts();
    }
}
