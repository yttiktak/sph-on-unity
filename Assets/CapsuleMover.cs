using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CapsuleMover : MonoBehaviour {

	public int index = 0;
	float[] datary;

	void Start () {
		datary = new float[9];
	}
	
	// Update is called once per frame
	void Update () {
		CloudMinimal.positionBuffer.GetData (datary, 0, index, 9);
		Vector3 newp;
		newp.x = datary [0];
		newp.y = datary [1];
		newp.z = datary [2];
		Vector3 newp2;
		newp2.x = datary [3];
		newp2.y = datary [4];
		newp2.z = datary [5];
		Vector3 newp3;
		newp3.x = datary [4];
		newp3.y = datary [5];
		newp3.z = datary [6];
		transform.position = newp;
		transform.LookAt (newp2, newp3);
	}
}
