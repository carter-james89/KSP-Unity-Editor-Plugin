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
            this.servo.Speed = 20;
            this.servo.Acceleration = 20;
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

            Debug.Log("Bases count " + bases.Count);
            if (bases.Count == 0)
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

                Debug.Log(this.servo.HostPart.name);

                //Move the x axis of the rotatron part. This happens in Unity script as well in Parts
                if (this.servo.HostPart.name == "IR.Rotatron.Basic.v3")
                {
                    servoAnchor.localEulerAngles += new Vector3(0, 0, 90);
                    anchorChild.localEulerAngles += new Vector3(0, 0, 90);
                    // DebugVector.DrawVector(servoAnchor);
                  //  DebugVector.DrawVector(anchorChild);
                    Debug.Log("Draw Vector");
                }

                DebugVector.DrawVector(servoAnchor);
            }
        }

        Transform contactPoint;
        LineRenderer footRenderer;
        public void CreateContactPoint()
        {

           // var joint = servo.HostPart.gameObject.AddComponent<SpringJoint>();

            contactPoint = new GameObject().transform;
            contactPoint.SetParent(transform);
            // contactPoint.localPosition = localPos;
            contactPoint.localPosition = Vector3.zero;
            contactPoint.rotation = memoryBridge.vesselControl.gimbal.rotation;
            DebugVector.DrawVector(contactPoint);

            footRenderer = contactPoint.gameObject.AddComponent<LineRenderer>();
            Material redMat = new Material(Shader.Find("Transparent/Diffuse"));
            redMat.color = Color.red;
            footRenderer.material = redMat;
            footRenderer.SetWidth(.1f, .1f);
        }

        public void ParentSet(Transform newParent)
        {
            //  servoAnchor.LookAt(newParent);         
        }

        public void CheckFootClearance()
        {
            LayerMask mask = (1 << 0) | (1 << 10);
            mask = ~mask;
            RaycastHit hit;
            //Vector3 rayRot = -hipRotServo.transform.right;
            contactPoint.rotation = memoryBridge.vesselControl.gimbal.rotation;

            Vector3 rayRot = -contactPoint.up;// -controller.launchVector.trans.up;
            float clearance = 0;
            if (Physics.Raycast(contactPoint.position, rayRot, out hit, 100, mask))
            {
               // if (hit.collider.gameObject.tag != "Runway")
                //    Debug.Log("hitting " + hit.collider.gameObject.name + " at layer " + hit.collider.gameObject.layer.ToString() + " with tag " + hit.collider.gameObject.tag.ToString() + " at distance " + hit.distance);
                // if (hit.collider.gameObject.name == "Kerbin Zn123222323")
                //     controller.walking = false;
                footRenderer.SetPosition(0, contactPoint.position);
                footRenderer.SetPosition(1, hit.point);
                clearance = hit.distance;
                
            }

            bool groundContact = false;
            var parts = servo.HostPart.children;

            foreach  (var part in parts)
            {
               // part.gr
                if (part.GroundContact)
                    groundContact = true;
            }
           // if(servo.HostPart.GroundContact != groundContact)
          //  {
           //     groundContact = servo.HostPart.GroundContact;
                memoryBridge.SetBool(servoName + "GroundContact", groundContact);
           // }
            memoryBridge.SetFloat(servoName + "KSPFootClearance", clearance);
        }
        //bool groundContact = false;
        public void CustomUpdate()
        {
            //var angleOffset = memoryBridge.GetVector3(servoName + "adjustedOffset");

            // if (angleOffset != Vector3.zero)
            //    Debug.Log(name + " set adjusted offset");
            //    servoAnchor.localEulerAngles = angleOffset;
            memoryBridge.SetFloat(servoName + "servoPos", servo.Position);

            
            servo.MoveTo(memoryBridge.GetFloat(servoName + "unityServoPos"), memoryBridge.GetFloat(servoName + "unityServoSpeed"));

            anchorChild.rotation = part.transform.rotation;
            if (this.servo.HostPart.name == "IR.Rotatron.Basic.v3")
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
