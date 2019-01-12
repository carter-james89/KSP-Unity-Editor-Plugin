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
    public RoboticLimbMirror limbMirror;
    public RoboticLimbIK limbIK;

    public RoboticServo servoBase;
    VesselControl vesselControl;

    public virtual void CustomAwake(MemoryBridge memoryBridge, string limbName, VesselControl vesselControl)
    {
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

        //limbMirror.SetStartAngles();
        //limbIK.SetStartAngles();
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
       // transform.localEulerAngles = Vector3.zero;
        transform.localPosition = Vector3.zero;
        transform.LookAt(baseMirror.up);//,vesselControl.mirrorVessel.transform.up);
        transform.rotation = Quaternion.LookRotation(baseMirror.up,vesselControl.adjustedGimbal.up);
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
        limbIK.servoWrist = mirrorLimb.servoWrist;
        limbIK.servoBase = mirrorLimb.servoBase;
        //limbIK.limbController = mirrorLimb.limbController;
        limbIK.limbEnd = mirrorLimb.limbEnd;
        Destroy(mirrorLimb);

        limbIK.ConvertToIKLimb(limbMirror);
        limbIK.SetLimbReference();
        limbIK.StoreGroupedServos();
    }
    bool IKactive = false;
    public void ActivateIK()
    {
        limbIK.ActivateIK();
        IKactive = true;
    }

    public void CustomUpdate()
    {
       limbMirror.MirrorServos();
       // if (!IKactive)
        limbIK.SetServos();

        if (Input.GetKeyDown(KeyCode.Alpha6))
            ActivateIK();

        if (IKactive)
        {
            limbIK.RunIK();
        }
    }

    void OnDestroy()
    {
        if (limbFile != null)
            limbFile.Close();
    }

}
