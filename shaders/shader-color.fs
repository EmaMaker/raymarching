#version 330 core

uniform vec2 u_resolution;
uniform float u_time;
uniform float u_deltatime;
uniform vec3 u_camorigin;
uniform vec3 u_camdir;
uniform vec3 u_camup;
out vec4 FragColor;


// START OF LIGHTNING
float ambientStrength = 0.20;
float specularStrength = 0.5;
vec3 lightColor = vec3(1.0);
vec3 ambient = lightColor * ambientStrength;
vec3 lightPos = vec3(5.0);
// END OF LIGHTNING


struct Phong{
    vec3 ambient;
    vec3 diffuse;
    vec3 specular;
    float shininess;

    float sdf;
} phong;

// START OF SDFs
phong sdfSphere(in phong color, in vec3 point, in vec3 center, float r)
{
    color.sdf = length(point - center) - r;
    return color;
}

phong sdfBox(in phong color, in vec3 point, in vec3 center, in vec3 b){
  vec3 q = abs(point - center) - b;
  phong.sdf =   length(max(q,0.0)) + min(max(q.x,max(q.y,q.z)),0.0));
  return color;
}

phong opUnion( phong d1, phong d2 ) {
    if(d1.sdf < d2.sdf) return d1;
    else return d2;
}

phong phongSphere = phong(vec3(1.0, 0.0, 0.0), vec3(1.0, 0.0, 0.0), ambient, 32.0, 0.0);
phong phongBox1 = phong(vec3(0.0, 1.0, 0.0), vec3(1.0, 0.0, 0.0), ambient, 32.0, 0.0);
phong phongBox2 = phong(vec3(0.0, 1.0, 0.0), vec3(1.0, 0.0, 0.0), ambient, 32.0, 0.0);

phong sdfScene(in vec3 p){
    return opUnion(
        opUnion(
            sdfSphere(phongSphere, p, vec3(0.0), 1), 
            sdfBox(phongBox1, p, vec3(2.0, 0.0, 0.0), vec3(0.8))
        ), 
        sdfBox(phongBox2, p, vec3(-5.0, 0.0, 0.0), vec3(2.0, 1.0, 0.5))
    );
}
// END OF SDFs

vec3 sceneNormal(in vec3 p){
    vec3 smallstep = vec3(0.00001, 0.0, 0.0);
    float dist = sdfScene(p).sdf;

    float gradient_x = sdfScene(p.xyz + smallstep.xyy).sdf - dist;
    float gradient_y = sdfScene(p.xyz + smallstep.yxy).sdf - dist;
    float gradient_z = sdfScene(p.xyz + smallstep.yyx).sdf - dist;

    return normalize(vec3(gradient_x, gradient_y, gradient_z));
}


vec3 ray_march(in vec3 ro, in vec3 rd)
{
    float total_dist = 0.0;
    vec3 pos;

    for(int i = 0; i < 100; i++){
        // incrementally travel following the ray
        pos = ro + rd * total_dist;

        // calculate distance from scene
        phong dist = sdfScene(pos);
        
        // if close to the scene, color the pixel as needed 
        if(dist.sdf <= 0.001){
            // Basic Phong illumination
            // diffuse
            lightDir = normalize(lightPos - pos);
            float diff = max(dot(sceneNormal(pos), lightDir), 0.0);
            vec3 diffuse = diff * dist.diffuse;

            // specular
            vec3 viewDir = normalize(u_camorigin - pos);
            vec3 reflectDir = reflect(-lightDir, sceneNormal(pos));

            float spec = pow(max(dot(viewDir, reflectDir), 0.0), 32);
            vec3 specular = dist.shininess * spec * dist.specular;

            return (dist.ambient + diffuse + specular) * dist.ambient;
        }

        // increment distance by the highest possible value (sphere marching)
        total_dist += dist.sdf;

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
    gl_FragColor = vec4(shaded_color, 1.0);
}
