using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using UnityEngine;
using Winterdom.IO.FileMap;

public class VesselSerializer : MonoBehaviour
{
    public List<Mesh> meshList;
    public List<Part> parts;
    MemoryMappedFile vesselFile;
    MemoryBridge memoryBridge;

    public Transform vessel, vesselOffset;

    string vesselName;

    public VesselControl.Vessel DrawVessel(MemoryBridge memoryBridge, Material drawMat, string vesselName)
    {
        VesselControl.Vessel newVessel;
        this.memoryBridge = memoryBridge;
        this.vesselName = vesselName;

        var binFormatter = new BinaryFormatter();
        var mStream = new MemoryStream();

        meshList = new List<Mesh>();
        parts = new List<Part>();
        // partList = new List<VesselPart>();

        var vesselObject = new GameObject();
        vesselObject.name = vesselName;
        vessel = vesselObject.transform;
        vessel.SetParent(transform);
        vessel.position = Vector3.zero;
        vessel.localEulerAngles = Vector3.zero;
        //DebugVector.DrawVector(vessel);

        var modelObject = new GameObject();
        modelObject.name = "Vessel Offset";
        vesselOffset = modelObject.transform;
        vesselOffset.SetParent(vessel);
        vesselOffset.position = Vector3.zero;
        vesselOffset.localEulerAngles = Vector3.zero;

        if (vesselFile == null)
            vesselFile = MemoryMappedFile.Open(MapAccess.FileMapAllAccess, "VesselFile" + memoryBridge.fileName);

        if (vesselFile == null)
        {
            Debug.Log("file is null");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
          
#endif
        }          

        int byteCount = 0;
        int partCount = 0;

        using (Stream mapStream = vesselFile.MapView(MapAccess.FileMapAllAccess, 0, 8))
        {
            var totalByteBuffer = new byte[4];
            var partCountBuffer = new byte[4];

            mapStream.Read(totalByteBuffer, 0, 4);
            mapStream.Read(partCountBuffer, 0, 4);

            byteCount = BitConverter.ToInt32(totalByteBuffer, 0);
            partCount = BitConverter.ToInt32(partCountBuffer, 0);
        }
        using (Stream mapStream = vesselFile.MapView(MapAccess.FileMapAllAccess, 0, byteCount))
        {
            mapStream.Position = 8;

            for (int i = 0; i < partCount; i++)
            {
                List<MeshRenderer> meshRenderers = new List<MeshRenderer>();
                //part unique id
                var totalpartBuffer = new byte[4];
                mapStream.Read(totalpartBuffer, 0, 4);
                var uniqueID= BitConverter.ToSingle(totalpartBuffer, 0);

                //parent unique id
                totalpartBuffer = new byte[4];
                mapStream.Read(totalpartBuffer, 0, 4);
                var parentUniqueID = BitConverter.ToSingle(totalpartBuffer, 0);

                //filter count
                totalpartBuffer = new byte[4];
                mapStream.Read(totalpartBuffer, 0, 4);
                var filterCount = BitConverter.ToSingle(totalpartBuffer, 0);

                GameObject newPart = new GameObject();
                var newPartObject = newPart.AddComponent(typeof(Part)) as Part;
                
                Vector3 vesselPartOffset = Vector3.zero;
                var tempBuffer = new Byte[4];
                mapStream.Read(tempBuffer, 0, 4);
                vesselPartOffset.x = BitConverter.ToSingle(tempBuffer, 0);
                mapStream.Read(tempBuffer, 0, 4);
                vesselPartOffset.y = BitConverter.ToSingle(tempBuffer, 0);
                mapStream.Read(tempBuffer, 0, 4);
                vesselPartOffset.z = BitConverter.ToSingle(tempBuffer, 0);
                newPart.transform.localPosition = vesselPartOffset;
                
                //rot offset
                Vector3 vesselPartRotOffset = Vector3.zero;
                tempBuffer = new Byte[4];
                mapStream.Read(tempBuffer, 0, 4);
                vesselPartRotOffset.x = BitConverter.ToSingle(tempBuffer, 0);
                mapStream.Read(tempBuffer, 0, 4);
                vesselPartRotOffset.y = BitConverter.ToSingle(tempBuffer, 0);
                mapStream.Read(tempBuffer, 0, 4);
                vesselPartRotOffset.z = BitConverter.ToSingle(tempBuffer, 0);
                newPart.transform.localEulerAngles = vesselPartRotOffset;

                //part name
                mapStream.Read(tempBuffer, 0, 4);
                var stringByteLength = BitConverter.ToSingle(tempBuffer, 0);
                var stringBuffer = new byte[(int)stringByteLength];
                mapStream.Read(stringBuffer, 0, stringBuffer.Length);
                newPartObject.kspPartName = ASCIIEncoding.ASCII.GetString(stringBuffer);

                
                GameObject meshObjects = new GameObject();
                meshObjects.name = "Mesh Objects";
                newPartObject.partMeshes = meshObjects;
                meshObjects.transform.SetParent(newPart.transform);
                meshObjects.transform.localEulerAngles = Vector3.zero;

                for (int k = 0; k < filterCount; k++)
                {
                   //Debug.Log("start writing filter at " + mapStream.Position);
                    var newMesh = Instantiate(Resources.Load("BlankPart", typeof(GameObject))) as GameObject;
                    newMesh.transform.SetParent(newPart.transform);
                    meshRenderers.Add(newMesh.GetComponent<MeshRenderer>());
                    newMesh.GetComponent<MeshRenderer>().material = drawMat;

                    Vector3 posVector = Vector3.zero;
                    tempBuffer = new Byte[4];
                    mapStream.Read(tempBuffer, 0, 4);
                    posVector.x = BitConverter.ToSingle(tempBuffer, 0);
                    mapStream.Read(tempBuffer, 0, 4);
                    posVector.y = BitConverter.ToSingle(tempBuffer, 0);
                    mapStream.Read(tempBuffer, 0, 4);
                    posVector.z = BitConverter.ToSingle(tempBuffer, 0);
                    newMesh.transform.localPosition = posVector;

                    var localEuler = Vector3.zero;
                    // tempBuffer = new Byte[4];
                    mapStream.Read(tempBuffer, 0, 4);
                    localEuler.x = BitConverter.ToSingle(tempBuffer, 0);
                    mapStream.Read(tempBuffer, 0, 4);
                    localEuler.y = BitConverter.ToSingle(tempBuffer, 0);
                    mapStream.Read(tempBuffer, 0, 4);
                    localEuler.z = BitConverter.ToSingle(tempBuffer, 0);
                    newMesh.transform.localEulerAngles = localEuler;

                    Vector3 scaleVector = Vector3.zero;
                    //  tempBuffer = new Byte[4];
                    mapStream.Read(tempBuffer, 0, 4);
                    scaleVector.x = BitConverter.ToSingle(tempBuffer, 0);
                    mapStream.Read(tempBuffer, 0, 4);
                    scaleVector.y = BitConverter.ToSingle(tempBuffer, 0);
                    mapStream.Read(tempBuffer, 0, 4);
                    scaleVector.z = BitConverter.ToSingle(tempBuffer, 0);
                    newMesh.transform.localScale = scaleVector;

                    var lengthBuffer = new Byte[4];
                    mapStream.Read(lengthBuffer, 0, 4);
                    var byteMeshLength = BitConverter.ToSingle(lengthBuffer, 0);


                    var meshBuffer = new Byte[(int)byteMeshLength];
                    mapStream.Read(meshBuffer, 0, (int)byteMeshLength);
                    newMesh.GetComponent<MeshFilter>().mesh = MeshSerializer.ReadMesh(meshBuffer);

                    //not sure why this doesnt work up top, but too lazy to figure it out
                    newMesh.transform.SetParent(meshObjects.transform);
                }

                newPartObject.CustomAwake((int)uniqueID, (int)parentUniqueID, vesselName,meshRenderers);
                parts.Add(newPartObject);
            }
        }

        newVessel.transform = vessel;
        newVessel.parts = parts;
        newVessel.meshOffset = vesselOffset;       

        foreach (var part in newVessel.parts)
        {
            part.vessel = newVessel;
            part.FindParent();
        }
        return newVessel;
    }

    public void SetVesselMaterial(Material newMat)
    {
        //foreach (var renderer in meshRenderers)
        //{
        //    renderer.material = newMat;
        //}
    }
    //VesselPart RawDeserializeEx(byte[] byteArr)
    //{
    //    VesselPart newPart = new VesselPart();

    //    int size = Marshal.SizeOf(newPart);
    //    IntPtr ptr = Marshal.AllocHGlobal(size);

    //    Marshal.Copy(byteArr, 0, ptr, size);

    //    newPart = (VesselPart)Marshal.PtrToStructure(ptr, newPart.GetType());
    //    Marshal.FreeHGlobal(ptr);

    //    return newPart;
    //}
    List<byte[]> byteArrayList = new List<byte[]>();
    void OnDestroy()
    {
        if (vesselFile != null)
            vesselFile.Close();
    }
}
