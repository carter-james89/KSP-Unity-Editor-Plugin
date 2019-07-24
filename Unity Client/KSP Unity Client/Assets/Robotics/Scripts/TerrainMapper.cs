using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainMapper : MonoBehaviour
{
    RoboticController mechController;
    public GameObject customGround;
    MeshFilter groundMeshFilter;
    MeshRenderer groundMeshRenderer;
    Vector3[] groundMeshvertices;
    public Material green, red, white;
    public enum Slope { Level, Increasing, Decreasing }
    public Slope groundSlope;

    public bool drawTranslateTriangle;

    public void Awake()
    {
        customGround = gameObject;
        mechController = FindObjectOfType<RoboticController>();

        groundMeshRenderer = customGround.GetComponent<MeshRenderer>();
        groundMeshFilter = customGround.GetComponent<MeshFilter>();
        groundMeshvertices = groundMeshFilter.mesh.vertices;
    }


    private void Update()
    {
      //  DrawGround();
    }

    public void ClearFootRenderers()
    {
        //if (mechController.translateLegGroup != null & drawTranslateTriangle)
        //{
        //    foreach (var leg in mechController.translateLegGroup)
        //    {
        //        leg.actualFootRenderer.SetPosition(0, Vector3.zero);
        //        leg.actualFootRenderer.SetPosition(1, Vector3.zero);
        //    }
        //}

    }

    public void DrawTranslateLegGroup()
    {
        //if (drawTranslateTriangle)
        //{
        //    for (int i = 0; i < 3; i++)
        //    {
        //        mechController.translateLegGroup[i].actualFootRenderer.SetPosition(0, mechController.translateLegGroup[i].actualFoot.position);

        //        if (i != 2)
        //            mechController.translateLegGroup[i].actualFootRenderer.SetPosition(1, mechController.translateLegGroup[i + 1].actualFoot.position);
        //        else
        //            mechController.translateLegGroup[i].actualFootRenderer.SetPosition(1, mechController.translateLegGroup[0].actualFoot.position);

        //    }
        //}

    }
    //public void DrawGround()
    //{

    //    //  Vector3[] vertices = groundMeshFilter.mesh.vertices;
    //    // var offset = customGround.transform.InverseTransformPoint(mechController.limbs[1].footGround.position);
    //    // Debug.Log("Vertices " + groundMeshvertices.Length);
    //    groundMeshvertices[6] = mechController.groupLeft.limb1.ground.position;
    //    //groundMeshvertices[4] = mechController.limbs[1].ground.position;
    //    //groundMeshvertices[3] = mechController.limbs[2].ground.position;
    //    //groundMeshvertices[5] = mechController.limbs[3].ground.position;
    //    //groundMeshvertices[1] = mechController.limbs[4].ground.position;
    //    //groundMeshvertices[0] = mechController.limbs[5].ground.position;
    //    //  groundMeshvertices[8].y = mechController.limbs[5].ground.position.y;
    //    // groundMeshvertices[7].y = mechController.limbs[2].ground.position.y;

    //    var groundAvg =
    //        mechController.limbs[0].ground.position +
    //        mechController.limbs[1].ground.position +
    //        mechController.limbs[2].ground.position +
    //        mechController.limbs[3].ground.position +
    //        mechController.limbs[4].ground.position +
    //        mechController.limbs[5].ground.position;
    //    var ground = groundAvg / 6;

    //    //groundMeshvertices[2] = ground;

    //    var targetVerticesPos = mechController.groupLeft.limb1.ground.position;
    //    groundMeshvertices[6] = new Vector3(targetVerticesPos.x, targetVerticesPos.y, targetVerticesPos.z);
    //    groundMeshvertices[16] = new Vector3(targetVerticesPos.x, targetVerticesPos.y, targetVerticesPos.z);
    //    groundMeshvertices[17] = new Vector3(targetVerticesPos.x, targetVerticesPos.y, targetVerticesPos.z);
    //    groundMeshvertices[15] = new Vector3(targetVerticesPos.x, ground.y - 2, targetVerticesPos.z);
    //    groundMeshvertices[18] = new Vector3(targetVerticesPos.x, ground.y - 2, targetVerticesPos.z);
    //    groundMeshvertices[32] = new Vector3(targetVerticesPos.x, ground.y - 2, targetVerticesPos.z);

    //    //targetVerticesPos = mechController.limbs[1].ground.position;
    //    //groundMeshvertices[4] = new Vector3(targetVerticesPos.x, targetVerticesPos.y, targetVerticesPos.z);
    //    //groundMeshvertices[20] = new Vector3(targetVerticesPos.x, targetVerticesPos.y, targetVerticesPos.z);
    //    //groundMeshvertices[19] = new Vector3(targetVerticesPos.x, ground.y - 2, targetVerticesPos.z);
    //    //groundMeshvertices[30] = new Vector3(targetVerticesPos.x, ground.y - 2, targetVerticesPos.z);

    //    //targetVerticesPos = mechController.limbs[2].ground.position;
    //    //groundMeshvertices[3] = new Vector3(targetVerticesPos.x, targetVerticesPos.y, targetVerticesPos.z);
    //    //groundMeshvertices[22] = new Vector3(targetVerticesPos.x, targetVerticesPos.y, targetVerticesPos.z);
    //    //groundMeshvertices[23] = new Vector3(targetVerticesPos.x, targetVerticesPos.y, targetVerticesPos.z);
    //    //groundMeshvertices[21] = new Vector3(targetVerticesPos.x, ground.y - 2, targetVerticesPos.z);
    //    //groundMeshvertices[24] = new Vector3(targetVerticesPos.x, ground.y - 2, targetVerticesPos.z);
    //    //groundMeshvertices[29] = new Vector3(targetVerticesPos.x, ground.y - 2, targetVerticesPos.z);

    //    //targetVerticesPos = mechController.limbs[3].ground.position;
    //    //groundMeshvertices[5] = new Vector3(targetVerticesPos.x, targetVerticesPos.y, targetVerticesPos.z);
    //    //groundMeshvertices[10] = new Vector3(targetVerticesPos.x, targetVerticesPos.y, targetVerticesPos.z);
    //    //groundMeshvertices[13] = new Vector3(targetVerticesPos.x, targetVerticesPos.y, targetVerticesPos.z);
    //    //groundMeshvertices[9] = new Vector3(targetVerticesPos.x, ground.y - 2, targetVerticesPos.z);
    //    //groundMeshvertices[14] = new Vector3(targetVerticesPos.x, ground.y - 2, targetVerticesPos.z);
    //    //groundMeshvertices[31] = new Vector3(targetVerticesPos.x, ground.y - 2, targetVerticesPos.z);

    //    //targetVerticesPos = mechController.limbs[4].ground.position;
    //    //groundMeshvertices[1] = new Vector3(targetVerticesPos.x, targetVerticesPos.y, targetVerticesPos.z);
    //    //groundMeshvertices[7] = new Vector3(targetVerticesPos.x, targetVerticesPos.y, targetVerticesPos.z);
    //    //groundMeshvertices[8] = new Vector3(targetVerticesPos.x, ground.y - 2, targetVerticesPos.z);
    //    //groundMeshvertices[27] = new Vector3(targetVerticesPos.x, ground.y - 2, targetVerticesPos.z);

    //    //targetVerticesPos = mechController.limbs[5].ground.position;
    //    //groundMeshvertices[0] = new Vector3(targetVerticesPos.x, targetVerticesPos.y, targetVerticesPos.z);
    //    //groundMeshvertices[11] = new Vector3(targetVerticesPos.x, targetVerticesPos.y, targetVerticesPos.z);
    //    //groundMeshvertices[26] = new Vector3(targetVerticesPos.x, targetVerticesPos.y, targetVerticesPos.z);
    //    //groundMeshvertices[12] = new Vector3(targetVerticesPos.x, ground.y - 2, targetVerticesPos.z);
    //    //groundMeshvertices[25] = new Vector3(targetVerticesPos.x, ground.y - 2, targetVerticesPos.z);
    //    //groundMeshvertices[28] = new Vector3(targetVerticesPos.x, ground.y - 2, targetVerticesPos.z);


    //    groundMeshFilter.mesh.vertices = groundMeshvertices;

    //    //var frontAvg = (mechController.limbs[0].footGround.position.y + mechController.limbs[3].footGround.position.y) / 2;
    //    //var backAvg = (mechController.limbs[2].footGround.position.y + mechController.limbs[5].footGround.position.y) / 2;

    //    //if (Math.Abs(frontAvg - backAvg) > .5)
    //    //{
    //    //    if ((frontAvg > backAvg) & groundSlope != Slope.Increasing)
    //    //    {
    //    //        groundSlope = Slope.Increasing;
    //    //        groundMeshRenderer.material = green;
    //    //    }
    //    //    else if ((frontAvg < backAvg) & groundSlope != Slope.Decreasing)
    //    //    {
    //    //        groundSlope = Slope.Decreasing;
    //    //        groundMeshRenderer.material = red;
        //    }
        //}
        //else if (groundSlope != Slope.Level)
        //{
        //    Debug.Log("slope Level");
        //    groundSlope = Slope.Level;
        //    groundMeshRenderer.material = white;
        //}


  // }
}
