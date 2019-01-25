using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using Winterdom.IO.FileMap;
using System.Threading;

namespace MemoryBridgeServer
{
    class MemoryBridge : MonoBehaviour
    {
        public List<CameraFeed> cameraFeeds;

        VesselControl vesselControl;
        FloatFile floatFile;
        MemoryFileConstants fileConstants;

        public string fileName;

        public bool bridgeConnected;
        bool serverActive = false;

        public float clientTransmittingFloat;

        public void StartServer(string fileName)
        {
            vessel = FlightGlobals.ActiveVessel;
            Debug.Log("Server start");
            bridgeConnected = false;
            clientTransmittingFloat = 0;
            this.fileName = fileName;
            fileConstants = gameObject.AddComponent(typeof(MemoryFileConstants)) as MemoryFileConstants;
            floatFile = gameObject.AddComponent(typeof(FloatFile)) as FloatFile;
            floatFile.StartServer(this);

            vesselControl = gameObject.AddComponent(typeof(VesselControl)) as VesselControl;
            vesselControl.ControlStart(vessel,this);

            cameraFeeds = new List<CameraFeed>();

            SetFloat("testfloat", 3);
            serverActive = true;
        }

        //void OnVesselUnpacked()
        //{
        //    vessel.VesselValues.
        //}

        void Update()
        {
            if (serverActive)
            {
                floatFile.StartUpdate();

                //bool clientTransmitting = GetBool("ClientTransmitting" + fileName);
                // Debug.Log(clientTransmitting);
                if (clientTransmittingFloat == 1 & !bridgeConnected)
                {
                    bridgeConnected = true;
                    Debug.Log("bridgeConnected");
                }

                if (clientTransmittingFloat == 0 & bridgeConnected)
                {
                    Debug.Log("Client stopped transmitting");
                    bridgeConnected = false;

                    switch (GetFloat("OnClientDisconnectOption" + fileName))
                    {
                        case 0:
                            FlightDriver.RevertToLaunch();
                            break;
                        case 1:
                            var game = GamePersistence.LoadGame("quicksave", HighLogic.SaveFolder, false, false);
                            FlightDriver.StartAndFocusVessel(game, game.flightState.activeVesselIdx);
                            break;
                    }
                }
                ReadClientValues();

                vesselControl.CustomUpdate();
                var valueTest = GetFloat("ClientCameraFeedCount" + fileName);
                //Debug.Log(valueTest);
                if (cameraFeeds.Count != valueTest)
                    CreateCameraFeed();
            }        
        }
        void LateUpdate()
        {
            if (serverActive)
            {
                WriteServerValues();
                floatFile.EndUpdate();
            }
           
        }
        void CreateCameraFeed()
        {
            //Debug.Log("build camera");
            var newFeed = gameObject.AddComponent(typeof(CameraFeed)) as CameraFeed;
            cameraFeeds.Add(newFeed);
            newFeed.BuildCameraFeed(this, fileName);
        }

        void ReadClientValues()
        {
            

        }

        void OnDestroy()
        {
            if (serverActive)
            {
                floatFile.CustomOnDestroy();
            }
        }

        public bool GetBool(string key)
        {
            bool returnBool = false;
            if (GetFloat(key) == 1)
                returnBool = true;
            return returnBool;
        }
        public void SetBool(string key, bool boolValue)
        {
            if (boolValue)
                SetFloat(key, 1);
            else
                SetFloat(key, 0);
        }
        public Quaternion GetQuaternion(string key)
        {
            Quaternion returnQuat = new Quaternion(0, 0, 0, 0);
            for (int i = 0; i < 4; i++)
            {
                switch (i)
                {
                    case 0:
                        returnQuat.w = GetFloat(key + "w");
                        break;
                    case 1:
                        returnQuat.x = GetFloat(key + "x");
                        break;
                    case 2:
                        returnQuat.y = GetFloat(key + "y");
                        break;
                    case 3:
                        returnQuat.z = GetFloat(key + "z");
                        break;
                }
            }
            return returnQuat;
        }
        public void SetQuaternion(string key, Quaternion value)
        {
            for (int i = 0; i < 4; i++)
            {
                switch (i)
                {
                    case 0:
                        SetFloat(key + "w", value.w);
                        break;
                    case 1:
                        SetFloat(key + "x", value.x);
                        break;
                    case 2:
                        SetFloat(key + "y", value.y);
                        break;
                    case 3:
                        SetFloat(key + "z", value.z);
                        break;
                }
            }
        }
        public Vector3 GetVector3(string key)
        {
            Vector3 returnVector = Vector3.zero;
            for (int i = 0; i < 3; i++)
            {
                if (i == 0)
                    returnVector.x = GetFloat(key + "x");
                else if (i == 1)
                    returnVector.y = GetFloat(key + "y");
                else
                    returnVector.z = GetFloat(key + "z");
            }
            return returnVector;
        }
        public void SetVector3(string key, Vector3 value)
        {
            for (int i = 0; i < 3; i++)
            {
                if (i == 0)
                    SetFloat(key + "x", value.x);
                else if (i == 1)
                    SetFloat(key + "y", value.y);
                else
                    SetFloat(key + "z", value.z);
            }
        }
        Vessel vessel;
        void WriteServerValues()
        {
            if (vessel)
            {
                SetVector3("LocalCOM" + fileName, vessel.localCoM);
                SetVector3("CurrentCOM" + fileName, vessel.CurrentCoM);
                
                SetVector3("Acceleration" + fileName, vessel.acceleration);
                SetVector3("obt_velocity" + fileName, vessel.obt_velocity);
                SetVector3("srf_velocity" + fileName, vessel.srf_velocity);

                SetFloat("altitude" + fileName, (float)vessel.altitude);
                SetFloat("heightFromTerrain" + fileName, (float)vessel.heightFromTerrain);
                SetFloat("horizontalSrfSpeed" + fileName, (float)vessel.horizontalSrfSpeed);
                SetFloat("latitude" + fileName, (float)vessel.latitude);
                SetFloat("longitude" + fileName, (float)vessel.longitude);
                SetFloat("mach" + fileName, (float)vessel.mach);

                SetFloat("srfSpeed" + fileName, (float)vessel.srfSpeed);
                SetFloat("terrainAltitude" + fileName, (float)vessel.terrainAltitude);
                SetFloat("verticalSpeed" + fileName, (float)vessel.verticalSpeed);
                SetFloat("currentStage" + fileName, (float)vessel.currentStage);
                SetFloat("geeForce_immediate" + fileName, (float)vessel.geeForce_immediate);
                SetFloat("heightFromSurface" + fileName, (float)vessel.heightFromSurface);
                SetFloat("obt_speed" + fileName, (float)vessel.obt_speed);

                SetBool("directSunlight" + fileName, vessel.directSunlight);
                SetBool("Landed" + fileName, vessel.Landed);
                SetBool("loaded" + fileName, vessel.loaded);
                SetBool("Splashed" + fileName, vessel.Splashed);
            }
        }

        public float GetFloat(string key)
        {
            if (!key.Contains(fileName))
                key = key + fileName;
            return floatFile.GetFloat(key);
        }
        public void SetFloat(string key, float value)
        {
            if (!key.Contains(fileName))
                key = key + fileName;
            floatFile.SetFloat(key, value);
        }
    }
}
