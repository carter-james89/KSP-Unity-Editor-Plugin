using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using Winterdom.IO.FileMap;
using UnityEngine;

namespace MemoryBridgeServer
{
    class IR_Manager : MonoBehaviour
    {
        MemoryMappedFile IRFile;
        List<RoboticArm> limbs;

        int byteCount = 0;
        List<string> groups;
        public void CustomStart(List<Part> parts, MemoryBridge memoryBridge)
        {
            limbs = new List<RoboticArm>();

            foreach (IRWrapper.IControlGroup group in IRWrapper.IRController.ServoGroups)
            {
                if (group.Name.ToLower().Contains("ik"))
                {
                    IRWrapper.IServo basePart = null;
                    IRWrapper.IServo wristPart = null;
                    foreach (var servo in group.Servos)
                    {
                        if (servo.Name.ToLower().Contains("base"))
                        {
                            basePart = servo;
                        }
                        if (servo.Name.ToLower().Contains("wrist"))
                        {
                            wristPart = servo;
                        }
                    }
                    var newLimb = gameObject.AddComponent(typeof(RoboticArm)) as RoboticArm;
                    newLimb.CustomAwake(group,basePart,wristPart,memoryBridge);
                    limbs.Add(newLimb);

                    byteCount += 4;
                    byteCount += newLimb.limbName.Length;
                }
            }

            byteCount += 16;
            IRFile = MemoryMappedFile.Create(MapProtection.PageReadWrite, byteCount, "IRFile" + memoryBridge.fileName);

            using (Stream mapStream = IRFile.MapView(MapAccess.FileMapAllAccess, 0, byteCount))
            {
                var bytePackage = BitConverter.GetBytes(byteCount);
                mapStream.Write(bytePackage, 0, 4);
                bytePackage = BitConverter.GetBytes(limbs.Count);
                mapStream.Write(bytePackage, 0, 4);

                mapStream.Position = 16;

                foreach (var limb in limbs)
                {
                    var nameBytePackage = Encoding.ASCII.GetBytes(limb.limbName);
                    bytePackage = BitConverter.GetBytes((float)nameBytePackage.Length);
                    mapStream.Write(bytePackage, 0, 4);
                    mapStream.Write(nameBytePackage, 0, nameBytePackage.Length);
                }
            }

            //legs = new List<MechLeg>();

                //foreach (IRWrapper.IControlGroup group in IRWrapper.IRController.ServoGroups)
                //{
                //    if (group.Name == "Leg One")
                //    {
                //        legOne = new MechLeg(memoryBridge, group, this, "Leg One");
                //        legs.Add(legOne);
                //    }
                //    else if (group.Name == "Leg Two")
                //    {
                //        legTwo = new MechLeg(memoryBridge, group, this, "Leg Two");
                //        legs.Add(legTwo);
                //    }
                //    else if (group.Name == "Leg Three")
                //    {
                //        legThree = new MechLeg(memoryBridge, group, this, "Leg Three");
                //        legs.Add(legThree);
                //    }
                //    else if (group.Name == "Leg Four")
                //    {
                //        legFour = new MechLeg(memoryBridge, group, this, "Leg Four");
                //        legs.Add(legFour);
                //    }
                //    else if (group.Name == "Leg Five")
                //    {
                //        legFive = new MechLeg(memoryBridge, group, this, "Leg Five");
                //        legs.Add(legFive);
                //    }
                //    else if (group.Name == "Leg Six")
                //    {
                //        legSix = new MechLeg(memoryBridge, group, this, "Leg Six");
                //        legs.Add(legSix);
                //    }
                //    else if (group.Name == "Arm")
                //    {
                //        armOne = new MechArm(memoryBridge, group, this);
                //    }
                //}
        }

        public void CustomUpdate()
        {
            foreach (var limb in limbs)
            {
                limb.CustomUpdate();
            }
        }
        void OnDestroy()
        {
            if (IRFile != null)
                IRFile.Close();
        }
    }
}
