using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using Winterdom.IO.FileMap;

public class IR_Manager : MonoBehaviour {

    MemoryMappedFile IRFile;
    
   // List<LimbControllersIK> IKlimbs;

    public void CustomAwake(MemoryBridge memoryBridge,VesselControl vesselControl, ref List<LimbController> limbs)
    {
        //IRFile = MemoryMappedFile.Open(MapAccess.FileMapAllAccess, "IRFile" + memoryBridge.fileName);
        //limbs = new List<LimbController>();
        //float byteCount;
        //float limbCount;
        //using (Stream mapStream = IRFile.MapView(MapAccess.FileMapAllAccess, 0, 16))
        //{
        //    var floatBuffer = new byte[4];

        //    mapStream.Read(floatBuffer, 0, 4);
        //    byteCount = BitConverter.ToInt32(floatBuffer, 0);
        //    mapStream.Read(floatBuffer, 0, 4);
        //    limbCount = BitConverter.ToInt32(floatBuffer, 0);
        //}
        //Debug.Log("vessel has limbs " + limbCount);
        //using (Stream mapStream = IRFile.MapView(MapAccess.FileMapAllAccess, 0, (int)byteCount))
        //{
        //    mapStream.Position = 16;

        //    for (int i = 0; i < limbCount; i++)
        //    {
        //        var floatBuffer = new byte[4];
        //        mapStream.Read(floatBuffer, 0, 4);
        //        var stringByteLength = BitConverter.ToSingle(floatBuffer, 0);

        //        var stringBuffer = new byte[(int)stringByteLength];
        //        mapStream.Read(stringBuffer, 0, stringBuffer.Length);
        //        string limbName = ASCIIEncoding.ASCII.GetString(stringBuffer);

        //        var newLimbObject = new GameObject();
        //        newLimbObject.name = limbName;
        //        newLimbObject.transform.SetParent(vesselControl.vessel.transform);
        //        LimbController newLimb;
        //        if (limbName.ToLower().Contains("leg"))
        //        {
        //            newLimb = newLimbObject.AddComponent<LegController>();
                    
        //        }
        //        else
        //        {
        //            newLimb = newLimbObject.AddComponent(typeof(LimbController)) as LimbController;
        //        }
                
        //        newLimb.CustomAwake(memoryBridge,  vesselControl);
        //        limbs.Add(newLimb);
        //      //  Debug.Log("Add limb " + limbName);
        //    }
        //}
    }

    void OnDestroy()
    {
        if (IRFile != null)
            IRFile.Close();
    }
}
