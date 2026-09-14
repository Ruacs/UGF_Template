using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GuideMaskTest : MonoBehaviour
{

    public Button btn_speedUp;
    public Button btn_shop;
    public GuideMask guideMask;
    // Start is called before the first frame update
    void Start()
    {
        guideMask.Init();
    }

    // Update is called once per frame
    void Update()
    {
        
    }



    private void OnGUI()
    {
            
        if(GUILayout.Button("关闭引导",GUILayout.Width(300), GUILayout.Height(100)))
        {
            guideMask.Close();
        }
        if (GUILayout.Button("加速按钮引导", GUILayout.Width(300), GUILayout.Height(100)))
        {
          guideMask.Play(btn_speedUp.GetComponent<RectTransform>());
        }
        if (GUILayout.Button("商店引导", GUILayout.Width(300), GUILayout.Height(100)))
        {
           guideMask.Play(btn_shop.GetComponent<RectTransform>());
        }

    }
}
