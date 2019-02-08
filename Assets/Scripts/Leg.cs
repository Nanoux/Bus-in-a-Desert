using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Leg : MonoBehaviour {

	public GameObject Cyl;
	GameObject strut;

	// Use this for initialization
	void Start () {
		strut = Instantiate (Cyl, ((transform.position + transform.forward * 8.3f - transform.parent.transform.up * 0.7f) + (transform.position + transform.parent.transform.up * 5f))/2f, transform.rotation,transform) as GameObject;
		strut.transform.LookAt (transform.position + transform.forward * 8.3f - transform.parent.transform.up * 0.7f);
		strut.transform.Rotate(new Vector3(90f,0f,0f));
		strut.transform.localScale = new Vector3 (0.3f/50f, Vector3.Distance(strut.transform.position,transform.position + transform.forward * 8.3f - transform.parent.transform.up * 0.7f)/50f, 0.3f/50f);
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
