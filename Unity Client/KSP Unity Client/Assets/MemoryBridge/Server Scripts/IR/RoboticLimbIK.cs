using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoboticLimbIK : RoboticLimb
{
    public enum GaitTarget { MidPoint, FrontPoint, BackPoint }
    public enum LegDirection { Forward, Reverse }
    public LegDirection legdDir = LegDirection.Forward;

    public struct IKAxis { public List<RoboticServoIK> servoGroup; public RoboticServoIK fixedAngleServo; public RoboticServoIK servo0; public RoboticServoIK servo1; public bool isBaseAxis; }
    public IKAxis IKAxisX, IKAxisY, IKAxisZ;
    public List<RoboticServoIK> servosIK;//, servoGroup0, servoGroup1, servoGroup2;

    public Transform IKtargetTransform, rotAxis;

    bool IKactive = false;

    public Transform gait { get; private set; }
    public Transform pointMid { get; private set; }
    public Transform pointFront { get; private set; }
    public Transform pointBack { get; private set; }

    public Transform testTarget;
    public int limbDistCheckCount = 0;
    public float servo1_0Dist, servo0_GroundPointDist, servo1_GroundPointDist;
    bool firstFrame = true;

    public Vector3 gaitStartPos;
    bool adjustedStride = false;

    public Transform translateStartPos;

    public bool debug;
    public float servoSpeed = 20;

    public Transform currentTarget;
    bool atTarget = true;

    Quaternion targetRot, startRot;
    Vector3 targetPos;

    public float strideLength = 1.7f;
    public float defaultBackPos;

    float localStridePercent;

    public struct GaitPoint { Transform target; LimbController.LegMode legMode; }
    public List<Transform> gaitSequenceTarget;
    public List<LimbController.LegMode> gaitSequenceMode;

    AnimationCurve gaitCurve;


    #region Setup
    public void AddGaitTarget(Transform newTarget, LimbController.LegMode legMode)
    {
        Debug.Log(name + " add gate target " + newTarget.name);
        if (gaitSequenceTarget == null)
        {
            gaitSequenceTarget = new List<Transform>();
        }
        gaitSequenceTarget.Add(newTarget);
        if (gaitSequenceMode == null)
        {
            gaitSequenceMode = new List<LimbController.LegMode>();
        }
        gaitSequenceMode.Add(legMode);
    }

    public override void CustomStart(LimbController limbController)
    {
        base.CustomStart(limbController);
    }

    public void StartGaitSequence()
    {
        UpdateTarget(0);
    }
    public void ResetGaitSequence()
    {
        gaitSequenceTarget = new List<Transform>();
        gaitSequenceMode = new List<LimbController.LegMode>();
    }
    public void ActivateIK()
    {
        gaitCurve = limbController.roboticController.curveX;
        foreach (var servo in servosIK)
        {
            servo.SetServo(servo.mirrorServo.kspStartAngle);
        }
        var newObject = Instantiate(Resources.Load("Gait", typeof(GameObject))) as GameObject;
        gait = newObject.transform;
        rotAxis = gait.Find("Axis Point");
        IKtargetTransform = rotAxis.Find("Limb Target");
        pointMid = gait.Find("Point Mid");
        pointFront = gait.Find("Point Front");
        pointBack = gait.Find("Point Back");
        //gait.position = limbController.limbMirror.limbEnd.position;
        gait.SetParent(transform);//limbController.roboticController.directionTarget);

        gait.position = transform.position + transform.up * limbController.roboticController.gaitDistance;
        var tempPos = gait.position;
        tempPos.y = limbController.limbMirror.trueLimbEnd.position.y;
        gait.position = tempPos;

        gaitStartPos = gait.localPosition;// transform.position - gait.position;//transform.InverseTransformPoint(gait.position);

        // var tempEuler = gait.eulerAngles;
        // tempEuler.y = limbController.vesselControl.vessel.transform.eulerAngles.y;
        //gait.eulerAngles = tempEuler;
        gait.transform.rotation = Quaternion.LookRotation(limbController.vesselControl.vessel.transform.forward, Vector3.up);

        IKtargetTransform.position = limbController.limbMirror.trueLimbEnd.position;

        pointFront.localPosition = new Vector3(0, 0, strideLength / 2);
        pointBack.localPosition = new Vector3(0, 0, (strideLength / -2));
        defaultBackPos = (strideLength / -2);// - .3f;
        IKactive = true;

        currentTarget = pointMid;

        RunIK();
    }
    #endregion

    #region GaitTargets
    public bool loopGait = true;
    public void MoveToNextTarget()
    {
        //int currentInt = 0;
        //for (int i = 0; i < gaitSequence.Count; i++)
        //{
        //    if (gaitSequence[i] == currentTarget)
        //    {
        //        currentInt = i;
        //    }
        //}
        //if (currentInt == gaitSequence.Count - 1)
        //{
        //    UpdateTarget(gaitSequence[0]);
        //}
        //else
        //{
        //    UpdateTarget(gaitSequence[currentInt + 1]);
        //}
        if (sequencePos != gaitSequenceTarget.Count - 1)
        {
            UpdateTarget(sequencePos + 1);
        }
        else if (loopGait)
        {
            StartGaitSequence();
        }
    }

    int sequencePos = 0;
    void UpdateTarget(int pos)
    {
        sequencePos = pos;
        var newTransform = gaitSequenceTarget[pos];
        var legMode = gaitSequenceMode[pos];

        // Debug.Log("target count " + gaitSequenceTarget.Count);
        //  Debug.Log("move to new target " + newTransform.gameObject);

        rotAxis.localEulerAngles = Vector3.zero;
        rotAxis.position = trueLimbEnd.position + (newTransform.position - trueLimbEnd.position) / 2;


        // Debug.LogError("");


        //var tempPos = IKtargetTransform.localPosition;
        //tempPos.x = 0;
        //tempPos.y = 0;
        //IKtargetTransform.localPosition = tempPos;
        //    Debug.Log("set " + name + " to current target pos " + currentTarget.name);
        if (legMode == LimbController.LegMode.Rotate)//newTargetOffset.z > 0)// & legdDir == LegDirection.Forward)
        {
            rotAxis.LookAt(newTransform, -Vector3.up);
            IKtargetTransform.position = trueLimbEnd.position;

            startRot = rotAxis.localRotation;
            targetRot = Quaternion.Euler(rotAxis.localEulerAngles + new Vector3(179, 0, 0));
            // targetRot = Quaternion.Euler(new Vector3(-180, 0, 0));
            limbController.legMode = LimbController.LegMode.Rotate;
        }
        else
        {
            IKtargetTransform.position = currentTarget.position;
            translateStartPos = currentTarget;
            //tempPos = IKtargetTransform.localPosition;
            //tempPos.x = 0;
            //tempPos.y = 0;
            //IKtargetTransform.localPosition = tempPos;
            limbController.legMode = LimbController.LegMode.Translate;
        }
        currentTarget = newTransform;
        atTarget = false;
    }
    #endregion

    public float gaitAdjust = 0;
    public void SetGaitRotation(float newRotation)
    {
        gaitAdjust = newRotation;
    }

    public float gaitCurveY;
    public float rotPercent;
    public void RunGait(float avgStridePercent)
    {
        if (!atTarget)
        {
            var dist = Vector3.Distance(IKtargetTransform.position, currentTarget.position);
            if (dist < .02f)
            {
                IKtargetTransform.position = currentTarget.position;
                atTarget = true;
            }
            else
            {
                var error = Vector3.Distance(IKtargetTransform.position, trueLimbEnd.position);
                if (error < .5f)
                    switch (limbController.legMode)
                    {
                        case LimbController.LegMode.Translate:
                            {
                                var speed = Time.deltaTime;
                                if(limbController.roboticController.walkCycle >= 1)
                                {
                                    speed *= limbController.roboticController.walkSpeed;
                                }
                                IKtargetTransform.position = Vector3.MoveTowards(IKtargetTransform.position, currentTarget.transform.position, speed);
                                var dir = IKtargetTransform.position - currentTarget.position;

                              

                                // if (CalculateTranslateStridePercent() < .95)
                                // IKtargetTransform.Translate(-dir.normalized * (limbController.roboticController.walkSpeed * Time.deltaTime));
                                //else
                                //{
                                //  IKtargetTransform.Translate(-gait.forward.normalized * (limbController.roboticController.walkSpeed * Time.deltaTime));
                                //}
                                //IKtargetTransform.Translate(currentTarget.transform.position);
                                //if (avgStridePercent > .8)
                                //{
                                //    limbController.servoAcceleration = 8;
                                //    var dir = translateStartPos.position - currentTarget.position;
                                //    IKtargetTransform.position = (currentTarget.position + (avgStridePercent * dir));
                                //    servoSpeed = 10;
                                //}
                                //else if (avgStridePercent > .3f)
                                //{
                                //    limbController.servoAcceleration = 13;
                                //    var dir = translateStartPos.position - currentTarget.position;
                                //    IKtargetTransform.position = (currentTarget.position + (avgStridePercent * dir));
                                //    servoSpeed = 20;
                                //}
                                //else if (!adjustedStride)
                                //{
                                //   IKtargetTransform.localPosition = Vector3.MoveTowards(IKtargetTransform.localPosition, new Vector3(0, 0, defaultBackPos), Time.deltaTime * 1);
                                //}
                                //else
                                //{
                                //    limbController.servoAcceleration = 13;
                                //    var dir = translateStartPos.position - currentTarget.position;
                                //    IKtargetTransform.position = (currentTarget.position + (avgStridePercent * dir));
                                //    servoSpeed = 20;
                                //}
                            }

                            //  IKtargetTransform.localPosition -= new Vector3(0, 0, 1.5f) * Time.deltaTime;
                            //  IKtargetTransform.position = Vector3.MoveTowards(IKtargetTransform.position, currentTarget.position, Time.deltaTime * .1f);                 
                            // IKtargetTransform.position = Vector3.Lerp(IKtargetTransform.position, currentTarget.position, Time.deltaTime * 2f);
                            break;
                        case LimbController.LegMode.Rotate:
                            // Debug.Log("roate to " + avgStridePercent);

                            // rotAxis.localEulerAngles = Vector3.Lerp(Vector3.zero, new Vector3(-180, 0, 0), avgStridePercent);

                            // rotAxis.localRotation = Quaternion.Lerp(Quaternion.identity, targetRot, avgStridePercent);
                            //if (avgStridePercent < .2)
                            //{
                            //    rotAxis.localRotation = Quaternion.Lerp(startRot, targetRot, .2f);
                            //}
                            //else
                            //{
                            if (limbController.roboticController.robotStatus == RoboticController.RobotStatus.Walking && limbController.roboticController.walkCycle >= 1)
                            {
                                gaitCurveY = gaitCurve.Evaluate(avgStridePercent);
                                if (avgStridePercent  < .5)
                                {
                                    Vector3 strideHeight = new Vector3(0, 0, -.3f);
                                    IKtargetTransform.localPosition = Vector3.Lerp(new Vector3(0, 0, -strideLength / 2), strideHeight, avgStridePercent * 2);
                                }
                                else if (avgStridePercent >= .5 )
                                {
                                    Vector3 strideHeight = new Vector3(0, 0, -.3f);
                                   // Debug.Log("set ik target percent " + (1 - (avgStridePercent * 2)));
                                    IKtargetTransform.localPosition = Vector3.Lerp(strideHeight, new Vector3(0, 0, -strideLength / 2),(avgStridePercent * 2) - 1);
                                }
                               // else
                               // {
                               //
                                //    IKtargetTransform.localPosition = new Vector3(0, 0, -strideLength / 2);

                               // }
                            }
                            
                                
                            rotAxis.localRotation = Quaternion.Lerp(startRot, targetRot, avgStridePercent + .03f);
                            //}

                            rotPercent = avgStridePercent;



                            //var yDif = limbController.limbMirror.limbEnd.position.y - limbController.ground.position.y;
                            //if (localStridePercent > .87)
                            //{
                            //    limbController.servoAcceleration = 20;
                            //    rotAxis.localRotation = Quaternion.RotateTowards(rotAxis.localRotation, targetRot, Time.deltaTime * (50));
                            //    servoSpeed = 20;
                            //}
                            //else if (localStridePercent > .1)
                            //{
                            //    limbController.servoAcceleration = 20;
                            //    rotAxis.localRotation = Quaternion.RotateTowards(rotAxis.localRotation, targetRot, Time.deltaTime * (limbController.roboticController.walkSpeed * 1f));
                            //}
                            //else //if (error < .1f)
                            //{
                            //    if (yDif < .2f)
                            //    {
                            //        limbController.servoAcceleration = 1;
                            //        servoSpeed = 1;

                            //    }
                            //    else
                            //    {
                            //        limbController.servoAcceleration = 20;
                            //        servoSpeed = 20;
                            //    }
                            //    rotAxis.localRotation = Quaternion.RotateTowards(rotAxis.localRotation, targetRot, Time.deltaTime * (20));
                            //}
                            break;
                        default:
                            break;
                    }
            }


        }
    }

    public float CalculateRotStridePercent()
    {
        localStridePercent = Quaternion.Angle(rotAxis.transform.localRotation, targetRot);
        localStridePercent = localStridePercent / 180;
        return localStridePercent;
    }

    public float translateDist, translateDistToTarget, currentStrideLength, translatePercent;

    public float CalculateTranslateStridePercent()
    {
        //IKtargetTransform.localPosition, currentTarget.transform.position,
        // var dist = Vector3.Distance(currentTarget.transform.position, IKtargetTransform.transform.position);
        translateDistToTarget = Vector3.Distance(IKtargetTransform.position, currentTarget.transform.position);

        currentStrideLength = Vector3.Distance(pointFront.position, pointBack.position);//strideLength;
                                                                                        //  strideLength = Vector3.Distance(pointFront.position, pointBack.position);
        if (limbController.roboticController.walkCycle == 0)
        {
            currentStrideLength = Vector3.Distance(pointMid.position, pointBack.position);
        }

        translateDist = currentStrideLength - translateDistToTarget;


        if (translateDist < .01f)
        {
            translateDist = 0.01f;
        }
        // Debug.Log(name + " " + Time.frameCount);
        //  Debug.Log("dist: " + translateDistToTarget + " dist traveled " + translateDist);// + " dist traveled");
        translatePercent = translateDist / currentStrideLength;
        //if (translatePercent > .95)
        //    translatePercent = 1;
        return translatePercent;
    }

    public void RunIK()
    {
        //var contactOffset = limbController.limbMirror.servoWrist.transform.InverseTransformPoint(limbController.limbMirror.limbEnd.position);
        //limbEnd.transform.localPosition = contactOffset;

        CalculateIK(IKAxisX);
        CalculateIK(IKAxisY);
        //   CalculateIK(IKAxisZ);
    }

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

        if (IKAxis.servo1)
        {
            var targetOffset = IKAxis.servo1.servoBase.InverseTransformPoint(IKtargetTransform.position);
            var angle0 = Math.Atan2(targetOffset.z, targetOffset.y);
            angle0 *= (180 / Math.PI);

            if (firstFrame)
            {
                //var children = GetComponentsInChildren<Transform>();
                //foreach (var child in children)
                //{
                //    if (child.name == "Foot(Clone)")
                //    {
                //        trueLimbEnd = child;
                //        DebugVector.DrawVector(trueLimbEnd, DebugVector.Direction.all, 5f, .1f, Color.red, Color.white, Color.blue);
                //    }

                //}
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

            var localPos = IKAxis.servo0.transform.InverseTransformPoint(trueLimbEnd.position);
            localPos.x = 0;
            var globalPos = IKAxis.servo0.transform.TransformPoint(localPos);
            servo0_GroundPointDist = Vector3.Distance(IKAxis.servo0.transform.position, globalPos);


            servo1_GroundPointDist = Vector3.Distance(IKAxis.servo1.transform.position, IKtargetTransform.position);

            float angle1 = LawOfCosines(servo1_0Dist, servo0_GroundPointDist, servo1_GroundPointDist);
            //var angle1 = LawOfCosines(servo1_0Dist, )

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
        }
        if (IKAxis.servo0)
        {

            if (IKAxis.fixedAngleServo)
            {

                // Debug.Log("run servo 0 fixed angle");
                // var yDif = IKAxis.servo0.groundPoint.position.y - IKtargetTransform.position.y;
                // Debug.Log(yDif);
                // var limbDist = Vector3.Distance(IKAxis.fixedAngleServo.transform.position, IKAxis.fixedAngleServo.groundPoint.position);
                var targetPos = IKtargetTransform.position;// + new Vector3(0, limbDist, 0);
                                                           //  if (testTarget)
                                                           //     testTarget.position = targetPos;

                var targetOffset = IKAxis.servo0.servoBase.InverseTransformPoint(targetPos);
                var angle = Math.Atan2(targetOffset.z, targetOffset.y);
                angle *= (180 / Math.PI);

                // angle += IKAxis.fixedAngleServo.setAngle;
                // angle = -IKAxis.fixedAngleServo.setAngle;

                //   IKAxis.servo0.SetServo((float)angle);// + IKAxis.fixedAngleServo.setAngle);
            }
            else
            {
                var footOffset = IKAxis.servo0.transform.InverseTransformPoint(trueLimbEnd.position);
                xOffset = footOffset.z;
                Vector3 targetOffset;
                //if (IKAxis.servo0 == servoBase && Math.Abs(footOffset.z) > .1f)
                //    targetOffset = IKAxis.servo0.servoBase.InverseTransformPoint(IKtargetTransform.position - (IKAxis.servo0.transform.forward * footOffset.z));
                //else
                //{
                targetOffset = IKAxis.servo0.servoBase.InverseTransformPoint(IKtargetTransform.position);
                //  }


                var angle = Math.Atan2(targetOffset.z, targetOffset.y);
                angle *= (180 / Math.PI);

                // Debug.Log(" servo 0 " + IKAxis.servo0.name);

                // angle -= IKAxis.servo0.targetOffset;
                IKAxis.servo0.SetServo((float)angle);
            }
        }


    }
    public float xOffset;
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

    public void DefaultStrideLength()
    {
        // pointFront.localPosition = new Vector3(0, 0, strideLength / 2);

        pointBack.localPosition = new Vector3(0, 0, -strideLength / 2);
        // adjustedStride = false;
    }

    public void UpdateStrideLength(float newStrideLength)
    {
        // pointFront.localPosition = new Vector3(0, 0, newStrideLength / 2);
        pointBack.localPosition = new Vector3(0, 0, -newStrideLength / 2);// - .1f);
                                                                          // adjustedStride = true;
    }

    public void StoreGroupedServos()
    {
        Debug.Log("store grouped servos");
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
            //set the wrist dist
            if (i == 0)
            {
                servoGroup[i].limbLength = Vector3.Distance(servoGroup[i].transform.position, limbController.contactPointOffset);

                var limbOffset = servoGroup[i].servoBase.InverseTransformPoint(limbEnd.position);
                var tempAngle = (float)(Math.Atan2(limbOffset.z, limbOffset.y));
                tempAngle = (float)(tempAngle * 180 / Math.PI);
                servoGroup[i].targetOffset = tempAngle;
            }
            else
            {
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

        var children = GetComponentsInChildren<Transform>();
        foreach (var child in children)
        {
            if (child.name.Contains("True End Point "))
            {
                trueLimbEnd = child;
                limbEnd = trueLimbEnd;
                //  trueLimbEnd.name = "Collision Point";
                //  DebugVector.DrawVector(trueLimbEnd, DebugVector.Direction.all, 5f, .1f, Color.red, Color.white, Color.blue);
            }
        }
    }
}
