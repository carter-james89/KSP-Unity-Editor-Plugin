using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LegController : LimbController
{
    [HideInInspector]
    public Transform ground, baseTarget;// contactPoint;
    [HideInInspector]
    public MeshRenderer groundRenderer;

    

    public override void CustomAwake(MemoryBridge memoryBridge,  VesselControl vesselControl, RoboticController roboticController)
    {
        base.CustomAwake(memoryBridge, vesselControl,roboticController);

        ground = Instantiate(GameObject.Find("Ground")).transform;

    }

    public void SetBaseTarget(Transform baseTarget)
    {
        this.baseTarget = baseTarget;
        Debug.Log("servo pos ", limbIK.IKAxisX.servo1.gameObject);
        this.baseTarget.position = limbIK.IKAxisX.servo1.transform.position;//limbMirror.servoBase.transform.position;
    }

    public void SetGaitHeight()
    {
        limbIK.gait.transform.rotation = Quaternion.LookRotation(vesselControl.vessel.transform.forward, Vector3.up);
        var tempEuler = limbIK.gait.eulerAngles;
        tempEuler.x = 0;
        limbIK.gait.eulerAngles = tempEuler;



        var baseOffset = baseTarget.InverseTransformPoint(limbIK.IKAxisX.servo1.transform.position);


        // var baseOffset = limbIK.IKAxisX.servo1.transform.position - baseTarget.transform.position;
        var globalPoint = limbIK.transform.TransformPoint(limbIK.gaitStartPos);
        globalPoint.y = ground.position.y;

        //var hipPointOffset = servoBase.transform.InverseTransformPoint(baseTarget.transform.position);
        //var angle = Math.Atan2(hipPointOffset.y, -hipPointOffset.z);
        //angle *= (180 / Math.PI);
        //baseRotOffset = (float)(90 - angle);

        hipHeightError = baseOffset.y;
        // hipHeightErrorGlobal = Vector3.Distance(baseTarget.position, adjustedGlobalPoint);

        if (legMode == LegMode.Translate || mirrorAtTarget)
        {
            //  Debug.Log("setting gait height");
            globalPoint.y += baseOffset.y;
            // limbIK.gait.localPosition = Vector3.Lerp(limbIK.gait.localPosition, limbIK.transform.InverseTransformPoint(globalPoint), Time.deltaTime * baseLerpSpeed);
            // limbIK.gait.position = Vector3.Lerp(limbIK.gait.position, globalPoint, Time.deltaTime * baseLerpSpeed);
            limbIK.gait.position = globalPoint;
            // ground.transform.localScale = Vector3.one;
            gaitBelowGround = ground.InverseTransformPoint(limbIK.gait.transform.position).y;
            //limbIK.gait.position = Vector3.Lerp(globalPoint,globalPoint + baseOffset.y,)
            //  limbIK.gait.position = ground.position - new Vector3(0, baseOffset.y, 0);
        }
        else
        {
            // limbIK.gait.localPosition = limbIK.transform.InverseTransformPoint(globalPoint);
            limbIK.gait.position = globalPoint;
        }
        targetEndPointError = Vector3.Distance(limbMirror.limbEnd.transform.position, limbIK.IKtargetTransform.position);
        //var tempEuler = limbIK.gait.eulerAngles;// = Vector3.zero;
        //tempEuler.x = 0;
        //limbIK.gait.eulerAngles = tempEuler;
    }
    public override bool MirrorLegAtTarget()
    {
        mirrorAtTarget = false;
        if (limbIK.currentTarget)
        {
            var groundClearance = ground.InverseTransformPoint(limbMirror.limbEnd.position).normalized;
            limbError = Vector3.Distance(limbMirror.limbEnd.position, limbIK.currentTarget.position);

            var yDif = limbMirror.limbEnd.position.y - ground.position.y;

            if (legMode == LegMode.Rotate)
            {
                if (yDif < .03f && limbError < .2f)// & groundContact)
                {
                    mirrorAtTarget = true;
                }
            }
            else
            {
                if (limbError < .05f)
                {
                    mirrorAtTarget = true;
                }
            }

        }
        return mirrorAtTarget;
    }
    float rawClearance;

    public Vector3 torque;
    public float explosionPotential;
    public float gExplodeChance;
    public bool footActive;
    public float velocity;
    public bool hasExploded;

    float footClearance = 0;
        bool groundContact = false;
    public void CheckClearance()
    {
        if (IKactive)//& legMode == LegMode.Translate)
        {
            //limbMirror.contactPoint.position = limbMirror.servoWrist.transform.TransformPoint(memoryBridge.GetVector3(limbMirror.servoWrist.servoName + "CollisionPoint"));
            // limbIK.contactPoint.position = limbIK.servoWrist.transform.TransformPoint(memoryBridge.GetVector3(limbMirror.servoWrist.servoName + "CollisionPoint"));
        }
        torque = memoryBridge.GetVector3(limbMirror.servoWrist.servoName + "torque");
        velocity = torque.magnitude;
        explosionPotential = memoryBridge.GetFloat(limbMirror.servoWrist.servoName + "explosionPotential");
        gExplodeChance = memoryBridge.GetFloat(limbMirror.servoWrist.servoName + "gExplodeChance");
        footActive = memoryBridge.GetBool(limbMirror.servoWrist.servoName + "active");
        hasExploded = memoryBridge.GetBool(limbMirror.servoWrist.servoName + "exploded");

        if (hasExploded)
        {
            CamUI.SetCamText(name + " has exploded");
            Debug.LogError("Exploded Leg : " + name + " Velocity : " + velocity + " Mode : " + limbIK.gaitSequenceMode.ToString() + " Percent : " + CalculateStridePercent());
        }

        rawClearance = memoryBridge.GetFloat(limbMirror.servoWrist.servoName + "KSPFootClearance");
        //if(limbMirror.limbEnd == null)
        //{
        //    Debug.Log("end point null");
        //}
        var contactPointOffSet = limbMirror.servoWrist.transform.position.y - limbMirror.trueLimbEnd.position.y;
        footClearance = rawClearance - contactPointOffSet;
        if (footClearance < .03f)
        {
            footClearance = 0;
        }
        groundContact = memoryBridge.GetBool(limbMirror.servoWrist.servoName + "GroundContact");

        //if (!collisionPoint)
        //{
        //    collisionPoint = new GameObject("CollisionPoint").transform;
        //   // DebugVector.DrawVector(collisionPoint, DebugVector.Direction.all, .5f, .1f, Color.red, Color.white, Color.blue);
        //}
        //collisionPoint.position = limbMirror.servoWrist.transform.TransformPoint(memoryBridge.GetVector3(limbMirror.servoWrist.servoName + "CollisionPoint"));

        // limbMirror.
        //if(limbIK)


        //if (!groundContact)
        // ground.position = limbMirror.servoWrist.transform.position - new Vector3(0, rawClearance, 0);
        var localPoint = limbMirror.limbEnd.localPosition - new Vector3(0, .4f, 0);
        ground.position = limbMirror.servoWrist.transform.TransformPoint(localPoint) - new Vector3(0, rawClearance, 0);
    }
}
