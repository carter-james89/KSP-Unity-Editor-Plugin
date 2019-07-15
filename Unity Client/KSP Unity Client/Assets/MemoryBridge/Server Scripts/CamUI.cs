using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CamUI : MonoBehaviour
{
    static  TMPro.TextMeshProUGUI UItext;
    public TMPro.TextMeshProUGUI CamText;
    private void Awake()
    {
        UItext = CamText;
     //   UItext = Transform.FindObjectOfType("Canvas")etComponentInChildren<TMPro.TextMeshProUGUI>();
    }

    public static void SetCamText(string newText)
    {
       // var UItext = GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (!UItext.gameObject.activeInHierarchy)
            UItext.gameObject.SetActive(true);

        UItext.text = newText;
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
