using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ImageEffects : MonoBehaviour {

	public Material Pixel;
	public Material Toon;

	public Toggle pixelBut;

	void OnRenderImage(RenderTexture src, RenderTexture dst){
		Graphics.Blit (src, dst);
		if (pixelBut.isOn) {
			Pixel.SetFloat ("_Width", Screen.width);
			Pixel.SetFloat ("_Height", Screen.height);
			Graphics.Blit (src, dst, Pixel);
		}
	}


}
