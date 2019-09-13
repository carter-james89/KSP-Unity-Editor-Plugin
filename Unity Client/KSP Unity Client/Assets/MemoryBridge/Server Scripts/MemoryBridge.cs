using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Winterdom.IO.FileMap;

public class MemoryBridge : MonoBehaviour
{
    public List<CameraFeed> cameraFeeds;
    FloatFile floatFile;
    MemoryMappedFile testFile;
    bool testStreamOpen = false;
    bool runningRead = false, runningWrite = false;
    public string fileName;
    public VesselControl vesselControl;
    public bool debugWriteToSelf;

    public Vector3 CoMD, CurrentCOM, LocalCOM, Acceleration, obt_velocity, srf_velocity;
    public float altitude, heightFromTerrain, horizontalSrfSpeed, latitude, longitude, mach, srfSpeed, terrainAltitude, verticalSpeed, currentStage;
    public float geeForce_immediate, heightFromSurface, obt_speed;
    public bool directSunlight, Landed, loaded, Splashed;

    public APIManager APImanager;

    public static bool bridgeActive;

    public float testFloat = 5, transmittingFloat;


    public RoboticController roboticController;

    public void StartClient(string fileName)
    {
        this.fileName = fileName;
        bridgeActive = false;
        transmittingFloat = 1;
        APImanager = gameObject.AddComponent(typeof(APIManager)) as APIManager;
        APImanager.CustomAwake(this);
        Debug.Log("API Built");

        floatFile = gameObject.AddComponent(typeof(FloatFile)) as FloatFile;
        floatFile.ConnectToServer(this);
        cameraFeeds = new List<CameraFeed>();

        vesselControl = gameObject.GetComponent<VesselControl>();
        vesselControl.ControlAwake(this);
        //  Debug.Log("Vessel Control Initialized");

        //roboticController = gameObject.GetComponent<RoboticController>();
        //if (roboticController)
        //{
        //    roboticController.CustomAwake(this);
        //    Debug.Log("Robotics Configured");
        var roboticAssembler = GetComponent<RoboticAssembler>();
        roboticAssembler.Assemble(this);
        //}        //roboticController = gameObject.GetComponent<RoboticController>();
        //if (roboticController)
        //{
        //    roboticController.CustomAwake(this);
        //    Debug.Log("Robotics Configured");
        //}


        // debugWriteToSelf = GetComponent<KSPMechs.MechManager>().debugWriteToSelf;

        // SetFloat("TestFromClient" + fileName, 4);
        SetBool("ClientTransmitting" + fileName, true);

        //for (int i = 0; i < 1000; i++)
        //{
        //    SetFloat("test" + i.ToString(), i);
        //}
    }


    void Update()
    {
        //Graphics.Draw
        floatFile.StartUpdate();

        SetClientValues();

        vesselControl.VesselUpdate();

        //  if (Input.GetKey(KeyCode.Keypad4))
       // roboticController.CustomUpdate();
        SetFloat("ClientCameraFeedCount" + fileName, cameraFeeds.Count);



        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            //for (int i = 0; i < 100; i++)
            //{
            //    SetFloat("test" + i.ToString() + Time.frameCount.ToString(), i);
            //}
        }

    }
    //public void ConnectionEstablished()
    //{
    //    vesselControl = gameObject.AddComponent(typeof(VesselControl)) as VesselControl;
    //    vesselControl.ControlAwake(this);
    //}


    void LateUpdate()
    {
        floatFile.EndUpdate();
    }

    public void CreateCameraFeed()
    {
        Debug.Log("Create new camera feed");
        var newFeed = gameObject.AddComponent(typeof(CameraFeed)) as CameraFeed;
        cameraFeeds.Add(newFeed);
        newFeed.StartFeed(this, fileName);
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
    public void SetVector3(string key, Vector3 value)
    {
        //Debug.Log("set vector3 with key " + key);
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

    public void OnDestroy()
    {
        transmittingFloat = 0;
        floatFile.CustomOnDestroy();
        //float();
    }

    void SetClientValues()
    {
        var actionGroup1Bool = Input.GetKeyDown(KeyCode.Alpha1);
        if (actionGroup1Bool)
        {
            Debug.Log("Toggle group 1");
            SetBool("ActionGroup1", actionGroup1Bool);
        }
        var actionGroup2Bool = Input.GetKeyDown(KeyCode.Alpha2);
        if (actionGroup2Bool)
            SetBool("ActionGroup2", actionGroup2Bool);
        var actionGroup3Bool = Input.GetKeyDown(KeyCode.Alpha3);
        if (actionGroup3Bool)
            SetBool("ActionGroup3", actionGroup3Bool);
        var actionGroup4Bool = Input.GetKeyDown(KeyCode.Alpha4);
        if (actionGroup4Bool)
            SetBool("ActionGroup4", actionGroup4Bool);
        var actionGroup5Bool = Input.GetKeyDown(KeyCode.Alpha5);
        if (actionGroup5Bool)
            SetBool("ActionGroup5", actionGroup5Bool);
        var actionGroup6Bool = Input.GetKeyDown(KeyCode.Alpha6);
        if (actionGroup6Bool)
            SetBool("ActionGroup6", actionGroup6Bool);
        var actionGroup7Bool = Input.GetKeyDown(KeyCode.Alpha7);
        if (actionGroup7Bool)
            SetBool("ActionGroup7", actionGroup7Bool);
        var actionGroup8Bool = Input.GetKeyDown(KeyCode.Alpha8);
        if (actionGroup8Bool)
            SetBool("ActionGroup8", actionGroup8Bool);
        var actionGroup9Bool = Input.GetKeyDown(KeyCode.Alpha9);
        if (actionGroup9Bool)
            SetBool("ActionGroup9", actionGroup9Bool);
        var actionGroup0Bool = Input.GetKeyDown(KeyCode.Alpha0);
        if (actionGroup0Bool)
            SetBool("ActionGroup0", actionGroup0Bool);

        SetFloat("Throttle", Input.GetAxis("Throttle"));
    }
}
