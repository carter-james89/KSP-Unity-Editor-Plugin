using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using UnityEngine;
using Winterdom.IO.FileMap;

namespace MemoryBridgeServer
{
    class VesselControl : MonoBehaviour
    {
        public Vessel vessel;
        MemoryBridge memoryBridge;

        VesselSerializer vesselSerializer;

        bool autoPilot = false;

        MemoryMappedFile vesselFile;

        public Transform gimbal;
        public Transform vesselCOM, adjustedVessel;

        IR_Manager IRmanager;

        Vector3 gimbalOffset = new Vector3(0, 90, 270);

        float rollInput = 1.1f, yawInput = 1.1f, pitchInput = 1.1f, throttle = 0;

        public void ControlStart(Vessel vessel, MemoryBridge memoryBridge)
        {
            this.vessel = vessel;
            this.memoryBridge = memoryBridge;

            //  var IVAcam = vessel.gameObject.GetComponentInChildren<IVACamera>();

            //  if (IVAcam)
            //      Debug.Log("IVA cam found on " + IVAcam.name);

            //Create a vessel vector that can be adjusted
            GameObject adjustedVesselObject = new GameObject();
            adjustedVessel = adjustedVesselObject.transform;
            adjustedVessel.SetParent(vessel.vesselTransform);
            adjustedVessel.localEulerAngles = Vector3.zero;
            adjustedVessel.localPosition = Vector3.zero;
            if (vessel.name.ToLower().Contains("aircraft"))
                adjustedVessel.localEulerAngles += new Vector3(-90, 0, 0);
            //DebugVector.DrawVector(adjustedVessel);

            //Create a transform for the vessels COM, placed as a child of the Vessel
            GameObject COMobject = new GameObject();
            vesselCOM = COMobject.transform;
            vesselCOM.SetParent(vessel.vesselTransform);
            vesselCOM.localEulerAngles = Vector3.zero;
            vesselCOM.localPosition = vessel.localCoM;
            // DebugVector.DrawVector(vesselCOM);

            GameObject gimbalObject = new GameObject();
            gimbal = gimbalObject.transform;
            gimbal.SetParent(vesselCOM);
            gimbal.localPosition = Vector3.zero;
            gimbal.localEulerAngles = Vector3.zero;
            //DebugVector.DrawVector(gimbal);

            if (vessel.name.ToLower().Contains("aircraft"))
                vesselCOM.localEulerAngles += new Vector3(-90, 0, 0);

            vesselSerializer = gameObject.AddComponent(typeof(VesselSerializer)) as VesselSerializer;
            vesselSerializer.SerializeVessel(this.vessel, this.memoryBridge, this);

            IRmanager = gameObject.AddComponent(typeof(IR_Manager)) as IR_Manager;
            IRmanager.CustomStart(vessel.Parts, memoryBridge);

            ToggleAutoPilot(true);
        }
        public void CustomUpdate()
        {
            var clientAutoPilot = memoryBridge.GetBool("ClientAutoPilotActive");
            if (clientAutoPilot & !autoPilot)
                ToggleAutoPilot(true);
            else if (!clientAutoPilot & autoPilot)
                ToggleAutoPilot(false);

            ReadClientInputs();

            //  Debug.Log(memoryBridge.GetFloat("TestFloatValue"));
            //   vesselTransform.localPosition = memoryBridge.GetVector3("VesselTransformLocalPos"+memoryBridge.fileName);
            vesselCOM.localPosition = vessel.localCoM;
            var vesselCOMoffset = vesselCOM.InverseTransformPoint(vessel.vesselTransform.position);
            memoryBridge.SetVector3("AdjustedVesselCOMoffset" + memoryBridge.fileName, vesselCOMoffset);
            var gimbalUp = vessel.mainBody.transform.position - vesselCOM.position;
            gimbal.LookAt(gimbalUp);
            gimbal.eulerAngles += gimbalOffset;//memoryBridge.GetVector3("GimbalOffset");

            //  adjustedGimbal.LookAt(gimbalUp,vessel.vesselTransform.position);
            // var tempEuler = adjustedGimbal.localRotation;
            //tempEuler.x = 0;
            //tempEuler.z = 0;
            //adjustedGimbal.localRotation = tempEuler;

            var vesselGimbleOffset = Quaternion.Inverse(gimbal.rotation) * adjustedVessel.rotation;//gimbal.eulerAngles - vesselTransform.eulerAngles;
            memoryBridge.SetQuaternion("VesselGimbleOffset" + memoryBridge.fileName, vesselGimbleOffset);

            IRmanager.CustomUpdate();
            

            if (autoPilot)
            {
                rollInput = memoryBridge.GetFloat("VesselRoll");
                yawInput = memoryBridge.GetFloat("VesselYaw");
                pitchInput = memoryBridge.GetFloat("VesselPitch");
            }
        }

        public void ToggleActionGroup(int group)
        {
            //VesselControl.SetThrottle(1);
            vessel.ActionGroups.ToggleGroup(KSPActionGroup.Custom04);
            // VesselControl.activeVessel.ActionGroups.ToggleGroup(KSPActionGroup.Brakes);
        }

        public void ToggleAutoPilot(bool active)
        {
            if (active)
            {
                Debug.Log("auto Pilot On");
                vessel.OnFlyByWire += new FlightInputCallback(SetFlightControls);

            }
            else
            {
                Debug.Log("auto pilot off");
                vessel.OnFlyByWire -= new FlightInputCallback(SetFlightControls);
            }
            autoPilot = active;
        }

        void SetFlightControls(FlightCtrlState state)
        {

            if (rollInput <= 1)
                state.roll = rollInput;
            //Debug.Log(memoryBridge.GetFloat("TestFloatValue"));

            //var pitchInput = memoryBridge.GetFloat("VesselPitch");
            if (pitchInput <= 1)
                state.pitch = pitchInput;


            //var yawInput = memoryBridge.GetFloat("VesselYaw");
            if (yawInput <= 1)
                state.yaw = yawInput;

            if (throttle > 0)
                state.mainThrottle = throttle;

        }
        void OnDestroy()
        {
            Destroy(gimbal.gameObject);
        }

        void ReadClientInputs()
        {
            var actionGroup1Bool = memoryBridge.GetBool("ActionGroup1");
            if (actionGroup1Bool)
            {
                vessel.ActionGroups.ToggleGroup(KSPActionGroup.Custom01);
                memoryBridge.SetFloat("ActionGroup1", 0);
            }


            throttle = memoryBridge.GetFloat("Throttle");

        }


    }


}
