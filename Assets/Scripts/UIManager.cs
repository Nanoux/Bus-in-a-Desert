using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine.VR;
//using UnityEditor;

public class UIManager : MonoBehaviour {

	public Bus Bus;
	public GameObject Camera2D;
	public GameObject CameraVR;

	public Transform TitleUI;
	//public Transform PauseUI;
	public Transform SettingsUI;

	public Dropdown AA;
	public Dropdown Vsync;
	public Dropdown Reflections;
	public Slider Framesrate;
	public Slider FOV;
	public Toggle isPixelated;
	public Toggle isAI;
	public Toggle isVRSupported;

	public Slider Audio;
	public Slider Music;

	float lastPause;

	void Start(){//AA,VSync,frame limit,reflections,fov,audio,music,
		QualitySettings.vSyncCount = 0;  // VSync must be disabled
		Application.targetFrameRate = 100;
		ChangeStep (0.001f);
		Load ();
	}
	public void StartGame(){
		if (UnityEngine.XR.XRSettings.loadedDeviceName.Equals ("OpenVR")) {
			UnityEngine.XR.XRSettings.enabled = true;
			UnityEngine.XR.XRSettings.LoadDeviceByName ("OpenVR");
			Bus.Recenter ();
		}
		TitleUI.gameObject.SetActive (false);
		Bus.driveSound.enabled = true;
		ChangeStep (1f);
	}
	/*public void ToPause(){
		if (!TitleUI.gameObject.activeSelf) {
			if (PauseUI.gameObject.activeSelf) {
				ChangeStep (1f);
			} else {
				ChangeStep (0.001f);
			}
			PauseUI.gameObject.SetActive (!PauseUI.gameObject.activeSelf);
		}
	}*/
	public void ToTitle(){
		TitleUI.gameObject.SetActive (true);
		//PauseUI.gameObject.SetActive (false);
		SettingsUI.gameObject.SetActive(false);
		Bus.driveSound.enabled = false;
		Save ();
		Load ();
		ChangeStep (0.001f);
	}
	public void ToSettings(){
		TitleUI.gameObject.SetActive (false);
		SettingsUI.gameObject.SetActive (true);
	}




	public void ChangeStep(float a){
		Time.timeScale = a;
		Time.fixedDeltaTime = Time.timeScale / 60f;
	}
	void FixedUpdate(){
		if (Input.GetAxis ("Pause") > 0 && lastPause == 0) {
			ToTitle ();
		}
		lastPause = Input.GetAxis ("Pause");
	}
	void Save(){
		BinaryFormatter bf = new BinaryFormatter ();
		FileStream file = File.Create (Application.persistentDataPath + "Settings.dat");

		SettingsFile settings = new SettingsFile ();
		settings.aaValue = AA.value;
		settings.vsyncValue = Vsync.value;
		settings.relfectionValue = Reflections.value;
		settings.frameValue = Framesrate.value;
		settings.fovValue = FOV.value;
		settings.isPixel = isPixelated.isOn;
		settings.isAI = isAI.isOn;
		settings.isVR = isVRSupported.isOn;
		settings.audioValue = Audio.value;
		settings.musicValue = Music.value;

		bf.Serialize (file, settings);
		file.Close ();
	}
	void Load(){
		if (File.Exists (Application.persistentDataPath + "Settings.dat")) {
			BinaryFormatter bf = new BinaryFormatter ();
			FileStream file = File.Open (Application.persistentDataPath + "Settings.dat",FileMode.Open);
			SettingsFile settings = (SettingsFile)bf.Deserialize (file);
			file.Close ();

			AA.value = settings.aaValue;
			Vsync.value = settings.vsyncValue;
			Reflections.value = settings.relfectionValue;
			Framesrate.value = settings.frameValue;
			FOV.value = settings.fovValue;
			isPixelated.isOn = settings.isPixel;
			isAI.isOn = settings.isAI;
			isVRSupported.isOn = settings.isVR;
			Audio.value = settings.audioValue;
			Music.value = settings.musicValue;

			if (settings.aaValue == 0)
				QualitySettings.antiAliasing = 0;
			if (settings.aaValue == 1)
				QualitySettings.antiAliasing = 2;
			if (settings.aaValue == 2)
				QualitySettings.antiAliasing = 4;
			if (settings.aaValue == 3)
				QualitySettings.antiAliasing = 8;

			QualitySettings.vSyncCount = settings.vsyncValue;

			ReflectionProbe[] probes = GameObject.FindObjectsOfType<ReflectionProbe> ();
			for (int i = 0; i < probes.Length; i++) {
				if (settings.relfectionValue == 0) {
					probes [i].enabled = false;
				}
				if (settings.relfectionValue == 1) {
					probes [i].enabled = true;
					probes [i].refreshMode = UnityEngine.Rendering.ReflectionProbeRefreshMode.OnAwake;
				}
				if (settings.relfectionValue == 2) {
					probes [i].enabled = true;
					probes [i].refreshMode = UnityEngine.Rendering.ReflectionProbeRefreshMode.EveryFrame;
				}
			}

			Application.targetFrameRate = (int)settings.frameValue;

			Bus.camFollow.GetComponent<Camera> ().fieldOfView = settings.fovValue;

			if (settings.isVR) {
				Camera2D.SetActive (false);
				CameraVR.SetActive (true);
				UnityEngine.XR.XRSettings.enabled = true;
				UnityEngine.XR.XRSettings.LoadDeviceByName ("OpenVR");

			} else {
				Camera2D.SetActive (true);
				CameraVR.SetActive (false);
				UnityEngine.XR.XRSettings.enabled = false;
				UnityEngine.XR.XRSettings.LoadDeviceByName ("None");
			}

		}
	}
	
}
[Serializable]
class SettingsFile{
	public int aaValue;
	public int vsyncValue;
	public int relfectionValue;
	public float frameValue;
	public float fovValue;
	public bool isPixel;
	public bool isAI;
	public bool isVR;
	public float audioValue;
	public float musicValue;
}
