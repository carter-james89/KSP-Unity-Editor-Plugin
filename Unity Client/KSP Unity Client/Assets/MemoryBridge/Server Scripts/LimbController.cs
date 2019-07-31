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

    protected bool IKactive = false;

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
    public RoboticController roboticController;

    public enum LegMode { Translate, Rotate }
    public LegMode legMode;

    public Vector3 contactPointOffset;

    public virtual void CustomAwake(MemoryBridge memoryBridge,  VesselControl vesselControl, RoboticController roboticController)
    {
        Debug.Log("Custom Awake Limb : " + name);
        this.memoryBridge = memoryBridge;
        this.vesselControl = vesselControl;
        this.roboticController = roboticController;
        limbFile = MemoryMappedFile.Open(MapAccess.FileMapAllAccess, name);
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

    public bool mirrorAtTarget;
    public float limbError;

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

    public bool debugClearance;

    public float hipHeightError, hipHeightErrorGlobal;
    public float gaitBelowGround;
    public float targetEndPointError;
    public float IKtargetError;
    public float IKtargetYOffset;

    public void SetGaitRotation(float newRotation)
    {
        limbIK.SetGaitRotation(newRotation);
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

    public virtual bool MirrorLegAtTarget()
    {
        return false;
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

    public void ActivateIK(bool createGait)
    {
       // Debug.Log("Limb : " + name + " Activate IK");
        SetServos();
        limbIK.ActivateIK(createGait);
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
       // Debug.Log(name + " mirror servo");
        limbMirror.MirrorServos();
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
        {
            limbFile.Dispose();
            limbFile.Close();
        }        
    }
}
