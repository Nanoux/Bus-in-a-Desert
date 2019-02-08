Shader "Custom/TerrainSurfaceShader" {
	Properties {
		testTexture("Texture", 2D) = "white"{}
		testScale("Scale", Float) = 1

	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows


		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.5
		#include "UnityCG.cginc"

		const static int maxLayerCount = 8;
		const static float epsilon = 1E-4;


		int roadWidth;
		int roadEdge;

		int layerCount;
		float3 baseColours[maxLayerCount];
		float baseStartHeights[maxLayerCount];
		float baseBlends[maxLayerCount];
		float baseColourStrength[maxLayerCount];
		float baseTextureScales[maxLayerCount];

		float minHeight;
		float maxHeight;

		sampler2D testTexture;
		float testScale;

		UNITY_DECLARE_TEX2DARRAY(baseTextures);

		struct Input {
			float3 worldPos;
			float3 worldNormal;
		};

		float inverseLerp(float a, float b, float value) {
			return saturate((value-a)/(b-a));
		}

		float3 triplanar(float3 worldPos, float scale, float3 blendAxes, int textureIndex) {
			float3 scaledWorldPos = worldPos / scale;
			float3 xProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.y, scaledWorldPos.z, textureIndex)) * blendAxes.x;
			float3 yProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.x, scaledWorldPos.z, textureIndex)) * blendAxes.y;
			float3 zProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.x, scaledWorldPos.y, textureIndex)) * blendAxes.z;
			return xProjection + yProjection + zProjection;
		}

		void surf (Input IN, inout SurfaceOutputStandard o) {
			float heightPercent = inverseLerp(minHeight,maxHeight, IN.worldPos.y);
			float3 blendAxes = abs(IN.worldNormal);
			blendAxes /= blendAxes.x + blendAxes.y + blendAxes.z;

			//if (abs(IN.worldPos.x) > 10){
				for (int i = 0; i < layerCount - 1; i ++) {
					float drawStrength = inverseLerp(-baseBlends[i]/2 - epsilon, baseBlends[i]/2, heightPercent - baseStartHeights[i]);

					float3 baseColour = baseColours[i] * baseColourStrength[i];
					float3 textureColour = triplanar(IN.worldPos, baseTextureScales[i], blendAxes, i) * (1-baseColourStrength[i]);

					o.Albedo = o.Albedo * (1-drawStrength) + (baseColour+textureColour) * drawStrength;
				}
			//}else{
			if (abs(IN.worldPos.x) < roadEdge && abs(IN.worldPos.x) > roadWidth){ 
				int i = layerCount - 1;
				float widthPercent = inverseLerp(roadEdge,roadWidth, abs(IN.worldPos.x));

				float drawStrength = inverseLerp(-baseBlends[i]/2 - epsilon, baseBlends[i]/2, widthPercent - (abs(IN.worldPos.x / roadEdge)));
				//drawStrength = 0.5;

				float3 baseColour = baseColours[i] * baseColourStrength[i];
				float3 textureColour = triplanar(IN.worldPos, baseTextureScales[i], blendAxes, i) * (1-baseColourStrength[i]);

				o.Albedo = o.Albedo * (1-drawStrength) + (baseColour+textureColour) * drawStrength;
			}
			if (abs(IN.worldPos.x) < roadWidth){
				int i = layerCount - 1;
				float widthPercent = 1;

				float drawStrength = inverseLerp(-baseBlends[i]/2 - epsilon, baseBlends[i]/2, widthPercent);
				//drawStrength = 0.5;

				float3 baseColour = baseColours[i] * baseColourStrength[i];
				float3 textureColour = triplanar(IN.worldPos, baseTextureScales[i], blendAxes, i) * (1-baseColourStrength[i]);

				o.Albedo = o.Albedo * (1-drawStrength) + (baseColour+textureColour) * drawStrength;
			}

		
		}


		ENDCG
	}
	//FallBack "Custom/Toon"
}