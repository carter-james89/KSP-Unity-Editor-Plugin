using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gait : MonoBehaviour
{
    public enum GaitTarget { MidPoint, FrontPoint, BackPoint }
    public Transform gait { get; private set; }
    public Transform pointMid { get; private set; }
    public Transform pointFront { get; private set; }
    public Transform pointBack { get; private set; }
    public Transform IKtargetTransform, rotAxis;
    public Transform currentTarget;

    List<Transform> gaitSequenceTarget;
    int sequencePos = 0;

    bool atTarget = true;

    Quaternion targetRot, startRot;
    public Transform translateStartPos;

    public enum GaitMode { Arc, Curve }
    public GaitMode gaitMode = GaitMode.Curve;

    public enum MovementType { Translate, Rotate }
    public MovementType movementMode;
    List<MovementType> movementModeSequence;

    public AnimationCurve gaitCurve;
    public float gaitCurveY, gaitCurveX;

    ServoLimb limb;

    int walkCycle = 0;
    float walkSpeed = 1;


    private void Awake()
    {
        var obj = Instantiate(Resources.Load("Limb Target", typeof(GameObject))) as GameObject;
        IKtargetTransform = obj.transform;

        rotAxis = transform.Find("Axis Point");
        IKtargetTransform.SetParent(rotAxis);
        pointMid = transform.Find("Point Mid");
        pointFront = transform.Find("Point Front");
        pointBack = transform.Find("Point Back");
        //gait.position = limbMirror.limbEnd.position;
    }
    public void Initialize(ServoLimb limb, AnimationCurve gaitCurve)
    {
        this.limb = limb;
        this.gaitCurve = gaitCurve;
        DefaultStrideLength();
    }

    public void AddGaitTarget(Transform newTarget, MovementType moveType)
    {
        Debug.Log(name + " add gate target " + newTarget.name);
        if (gaitSequenceTarget == null)
        {
            gaitSequenceTarget = new List<Transform>();
        }
        gaitSequenceTarget.Add(newTarget);
        if (movementModeSequence == null)
        {
            movementModeSequence = new List<MovementType>();
        }
        movementModeSequence.Add(moveType);
    }

    public void StartGait()
    {
        UpdateTarget(0);
    }
    //TODO : this is using the IK end point, but i think it needs to use mirror
    void UpdateTarget(int pos)
    {
        sequencePos = pos;
        var newTransform = gaitSequenceTarget[pos];
        var legMode = movementModeSequence[pos];

        rotAxis.localEulerAngles = Vector3.zero;
        rotAxis.position = limb.limbEndPointIK.position + (newTransform.position - limb.limbEndPointIK.position) / 2;

        if (legMode == MovementType.Rotate)//newTargetOffset.z > 0)// & legdDir == LegDirection.Forward)
        {
            rotAxis.LookAt(newTransform, -Vector3.up);
            IKtargetTransform.position = limb.limbEndPointIK.position;

            startRot = rotAxis.localRotation;
            targetRot = Quaternion.Euler(rotAxis.localEulerAngles + new Vector3(179, 0, 0));
            legMode = MovementType.Rotate;
        }
        else
        {
            IKtargetTransform.position = currentTarget.position;
            translateStartPos = currentTarget;
            legMode = MovementType.Translate;
        }
        currentTarget = newTransform;
        atTarget = false;
    }

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
                var error = Vector3.Distance(IKtargetTransform.position, limb.limbEndPointIK.position);
                if (error < .5f)
                    switch (movementMode)
                    {
                        case MovementType.Translate:
                            {
                                var speed = Time.deltaTime;
                                if (walkCycle >= 1)
                                {
                                    speed *= walkSpeed;
                                }
                                IKtargetTransform.position = Vector3.MoveTowards(IKtargetTransform.position, currentTarget.transform.position, speed);
                                var dir = IKtargetTransform.position - currentTarget.position;
                            }
                            break;
                        case MovementType.Rotate:
                            if (walkCycle >= 1)
                            {

                                if (gaitMode == GaitMode.Curve)
                                {
                                    pointBack.localScale = Vector3.one;

                                    var length = Math.Abs(gaitCurve.keys[0].time) + gaitCurve.keys[gaitCurve.keys.Length - 1].time;
                                    gaitCurveX = (avgStridePercent + .03f) * length;

                                    IKtargetTransform.position = pointBack.TransformPoint(new Vector3(0, 0, gaitCurveX));

                                    gaitCurveY = gaitCurve.Evaluate(gait.InverseTransformPoint(IKtargetTransform.position).z);



                                    IKtargetTransform.position = pointBack.TransformPoint(new Vector3(0, gaitCurveY, gaitCurveX));
                                }
                                else
                                {
                                    rotAxis.localRotation = Quaternion.Lerp(startRot, targetRot, avgStridePercent + .03f);
                                }
                            }
                            else
                                rotAxis.localRotation = Quaternion.Lerp(startRot, targetRot, avgStridePercent + .03f);
                          //  rotPercent = avgStridePercent;
                            break;
                        default:
                            break;
                    }
            }


        }
    }

    public void SetGaitHeight()
    {
        transform.rotation = Quaternion.LookRotation(limb.robotManager.vessel.transform.forward, Vector3.up);
        var tempEuler = transform.eulerAngles;
        tempEuler.x = 0;
        transform.eulerAngles = tempEuler;

        var baseOffset = limb.baseTarget.InverseTransformPoint(limb.servos[0].transform.position);

        var globalPoint = limb.transform.TransformPoint(limb.defaultGaitLocalPos);
        globalPoint.y = limb.groundPoint.position.y;
       // var hipHeightError = baseOffset.y;

        if (movementMode == MovementType.Translate || limb.mirrorAtTarget)
        {
           globalPoint.y += baseOffset.y;
            transform.position = globalPoint;
           // gaitBelowGround = ground.InverseTransformPoint(limbIK.gait.transform.position).y;
        }
        else
        {
            transform.position = globalPoint;
        }
       // targetEndPointError = Vector3.Distance(limbMirror.limbEnd.transform.position, limbIK.IKtargetTransform.position);
    }

    public void DefaultStrideLength()
    {
        Debug.Log("set default stride length");
        // pointFront.localPosition = new Vector3(0, 0, strideLength / 2);

        if (gaitMode == GaitMode.Arc)
        {
            // pointFront.localPosition = new Vector3(0, 0, strideLength / 2);
            // pointBack.localPosition = new Vector3(0, 0, (strideLength / -2));
        }
        else
        {

            pointBack.localPosition = new Vector3(0, 0, -gaitCurve.keys[gaitCurve.keys.Length - 1].time);// gaitCurve.keys[0].time);
            pointFront.localPosition = new Vector3(0, 0, gaitCurve.keys[gaitCurve.keys.Length - 1].time);
        }

        // pointBack.localPosition = new Vector3(0, 0, -strideLength / 2);
        // adjustedStride = false;
    }
}
