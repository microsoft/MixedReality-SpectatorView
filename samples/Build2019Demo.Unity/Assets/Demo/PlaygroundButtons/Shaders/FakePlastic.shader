// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "FakePlastic" {

Properties {

    [Header(Diffuse And Specular)]
        _Albedo_("Albedo", Color) = (0.478431,0.478431,0.478431,1)
        _Specular_("Specular", Range(0,5)) = 0.5
        _Shininess_("Shininess", Range(0,10)) = 10
        _Vx_Color_Blend_("Vx Color Blend", Range(0,1)) = 0
     
    [Header(Reflection)]
        _Reflection_("Reflection", Range(0,2)) = 1
        _Front_Reflect_("Front Reflect", Range(0,1)) = 0.2
        _Edge_Reflect_("Edge Reflect", Range(0,1)) = 1
        _Power_("Power", Range(0,10)) = 4
     
    [Header(SSS)]
        _SSS_Intensity_("SSS Intensity", Range(0,1)) = 0
        _SSS_Base_Color_("SSS Base Color", Color) = (1,1,1,1)
        _SSS_Vx_Color_Mix_("SSS Vx Color Mix", Range(0,1)) = 0
     
    [Header(Sun)]
        _Sun_Theta_("Sun Theta", Range(0,1)) = 0.426
        _Sun_Phi_("Sun Phi", Range(0,1)) = 0.3
        _Indirect_Diffuse_("Indirect Diffuse", Range(0,1)) = 0.4
     
    [Header(Environment)]
        _Sky_Color_("Sky Color", Color) = (0.694118,0.803922,1,1)
        _Horizon_Color_("Horizon Color", Color) = (0.843137,0.87451,1,1)
        _Ground_Color_("Ground Color", Color) = (0.4,0.415686,0.501961,1)
        _Horizon_Power_("Horizon Power", Range(0,10)) = 1
     

}

SubShader {
    Tags{ "RenderType" = "Opaque" }
    Blend Off

    LOD 100


    Pass

    {

    CGPROGRAM

    #pragma vertex vert
    #pragma fragment frag
    #pragma multi_compile_instancing
    #pragma target 4.0

    #include "UnityCG.cginc"

    float _Sun_Theta_;
    float _Sun_Phi_;
    float _Indirect_Diffuse_;
    float4 _Sky_Color_;
    float4 _Horizon_Color_;
    float4 _Ground_Color_;
    float _Horizon_Power_;
    float _Reflection_;
    float _Front_Reflect_;
    float _Edge_Reflect_;
    float _Power_;
    float _SSS_Intensity_;
    float4 _SSS_Base_Color_;
    float _SSS_Vx_Color_Mix_;
    float4 _Albedo_;
    float _Specular_;
    float _Shininess_;
    float _Vx_Color_Blend_;




    struct VertexInput {
        float4 vertex : POSITION;
        half3 normal : NORMAL;
        float2 uv0 : TEXCOORD0;
        float4 color : COLOR;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    struct VertexOutput {
        float4 pos : SV_POSITION;
        half4 normalWorld : TEXCOORD5;
        float3 posWorld : TEXCOORD7;
        float4 vertexColor : COLOR;
      UNITY_VERTEX_OUTPUT_STEREO
    };

    // declare parm vars here

    //BLOCK_BEGIN Object_To_World_Pos 35

    void Object_To_World_Pos_B35(
        float3 Pos_Object,
        out float3 Pos_World    )
    {
        Pos_World=(mul(unity_ObjectToWorld, float4(Pos_Object, 1)));
        
    }
    //BLOCK_END Object_To_World_Pos

    //BLOCK_BEGIN Object_To_World_Normal 37

    void Object_To_World_Normal_B37(
        float3 Nrm_Object,
        out float3 Nrm_World    )
    {
        Nrm_World=UnityObjectToWorldNormal(Nrm_Object);
        
    }
    //BLOCK_END Object_To_World_Normal


    VertexOutput vert(VertexInput vertInput)
    {
        UNITY_SETUP_INSTANCE_ID(vertInput);
        VertexOutput o;
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);


        float3 Pos_World_Q35;
        Object_To_World_Pos_B35(vertInput.vertex.xyz,Pos_World_Q35);

        float3 Nrm_World_Q37;
        Object_To_World_Normal_B37(vertInput.normal,Nrm_World_Q37);

        float3 Position = Pos_World_Q35;
        float3 Normal = Nrm_World_Q37;
        float2 UV = vertInput.uv0;
        float3 Tangent = float3(0,0,0);
        float3 Binormal = float3(0,0,0);
        float4 Color = vertInput.color;


        o.pos = UnityObjectToClipPos(vertInput.vertex);
        o.pos = mul(UNITY_MATRIX_VP, float4(Position,1));
        o.posWorld = Position;
        o.normalWorld.xyz = Normal; o.normalWorld.w=1.0;
        o.vertexColor = Color;

        return o;
    }

    //BLOCK_BEGIN Fragment_Main 42

    float4 SampleEnv(float3 D, float4 S, float4 H, float4 G, float exponent)
    {
        float k = pow(abs(D.y),exponent);
        float4 C;
        if (D.y>0.0) {
            C=lerp(H,S,float4(k,k,k,k));
        } else {
            C=lerp(H,G,float4(k,k,k,k));    
        }
        return C;
    }
    
    void Fragment_Main_B42(
        float Sun_Theta,
        float Sun_Phi,
        float3 Position,
        float3 Eye,
        float3 Normal,
        float4 Albedo,
        float Fresnel_Reflect,
        float Shininess,
        float3 Incident,
        float4 Horizon_Color,
        float4 Sky_Color,
        float4 Ground_Color,
        float Indirect_Diffuse,
        float Specular,
        float Horizon_Power,
        float Reflection,
        out float4 Result    )
    {
        
        float theta = Sun_Theta * 2.0 * 3.14159;
        float phi = Sun_Phi * 3.14159;
        
        float3 lightDir =  float3(cos(phi)*cos(theta),sin(phi),cos(phi)*sin(theta));
        float NdotL = max(dot(lightDir,Normal),0.0);
        
        //float3 H = normalize(Normal-Incident);
        float3 R = reflect(Incident,Normal);
        float RdotL = max(0.0,dot(R,lightDir));
        float specular = pow(RdotL,Shininess);
        
        float4 reflected = SampleEnv(R,Sky_Color,Horizon_Color,Ground_Color,Horizon_Power);
        float4 gi = lerp(Ground_Color,Sky_Color,float4(Normal.y*0.5+0.5,Normal.y*0.5+0.5,Normal.y*0.5+0.5,Normal.y*0.5+0.5));
        //SampleEnv(Normal,Sky_Color,Horizon_Color,Ground_Color,1);
        
        Result = (NdotL + gi * Indirect_Diffuse) * Albedo * (1.0-Fresnel_Reflect) + (specular*Specular + Fresnel_Reflect * reflected*Reflection);
        
    }
    //BLOCK_END Fragment_Main

    //BLOCK_BEGIN Fast_Fresnel 40

    void Fast_Fresnel_B40(
        float Front_Reflect,
        float Edge_Reflect,
        float Power,
        float3 Incident,
        float3 Normal,
        out float Transmit,
        out float Reflect    )
    {
        
        float d = abs(dot(Incident,Normal));
        Reflect = Front_Reflect+(Edge_Reflect-Front_Reflect)*pow(1-d,Power);
        Transmit=1-Reflect;
        
    }
    //BLOCK_END Fast_Fresnel


    //fixed4 frag(VertexOutput fragInput, fixed facing : VFACE) : SV_Target
    half4 frag(VertexOutput fragInput) : SV_Target
    {
        half4 result;

        // Normalize3
        float3 Normalized_Q39 = normalize(fragInput.normalWorld.xyz);

        // Incident3
        float3 Incident_Q41 = normalize(fragInput.posWorld - _WorldSpaceCameraPos);

        // Mix_Colors
        float4 Color_At_T_Q47 = lerp(float4(1,1,1,1), fragInput.vertexColor,float4( _SSS_Vx_Color_Mix_, _SSS_Vx_Color_Mix_, _SSS_Vx_Color_Mix_, _SSS_Vx_Color_Mix_));

        // Mix_Colors
        float4 Color_At_T_Q48 = lerp(float4(1,1,1,1), fragInput.vertexColor,float4( _Vx_Color_Blend_, _Vx_Color_Blend_, _Vx_Color_Blend_, _Vx_Color_Blend_));

        // Multiply_Colors
        float4 Product_Q53 = Color_At_T_Q47 * _SSS_Base_Color_ * _SSS_Intensity_;

        // Multiply_Colors
        float4 Product_Q45 = _Albedo_ * Color_At_T_Q48;

        float Transmit_Q40;
        float Reflect_Q40;
        if (true) {
          Fast_Fresnel_B40(_Front_Reflect_,_Edge_Reflect_,_Power_,Incident_Q41,Normalized_Q39,Transmit_Q40,Reflect_Q40);
        } else {
          Transmit_Q40 = 1;
          Reflect_Q40 = 0;
        }

        float4 Result_Q42;
        Fragment_Main_B42(_Sun_Theta_,_Sun_Phi_,fragInput.posWorld,_WorldSpaceCameraPos,Normalized_Q39,Product_Q45,Reflect_Q40,_Shininess_,Incident_Q41,_Horizon_Color_,_Sky_Color_,_Ground_Color_,_Indirect_Diffuse_,_Specular_,_Horizon_Power_,_Reflection_,Result_Q42);

        // Add_Scaled_Color
        float4 Sum_Q46 = Result_Q42 + Product_Q53;

        float4 Out_Color = Sum_Q46;
        float Clip_Threshold = 0;

        result = Out_Color;
        return result;
    }

    ENDCG
  }
 }
}
