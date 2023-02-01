#version 330 core

in vec2 fragcoord;

uniform vec2 u_resolution;
uniform float u_time;
uniform float u_deltatime;
uniform vec3 u_camorigin;
uniform vec3 u_camdir;
uniform vec3 u_camup;

<<<<<<< HEAD
out vec4 FragColor;

<<<<<<< HEAD
vec3 sphere1Color = vec3(1.0, 0.0, 0.0);
vec3 box1Color = vec3(0.0, 1.0, 0.0);
vec3 box2Color = vec3(0.0, 0.0, 1.0);
vec3 boxLightColor = vec3( 1.0);

<<<<<<< HEAD
// START OF LIGHTNING
vec3 lightColor = vec3(1.0);
vec3 lightPos = vec3(5.0);
vec3 lightDir;
// END OF LIGHTNING

struct phongdata{
    vec3 ambient;
    vec3 diffuse;
    vec3 specular;
    float shininess;
};

struct phong{
    phongdata data;
    float sdf;
};

// START OF SDFs
float sdfSphere(in vec3 point, in vec3 center, float r)
{
    return length(point - center) - r;
}

float sdfBox(in vec3 point, in vec3 center, in vec3 b){
  vec3 q = abs(point - center) - b;
  return length(max(q,0.0)) + min(max(q.x,max(q.y,q.z)),0.0);
}

float opUnion( float d1, float d2 ) { return min(d1,d2); }

float sdfScene(in vec3 p){
    return opUnion(opUnion( sdfSphere(p, vec3(0.0), 1), sdfBox(p, vec3(2.0, 0.0, 0.0), vec3(0.8))), sdfBox(p, vec3(-5.0, 0.0, 0.0), vec3(2.0, 1.0, 0.5)));
}


vec3 sceneNormal(in vec3 p){
    vec3 smallstep = vec3(0.0001, 0.0, 0.0);

    float gradient_x = sdfScene(p.xyz + smallstep.xyy) - sdfScene(p.xyz - smallstep.xyy);
    float gradient_y = sdfScene(p.xyz + smallstep.yxy) - sdfScene(p.xyz - smallstep.yxy);
    float gradient_z = sdfScene(p.xyz + smallstep.yyx) - sdfScene(p.xyz - smallstep.yyx);

    return normalize(vec3(gradient_x, gradient_y, gradient_z));
}

vec3 ray_march(in vec3 ro, in vec3 rd)
{
    float total_dist = 0.0;
    vec3 pos;

    for(int i = 0; i < 35; i++){
        pos = ro + rd * total_dist;

        float dist = sdfScene(pos);

        if(dist <= 0.001){
            return (sceneNormal(pos) * 0.5 + 0.5);
        }
        total_dist += dist;
        if(total_dist > 4000) break;

    }
    return vec3(0.0);
}

void main()
{
    // ray direction on canvas, normalized
    vec2 uv = (gl_FragCoord.xy/u_resolution) * 2 - 1;
    uv.x *= u_resolution.x/u_resolution.y; //account for aspect ratio
    
    // https://github.com/electricsquare/raymarching-workshop
    vec3 camright = normalize(cross(u_camdir, u_camup));
    float fPersp=tan(radians(70.0));

    // recompute the up vector, in case the camera diverges a lot from u_camup. This avoids weird distortions when looking up or down
    vec3 camup = normalize(cross(camright, u_camdir));

    vec3 rd = normalize(uv.x * camright + uv.y * camup + u_camdir * fPersp);

    vec3 shaded_color = ray_march(u_camorigin, rd);
    gl_FragColor = vec4(shaded_color, 1.0);
}
