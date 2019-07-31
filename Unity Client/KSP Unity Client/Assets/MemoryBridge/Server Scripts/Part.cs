using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Part : MonoBehaviour
{
    string vesselName;
    public string kspPartName;
    VesselControl vesselControl;
    public int ID, parentID = 0;
    List<MeshRenderer> meshRenderers;
    public LineRenderer lineRenderer;
    public GameObject partMeshes;
    public VesselControl.Vessel vessel;
    public RoboticServo servo;
    GameObject jointObject;

    public void CustomAwake(int partID, int parentID, string vesselName, List<MeshRenderer> meshRenderers)
    {
        ID = partID;
        this.parentID = parentID;
        name = ID.ToString();
        this.vesselName = vesselName;
        this.meshRenderers = meshRenderers;

        if (kspPartName == "IR.Rotatron.Basic.v3")
        {
            transform.localEulerAngles += new Vector3(0, 0, 90);
            foreach (var mesh in meshRenderers)
            {
                mesh.transform.localEulerAngles -= new Vector3(0, 0, 90);
            }
        }

        // partMeshes.hideFlags = HideFlags.HideInHierarchy;
    }

    public void ToggleRenderers(bool toggle)
    {
        partMeshes.SetActive(toggle);
        //Debug.Log(name);
        //foreach (var mesh in meshRenderers)
        //{
        //    mesh.gameObject.SetActive(toggle);
        //    //mesh.hideFlags = HideFlags.HideInHierarchy;
        //}
    }

    public void ServoAdded(RoboticServo servo)
    {
        ToggleRenderers(false);
        GameObject fakeJoint = null;

        if (kspPartName == "IR.Rotatron.Basic.v3")
        {
            fakeJoint = Instantiate(Resources.Load("Rotatron", typeof(GameObject))) as GameObject;
            fakeJoint.transform.SetParent(transform);
            fakeJoint.transform.localPosition = Vector3.zero;
            fakeJoint.transform.localEulerAngles = new Vector3(0, 0, 90);
            lineRenderer = fakeJoint.GetComponent<LineRenderer>();
        }
        if (kspPartName == "IR.Pivotron.RangeNinety" || kspPartName == "IR.Pivotron.Sixty.v3" || kspPartName == "IR.Pivotron.Hinge.Basic.v3" || kspPartName == "IR.Pivotron.Basic" || kspPartName == "IR.Pivotron.OneTwenty.v3" || kspPartName == "IR.Pivotron.Basic.v3")
        {
            fakeJoint = Instantiate(Resources.Load("Pivotron", typeof(GameObject))) as GameObject;
            fakeJoint.transform.SetParent(transform);
            fakeJoint.transform.localPosition = Vector3.zero;
            fakeJoint.transform.localEulerAngles = new Vector3(0, 0, 90);
            lineRenderer = fakeJoint.GetComponent<LineRenderer>();
        }
        if (kspPartName == "IR.Extendatron.BasicHalf" || kspPartName == "IR.Extendatron.RightHalf.v3")
        {
            fakeJoint = Instantiate(Resources.Load("Extend", typeof(GameObject))) as GameObject;
            fakeJoint.transform.SetParent(transform);
            fakeJoint.transform.localPosition = Vector3.zero;
            fakeJoint.transform.localEulerAngles = new Vector3(90, 0, 0);
            lineRenderer = fakeJoint.GetComponent<LineRenderer>();
        }

        jointObject = fakeJoint;
        this.servo = servo;
    }
    public void FindParent()
    {
        // Debug.Log("part: " + name);
        //  Debug.Log("parent " + parentName);
        bool foundParent = false;
        foreach (var part in vessel.parts)
        {
            if (part.ID == parentID)
            {
                transform.SetParent(part.transform);
                foundParent = true;
            }

            //if (part.parentID != 0)
            //{
            //    transform.SetParent(GameObject.Find(parentName).transform);
            //}
            //else
            //{
            //    transform.SetParent(GameObject.Find(vesselName).transform.Find("Vessel Offset"));
            //    transform.localEulerAngles = Vector3.zero;
            //}
        }
        if (!foundParent)
            transform.SetParent(vessel.meshOffset);
    }
    public void SetJointColor(Color color)
    {
        if (jointObject)
            jointObject.GetComponent<MeshRenderer>().material.color = color;
    }
}
