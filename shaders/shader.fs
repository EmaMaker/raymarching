#version 330 core

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

// START OF SDFs
float sdfSphere(in vec3 point, in vec3 center, float r)
{
    return length(point - center) - r;
}

float sdfBox(in vec3 point, in vec3 center, in vec3 b){
  vec3 q = abs(point - center) - b;
  return length(max(q,0.0)) + min(max(q.x,max(q.y,q.z)),0.0);
}

vec4 opUnion( vec4 d1, vec4 d2 ) {
    if(d1.w < d2.w) return d1;
    else return d2;
}

vec4 sdfScene(in vec3 p){
    return opUnion( 
        opUnion( 
            vec4(sphere1Color, sdfSphere(p, vec3(0.0), 1)), 
            vec4(box1Color, sdfBox(p, vec3(2.0, 0.0, 0.0), vec3(0.8)))
        ),
        vec4(box2Color, sdfBox(p, vec3(-5.0, 0.0, 0.0), vec3(2.0, 1.0, 0.5)))
    );
}
// END OF SDFs

vec3 sceneNormal(in vec3 p){
    vec3 smallstep = vec3(0.00001, 0.0, 0.0);
    float sdf = sdfScene(p).w;

    float gradient_x = sdfScene(p.xyz + smallstep.xyy).w - sdf;
    float gradient_y = sdfScene(p.xyz + smallstep.yxy).w - sdf;
    float gradient_z = sdfScene(p.xyz + smallstep.yyx).w - sdf;

    return normalize(vec3(gradient_x, gradient_y, gradient_z));
}

// START OF LIGHTNING
float ambientStrength = 0.15;
float specularStrength = 0.5;
vec3 lightColor = vec3(1.0);
vec3 ambient = lightColor * ambientStrength;
vec3 lightPos = vec3(5.0);
vec3 lightDir;
// END OF LIGHTNING

vec3 ray_march(in vec3 ro, in vec3 rd)
{
    float total_dist = 0.0;
    vec3 pos;

    for(int i = 0; i < 100; i++){
        // incrementally travel following the ray
        pos = ro + rd * total_dist;

        // calculate distance from scene
        vec4 dist = sdfScene(pos);
        
        // if close to the scene, color the pixel as needed 
        if(dist.w <= 0.001){
            // Basic Phong illumination
            // diffuse
            lightDir = normalize(lightPos - pos);
            float diff = max(dot(sceneNormal(pos), lightDir), 0.0);
            vec3 diffuse = diff * lightColor;

            // specular
            vec3 viewDir = normalize(u_camorigin - pos);
            vec3 reflectDir = reflect(-lightDir, sceneNormal(pos));

            float spec = pow(max(dot(viewDir, reflectDir), 0.0), 32);
            vec3 specular = specularStrength * spec * lightColor;

            return (ambient + diffuse + specular) * dist.xyz;
        }

        // increment distance by the highest possible value (sphere marching)
        total_dist += dist.w;

        // if too far out, bail out
        if(total_dist > 1000) break;

    }

    // no hit, return background color
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
    FragColor = vec4(shaded_color, 1.0);
}
