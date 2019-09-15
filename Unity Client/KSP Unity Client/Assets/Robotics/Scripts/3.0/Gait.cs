using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gait : MonoBehaviour
{
    public enum GaitTarget { MidPoint, FrontPoint, BackPoint }
    public Transform gait { get; private set; }
    public Transform pointMid { get; private set; }
    public Transform pointFront { get; private set; }
    public Transform pointBack { get; private set; }
    public Transform IKtargetTransform, rotAxis;

    private void Awake()
    {
        var obj = Instantiate(Resources.Load("Limb Target", typeof(GameObject))) as GameObject;
        IKtargetTransform = obj.transform;

        rotAxis = transform.Find("Axis Point");
        IKtargetTransform.SetParent(rotAxis);
        pointMid = transform.Find("Point Mid");
        pointFront = transform.Find("Point Front");
        pointBack = transform.Find("Point Back");
        //gait.position = limbController.limbMirror.limbEnd.position;
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
