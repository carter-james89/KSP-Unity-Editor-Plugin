using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class APIManager : MonoBehaviour {
    MemoryBridge memoryBridge;
	public void CustomAwake(MemoryBridge memoryBridge)
    {
        this.memoryBridge = memoryBridge;
    }

    public void ReadServerAPI()
    {
       // memoryBridge.CoMD = memoryBridge.GetVector3("CoMD" + memoryBridge.fileName);
        memoryBridge.CurrentCOM = memoryBridge.GetVector3("CurrentCOM" + memoryBridge.fileName);
        memoryBridge.LocalCOM = memoryBridge.GetVector3("LocalCOM" + memoryBridge.fileName);
        memoryBridge.Acceleration = memoryBridge.GetVector3("Acceleration" + memoryBridge.fileName);
        memoryBridge.obt_velocity = memoryBridge.GetVector3("obt_velocity" + memoryBridge.fileName);
        memoryBridge.srf_velocity = memoryBridge.GetVector3("srf_velocity" + memoryBridge.fileName);

        memoryBridge.altitude = memoryBridge.GetFloat("altitude" + memoryBridge.fileName);
        memoryBridge.heightFromTerrain = memoryBridge.GetFloat("heightFromTerrain" + memoryBridge.fileName);
        memoryBridge.horizontalSrfSpeed = memoryBridge.GetFloat("horizontalSrfSpeed" + memoryBridge.fileName);
        memoryBridge.latitude = memoryBridge.GetFloat("latitude" + memoryBridge.fileName);
        memoryBridge.longitude = memoryBridge.GetFloat("longitude" + memoryBridge.fileName);
        memoryBridge.mach = memoryBridge.GetFloat("mach" + memoryBridge.fileName);

        memoryBridge.srfSpeed = memoryBridge.GetFloat("srfSpeed" + memoryBridge.fileName);
        memoryBridge.terrainAltitude = memoryBridge.GetFloat("terrainAltitude" + memoryBridge.fileName);
        memoryBridge.verticalSpeed = memoryBridge.GetFloat("verticalSpeed" + memoryBridge.fileName);
        memoryBridge.currentStage = memoryBridge.GetFloat("currentStage" + memoryBridge.fileName);
        memoryBridge.geeForce_immediate = memoryBridge.GetFloat("geeForce_immediate" + memoryBridge.fileName);
        memoryBridge.heightFromSurface = memoryBridge.GetFloat("heightFromSurface" + memoryBridge.fileName);
        memoryBridge.obt_speed = memoryBridge.GetFloat("obt_speed" + memoryBridge.fileName);

        memoryBridge.directSunlight = memoryBridge.GetBool("directSunlight" + memoryBridge.fileName);
        memoryBridge.Landed = memoryBridge.GetBool("Landed" + memoryBridge.fileName);
        memoryBridge.loaded =  memoryBridge.GetBool("loaded" + memoryBridge.fileName);
        memoryBridge.Splashed = memoryBridge.GetBool("Splashed" + memoryBridge.fileName);
    }
}
