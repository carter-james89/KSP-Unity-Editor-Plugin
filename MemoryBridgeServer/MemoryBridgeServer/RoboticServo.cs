using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MemoryBridgeServer
{
    class RoboticServo : MonoBehaviour
    {
        public IRWrapper.IServo servo;
        public Part part;
        public string servoName;
        public string parentServoName;
        public int parentID = 0;
        MemoryBridge memoryBridge;
        Transform servoAnchor, anchorChild, servoBase;
        public void CustomStart(IRWrapper.IServo servo, MemoryBridge memoryBridge)
        {
            this.servo = servo;
            servoName = servo.Name + servo.HostPart.gameObject.GetInstanceID();
            part = servo.HostPart;

            this.memoryBridge = memoryBridge;

            memoryBridge.SetFloat(servoName + "unityServoPos", servo.Position);

            memoryBridge.SetFloat(servoName + "minPos", servo.MinPosition);
            Debug.Log("servo min pos " + servo.MinPosition);
            memoryBridge.SetFloat(servoName + "maxPos", servo.MaxPosition);
            Debug.Log("servo max pos " + servo.MaxPosition);

            // memoryBridge.SetFloat(servoName + "ServoSetPos", 210f);
            //  memoryBridge.SetFloat(servoName + "ServoSetSpeed", 210f);

            //search the parent part for a transform named "Base"
            var partParent = part.parent;
            var parentChildren = partParent.GetComponentsInChildren<Transform>();
            var bases = new List<Transform>();
            foreach (var child in parentChildren)
            {
                if (child.name.ToLower().Contains("base"))
                    bases.Add(child);
            }

            if(bases.Count == 0)
            {
                Debug.Log(servo.Name + " does not have a base");
            }
            else
            {
                //If there are multiple bases on the parent, use the closest one
                if (bases.Count > 1)
                {
                    var minDist = Mathf.Infinity;
                    foreach (var basePart in bases)
                    {
                        var dist = Vector3.Distance(transform.position, basePart.position);
                        if (dist < minDist)
                        {
                            servoBase = basePart;
                            minDist = dist;
                        }
                    }
                }
                else
                    servoBase = bases[0];

                var newObject = new GameObject();
                newObject.name = "Servo Anchor";
                servoAnchor = newObject.transform;
                servoAnchor.SetParent(servoBase);
                servoAnchor.localPosition = Vector3.zero;
                servoAnchor.localEulerAngles = Vector3.zero;

                newObject = new GameObject();
                newObject.name = "Servo Child";
                anchorChild = newObject.transform;
                anchorChild.SetParent(servoAnchor);
                anchorChild.localPosition = Vector3.zero;
                anchorChild.rotation = part.transform.rotation;

                //Move the x axis of the rotatron part. This happens in Unity script as well in Parts
                if (this.servo.HostPart.name == "IR.Rotatron.Basic")
                {
                    servoAnchor.localEulerAngles += new Vector3(0, 0, 90);
                    anchorChild.localEulerAngles += new Vector3(0, 0, 90);
                    // DebugVector.DrawVector(servoAnchor);
                    DebugVector.DrawVector(anchorChild);
                }

                    // DebugVector.DrawVector(servoAnchor);
                }         
        }

        public void ParentSet(Transform newParent)
        {
            //  servoAnchor.LookAt(newParent);         
        }

        public void CustomUpdate()
        {
            //var angleOffset = memoryBridge.GetVector3(servoName + "adjustedOffset");

            // if (angleOffset != Vector3.zero)
            //    Debug.Log(name + " set adjusted offset");
            //    servoAnchor.localEulerAngles = angleOffset;
            memoryBridge.SetFloat(servoName + "servoPos", servo.Position);

            servo.MoveTo(memoryBridge.GetFloat(servoName + "unityServoPos"), 10);

            anchorChild.rotation = part.transform.rotation;
            if (this.servo.HostPart.name == "IR.Rotatron.Basic")
            {
                anchorChild.localEulerAngles += new Vector3(0, 0, 90);
            }
            memoryBridge.SetVector3(servoName + "servoLocalEuler", anchorChild.localEulerAngles);
            memoryBridge.SetQuaternion(servoName + "servoLocalRot", anchorChild.localRotation);

            //var servoAngle = memoryBridge.GetFloat(servoName + "ServoSetPos");
            //var servoSpeed = memoryBridge.GetFloat(servoName + "ServoSetSpeed");
            //if (servoAngle < 200)
            //    servo.MoveTo(servoAngle, servoSpeed);
        }
    }
}
