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

        RoboticArm arm;

        void Start()
        {
            // memoryBridge.SetFloat(servoName + "unityServoPos", servo.Position);
        }

        public void CustomStart(IRWrapper.IServo servo, ref MemoryBridge memoryBridge, RoboticArm arm)
        {
            this.servo = servo;
            //this.servo.SpeedLimit = 20;

            this.arm = arm;
            servoName = servo.Name + servo.HostPart.gameObject.GetInstanceID();
            part = servo.HostPart;

            this.memoryBridge = memoryBridge;

            Debug.Log(servo.Name + " servo name");
            if (servo == null)
                Debug.Log("null position " + servo.Position);
            Debug.Log("Get position");
            memoryBridge.SetFloat(servoName + "unityServoPos", servo.Position);
            Debug.Log("got position");

            memoryBridge.SetFloat(servoName + "minPos", servo.MinPosition);
            Debug.Log("servo min pos " + servo.MinPosition);
            memoryBridge.SetFloat(servoName + "maxPos", servo.MaxPosition);
            Debug.Log("servo max pos " + servo.MaxPosition);

            this.servo.Acceleration = 999;
            this.servo.Speed = 999;

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
                DebugVector.DrawVector(anchorChild);
            }
        }

        Transform contactPoint;
        LineRenderer footRenderer;
        List<Part> footParts;

        public void CreateContactPoint(Vector3 contactPos)
        {
            var parts = servo.HostPart.FindChildParts<Part>(true);
            footParts = new List<Part>();
            foreach (var part in parts)
            {
                footParts.Add(part);
                Debug.Log(name + " attatched foot parts " + part.name);
            }

            contactPoint = new GameObject().transform;
            contactPoint.SetParent(transform);
            contactPoint.localPosition = contactPos - new Vector3(0, .4f, 0);
            //contactPoint.localPosition = Vector3.zero;
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

        void OnDestroy()
        {
            if (footPart)
                footPart.OnJustAboutToBeDestroyed -= PartAboutToBeDestroyed;
        }
        Part footPart;
        Rigidbody footBody;
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

            foreach (var part in footParts)
            {
                if (part.GroundContact)
                {
                    if (!footPart)
                    {
                        footPart = part;
                        footBody = footPart.Rigidbody;
                        footPart.OnJustAboutToBeDestroyed += PartAboutToBeDestroyed;
                        memoryBridge.SetBool(servoName + "exploded", false);
                    }

                    groundContact = true;

                    var closestPoint = part.collider.ClosestPoint(memoryBridge.vesselControl.vessel.mainBody.transform.position);

                    if (!collisionPoint)
                    {
                        collisionPoint = new GameObject().transform;

                        DebugVector.DrawVector(collisionPoint);
                    }
                    collisionPoint.position = closestPoint;

                    var collisionPointOffset = transform.InverseTransformPoint(collisionPoint.position);
                    memoryBridge.SetVector3(servoName + "CollisionPoint", collisionPointOffset);
                }
            }
            if (footPart)
            {
                memoryBridge.SetVector3(servoName + "torque", footBody.velocity);
                memoryBridge.SetFloat(servoName + "explosionPotential", footPart.explosionPotential);
                memoryBridge.SetFloat(servoName + "gExplodeChance", footPart.gExplodeChance);
                memoryBridge.SetBool(servoName + "active", footPart.gameObject.activeInHierarchy);

                // var events = footPart.Events;
                // events.

                // footPart.explosionPotential;
                // footPart.gExplodeChance()
            }
            // if(servo.HostPart.GroundContact != groundContact)
            //  {
            //     groundContact = servo.HostPart.GroundContact;
            memoryBridge.SetBool(servoName + "GroundContact", groundContact);
            // }
            memoryBridge.SetFloat(servoName + "KSPFootClearance", clearance);
        }
        //void OnCollisionStay(Collision collision)
        //{

        void PartAboutToBeDestroyed()
        {
            Debug.Log("part exploded");
            memoryBridge.SetBool(servoName + "exploded", true);
        }
        void JointBroken()
        {

        }
        Transform collisionPoint;

        //    Debug.Log(part.name + " collision stay");
        //}
        //bool groundContact = false;
        bool running = false;
        public void CustomUpdate()
        {
            //var angleOffset = memoryBridge.GetVector3(servoName + "adjustedOffset");

            // if (angleOffset != Vector3.zero)
            //    Debug.Log(name + " set adjusted offset");
            //    servoAnchor.localEulerAngles = angleOffset;
          
            memoryBridge.SetFloat(servoName + "servoPos", servo.Position);

            //var acceleration = memoryBridge.GetFloat(servoName + "unityServoAcceleration");
            //if (servo.Acceleration != acceleration && acceleration != 0)
            //    servo.Acceleration = acceleration;


            
            //
            if (Input.GetKey(KeyCode.Keypad5))
            {
               // Debug.Log("start moving servo");
                running = true;
             //   servo.MoveTo(memoryBridge.GetFloat(servoName + "unityServoPos"), memoryBridge.GetFloat(servoName + "unityServoSpeed"));
                servo.MoveTo(90, 5);
            }
           // if(running)
               servo.MoveTo(memoryBridge.GetFloat(servoName + "unityServoPos"), memoryBridge.GetFloat(servoName + "unityServoSpeed"));
            // servo.mo

            anchorChild.rotation = part.transform.rotation;
            if (this.servo.HostPart.name == "IR.Rotatron.Basic.v3")
            {
                anchorChild.localEulerAngles += new Vector3(0, 0, 90);
            }
            memoryBridge.SetVector3(servoName + "servoLocalEuler", anchorChild.localEulerAngles);
            memoryBridge.SetQuaternion(servoName + "servoLocalRot", anchorChild.localRotation);


            if (arm.limbName.Contains("neck") && servo.Name.Contains("base"))
            {
              //  Debug.Log(anchorChild.localRotation.ToString());
            }

            //var servoAngle = memoryBridge.GetFloat(servoName + "ServoSetPos");
            //var servoSpeed = memoryBridge.GetFloat(servoName + "ServoSetSpeed");
            //if (servoAngle < 200)
            //    servo.MoveTo(servoAngle, servoSpeed);
        }
    }
}
