using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VR;

public class Bus : MonoBehaviour
{
	public float torque = 50;
	public float idealRPM;
	public float maxRPM;
	public float steerforce = 2f;

	public WheelCollider[] WheelCols = new WheelCollider[4];
	public Transform[] Wheelmeshes = new Transform[4];

	public Transform COM;
	public Transform steeringWheel;

	public GameObject Blown;
	GameObject blownInstance;
	UIManager manager;

	public Text record;
	public Text currentText;
	public Text quote;
	public Text quoteBack;

	public ParticleSystem leftEmit;
	public ParticleSystem rightEmit;

	public AudioSource driveSound;
	public AudioSource windSound;

	[HideInInspector]
	public int startPoint = 0;
	[HideInInspector]
	public int distance;
	[HideInInspector]
	public int current;
	[HideInInspector]
	public int currentOffset;
	int currentFinal;

	bool grounded = false;

	float lastRecenter;
	float steer = 0;
	float timer = 0f;
	float timeLength = 5f;
	bool timing = false;

	TerrainGenerator generator;
	//[HideInInspector]
	public GameObject camFollow;

	public string[] quotes;

	void Awake(){
		generator = GameObject.FindObjectOfType<TerrainGenerator> ();
		manager = GameObject.FindObjectOfType<UIManager> ();
	}

	void FixedUpdate ()
	{
		//Input.GetAxisRaw ("Mouse X");
		//GetComponent<Rigidbody> ().AddForce (transform.forward * 20000f);
		//Steering Logic

		steer = Input.GetAxis ("Horizontal");
		if (manager.isAI.isOn){
			if ((transform.position + transform.forward * 10).x > 0.1f) {
				steer = -1;
			} else if ((transform.position + transform.forward * 10).x < -0.1f) {
				steer = 1;
			} else {
				steer = 0;
			}
		}
		float finalangle = steer * 25f;
		steeringWheel.localEulerAngles = new Vector3 (steeringWheel.localEulerAngles.x,steeringWheel.localEulerAngles.y,finalangle * (360f / 25f));
		WheelCols [0].steerAngle = finalangle;
		WheelCols [1].steerAngle = finalangle;
		for (int i = 0; i < WheelCols.Length; i++) {
			Quaternion rot;
			Vector3 pos;
			WheelCols [i].GetWorldPose (out pos, out rot);
			Wheelmeshes [i].transform.position = pos;
			Wheelmeshes [i].transform.rotation = rot;
		}

		//SkyTorque Logic
		grounded = false;
		for (int i = 0; i < 4; i++) {
			if (WheelCols [i].isGrounded == true)
				grounded = true;
		}
		if (!grounded) {
			GetComponent<Rigidbody> ().AddRelativeTorque (Input.GetAxis ("SkyTorque") * 6600f, 0f, Input.GetAxis ("Horizontal") * -2200f);
		}

		//Acceleration Logic
		float scaledTorque = Input.GetAxis("Vertical") * torque;
		if (manager.isAI.isOn) {
			scaledTorque = torque;
		}
		if (WheelCols [2].rpm < idealRPM)
			scaledTorque = Mathf.Lerp (scaledTorque / 10f, scaledTorque, WheelCols [2].rpm / idealRPM);
		else
			scaledTorque = Mathf.Lerp (scaledTorque, 0, (WheelCols [2].rpm - idealRPM) / (maxRPM - idealRPM));
		for (int i = 2; i < WheelCols.Length; i++) {
			WheelCols [i].motorTorque = scaledTorque;
		}
		ParticleSystem.MainModule settingsL = leftEmit.main;
		settingsL.startLifetime = (GetComponent<Rigidbody> ().velocity.z / 25f) * 3;
		settingsL.startSpeed = GetComponent<Rigidbody> ().velocity.z;
		leftEmit.transform.eulerAngles = new Vector3 (0f,180f,0f);
		ParticleSystem.MainModule settingsR = rightEmit.main;
		settingsR.startLifetime = (GetComponent<Rigidbody> ().velocity.z / 25f) * 3;
		settingsR.startSpeed = GetComponent<Rigidbody> ().velocity.z;
		rightEmit.transform.eulerAngles = new Vector3 (0f,180f,0f);
		//Debug.Log (GetComponent<Rigidbody> ().velocity.magnitude);

		driveSound.volume = (GetComponent<Rigidbody> ().velocity.z / 25f) * (manager.Audio.value / 100f);
		windSound.volume = (1-(GetComponent<Rigidbody> ().velocity.z / 25f)) * (manager.Audio.value / 100f);
		//Game Logic
		if (timing){
			timer += Time.fixedDeltaTime;
			if (timer < 1f) {
				quote.color = new Color (1,1,1,timer);
				quoteBack.color = new Color (0,0,0,timer);
			}
			if (timer > timeLength - 1) {
				quote.color = new Color (1,1,1,1-(timer - (timeLength-1)));
				quoteBack.color = new Color (0,0,0,1-(timer - (timeLength-1)));
			}
			if (timer > timeLength) {
				timing = false;
				transform.position = Vector3.up * 2.5f;
				transform.rotation = Quaternion.identity;
				GetComponent<Rigidbody> ().velocity = Vector3.zero;
				GetComponent<Rigidbody> ().angularVelocity = Vector3.zero;
				if (!UnityEngine.XR.XRSettings.loadedDeviceName.Equals("OpenVR"))
					transform.parent.GetComponentInChildren<ChaseCamera> ().car = this.gameObject.transform;
				else {
					//camFollow.vrRig.transform.parent = this.transform;
					manager.CameraVR.transform.parent = this.transform;
					Recenter ();
				}
				for (int i = 0; i < transform.childCount; i++) {
					if (!transform.GetChild(i).Equals(manager.CameraVR.transform))
						transform.GetChild (i).gameObject.SetActive (true);
				}
				GetComponent<Rigidbody>().isKinematic = false;
				current = 0;
				currentOffset = 0;
				startPoint = 0;
				currentFinal = 0;
				generator.Start ();
				Destroy (blownInstance);
			}
		}

		if ((Mathf.Abs(transform.position.x) > generator.textureSettings.roadWidth || transform.position.y < -10f || Vector3.Dot(Vector3.up,transform.up) < 0.2f) && transform.GetChild(0).gameObject.activeSelf){
			RestartGame();
		}
		if (Mathf.RoundToInt (transform.position.z - startPoint + currentOffset) > currentFinal) {
			current = Mathf.RoundToInt (transform.position.z - startPoint);
			currentFinal = current + currentOffset;
		}
		if (currentFinal > distance) {
			distance = currentFinal;
		}


		//vr
		if (UnityEngine.XR.XRSettings.loadedDeviceName.Equals("OpenVR") && Input.GetAxis ("Recenter") > 0 && lastRecenter == 0) {
			Recenter ();
		}
		lastRecenter = Input.GetAxis ("Recenter");
		record.text = "Record: " + distance + " M    ";
		currentText.text = "Current: " + currentFinal + " M    ";
			
		GetComponent<Rigidbody> ().centerOfMass = COM.localPosition;
	}
	void RestartGame(){
		timer = 0f;
		timing = true;
		int index = Random.Range (0, quotes.Length);
		quote.text = quotes [index];
		quoteBack.text = quotes [index];
		blownInstance = Instantiate (Blown, transform.position, transform.rotation) as GameObject;
		blownInstance.transform.GetChild(0).GetComponent<Rigidbody> ().velocity = GetComponent<Rigidbody> ().velocity;
		blownInstance.transform.GetChild(0).GetComponent<Rigidbody> ().angularVelocity = GetComponent<Rigidbody> ().angularVelocity;
		if (!UnityEngine.XR.XRSettings.loadedDeviceName.Equals("OpenVR"))
			transform.parent.GetComponentInChildren<ChaseCamera> ().car = blownInstance.transform.GetChild (0).transform;
		else
			manager.CameraVR.transform.parent = blownInstance.transform.GetChild (0).transform;
		for (int i = 0; i < transform.childCount; i++) {
			if (!transform.GetChild(i).Equals(manager.CameraVR))
				transform.GetChild (i).gameObject.SetActive (false);
		}
		GetComponent<Rigidbody>().isKinematic = true;
	}
	public void Recenter(){
		manager.CameraVR.transform.localEulerAngles = Vector3.zero;
		manager.CameraVR.transform.localPosition = new Vector3 (camFollow.GetComponent<ChaseCamera>().POVpos.x - manager.CameraVR.transform.GetChild(0).transform.localPosition.x,-0.537f,camFollow.GetComponent<ChaseCamera>().POVpos.z - manager.CameraVR.transform.GetChild(0).transform.localPosition.z);
	}
}
