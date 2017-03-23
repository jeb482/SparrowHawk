/**
*  Copyright 2014-2015 Program of Computer Graphics, Cornell University
*     580 Rhodes Hall
*     Cornell University
*     Ithaca NY 14853
*  Web: http://www.graphics.cornell.edu/
*
*  Not for commercial use. Do not redistribute without permission.
*
*  @file    ShaderSourceCode.h
*  @brief Shader source code for Pixel counter
*
*  This file provides based source code for the vertex and fragment shaders for the pixel counter.
*
*  Note: Normally this would call vert and frag programs, however Rhino is extremely specific about
*  the location of these files, and all attempts to supply the exact path failed. Therefore,
*  we put the source code of the shaders as an input string here so they could be found.
*
*  @author  Joseph T. Kider Jr. (kiderj@graphics.cornell.edu)
*  @date    10/01/2014
*/
namespace SparrowHawk.Material
{

public static class ShaderSource { 
// Read the Vertex Shader code from the file
public static string SingleColorVertShader
= @"#version 330 core
uniform mat4 modelTransform;
uniform mat4 viewProjTransform;
in vec3 position;
void main()
{
    gl_Position = viewProjTransform*(modelTransform*vec4(position, 1));
}";

 // Read the Fragment Shader code from the file
 public static string SingleColorFragShader
= @"#version 330 core
uniform vec4 color;
out vec4 out_color;
void main() 
{
    out_color = color;
}";

// Read the Vertex Shader code from the file
 public static string TextureVertShader  
= @"#version 330 core
uniform mat4 viewProjTransform;
uniform mat4 modelTransform;
in vec3 position;
in vec2 uvs;
smooth out vec2 fuvs;
void main()
{
	fuvs = uvs;
	gl_Position = viewProjTransform * (modelTransform * vec4(position, 1.0));
}";

// Read the Fragment Shader code from the file
public static string TextureFragShader
= @"#version 330 core
uniform sampler2D tex;
smooth in vec2 fuvs;
out vec4 out_color;
void main()
{
	out_color = vec4(texture2D(tex, fuvs).xyz,1);
}";

// Also colors appropriate sector.
public static string RadialMenuFragShader
= @"#version 330 core
uniform sampler2D tex;
uniform float theta_min;
uniform float theta_max;
smooth in vec2 fuvs;
out vec4 out_color;
void main()
{
    bool invert = false;
    float theta = atan(.5 - fuvs.y, fuvs.x - .5);
    if (theta < 0) {
        theta = theta + 6.283;    
    }
    if (theta > theta_min && theta < theta_max)
        invert = true;
	out_color = vec4(texture2D(tex, fuvs).xyz,1);    
    if (invert)
        out_color.xyz = 1 - out_color.xyz;
    
    
}";


//out_color = texture2D(texture, fuvs) * color;\
// Read the Vertex Shader code from the file
public static string MeshVertexShader
= @"#version 330 core
uniform mat4 modelViewProj;
uniform mat4 modelTransform;
uniform mat4 modelIT;
in vec3 position;
in vec3 normal;
in vec4 colors;
smooth out vec4 color;
smooth out vec3 fnormal;
void main()
{
	color = colors;
	fnormal = (modelIT*vec4(normal,1)).xyz;
	gl_Position = modelViewProj * (modelTransform*vec4(position, 1.0));
}";

// Read the Fragment Shader code from the file
public static string MeshFragShader
= @"#version 330 core
smooth in vec4 color;
smooth in vec3 fnormal;
uniform vec4 uColor;
out vec4 out_color;
void main()
{
	vec3 ambient = vec3(.2,.2,.2)*color.xyz;
	vec3 diffuse = max(dot(normalize(vec3(1,1,0)), fnormal),0)*color.xyz;
	out_color = vec4(ambient+diffuse,1);
} ";

public static string RGBNormalVertShader
= @"#version 330 core
uniform mat4 viewProjTransform;
uniform mat4 modelTransform;
uniform mat4 modelInvTrans;
in vec3 position;
in vec3 normal;
smooth out vec3 fnormal;
void main()
{
	fnormal = (modelInvTrans*vec4(normal,0)).xyz;
	gl_Position = viewProjTransform * (modelTransform*vec4(position, 1.0));
}";

public static string RGBNormalFragShader
= @"#version 330 core
uniform float alpha;
smooth in vec3 fnormal;
out vec4 out_color;
void main()
{
	out_color = vec4((fnormal + 1)/2, alpha);
}";

// Read the Fragment Shader code from the file
public static string FragmentShaderCode_Render2
= @"#version 330 core
smooth in vec4 color;
uniform vec4 uColor;
uniform bool fill;
out vec4 out_color;
void main()
{
	if(fill)
	   out_color = color;
	else
		out_color = vec4(1.0f, 1.0f, 0.0f, 1.0f);
}";


// Read the Fragment Shader code from the file
public static string FragmentShaderCode_Line_Render
= @"#version 330 core
smooth in vec4 color;
uniform vec4 uColor;
out vec4 out_color;
void main()
{
	out_color = vec4(1.0f,0.0f,0.0f,1.0f);
}";

public static string NaiveVertexShader
= @"#version 330 core
in vec3 position;
void main()
{
    gl_Position = vec4(position, 1);
}";

public static string NaiveFragShader
= @"#version 330 core
out vec4 out_color;
void main() 
{
    out_color = vec4(1.0f,0.0f,0.0f,1.0f);
}
"; 
}
}

