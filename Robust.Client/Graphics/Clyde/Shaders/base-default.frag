#version 330 core

out vec4 FragColor;

in vec2 UV;
in vec2 Pos;

uniform sampler2D TEXTURE;
uniform sampler2D lightMap;
uniform vec4 modulate;

layout (std140) uniform uniformConstants
{
    vec2 SCREEN_PIXEL_SIZE;
    float TIME;
};

uniform vec2 TEXTURE_PIXEL_SIZE;

#line 1000
// [SHADER_HEADER_CODE]

void main()
{
    vec4 FRAGCOORD = gl_FragCoord;

    vec4 COLOR;

    #line 10000
    // [SHADER_CODE]

    vec3 lightSample = texture(lightMap, Pos).rgb;

    FragColor = COLOR * modulate * vec4(lightSample, 1);
}
