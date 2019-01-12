using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MemoryBridgeServer
{
    public class DebugVector : MonoBehaviour
    {
        public Transform trans;
        // Transform anchorTrans;

        GameObject xObject;
        GameObject yObject;
        GameObject zObject;

        float xDis;
        float yDis;
        float zDis;
         

        public static void DrawVector(Transform transformToDraw)
        {
            var fileConstants = transformToDraw.gameObject.AddComponent(typeof(DebugVector)) as DebugVector;
        }
        public void OnEnable()
        {
            GameObject anchorObject = new GameObject();
            this.trans = anchorObject.transform;

            //add thee children to the anchor object
            xObject = new GameObject();
            yObject = new GameObject();
            zObject = new GameObject();
            //match the rotations
            xObject.transform.rotation = this.trans.localRotation;
            yObject.transform.rotation = this.trans.localRotation;
            zObject.transform.rotation = this.trans.localRotation;
            //xObject.transform.Rotate(new Vector3(0, 90, 0));
            //set the children to the anchor object
            xObject.transform.parent = this.trans;
            yObject.transform.parent = this.trans;
            zObject.transform.parent = this.trans;
            //move children to anchors origin
            xObject.transform.localPosition = new Vector3(0, 0, 0);
            yObject.transform.localPosition = new Vector3(0, 0, 0);
            zObject.transform.localPosition = new Vector3(0, 0, 0);
            //match anchors rotation then set parent, then move to origin
            this.trans.rotation = transform.rotation;
            this.trans.SetParent(transform);
            this.trans.localPosition = new Vector3(0, 0, 0);
            // this.trans.position = transform.position;


                DrawVector();
            


        }

        public void Rotate(Vector3 newRot)
        {
            trans.Rotate(newRot);
        }
        public void DrawVector()
        {
            float length = .8f;
            float width = .05f;
            LineRenderer xRenderer = xObject.AddComponent<LineRenderer>();
            xRenderer.useWorldSpace = false;
            xRenderer.SetPosition(0, xObject.transform.localPosition);
            xRenderer.SetPosition(1, new Vector3(xObject.transform.localPosition.x + length, xObject.transform.localPosition.y, xObject.transform.localPosition.z));
            xRenderer.SetWidth(width, width);
 
            Material redMat = new Material(Shader.Find("Transparent/Diffuse"));
            redMat.color = Color.red;
            xRenderer.material = redMat;

            yRenderer = yObject.AddComponent<LineRenderer>();
            yRenderer.useWorldSpace = false;
            yRenderer.SetPosition(0, yObject.transform.localPosition);
            yRenderer.SetPosition(1, new Vector3(yObject.transform.localPosition.x, yObject.transform.localPosition.y + length, yObject.transform.localPosition.z));
            Material greenMat = new Material(Shader.Find("Transparent/Diffuse"));
            greenMat.color = Color.green;
            yRenderer.material = greenMat;
            yRenderer.SetWidth(width, width);


            LineRenderer zRenderer = zObject.AddComponent<LineRenderer>();
            zRenderer.useWorldSpace = false;
            zRenderer.SetPosition(0, zObject.transform.localPosition);
            zRenderer.SetPosition(1, new Vector3(zObject.transform.localPosition.x, zObject.transform.localPosition.y, zObject.transform.localPosition.z + length));
            Material blueMat = new Material(Shader.Find("Transparent/Diffuse"));
            blueMat.color = Color.blue;
            zRenderer.material = blueMat;
            zRenderer.SetWidth(width, width);

        }
        LineRenderer yRenderer;
        public void Update()
        {
            //yRenderer.SetPosition(1, new Vector3(yObject.transform.localPosition.x, yObject.transform.localPosition.y + -VesselControl.offSet.y, yObject.transform.localPosition.z));
        }
    }
}
