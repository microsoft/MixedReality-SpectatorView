using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rotate : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log("Local :" + transform.localRotation);
        Debug.Log("Global :" + transform.rotation);
        transform.rotation = Quaternion.Euler(0, 0, 0);
    }
}
