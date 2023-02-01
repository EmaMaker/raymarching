#version 330 core

in vec2 fragcoord;

uniform vec2 u_resolution;
uniform float u_time;
uniform float u_deltatime;
uniform vec3 u_camorigin;
uniform vec3 u_camdir;
uniform vec3 u_camup;

out vec4 FragColor;

vec3 sphere1Color = vec3(1.0, 0.0, 0.0);
vec3 box1Color = vec3(0.0, 1.0, 0.0);
vec3 box2Color = vec3(0.0, 0.0, 1.0);
vec3 boxLightColor = vec3( 1.0);

// START OF SDFs
float sdfSphere(in vec3 point, in vec3 center, float r)
{
    return length(point - center) - r;
}

float sdfBox(in vec3 point, in vec3 b){
  vec3 q = abs(point) - b;
  return length(max(q,0.0)) + min(max(q.x,max(q.y,q.z)),0.0);
}

float opUnion( float d1, float d2 ) { return min(d1,d2); }

vec3 opFiniteRepeat(in vec3 pos, in vec3 start, in vec3 reps, in vec3 replength){    
    vec3 m = mod(reps, 2); // 0 if even, 1 if odd
    vec3 m1 = vec3(1.0) - m; //1 if even , 0 if odd
    
    vec3 s = vec3(start+0.5*m1*replength);

    vec3 d = round((pos-s) / replength);

    vec3 r1 = (reps-m)*0.5;
    vec3 r = clamp(d, -r1, r1 - m1 ); //m - vec3(1.0) should be the same;

    return pos-s-r*replength;
}

float sdfScene(in vec3 p){
    return opUnion( sdfSphere(p, vec3(0.0), 1), sdfBox(opFiniteRepeat(p, vec3(0.0), vec3(2.0, 3.0, 5.0), vec3(3.0+1.0+sin(u_time))), vec3(0.8)));
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
