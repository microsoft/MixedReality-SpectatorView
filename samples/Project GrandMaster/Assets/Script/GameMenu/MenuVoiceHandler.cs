using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuVoiceHandler : MonoBehaviour
{


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ToggleMenuOn()
    {
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);
    }

    public void ToggleMenuOff()
    {
        if (gameObject.activeSelf)
            gameObject.SetActive(false);
    }
}
