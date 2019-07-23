using System.Collections.Generic;
using Winterdom.IO.FileMap;
using UnityEngine;
using System.IO;
using System;
using System.Text;

public class LimbController : MonoBehaviour
{
    public enum LimbAxis { X, Y, Z }
    MemoryMappedFile limbFile;
    protected MemoryBridge memoryBridge;

    List<RoboticServoIK> servosIK;

    Transform baseMirror, baseIK;
    [HideInInspector]
    public RoboticLimbMirror limbMirror;
    [HideInInspector]
    public RoboticLimbIK limbIK;

    public RoboticServo servoBase;
    [HideInInspector]
    public VesselControl vesselControl;

    bool gaitSequenceActive = false;

    [HideInInspector]
    public Transform ground, baseTarget;// contactPoint;
    [HideInInspector]
    public MeshRenderer groundRenderer;

    [HideInInspector]
    public RoboticController roboticController;

    public enum LegMode { Translate, Rotate }
    public LegMode legMode;

    public Vector3 contactPointOffset;

   

    public virtual void CustomAwake(MemoryBridge memoryBridge, string limbName, VesselControl vesselControl)
    {
        Debug.Log("Custom Awake Limb : " + limbName);
        this.memoryBridge = memoryBridge;
        this.vesselControl = vesselControl;
        limbFile = MemoryMappedFile.Open(MapAccess.FileMapAllAccess, limbName);
        float byteCount;
        float servoCount;
        using (Stream mapStream = limbFile.MapView(MapAccess.FileMapAllAccess, 0, 16))
        {
            var floatBuffer = new byte[4];
            mapStream.Read(floatBuffer, 0, 4);
            byteCount = BitConverter.ToSingle(floatBuffer, 0);
            mapStream.Read(floatBuffer, 0, 4);
            servoCount = BitConverter.ToSingle(floatBuffer, 0);
        }
        var servosMirror = new List<RoboticServoMirror>();

        ground = Instantiate(GameObject.Find("Ground")).transform;

        using (Stream mapStream = limbFile.MapView(MapAccess.FileMapAllAccess, 0, (int)byteCount))
        {
            mapStream.Position = 16;

            for (int i = 0; i < servoCount; i++)
            {
                //servo name
                var floatBuffer = new byte[4];
                mapStream.Read(floatBuffer, 0, 4);
                var stringByteLength = BitConverter.ToSingle(floatBuffer, 0);

                var stringBuffer = new byte[(int)stringByteLength];
                mapStream.Read(stringBuffer, 0, stringBuffer.Length);
                string servoName = ASCIIEncoding.ASCII.GetString(stringBuffer);

                //servo parent id
                mapStream.Read(floatBuffer, 0, 4);
                var partID = BitConverter.ToSingle(floatBuffer, 0);
                mapStream.Read(floatBuffer, 0, 4);
                var parentID = BitConverter.ToSingle(floatBuffer, 0);

                foreach (var part in vesselControl.vessel.parts)
                {
                    if (part.ID == partID)
                    {
                        var newServo = part.gameObject.AddComponent(typeof(RoboticServoMirror)) as RoboticServoMirror;
                        newServo.CustomStart(servoName, memoryBridge, this, (int)parentID);
                        servosMirror.Add(newServo);
                    }
                }
            }
        }

        foreach (var servo in servosMirror)
        {
            servo.CreateBaseAnchor();
        }
        CreateRoboticLegs(servosMirror);
    }


    public float CalculateStridePercent()
    {
        float returnFloat = 300;
        //   Debug.Log(name + " " + legMode + " - " + Time.frameCount);
        if (legMode == LegMode.Rotate)
        {
            //returnFloat = limbIK.CalculateRotStridePercent();
        }
        else
        {
            returnFloat = limbIK.CalculateTranslateStridePercent();
        }
        return returnFloat;
    }
    public void StartGait()
    {
        gaitSequenceActive = true;
        limbIK.StartGaitSequence();
    }
    [HideInInspector]
    public float servoAcceleration = 5;
    public float baseLerpSpeed = 2;
    [HideInInspector]
    public float baseError;
    [HideInInspector]
    public float baseRotOffset;

    public float hipHeightError, hipHeightErrorGlobal;
    public float gaitBelowGround;
    public float targetEndPointError;
    public float IKtargetError;
    public float IKtargetYOffset;

    public void SetGaitRotation(float newRotation)
    {
        limbIK.SetGaitRotation(newRotation);
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

    public void RunGait(float stridePercentAvg)
    {
        //move the gait for active balance
        if (limbIK.gait)
        {
            limbIK.RunGait(stridePercentAvg);
        }
    }

    public void LimbUpdate()
    {
        limbIK.RunIK();
        SetServos();
        IKtargetError = Vector3.Distance(limbIK.limbEnd.position, limbIK.IKtargetTransform.position);
        IKtargetYOffset = limbIK.limbEnd.position.y - limbIK.IKtargetTransform.position.y;
    }
    public void SetServos()
    {
        limbIK.SetServos();
    }
    public bool mirrorAtTarget;
    public float limbError;
    public bool MirrorLegAtTarget()
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
    public bool debugClearance;
    float footClearance = 0;
    public bool groundContact;
   // Transform collisionPoint;

    public void SetBaseTarget(Transform baseTarget)
    {
        this.baseTarget = baseTarget;
        Debug.Log("servo pos ", limbIK.IKAxisX.servo1.gameObject);
        this.baseTarget.position = limbIK.IKAxisX.servo1.transform.position;//limbMirror.servoBase.transform.position;
    }
    //Creates RoboticLimb components for the mirror and IK leg, adds them to limb base servo base, then aranges the hiarchy
    void CreateRoboticLegs(List<RoboticServoMirror> servosMirror)
    {
        //Set up mirrorLimb component on the base part base
        baseMirror = servoBase.servoBase.transform;
        limbMirror = baseMirror.gameObject.AddComponent(typeof(RoboticLimbMirror)) as RoboticLimbMirror;
        limbMirror.CustomStart(this);
        limbMirror.servosMirror = servosMirror;
        limbMirror.SetLimbReference();

        limbMirror.FindEndPoint();
        //Move the Limb controller component Object into place
        transform.SetParent(baseMirror);
        transform.localPosition = Vector3.zero;
        transform.LookAt(baseMirror.up);//,vesselControl.mirrorVessel.transform.up);
        transform.rotation = Quaternion.LookRotation(baseMirror.up, vesselControl.adjustedGimbal.up);
        // transform.rotation = vesselControl.mirrorVessel.transform.rotation;
        transform.SetParent(baseMirror.parent);
        baseMirror.SetParent(transform);
        //get mirror limb groups before generating IK limb
        limbMirror.CalculateGroups();

        baseIK = Instantiate(baseMirror).transform;
        baseMirror.name += "Mirror Arm";
        baseIK.name += "IK Arm";
        baseIK.SetParent(transform);
        baseIK.rotation = limbMirror.transform.rotation;
        baseIK.localPosition = Vector3.zero;
        ////Mirror leg and limb controller are in place, set reference for IK limb that was generated, then build the leg
        var mirrorLimb = baseIK.gameObject.GetComponent<RoboticLimbMirror>();
        limbIK = baseIK.gameObject.AddComponent<RoboticLimbIK>();
        limbIK.CustomStart(this);
      //  limbIK.servoWrist = mirrorLimb.servoWrist;
     //   limbIK.servoBase = mirrorLimb.servoBase;
        //limbIK.limbController = mirrorLimb.limbController;
      //  limbIK.limbEnd = mirrorLimb.limbEnd;
        Destroy(mirrorLimb);


       
        limbIK.ConvertToIKLimb(limbMirror);
        limbIK.SetLimbReference();


        // limbIK.FindEndPoint(false, "ik");
        limbIK.StoreGroupedServos();
    }
    bool IKactive = false;
    public void ActivateIK()
    {
        SetServos();
        limbIK.ActivateIK();
        SetServos();
        IKactive = true;
        mirrorAtTarget = true;
    }

    public void AddGaitTarget(Transform newTarget, LimbController.LegMode newMode)
    {
        limbIK.AddGaitTarget(newTarget, newMode);
    }
    public void ResetGaitSequence()
    {
        limbIK.ResetGaitSequence();
    }
    public void NextGaitSeguence()
    {
        limbIK.MoveToNextTarget();
    }

    public void MirrorServos()
    {
        limbMirror.MirrorServos();
    }
    float rawClearance;

    public Vector3 torque;
    public float explosionPotential;
    public float gExplodeChance;
    public bool footActive;
    public float velocity;
    public bool hasExploded;

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
    //public void SetBaseHeight(float globalY)
    //{
    //    if (limbIK.gait)
    //    {
    //        var tempPos = baseTarget.position;
    //        tempPos.y = globalY;
    //        baseTarget.position = tempPos;
    //    }
    //}

    void OnDestroy()
    {
        if (limbFile != null)
            limbFile.Close();
    }
}
