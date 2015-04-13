/*
* Shader utilizado para aplicar AlphaBlending
*/


//Variables utilizadas por el Vertex Shader
float4x4 matWorld;
float4x4 matWorldView;
float4x4 matWorldViewProj;

//Input del Vertex Shader
struct VS_INPUT 
{
   float4 Position : POSITION0;
   float3 Normal :   NORMAL0;
   float4 Color : COLOR;
   float2 Texcoord : TEXCOORD0;
};

//Output del Vertex Shader
struct VS_OUTPUT 
{
   float4 Position :        POSITION0;
   float2 Texcoord :        TEXCOORD0;
   float3 Normal :          TEXCOORD1;
   
};

//Vertex Shader
VS_OUTPUT vs_main( VS_INPUT Input )
{
   VS_OUTPUT Output;

   //Proyectar posicion
   Output.Position = mul( Input.Position, matWorldViewProj );
   
   //Las Texcoord quedan igual
   Output.Texcoord = Input.Texcoord;
   
   //Proyectar normal
   Output.Normal = mul( Input.Normal, matWorld );
      
   return Output;
   
}

//Variables utilizadas por el Pixel Shader
float alphaValue = 1;

//Textura utilizada por el Pixel Shader
texture diffuseMap_Tex;
sampler2D diffuseMap = sampler_state
{
   Texture = (diffuseMap_Tex);
   ADDRESSU = WRAP;
   ADDRESSV = WRAP;
   MINFILTER = LINEAR;
   MAGFILTER = LINEAR;
   MIPFILTER = LINEAR;
};

//Input del Pixel Shader
struct PS_INPUT 
{
   float2 Texcoord : TEXCOORD0;
};

//Pixel Shader
float4 ps_main( PS_INPUT Input ) : COLOR0
{      
   //Obtener el texel de textura
   float4 fvBaseColor = tex2D( diffuseMap, Input.Texcoord );
   fvBaseColor.a = alphaValue;
   return fvBaseColor;
}



/*
* Technique default
*/
technique DefaultTechnique
{
   pass Pass_0
   {
	  VertexShader = compile vs_2_0 vs_main();
	  PixelShader = compile ps_2_0 ps_main();
   }

}


//******************************************************************************//


//Input del Vertex Shader
struct VS_INPUT_NO_TEXTURE 
{
   float4 Position : POSITION0;
   float3 Normal : NORMAL0;
   float4 Color : COLOR;
};

//Output del Vertex Shader
struct VS_OUTPUT_NO_TEXTURE  
{
   float4 Position : POSITION0;
   float4 Color : COLOR;
   float3 Normal : TEXCOORD0;
   
};

//Vertex Shader
VS_OUTPUT_NO_TEXTURE  vs_main_noTexture( VS_INPUT_NO_TEXTURE  Input )
{
   VS_OUTPUT_NO_TEXTURE  Output;

   //Proyectar posicion
   Output.Position = mul( Input.Position, matWorldViewProj );
   
   //Color
   Output.Color = Input.Color;
   
   //Proyectar normal
   Output.Normal = mul( Input.Normal, matWorld );
      
   return Output;
}

//Input del Pixel Shader
struct PS_INPUT_NO_TEXTURE 
{
   float4 Color : COLOR;
};

//Pixel Shader
float4 ps_main_noTexture( PS_INPUT_NO_TEXTURE Input ) : COLOR0
{     
	float4 c = Input.Color;
	c.a = alphaValue;
	return c;
}

/*
* Technique NoTexture
*/
technique NoTextureTechnique
{
   pass Pass_0
   {
	  VertexShader = compile vs_2_0 vs_main_noTexture();
	  PixelShader = compile ps_2_0 ps_main_noTexture();
   }

}



//******************************************************************************//


//Input del Vertex Shader
struct VS_INPUT_ONLY_COLOR
{
   float4 Position : POSITION0;
   float4 Color : COLOR;
};

//Output del Vertex Shader
struct VS_OUTPUT_ONLY_COLOR
{
   float4 Position : POSITION0;
   float4 Color : COLOR;
   
};

//Vertex Shader
VS_OUTPUT_ONLY_COLOR vs_main_onlyColor( VS_INPUT_ONLY_COLOR  Input )
{
   VS_OUTPUT_ONLY_COLOR Output;

   //Proyectar posicion
   Output.Position = mul( Input.Position, matWorldViewProj );
   
   //Color
   Output.Color = Input.Color;
      
   return Output;
}

//Input del Pixel Shader
struct PS_INPUT_ONLY_COLOR
{
   float4 Color : COLOR;
};

//Pixel Shader
float4 ps_main_onlyColor( PS_INPUT_ONLY_COLOR Input ) : COLOR0
{     
	float4 c = Input.Color;
	c.a = alphaValue;
	return c;
}

/*
* Technique OnlyColor
*/
technique OnlyColorTechnique
{
   pass Pass_0
   {
	  VertexShader = compile vs_2_0 vs_main_onlyColor();
	  PixelShader = compile ps_2_0 ps_main_onlyColor();
   }

}
