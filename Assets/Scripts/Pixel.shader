Shader "Custom/Pixel"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Divider ("Divider", Float) = 20
		//_Width ("_Width", float) = 200;
		//_Height ("_Height", float) = 200;
	}
	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			float _Divider;
			float _Width;
			float _Height;
			sampler2D _MainTex;

			fixed4 frag (v2f i) : SV_Target
			{
				float2 uv = i.uv;
				uv.x *= (_Width / _Divider);
				uv.y *= (_Height / _Divider);
				uv.x = round(uv.x) / (_Width / _Divider);
				uv.y = round(uv.y) / (_Height / _Divider);
				fixed4 col = tex2D(_MainTex, uv);
				return col;
			}
			ENDCG
		}
	}
}
