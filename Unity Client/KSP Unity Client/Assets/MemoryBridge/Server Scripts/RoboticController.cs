using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoboticController : MonoBehaviour {

    IR_Manager IRmanager;
    List<LimbController> limbs;
    public void CustomAwake(MemoryBridge memoryBridge)
    {
        IRmanager = gameObject.GetComponent<IR_Manager>();
        IRmanager.CustomAwake(memoryBridge, memoryBridge.vesselControl, ref limbs);
    }

    public void CustomUpdate()
    {
        foreach (var limb in limbs)
        {
            limb.CustomUpdate();
        }
    }
}
