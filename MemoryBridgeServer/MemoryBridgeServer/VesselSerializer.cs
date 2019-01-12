using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using Winterdom.IO.FileMap;

namespace MemoryBridgeServer
{
    class VesselSerializer :MonoBehaviour
    {
        MemoryMappedFile vesselFile;
        Vessel vessel;
        MemoryBridge memoryBridge;
        VesselControl vesselControl;

        [Serializable]
        struct VesselPart
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)]
            public string name;
            public Vector3 localEulerAngles;
            public Vector3 scale;
            // [MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
            public byte[] meshArray;
        }

        VesselPart partStruct;
        public List<MemoryPart> partList;
        public void SerializeVessel(Vessel vessel, MemoryBridge memoryBridge, VesselControl vesselControl)
        {
            Debug.Log("Vessel Control Awake");
            this.vessel = vessel;
            this.memoryBridge = memoryBridge;
            this.vesselControl = vesselControl;

            Debug.Log("Current Vessel " + vessel.name);

           // var kspPartList = this.vessel.Parts;
            Debug.Log("try to serialize custom part");

            //WORKS
            BuildVessel();
        }

        void OnDestroy()
        {
            if (vesselFile != null)
                vesselFile.Close();
        }
        void BuildVessel()
        {
            var kspPartList = vessel.Parts;
            partList = new List<MemoryPart>();
            var byteArrayList = new List<byte[]>();
            int byteCount = 0;
            foreach (var part in kspPartList)
            {
                //   Debug.Log("Part name to examine " + part.name);
                if (part.name != "strutConnector")
                {
                    var newPart = new MemoryPart(part,vesselControl);
                    partList.Add(newPart);
                    byteCount += newPart.byteCount;
                }
            }

            //for the header
            //+ 8 bytes for header
            //*12 bytes for VesselOffset
            //*12 bytes for PartScale
            //*12 bytes for Rotation
            byteCount += 8;

            Debug.Log("vessel file byte count: " + byteCount);
            Debug.Log("vessel mesh count: " + partList.Count);
            vesselFile = MemoryMappedFile.Create(MapProtection.PageReadWrite, byteCount, "VesselFile" + memoryBridge.fileName);

            using (Stream mapStream = vesselFile.MapView(MapAccess.FileMapAllAccess, 0, byteCount))
            {
                var byteCountPackage = BitConverter.GetBytes(byteCount);
                mapStream.Write(byteCountPackage, 0, 4);
                var partCount = BitConverter.GetBytes(partList.Count);
                mapStream.Write(partCount, 0, 4);

                foreach (var memPart in partList)
                {
                      Debug.Log("start writing part at position " + mapStream.Position);
                    //THESE BYTES ARE ALLOCATED IN THE CLASS
                    mapStream.Write(BitConverter.GetBytes(memPart.uniqueID), 0, 4);
                    mapStream.Write(BitConverter.GetBytes(memPart.parentID), 0, 4);
                    mapStream.Write(BitConverter.GetBytes((float)memPart.meshList.Count), 0, 4);

                    //THESE BYTES ARE ALLOCATED IN THE CLASS
                    mapStream.Write(BitConverter.GetBytes((float)memPart.offset.x), 0, 4);
                    mapStream.Write(BitConverter.GetBytes((float)memPart.offset.y), 0, 4);
                    mapStream.Write(BitConverter.GetBytes((float)memPart.offset.z), 0, 4);

                    //THESE BYTES ARE ALLOCATED IN THE CLASS
                    mapStream.Write(BitConverter.GetBytes((float)memPart.rotOffset.x), 0, 4);
                    mapStream.Write(BitConverter.GetBytes((float)memPart.rotOffset.y), 0, 4);
                    mapStream.Write(BitConverter.GetBytes((float)memPart.rotOffset.z), 0, 4);
                    //for name, allocated in class
                    var bytePackage = BitConverter.GetBytes((float)memPart.nameBytePackage.Length);
                    mapStream.Write(bytePackage, 0, 4);
                    mapStream.Write(memPart.nameBytePackage, 0, memPart.nameBytePackage.Length);

                    foreach (var filter in memPart.meshList)
                    {
                        //Debug.Log("start writing filter at position " + mapStream.Position);
                        //THESE BYTES ARE ALLOCATED IN THE CLASS
                        mapStream.Write(BitConverter.GetBytes((float)filter.vesselPartOffset.x), 0, 4);
                        mapStream.Write(BitConverter.GetBytes((float)filter.vesselPartOffset.y), 0, 4);
                        mapStream.Write(BitConverter.GetBytes((float)filter.vesselPartOffset.z), 0, 4);

                        //THESE BYTES ARE ALLOCATED IN THE CLASS
                        var partEulerAngle = filter.vesselPartLocalEuler;
                        //mapStream.Write(BitConverter.GetBytes((float)partEulerAngle.w), 0, 4);
                        mapStream.Write(BitConverter.GetBytes((float)partEulerAngle.x), 0, 4);
                        mapStream.Write(BitConverter.GetBytes((float)partEulerAngle.y), 0, 4);
                        mapStream.Write(BitConverter.GetBytes((float)partEulerAngle.z), 0, 4);

                        //THESE BYTES ARE ALLOCATED IN THE CLASS
                        var partScale = filter.lossyScale;
                        mapStream.Write(BitConverter.GetBytes((float)partScale.x), 0, 4);
                        mapStream.Write(BitConverter.GetBytes((float)partScale.y), 0, 4);
                        mapStream.Write(BitConverter.GetBytes((float)partScale.z), 0, 4);

                        //THESE BYTES ARE ALLOCATED IN THE CLASS
                        mapStream.Write(BitConverter.GetBytes((float)filter.byteArrMesh.Length), 0, 4);

                        // Debug.Log(memPart.name + " has a mesh byte array count of " + (filter.byteArrMesh.Length));
                        // mapStream.Write(BitConverter.GetBytes((float)filter.byteArrMesh.Length), 0, 4);
                        mapStream.Write(filter.byteArrMesh, 0, filter.byteArrMesh.Length);
                    }
                }
            }
            Debug.Log("Control Awake and Running");
        }
    }
    class MemoryPart
    {
        public string name;
        public float uniqueID, parentID = 0;
        //int meshCount;

        public byte[] byteArrTexture;
        public int byteCount;
        public Part kspPart;
        //public float partID;
        public Vector3 offset,rotOffset;

        public struct ModelFilter { public Vector3 vesselPartOffset; public Vector3 vesselPartLocalEuler; public Vector3 lossyScale; public byte[] byteArrMesh; }

        public List<ModelFilter> meshList = new List<ModelFilter>();

        public byte[] nameBytePackage;

        public MemoryPart(Part part, VesselControl vesselControl)
        {
            kspPart = part;
            byteCount = 0;
            name = part.name;
            uniqueID = part.gameObject.GetInstanceID();
            try
            {
                parentID = part.parent.gameObject.GetInstanceID();
            }
            catch { Debug.Log(part.name + " does not have a parent"); }
               

            var modelMeshes = part.FindModelComponents<MeshFilter>();

            //var modelSkinnedMeshes = part.FindModelComponents<SkinnedMeshRenderer>();

            //  var combinedArray = new Mesh[modelMeshes.Count + modelSkinnedMeshes.Count];
            //  modelMeshes.CopyTo(combinedArray);
            offset = vesselControl.adjustedVessel.InverseTransformPoint(part.transform.position);//vesselControl.adjustedVessel.InverseTransformPoint(filter.transform.position);
            var tempParent = part.transform.parent;
            part.transform.SetParent(vesselControl.adjustedVessel);//vesselControl.vesselCOM);
                                                       //  var rotOffset = Quaternion.Inverse(part.vessel.vesselTransform.rotation) * filter.transform.rotation;
            rotOffset = part.transform.localEulerAngles;

            part.transform.SetParent(tempParent);

            meshList = new List<ModelFilter>();

            //for uniqueID
            byteCount += 4;
            //for parent uniqueID
            byteCount += 4;
            //for meshfilter count
            byteCount += 4;
            //for localPosition vector3
            byteCount += 12;
            //for localRot vector3
            byteCount += 12;

            //for part name string count
            byteCount += 4;
            //for part name
            nameBytePackage = Encoding.ASCII.GetBytes(name);
            byteCount += nameBytePackage.Length;


            foreach (var filter in modelMeshes)
            {
                Debug.Log(filter.transform.name + " filter found on part " + part.name);
                bool serialize = false;
                if (!filter.name.ToLower().Contains("col"))
                {
                    var gearModule = part.GetComponent<ModuleWheelBase>();
                    if (!gearModule)
                        serialize = true;
                }
                else
                {
                    var engineModule = part.GetComponent<ModuleEngines>();
                    if (engineModule)
                        serialize = true;
                }

                if (serialize)
                {
                    ModelFilter modelFilter = new ModelFilter();

                    byteCount += 12;
                    byteCount += 12;
                    byteCount += 12;

                    modelFilter.vesselPartOffset = part.transform.InverseTransformPoint(filter.transform.position);//vesselControl.adjustedVessel.InverseTransformPoint(filter.transform.position);
                    tempParent = filter.transform.parent;
                    filter.transform.SetParent(part.transform);//vesselControl.vesselCOM);
                    //  var rotOffset = Quaternion.Inverse(part.vessel.vesselTransform.rotation) * filter.transform.rotation;
                    modelFilter.vesselPartLocalEuler = filter.transform.localEulerAngles;
                    filter.transform.SetParent(tempParent);
                    modelFilter.lossyScale = filter.transform.lossyScale;

                    //for mesh length
                    byteCount += 4;

                    var mesh = filter.mesh;
                    modelFilter.byteArrMesh = MeshSerializer.WriteMesh(mesh, true);
                    meshList.Add(modelFilter);
                    byteCount += modelFilter.byteArrMesh.Length;
                }
            }


            //Texture2D texture = part.FindModelComponent<MeshRenderer>().material.GetTexture("_MainTex") as Texture2D;

            ////var tex = new Texture2D(texture.height, texture.width, TextureFormat.RGB24, false);
            ////Graphics.ConvertTexture(texture, tex);
            //Color32[] pix = texture.GetPixels32();
            //System.Array.Reverse(pix);
            //Texture2D destTex = new Texture2D(texture.width, texture.height);
            //destTex.SetPixels32(pix);
            //destTex.Apply();


            //byteArrTexture = destTex.GetRawTextureData();

            // byteCount = byteArrMesh.Length;// + byteArrTexture.Length;
            //  Debug.Log(part.name + " mesh byte length " + byteArrMesh.Length);
            //byteArrayList.Add(MeshSerializer.WriteMesh(mesh, true));
        }
    }
    //public byte[] RawSerializeEx(object anything)
    //{
    //    //int rawsize = Marshal.SizeOf(anything);
    //    //byte[] rawdatas = new byte[rawsize];
    //    //GCHandle handle = GCHandle.Alloc(rawdatas, GCHandleType.Pinned);
    //    //IntPtr buffer = handle.AddrOfPinnedObject();
    //    //Marshal.StructureToPtr(anything, buffer, false);
    //    //handle.Free();
    //    //return rawdatas;
    //    int size = Marshal.SizeOf(anything);
    //    byte[] arr = new byte[size];

    //    IntPtr ptr = Marshal.AllocHGlobal(size);
    //    Marshal.StructureToPtr(anything, ptr, true);
    //    Marshal.Copy(ptr, arr, 0, size);
    //    Marshal.FreeHGlobal(ptr);
    //    return arr;
    //}

}
