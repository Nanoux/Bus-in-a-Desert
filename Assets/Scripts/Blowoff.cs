using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Blowoff : MonoBehaviour {

	public Vector3 direction;

	// Use this for initialization
	void Start () {
		GetComponent<Rigidbody> ().velocity = direction;
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
