using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR;
using System.IO;

public class ChaseCamera : MonoBehaviour {

	public Transform car;
	public float cardist,carheight,rotdamp,heightdamp, distovr;
	public Vector3 POVpos;
	private Vector3 rotVect;

	public bool isFirst = true;

	float lastSwitch;


	// Use this for initialization
	void Update () {
		if (Input.GetAxis ("POV") > 0 && lastSwitch == 0) {
			isFirst = !isFirst;
		}
		lastSwitch = Input.GetAxis ("POV");
		if (isFirst) {
			transform.position = car.TransformPoint (POVpos);
			float camY = (transform.eulerAngles.y > 180f) ? transform.eulerAngles.y - 360f : transform.eulerAngles.y;
			float carY = (car.eulerAngles.y > 180f) ? car.eulerAngles.y - 360f : car.eulerAngles.y;
			transform.eulerAngles = new Vector3 (car.eulerAngles.x,Mathf.Lerp(camY,carY + Input.GetAxis ("Look") * 90, rotdamp * Time.deltaTime),car.eulerAngles.z);
		}

	}
	
	// Update is called once per frame
	void FixedUpdate () {
		if (isFirst) {
		} else {
			float wantedangle = car.eulerAngles.y + Input.GetAxis ("Look") * 90;
			if (Input.GetAxis ("Flip") > 0f)
				wantedangle += 180f;
			float wantedheight = car.position.y + carheight;
			float camangle = transform.eulerAngles.y;
			float camheight = transform.position.y;
			camangle = Mathf.LerpAngle (camangle, wantedangle, rotdamp * Time.deltaTime);
			camheight = Mathf.Lerp (camheight, wantedheight, heightdamp * Time.deltaTime);
			Quaternion currentrot = Quaternion.Euler (0f, camangle, 0f);
			transform.position = car.position;
			transform.position -= currentrot * Vector3.forward * cardist;
			transform.position = new Vector3 (transform.position.x, camheight, transform.position.z);
			transform.LookAt (car.position + Vector3.up * distovr);

			float speed = car.gameObject.GetComponent<Rigidbody> ().velocity.magnitude;
		}
	}
}
