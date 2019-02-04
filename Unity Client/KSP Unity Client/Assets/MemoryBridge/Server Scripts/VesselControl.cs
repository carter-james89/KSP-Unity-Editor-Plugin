
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using Winterdom.IO.FileMap;

public class VesselControl : MonoBehaviour
{

    public enum FlightControlMode { RawStick,Assist,Automous}
    public FlightControlMode controlMode;

    public struct Vessel { public Transform transform; public List<Part> parts; public Transform meshOffset; }
    public Vessel vessel;

    MemoryMappedFile vesselFile;
    MemoryBridge memoryBridge;

    public Vector3 PIDvaluesRollHigh, PIDvaluesRollLow;
    public Vector3 PIDvaluesPitchHigh, PIDvaluesPitchLow;
    public Vector3 PIDvaluesYawHigh, PIDvaluesYawLow;

    public List<Part> vesselParts;
    public Vector3 gimbalOffset, vesselOffset;

    public Transform COM;

    Material mirrorMat, targetMat, inRangeMat;

    public Transform adjustedGimbal;

    public VesselSerializer targetVessel, mirrorVessel;

    PID PIDyaw, PIDpitch, PIDroll;

    public void ControlAwake(MemoryBridge memoryBridge)
    {
        this.memoryBridge = memoryBridge;

        ToggleAutoPilot(true);

        mirrorMat = Resources.Load("MirrorMat", typeof(Material)) as Material;
        targetMat = Resources.Load("TargetMat", typeof(Material)) as Material;
        inRangeMat = Resources.Load("InRangeMat", typeof(Material)) as Material;

        // gimbalOffset = Vector3.zero;
        mirrorVessel = gameObject.AddComponent(typeof(VesselSerializer)) as VesselSerializer;
        vessel = mirrorVessel.DrawVessel(this.memoryBridge, mirrorMat, "Mirror Vessel COM");
        Debug.Log("Mirror Vessel Built with Part Count : " + vessel.parts.Count);
        targetVessel = gameObject.AddComponent(typeof(VesselSerializer)) as VesselSerializer;
        targetVessel.DrawVessel(this.memoryBridge, targetMat, "Target Vessel COM");
       // Debug.Log("Target Vessel Copied from Mirror");

        vessel.transform.SetParent(transform.Find("Gimbal").transform);


        PIDpitch = new PID(PIDvaluesPitchHigh, PIDvaluesPitchLow);
        PIDyaw = new PID(PIDvaluesYawHigh, PIDvaluesYawLow);
        PIDroll = new PID(PIDvaluesRollHigh, PIDvaluesRollLow);

        GameObject gimbalObject = new GameObject();
        gimbalObject.name = "Adjusted Gimbal";
        adjustedGimbal = gimbalObject.transform;
        adjustedGimbal.SetParent(GameObject.Find("Gimbal").transform);
        // DebugVector.DrawVector(adjustedGimbal);

        targetVessel.vessel.SetParent(transform.Find("Gimbal").transform);

        memoryBridge.SetFloat("VesselYaw", 1.1f);
        memoryBridge.SetFloat("VesselPitch", 1.1f);
        memoryBridge.SetFloat("VesselRoll", 1.1f);

        //memoryBridge.SetFloat("testnull" , null);

        //   targetVessel.gameObject.SetActive(false);
        //  GameObject COMobject = new GameObject();
        //  COMobject.name = "COM";
        //  COM = COMobject.transform;

        // var vesselCOM = memoryBridge.LocalCOM;
        //  var convertedCOM = new Vector3(vesselCOM.x, -vesselCOM.z, vesselCOM.y);
        //  COM.localPosition = convertedCOM;

        //COM.SetParent(transform.parent);
        //transform.SetParent(COM);

        //COM.position = Vector3.zero;

        if(controlMode == FlightControlMode.RawStick)
        {
            targetVessel.vessel.gameObject.SetActive(false);
        }

    }
    bool autoPilot = false;
    bool vesselInRange = false;
    public void VesselUpdate()
    {
        var vesselOffst = memoryBridge.GetVector3("AdjustedVesselCOMoffset" + memoryBridge.fileName);
        vessel.meshOffset.localPosition = vesselOffst;
        targetVessel.vesselOffset.localPosition = vesselOffst;

        var kspOffset = memoryBridge.GetQuaternion("VesselGimbleOffset" + memoryBridge.fileName);
        vessel.transform.rotation = kspOffset;

        adjustedGimbal.rotation = vessel.transform.rotation;

        var tempRot = adjustedGimbal.localRotation;
        tempRot.x = 0;
        tempRot.z = 0;
        adjustedGimbal.localRotation = tempRot;

        if(controlMode == FlightControlMode.RawStick)
        {
            targetVessel.vessel.rotation = adjustedGimbal.rotation;

            System.TimeSpan deltaTime = TimeSpan.FromSeconds(Time.fixedDeltaTime);

            bool recievingPitchInput = false;
            bool recievingRollInput = false;
            bool recievingYawInput = false;

            var rollInput = Input.GetAxis("Roll");
            var pitchInput = Input.GetAxis("Pitch");
            var yawInput = Input.GetAxis("Yaw");

          //  Debug.Log(pitcInput);

            if (rollInput != 0)
                recievingRollInput = true;
            if (pitchInput !=0)
                recievingPitchInput = true;
            if (yawInput !=0)
                recievingYawInput = true;

           // var tempVesselRot = targetVessel.vessel.transform.rotation;
            if (recievingYawInput)
            {
                memoryBridge.SetFloat("VesselYaw", yawInput);
               // tempVesselRot.y = mirrorVessel.vessel.transform.rotation.y;
            }
            else
            {
                memoryBridge.SetFloat("VesselYaw", 1.1f);
            }
            if (recievingPitchInput)
            {
                memoryBridge.SetFloat("VesselPitch", pitchInput);
               // tempVesselRot.x = mirrorVessel.vessel.transform.rotation.x;
            }
            else
            {
                memoryBridge.SetFloat("VesselPitch", 1.1f);
            }
            if (recievingRollInput)
            {
                memoryBridge.SetFloat("VesselRoll", rollInput);
               // tempVesselRot.z = mirrorVessel.vessel.transform.rotation.z;
            }
            else
            {
                memoryBridge.SetFloat("VesselRoll", 1.1f);
            }
            //  targetVessel.vessel.transform.rotation = tempVesselRot;

            var targetVesselOffset = Quaternion.Inverse(targetVessel.vessel.rotation) * vessel.transform.rotation;

            //if (rollInput > 0 & Math.Abs(rollInput) <= 1)
            //{
            //    memoryBridge.SetFloat("VesselRoll", rollInput);
            //}
            //else
            //{
            //    memoryBridge.SetFloat("VesselRoll", PIDroll.CalculateResult(targetVesselOffset.z * 10, deltaTime));
            //}
           
            //memoryBridge.SetFloat("VesselPitch", pitchInput);
            //memoryBridge.SetFloat("VesselYaw", yawInput);

            // var targetVesselOffset = targetVessel.vessel.localEulerAngles - mirrorVessel.vessel.localEulerAngles;
            
         //   Debug.Log(targetVesselOffset);

           

           // memoryBridge.SetFloat("VesselPitch", PIDpitch.CalculateResult(targetVesselOffset.x * 10, deltaTime));

            
        }


        //  memoryBridge.SetFloat("VesselYaw", PIDyaw.CalculateResult(targetVesselOffset.y, deltaTime));

        //var rawRoll = targetVesselOffset.x;
        //float rollError = 0;

        //if (rawRoll > 180)
        //{
        //    rollError = 360 - rawRoll;
        //}
        //else
        //{
        //    rollError = -rawRoll;
        //}



        //if (autoPilot)
        //{
        //    //Debug.Log(Quaternion.Angle(adjustedGimbal.rotation, transform.rotation));
        //    var targetAngle = Quaternion.Angle(adjustedGimbal.rotation, transform.rotation);
        //    if (targetAngle < 3 & !vesselInRange)
        //    {
        //        OnVesselEnterTargerRange();
        //    }
        //    if (targetAngle >= 3 & vesselInRange)
        //    {
        //        OnVesselExitTargetRange();
        //    }
        //}

    }
    class ProximityPID
    {
        PidController trgtPID;
        public ProximityPID(double P, double I, double D, double max, double min)
        {
            trgtPID = new PidController(P, I, D, max, min);
            trgtPID.SetPoint = 0;
        }
        public double FixedUpdate(double processVariable, System.TimeSpan deltaTime)
        {
            trgtPID.ProcessVariable = processVariable;
            double trgtAngle = trgtPID.ControlVariable(deltaTime);

            return trgtAngle;
        }
    }


    class PID
    {
        PidController PIDlow, PIDhigh;
        public enum PIDMode { High, Low }
        public PIDMode mode;
        public Quaternion lockVector;
        // Vessel activeVessel;

        public bool pidRunning = false;

        public PID(Vector3 PIDvaluesHigh, Vector3 PIDvaluesLow)
        {
            PIDhigh = new PidController(PIDvaluesHigh.x, PIDvaluesHigh.y, PIDvaluesHigh.z, 1, -1);
            PIDlow = new PidController(PIDvaluesLow.x, PIDvaluesLow.y, PIDvaluesLow.z, 1, -1);
            PIDhigh.SetPoint = 0;
            PIDlow.SetPoint = 0;

            mode = PIDMode.Low;
        }
        public void StartPID()
        {
            pidRunning = true;
        }

        public float CalculateResult(float error, System.TimeSpan deltaTime)
        {
            float result = 0;
            if (pidRunning)
            {
                switch (mode)
                {
                    case PIDMode.Low:
                        PIDlow.ProcessVariable = error;
                        result = (float)PIDlow.ControlVariable(deltaTime);
                        break;
                    case PIDMode.High:
                        PIDhigh.ProcessVariable = error;
                        result = (float)PIDhigh.ControlVariable(deltaTime);
                        break;
                }
            }
            return result;
        }
    }


    public void ToggleAutoPilot(bool active)
    {
        if (active)
        {
            memoryBridge.SetBool("ClientAutoPilotActive", true);
        }
        else
        {
            memoryBridge.SetBool("ClientAutoPilotActive", false);
        }
        autoPilot = active;
    }

    void OnVesselEnterTargerRange()
    {
        Debug.Log("Acceptable range");
        vesselInRange = true;
        mirrorVessel.gameObject.SetActive(false);
        targetVessel.SetVesselMaterial(inRangeMat);
        //  targetVessel.set
    }
    void OnVesselExitTargetRange()
    {
        Debug.Log("Left Acceptable range");
        vesselInRange = false;
        mirrorVessel.gameObject.SetActive(true);
        targetVessel.SetVesselMaterial(targetMat);
    }

}
