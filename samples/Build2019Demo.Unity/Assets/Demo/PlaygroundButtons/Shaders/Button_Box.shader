// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Button_Box" {

Properties {

    [Header(Wireframe)]
        _Edge_Width_("Edge Width", Range(0,1)) = 0.17
        _Edge_Color_("Edge Color", Color) = (0.114,0.114,0.114,0.114)
     
    [Header(Proximity)]
        _Proximity_Max_Intensity_("Proximity Max Intensity", Range(0,1)) = 1
        _Proximity_Far_Distance_("Proximity Far Distance", Range(0,1)) = 0.19
        _Proximity_Near_Radius_("Proximity Near Radius", Range(0,1)) = 0.05
     
    [Header(Selection)]
        _Selection_Fuzz_("Selection Fuzz", Range(0,1)) = 0.5
        _Selected_("Selected", Range(0,1)) = 0
        _Selection_Fade_("Selection Fade", Range(0,1)) = 0
        _Selection_Fade_Size_("Selection Fade Size", Range(0,1)) = 0.3
        _Selected_Distance_("Selected Distance", Range(0,1)) = 0.06
        _Selected_Fade_Length_("Selected Fade Length", Range(0,1)) = 0.06
     
    [Header(Blob)]
        [Toggle] _Blob_Enable_("Blob Enable", Float) = 1
        _Blob_Position_("Blob Position", Vector) = (0, 0.0, -0.55, 1)
        _Blob_Intensity_("Blob Intensity", Range(0,3)) = 1
        _Blob_Near_Size_("Blob Near Size", Range(0,1)) = 0.255
        _Blob_Far_Size_("Blob Far Size", Range(0,1)) = 0.511
        _Blob_Near_Distance_("Blob Near Distance", Range(0,1)) = 0
        _Blob_Far_Distance_("Blob Far Distance", Range(0,1)) = 0.2
        _Blob_Fade_Length_("Blob Fade Length", Range(0,1)) = 0.3
        _Blob_Inner_Fade_("Blob Inner Fade", Range(0.001,1)) = 0.01
        _Blob_Pulse_("Blob Pulse", Range(0,1)) = 0
        _Blob_Fade_("Blob Fade", Range(0,1)) = 1
     
    [Header(Blob Texture)]
        [NoScaleOffset] _Blob_Texture_("Blob Texture", 2D) = "" {}
     
    [Header(Blob 2)]
        [Toggle] _Blob_Enable_2_("Blob Enable 2", Float) = 0
        _Blob_Position_2_("Blob Position 2", Vector) = (10, 10.1, -0.6, 1)
        _Blob_Near_Size_2_("Blob Near Size 2", Range(0,1)) = 0.02
        _Blob_Inner_Fade_2_("Blob Inner Fade 2", Range(0,1)) = 0.1
        _Blob_Pulse_2_("Blob Pulse 2", Range(0,1)) = 0
        _Blob_Fade_2_("Blob Fade 2", Range(0,1)) = 1
     
    [Header(Active Face)]
        _Active_Face_Dir_("Active Face Dir", Vector) = (0, 0, -1, 1)
     
    [Header(Hololens Edge Fade)]
        [Toggle] _Enable_Fade_("Enable Fade", Float) = 1
        _Fade_Width_("Fade Width", Range(0,10)) = 1.5
        [Toggle] _Smooth_Active_Face_("Smooth Active Face", Float) = 0
     
    [Header(Debug)]
        [Toggle] _Show_Frame_("Show Frame", Float) = 0
     

    [Header(Global)]
        [Toggle] Use_Global_Left_Index("Use Global Left Index", Float) = 0
        [Toggle] Use_Global_Right_Index("Use Global Right Index", Float) = 0
}

SubShader {
    Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
    Blend One One
    Cull Off
    ZWrite Off
    Tags {"DisableBatching" = "True"}

    LOD 100


    Pass

    {

    CGPROGRAM

    #pragma vertex vert
    #pragma fragment frag
    #pragma multi_compile_instancing
    #pragma target 4.0

    #include "UnityCG.cginc"

    bool _Enable_Fade_;
    float _Fade_Width_;
    bool _Smooth_Active_Face_;
    sampler2D _Blob_Texture_;
    bool _Blob_Enable_2_;
    float3 _Blob_Position_2_;
    float _Blob_Near_Size_2_;
    float _Blob_Inner_Fade_2_;
    float _Blob_Pulse_2_;
    float _Blob_Fade_2_;
    float3 _Active_Face_Dir_;
    float _Edge_Width_;
    float4 _Edge_Color_;
    float _Proximity_Max_Intensity_;
    float _Proximity_Far_Distance_;
    float _Proximity_Near_Radius_;
    bool _Blob_Enable_;
    float3 _Blob_Position_;
    float _Blob_Intensity_;
    float _Blob_Near_Size_;
    float _Blob_Far_Size_;
    float _Blob_Near_Distance_;
    float _Blob_Far_Distance_;
    float _Blob_Fade_Length_;
    float _Blob_Inner_Fade_;
    float _Blob_Pulse_;
    float _Blob_Fade_;
    bool _Show_Frame_;
    float _Selection_Fuzz_;
    float _Selected_;
    float _Selection_Fade_;
    float _Selection_Fade_Size_;
    float _Selected_Distance_;
    float _Selected_Fade_Length_;

    bool Use_Global_Left_Index;
    bool Use_Global_Right_Index;
    float4 Global_Left_Index_Tip_Position;
    float4 Global_Right_Index_Tip_Position;
    float4 Global_Left_Thumb_Tip_Position;
    float4 Global_Right_Thumb_Tip_Position;

    fixed _ClipBoxSide;
    float4 _ClipBoxSize;
    float4x4 _ClipBoxInverseTransform;

    inline float PointVsBox(float3 worldPosition, float3 boxSize, float4x4 boxInverseTransform)
    {
         float3 distance = abs(mul(boxInverseTransform, float4(worldPosition, 1.0))) - boxSize;
         return length(max(distance, 0.0)) + min(max(distance.x, max(distance.y, distance.z)), 0.0);
    }


    struct VertexInput {
        float4 vertex : POSITION;
        half3 normal : NORMAL;
        float2 uv0 : TEXCOORD0;
        float4 tangent : TANGENT;
        float4 color : COLOR;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    struct VertexOutput {
        float4 pos : SV_POSITION;
        half4 normalWorld : TEXCOORD5;
        float2 uv : TEXCOORD0;
        float3 posWorld : TEXCOORD7;
        float4 tangent : TANGENT;
        float4 binormal : TEXCOORD6;
        float4 vertexColor : COLOR;
        float4 extra1 : TEXCOORD4;
      UNITY_VERTEX_OUTPUT_STEREO
    };

    // declare parm vars here

    //BLOCK_BEGIN Blob_Vertex 778

    void Blob_Vertex_B778(
        float3 Position,
        float3 Normal,
        float3 Tangent,
        float3 Bitangent,
        float3 Blob_Position,
        float Intensity,
        float Blob_Near_Size,
        float Blob_Far_Size,
        float Blob_Near_Distance,
        float Blob_Far_Distance,
        float4 Vx_Color,
        float2 UV,
        float3 Face_Center,
        float2 Face_Size,
        float2 In_UV,
        float Blob_Fade_Length,
        float Selection_Fade,
        float Selection_Fade_Size,
        float Inner_Fade,
        float3 Active_Face_Center,
        float Blob_Pulse,
        float Blob_Fade,
        float Blob_Enabled,
        out float3 Out_Position,
        out float2 Out_UV,
        out float3 Blob_Info    )
    {
        
        float blobSize, fadeIn;
        float3 Hit_Position;
        Blob_Info = float3(0.0,0.0,0.0);
        
        float Hit_Distance = dot(Blob_Position-Face_Center, Normal);
        Hit_Position = Blob_Position - Hit_Distance * Normal;
        
        float absD = abs(Hit_Distance);
        float lerpVal = clamp((absD-Blob_Near_Distance)/(Blob_Far_Distance-Blob_Near_Distance),0.0,1.0);
        fadeIn = 1.0-clamp((absD-Blob_Far_Distance)/Blob_Fade_Length,0.0,1.0);
        
        float innerFade = 1.0-clamp(-Hit_Distance/Inner_Fade,0.0,1.0);
        
        //compute blob size
        float farClip = saturate(1.0-step(Blob_Far_Distance+Blob_Fade_Length,absD));
        float size = lerp(Blob_Near_Size,Blob_Far_Size,lerpVal)*farClip;
        blobSize = lerp(size,Selection_Fade_Size,Selection_Fade)*innerFade*Blob_Enabled;
        Blob_Info.x = lerpVal*0.5+0.5;
            
        Blob_Info.y = fadeIn*Intensity*(1.0-Selection_Fade)*Blob_Fade;
        Blob_Info.x *= (1.0-Blob_Pulse);
        
        //compute blob position
        float3 delta = Hit_Position - Face_Center;
        float2 blobCenterXY = float2(dot(delta,Tangent),dot(delta,Bitangent));
        
        float2 quadUVin = 2.0*UV-1.0;  // remap to (-.5,.5)
        float2 blobXY = blobCenterXY+quadUVin*blobSize;
        
        //keep the quad within the face
        float2 blobClipped = clamp(blobXY,-Face_Size*0.5,Face_Size*0.5);
        float2 blobUV = (blobClipped-blobCenterXY)/max(blobSize,0.0001)*2.0;
        
        float3 blobCorner = Face_Center + blobClipped.x*Tangent + blobClipped.y*Bitangent;
        
        //blend using VxColor.r=1 for blob quad, 0 otherwise
        Out_Position = lerp(Position,blobCorner,Vx_Color.rrr);
        Out_UV = lerp(In_UV,blobUV,Vx_Color.rr);
        
    }
    //BLOCK_END Blob_Vertex

    //BLOCK_BEGIN Proximity_Vertex 786

    float2 ProjectProximity(
        float3 blobPosition,
        float3 position,
        float3 center,
        float3 dir,
        float3 xdir,
        float3 ydir,
        out float vdistance
    )
    {
        float3 delta = blobPosition - position;
        float2 xy = float2(dot(delta,xdir),dot(delta,ydir));
        vdistance = abs(dot(delta,dir));
        return xy;
    }
    
    void Proximity_Vertex_B786(
        float3 Blob_Position,
        float3 Blob_Position_2,
        float3 Active_Face_Center,
        float3 Active_Face_Dir,
        float3 Position,
        float Proximity_Far_Distance,
        out float4 Extra1,
        out float Distance_To_Face,
        out float Intensity    )
    {
        
        float3 Active_Face_Dir_X = normalize(float3(Active_Face_Dir.y-Active_Face_Dir.z,Active_Face_Dir.z-Active_Face_Dir.x,Active_Face_Dir.x-Active_Face_Dir.y));
        float3 Active_Face_Dir_Y = cross(Active_Face_Dir,Active_Face_Dir_X);
        
        float distz1,distz2;
        Extra1.xy = ProjectProximity(Blob_Position,Position,Active_Face_Center,Active_Face_Dir,Active_Face_Dir_X,Active_Face_Dir_Y,distz1);
        Extra1.zw = ProjectProximity(Blob_Position_2,Position,Active_Face_Center,Active_Face_Dir,Active_Face_Dir_X,Active_Face_Dir_Y,distz2);
        
        Distance_To_Face = dot(Active_Face_Dir,Position-Active_Face_Center);
        Intensity = 1.0 - saturate(min(distz1,distz2)/Proximity_Far_Distance);
        
    }
    //BLOCK_END Proximity_Vertex

    //BLOCK_BEGIN Holo_Edge_Vertex 774

    void Holo_Edge_Vertex_B774(
        float3 Incident,
        float3 Normal,
        float2 UV,
        float3 Tangent,
        float3 Bitangent,
        bool Smooth_Active_Face,
        float Active,
        out float4 Holo_Edges    )
    {
        float NdotI = dot(Incident,Normal);
        
        float2 flip = (UV-float2(0.5,0.5));
        float udot = dot(Incident,Tangent)*flip.x*NdotI;
        float uval = (udot>0.0 ? 0.0 : 1.0);
        
        float vdot = -dot(Incident,Bitangent)*flip.y*NdotI;
        float vval = (vdot>0.0 ? 0.0 : 1.0);
        
        if (Smooth_Active_Face && Active>0.0) {
             float d = 1.0; //abs(dot(Normal,Incident));
             uval=max(d,uval); vval=max(d,vval);
        }
        Holo_Edges = float4(1.0,1.0,1.0,1.0)-float4(uval*UV.x,uval*(1.0-UV.x),vval*UV.y,vval*(1.0-UV.y));
    }
    //BLOCK_END Holo_Edge_Vertex

    //BLOCK_BEGIN Object_To_World_Pos 739

    void Object_To_World_Pos_B739(
        float3 Pos_Object,
        out float3 Pos_World    )
    {
        Pos_World=(mul(unity_ObjectToWorld, float4(Pos_Object, 1)));
        
    }
    //BLOCK_END Object_To_World_Pos

    //BLOCK_BEGIN Choose_Blob 767

    void Choose_Blob_B767(
        float4 Vx_Color,
        float3 Position1,
        float3 Position2,
        bool Blob_Enable_1,
        bool Blob_Enable_2,
        float Near_Size_1,
        float Near_Size_2,
        float Blob_Inner_Fade_1,
        float Blob_Inner_Fade_2,
        float Blob_Pulse_1,
        float Blob_Pulse_2,
        float Blob_Fade_1,
        float Blob_Fade_2,
        out float3 Position,
        out float Near_Size,
        out float Inner_Fade,
        out float Blob_Enable,
        out float Fade,
        out float Pulse    )
    {
        Position = Position1*(1.0-Vx_Color.g)+Vx_Color.g*Position2;
        
        float b1 = Blob_Enable_1 ? 1.0 : 0.0;
        float b2 = Blob_Enable_2 ? 1.0 : 0.0;
        Blob_Enable = b1+(b2-b1)*Vx_Color.g;
        
        Pulse = Blob_Pulse_1*(1.0-Vx_Color.g)+Vx_Color.g*Blob_Pulse_2;
        Fade = Blob_Fade_1*(1.0-Vx_Color.g)+Vx_Color.g*Blob_Fade_2;
        Near_Size = Near_Size_1*(1.0-Vx_Color.g)+Vx_Color.g*Near_Size_2;
        Inner_Fade = Blob_Inner_Fade_1*(1.0-Vx_Color.g)+Vx_Color.g*Blob_Inner_Fade_2;
    }
    //BLOCK_END Choose_Blob

    //BLOCK_BEGIN Wireframe_Vertex 783

    void Wireframe_Vertex_B783(
        float3 Position,
        float3 Normal,
        float3 Tangent,
        float3 Bitangent,
        float Edge_Width,
        float2 Face_Size,
        out float3 Wire_Vx_Pos,
        out float2 UV,
        out float2 Widths    )
    {
        Widths.xy = Edge_Width/Face_Size;
        
        float x = dot(Position,Tangent);
        float y = dot(Position,Bitangent);
        
        float dx = 0.5-abs(x);
        float newx = (0.5 - dx * Widths.x * 2.0)*sign(x);
        
        float dy = 0.5-abs(y);
        float newy = (0.5 - dy * Widths.y * 2.0)*sign(y);
        
        Wire_Vx_Pos = Normal * 0.5 + newx * Tangent + newy * Bitangent;
        
        UV.x = dot(Wire_Vx_Pos,Tangent) + 0.5;
        UV.y = dot(Wire_Vx_Pos,Bitangent) + 0.5;
    }
    //BLOCK_END Wireframe_Vertex

    //BLOCK_BEGIN Selection_Vertex 779

    float2 ramp2(float2 start, float2 end, float2 x)
    {
       return clamp((x-start)/(end-start),float2(0.0,0.0),float2(1.0,1.0));
    }
    
    float computeSelection(
        float3 blobPosition,
        float3 normal,
        float3 tangent,
        float3 bitangent,
        float3 faceCenter,
        float2 faceSize,
        float selectionFuzz,
        float farDistance,
        float fadeLength
    )
    {
        float3 delta = blobPosition - faceCenter;
        float absD = abs(dot(delta,normal));
        float fadeIn = 1.0-clamp((absD-farDistance)/fadeLength,0.0,1.0);
        
        float2 blobCenterXY = float2(dot(delta,tangent),dot(delta,bitangent));
    
        float2 innerFace = faceSize * (1.0-selectionFuzz) * 0.5;
        float2 selectPulse = ramp2(-faceSize*0.5,-innerFace,blobCenterXY)-ramp2(innerFace,faceSize*0.5,blobCenterXY);
    
        return selectPulse.x * selectPulse.y * fadeIn;
    }
    
    void Selection_Vertex_B779(
        float3 Blob_Position,
        float3 Blob_Position_2,
        float3 Face_Center,
        float2 Face_Size,
        float3 Normal,
        float3 Tangent,
        float3 Bitangent,
        float Selection_Fuzz,
        float Selected,
        float Far_Distance,
        float Fade_Length,
        float3 Active_Face_Dir,
        out float Show_Selection    )
    {
        float select1 = computeSelection(Blob_Position,Normal,Tangent,Bitangent,Face_Center,Face_Size,Selection_Fuzz,Far_Distance,Fade_Length);
        float select2 = computeSelection(Blob_Position_2,Normal,Tangent,Bitangent,Face_Center,Face_Size,Selection_Fuzz,Far_Distance,Fade_Length);
        
        float Active = max(0.0,dot(Active_Face_Dir,Normal));
        
        Show_Selection = lerp(max(select1,select2),1.0,Selected)*Active;
    }
    //BLOCK_END Selection_Vertex

    //BLOCK_BEGIN Proximity_Visibility 788

    void Proximity_Visibility_B788(
        float Selection,
        float3 Proximity_Center,
        float3 Proximity_Center_2,
        float Input_Width,
        float Proximity_Far_Distance,
        float Proximity_Radius,
        float3 Active_Face_Center,
        float3 Active_Face_Dir,
        out float Width    )
    {
        //make all edges invisible if no proximity or selection visible
        float3 boxEdges = (mul((float3x3)unity_ObjectToWorld, float3(0.5,0.5,0.5)));
        float boxMaxSize = length(boxEdges);
        
        float d1 = dot(Proximity_Center-Active_Face_Center, Active_Face_Dir);
        float3 blob1 = Proximity_Center - d1 * Active_Face_Dir;
        
        float d2 = dot(Proximity_Center_2-Active_Face_Center, Active_Face_Dir);
        float3 blob2 = Proximity_Center_2 - d2 * Active_Face_Dir;
        
        //float3 objectOriginInWorld = (mul(_Object2World, float4(float3(0.0,0.0,0.0), 1)));
        float3 delta1 = blob1 - Active_Face_Center;
        float3 delta2 = blob2 - Active_Face_Center;
        
        float dist1 = dot(delta1,delta1);
        float dist2 = dot(delta2,delta2);
        
        float nearestProxDist = sqrt(min(dist1,dist2));
        
        //Width = Input_Width * (1.0 - step(boxMaxSize+Proximity_Radius,nearestProxDist)*(1.0-step(Selection,0.0)));
        Width = Input_Width * (1.0 - step(boxMaxSize+Proximity_Radius,nearestProxDist))*(1.0-step(Proximity_Far_Distance,min(d1,d2))*(1.0-step(0.0001,Selection)));
        
    }
    //BLOCK_END Proximity_Visibility


    VertexOutput vert(VertexInput vertInput)
    {
        UNITY_SETUP_INSTANCE_ID(vertInput);
        VertexOutput o;
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);


        // Active_Face_Center
        float3 Active_Face_Center_Q780;
        Active_Face_Center_Q780 = (mul(unity_ObjectToWorld, float4(_Active_Face_Dir_*0.5, 1)));
        
        // Pick_Local_Or_Global_Left
        float3 Blob_Position_Q771 =  (Use_Global_Left_Index ? Global_Left_Index_Tip_Position.xyz :  _Blob_Position_);

        // Pick_Local_Or_Global_Right
        float3 Blob_Position_Q772 =  (Use_Global_Right_Index ? Global_Right_Index_Tip_Position.xyz :  _Blob_Position_2_);

        // Object_To_World_Dir
        float3 Tangent_World_Q757;
        Tangent_World_Q757=(mul((float3x3)unity_ObjectToWorld, vertInput.tangent));
        
        // Object_To_World_Dir
        float3 Binormal_World_Q758;
        Binormal_World_Q758=(mul((float3x3)unity_ObjectToWorld, (normalize(cross(vertInput.normal,vertInput.tangent)))));
        
        // Object_To_World_Normal
        float3 Normal_World_Q756;
        Normal_World_Q756=UnityObjectToWorldNormal(vertInput.normal);
        
        // Scale3
        float3 Result_Q744 = 0.5 * vertInput.normal;

        // Object_To_World_Normal
        float3 Active_Face_Dir_Q761 = normalize(UnityObjectToWorldNormal(_Active_Face_Dir_));

        // Normalize3
        float3 Normal_World_N_Q755 = normalize(Normal_World_Q756);

        // Normalize3
        float3 Tangent_World_N_Q754 = normalize(Tangent_World_Q757);

        // Normalize3
        float3 Binormal_World_N_Q759 = normalize(Binormal_World_Q758);

        float3 Position_Q767;
        float Near_Size_Q767;
        float Inner_Fade_Q767;
        float Blob_Enable_Q767;
        float Fade_Q767;
        float Pulse_Q767;
        Choose_Blob_B767(vertInput.color,Blob_Position_Q771,Blob_Position_Q772,_Blob_Enable_,_Blob_Enable_2_,_Blob_Near_Size_,_Blob_Near_Size_2_,_Blob_Inner_Fade_,_Blob_Inner_Fade_2_,_Blob_Pulse_,_Blob_Pulse_2_,_Blob_Fade_,_Blob_Fade_2_,Position_Q767,Near_Size_Q767,Inner_Fade_Q767,Blob_Enable_Q767,Fade_Q767,Pulse_Q767);

        // Object_To_World_Pos
        float3 Face_Center_Q760;
        Face_Center_Q760=(mul(unity_ObjectToWorld, float4(Result_Q744, 1)));
        
        // Face_Size
        float2 Face_Size_Q782 = float2(length(Tangent_World_Q757),length(Binormal_World_Q758));

        float Show_Selection_Q779;
        Selection_Vertex_B779(Blob_Position_Q771,Blob_Position_Q772,Face_Center_Q760,Face_Size_Q782,Normal_World_N_Q755,Tangent_World_N_Q754,Binormal_World_N_Q759,_Selection_Fuzz_,_Selected_,_Selected_Distance_,_Selected_Fade_Length_,Active_Face_Dir_Q761,Show_Selection_Q779);

        // Active_Face
        float Active_Q762 = max(0.0,dot(Active_Face_Dir_Q761,Normal_World_N_Q755));

        float Width_Q788;
        Proximity_Visibility_B788(Show_Selection_Q779,Blob_Position_Q771,Blob_Position_Q772,_Edge_Width_,_Proximity_Far_Distance_,_Proximity_Near_Radius_,Active_Face_Center_Q780,Active_Face_Dir_Q761,Width_Q788);

        float3 Wire_Vx_Pos_Q783;
        float2 UV_Q783;
        float2 Widths_Q783;
        Wireframe_Vertex_B783(vertInput.vertex.xyz,vertInput.normal,vertInput.tangent,(normalize(cross(vertInput.normal,vertInput.tangent))),Width_Q788,Face_Size_Q782,Wire_Vx_Pos_Q783,UV_Q783,Widths_Q783);

        // Pack_For_Vertex
        float3 Vec3_Q753 = float3(Widths_Q783.x,Widths_Q783.y,vertInput.color.r);

        float3 Pos_World_Q739;
        Object_To_World_Pos_B739(Wire_Vx_Pos_Q783,Pos_World_Q739);

        // Incident3
        float3 Incident_Q765 = normalize(Pos_World_Q739-_WorldSpaceCameraPos);

        float3 Out_Position_Q778;
        float2 Out_UV_Q778;
        float3 Blob_Info_Q778;
        Blob_Vertex_B778(Pos_World_Q739,Normal_World_N_Q755,Tangent_World_N_Q754,Binormal_World_N_Q759,Position_Q767,_Blob_Intensity_,Near_Size_Q767,_Blob_Far_Size_,_Blob_Near_Distance_,_Blob_Far_Distance_,vertInput.color,vertInput.uv0,Face_Center_Q760,Face_Size_Q782,UV_Q783,_Blob_Fade_Length_,_Selection_Fade_,_Selection_Fade_Size_,Inner_Fade_Q767,Active_Face_Center_Q780,Pulse_Q767,Fade_Q767,Blob_Enable_Q767,Out_Position_Q778,Out_UV_Q778,Blob_Info_Q778);

        float4 Extra1_Q786;
        float Distance_To_Face_Q786;
        float Intensity_Q786;
        Proximity_Vertex_B786(Blob_Position_Q771,Blob_Position_Q772,Active_Face_Center_Q780,_Active_Face_Dir_,Pos_World_Q739,_Proximity_Far_Distance_,Extra1_Q786,Distance_To_Face_Q786,Intensity_Q786);

        float4 Holo_Edges_Q774;
        Holo_Edge_Vertex_B774(Incident_Q765,Normal_World_N_Q755,vertInput.uv0,Tangent_World_Q757,Binormal_World_Q758,_Smooth_Active_Face_,Active_Q762,Holo_Edges_Q774);

        // From_XYZ
        float3 Vec3_Q745 = float3(Show_Selection_Q779,Distance_To_Face_Q786,Intensity_Q786);

        float3 Position = Out_Position_Q778;
        float2 UV = Out_UV_Q778;
        float3 Tangent = Blob_Info_Q778;
        float3 Binormal = Vec3_Q745;
        float3 Normal = Vec3_Q753;
        float4 Extra1 = Extra1_Q786;
        float4 Color = Holo_Edges_Q774;


        o.pos = UnityObjectToClipPos(vertInput.vertex);
        o.pos = mul(UNITY_MATRIX_VP, float4(Position,1));
        o.posWorld = Position;
        o.normalWorld.xyz = Normal; o.normalWorld.w=1.0;
        o.uv = UV;
        o.tangent.xyz = Tangent; o.tangent.w=1.0;
        o.binormal.xyz = Binormal; o.binormal.w=1.0;
        o.vertexColor = Color;
        o.extra1=Extra1;

        return o;
    }

    //BLOCK_BEGIN Holo_Edge_Fragment 764

    void Holo_Edge_Fragment_B764(
        float4 Edges,
        float Edge_Width,
        out float NotEdge    )
    {
        float2 c = float2(min(Edges.r,Edges.g),min(Edges.b,Edges.a));
        float2 df = fwidth(c)*Edge_Width;
        float2 g = saturate(c/df);
        NotEdge = g.x*g.y;
    }
    //BLOCK_END Holo_Edge_Fragment

    //BLOCK_BEGIN Blob_Fragment 768

    void Blob_Fragment_B768(
        float2 UV,
        float3 Blob_Info,
        sampler2D Blob_Texture,
        out float4 Blob_Color    )
    {
        float k = dot(UV,UV);
        Blob_Color = Blob_Info.y * tex2D(Blob_Texture,float2(float2(sqrt(k),Blob_Info.x).x,1.0-float2(sqrt(k),Blob_Info.x).y))*(1.0-saturate(k));
    }
    //BLOCK_END Blob_Fragment

    //BLOCK_BEGIN Wireframe_Fragment 784

    void Wireframe_Fragment_B784(
        float Proximity,
        float4 Edge_Color,
        float3 Widths,
        float2 UV,
        out float4 Wireframe    )
    {
        float2 xy = abs(float2(0.5,0.5)-UV)*2.0;
        float2 ctr = min(xy,float2(1.0,1.0)-Widths.xy);
        float2 ab = saturate((xy-ctr)/Widths.xy);
        //changed 10-22-18
        //float Result1 = dot(ab,ab);
        //Wireframe = Result1 * Result1 * Proximity * Edge_Color;
        
        //added 12-14-18
        if (false) {
            float2 ss = float2(1.0,1.0) - smoothstep(float2(0.0,0.0),float2(1.0,1.0),ab);
            Wireframe = (1.0-ss.x*ss.y) * Proximity * Edge_Color;
        } else {
            Wireframe = dot(ab,ab) * Proximity * Edge_Color;
        }
        
    }
    //BLOCK_END Wireframe_Fragment

    //BLOCK_BEGIN Proximity 787

    void Proximity_B787(
        float3 Proximity_Center,
        float3 Proximity_Center_2,
        float Proximity_Max_Intensity,
        float Proximity_Near_Radius,
        float3 Position,
        float3 Show_Selection,
        float4 Extra1,
        float Dist_To_Face,
        float Intensity,
        out float Proximity    )
    {
        float2 delta1 = Extra1.xy;
        float2 delta2 = Extra1.zw;
        
        float d2 = sqrt(min(dot(delta1,delta1),dot(delta2,delta2)) + Dist_To_Face*Dist_To_Face);
        
        //float d = distance(Proximity_Center.xyz,Position);
        Proximity = Intensity * Proximity_Max_Intensity * (1.0-saturate(d2/Proximity_Near_Radius))*(1.0-Show_Selection.x)+Show_Selection.x;
        
    }
    //BLOCK_END Proximity

    //BLOCK_BEGIN To_XYZ 776

    void To_XYZ_B776(
        float3 Vec3,
        out float X,
        out float Y,
        out float Z    )
    {
        X=Vec3.x;
        Y=Vec3.y;
        Z=Vec3.z;
        
    }
    //BLOCK_END To_XYZ


    //fixed4 frag(VertexOutput fragInput, fixed facing : VFACE) : SV_Target
    half4 frag(VertexOutput fragInput) : SV_Target
    {
        clip(PointVsBox(fragInput.posWorld, _ClipBoxSize.xyz, _ClipBoxInverseTransform) * _ClipBoxSide);
        half4 result;

        float NotEdge_Q764;
        if (_Enable_Fade_) {
          Holo_Edge_Fragment_B764(fragInput.vertexColor,_Fade_Width_,NotEdge_Q764);
        } else {
          NotEdge_Q764 = 1;
        }

        float4 Blob_Color_Q768;
        Blob_Fragment_B768(fragInput.uv,fragInput.tangent.xyz,_Blob_Texture_,Blob_Color_Q768);

        // Is_Quad
        float Is_Quad_Q750;
        Is_Quad_Q750=fragInput.normalWorld.xyz.z;
        
        // Pick_Local_Or_Global_Left
        float3 Blob_Position_Q771 =  (Use_Global_Left_Index ? Global_Left_Index_Tip_Position.xyz :  _Blob_Position_);

        // Pick_Local_Or_Global_Right
        float3 Blob_Position_Q772 =  (Use_Global_Right_Index ? Global_Right_Index_Tip_Position.xyz :  _Blob_Position_2_);

        float X_Q776;
        float Y_Q776;
        float Z_Q776;
        To_XYZ_B776(fragInput.binormal.xyz,X_Q776,Y_Q776,Z_Q776);

        float Proximity_Q787;
        Proximity_B787(Blob_Position_Q771,Blob_Position_Q772,_Proximity_Max_Intensity_,_Proximity_Near_Radius_,fragInput.posWorld,fragInput.binormal.xyz,fragInput.extra1,Y_Q776,Z_Q776,Proximity_Q787);

        float4 Wireframe_Q784;
        Wireframe_Fragment_B784(Proximity_Q787,_Edge_Color_,fragInput.normalWorld.xyz,fragInput.uv,Wireframe_Q784);

        // Mix_Colors
        float4 Wire_Or_Blob_Q749 = lerp(Wireframe_Q784, Blob_Color_Q768,float4( Is_Quad_Q750, Is_Quad_Q750, Is_Quad_Q750, Is_Quad_Q750));

        // Conditional_Color
        float4 Result_Q748;
        Result_Q748 = _Show_Frame_ ? float4(0.3,0.3,0.3,0.3) : Wire_Or_Blob_Q749;
        
        // Scale_Color
        float4 Final_Color_Q766 = NotEdge_Q764 * Result_Q748;

        float4 Out_Color = Final_Color_Q766;
        float Clip_Threshold = 0;
        bool To_sRGB = false;

        result = Out_Color;
        return result;
    }

    ENDCG
  }
 }
}
