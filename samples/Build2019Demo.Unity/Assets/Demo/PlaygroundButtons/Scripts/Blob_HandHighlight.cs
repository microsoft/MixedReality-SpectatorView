using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Blob_HandHighlight : MonoBehaviour
{
	[SerializeField]
	[RevertPropertyChanges(typeof(Vector3), "_Blob_Position_")]
	private Material boundingBoxMat = null;

	private Vector3 lastPos;

	// Start is called before the first frame update
	void Start()
    {
		lastPos = this.transform.position;
    }

    // Update is called once per frame
    void Update()
    {
		var currPos = this.transform.position;
		if (currPos != lastPos)
		{
			boundingBoxMat.SetVector("_Blob_Position_", currPos);
			lastPos = currPos;
		}
	}
}
