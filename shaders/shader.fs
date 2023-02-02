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
vec3 boxLightColor = vec3( 1.0);

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

phongdata phongSphere = phongdata(vec3(1.0, 0.0, 0.0), vec3(1.0, 0.0, 0.0), vec3(1.0, 0.0, 0.0), 256.0);
phongdata phongBox1 = phongdata(vec3(0.0, 1.0, 0.0), vec3(0.0, 1.0, 0.0), vec3(0.0, 1.0, 0.0), 32.0);
phongdata phongBox2 = phongdata(vec3(0.0, 0.0, 1.0), vec3(0.0, 0.0, 1.0), vec3(0.0, 0.0, 1.0), 32.0);
phongdata phongLightBox = phongdata(lightColor, lightColor, lightColor,  256.0);

// START OF SDFs
float sdfSphere(in vec3 point, float r)
{
    return length(point) - r;
}

float sdfBox(in vec3 point, in vec3 b){
  vec3 q = abs(point) - b;
  return length(max(q,0.0)) + min(max(q.x,max(q.y,q.z)),0.0);
}

phong opUnion( phong d1, phong d2 ) {
    if(d1.sdf < d2.sdf) return d1;
    else return d2;
}

phong opSmoothUnion( phong d1, phong d2, float k ) {
    float h = clamp( 0.5 + 0.5*(d2.sdf-d1.sdf)/k, 0.0, 1.0 );
    float m = (d1.sdf + d2.sdf) / 2.0;

    phongdata data;
    data.ambient = mix(d2.data.ambient, d1.data.ambient, h) - vec3(k*h*(1.0-h));
    data.diffuse = mix(d2.data.diffuse, d1.data.diffuse, h) - vec3(k*h*(1.0-h));
    data.specular = mix(d2.data.specular, d1.data.specular, h) - vec3(k*h*(1.0-h));
    data.shininess = mix( d2.data.shininess, d1.data.shininess, h ) - k*h*(1.0-h);
    phong ret = phong(data, mix( d2.sdf, d1.sdf, h ) - k*h*(1.0-h));

    return ret;
}

phong opIntersection( phong d1, phong d2 ) {
    if(d1.sdf < d2.sdf) return d2;
    else return d1;
}

// from d2 substract d1
phong opDifference( phong d1, phong d2 ) {
    if(-d1.sdf > d2.sdf) {
        d1.sdf *= -1;
        return d1;
    }
    else return d2;
}

vec3 opInfiniteRepeat(vec3 pos, vec3 replength){
    return mod(pos+replength*0.5, replength) - replength*0.5;
}
//works well if bounding box of object < replength
//reps is the number of times the pattern gets repeated on each axis in each direction (e.g. 1 -> 1 up and 1 down)
vec3 opFiniteRepeat(vec3 pos, vec3 start, vec3 reps, vec3 replength){
    vec3 d = round((pos - start) / replength);
    vec3 r = clamp(d, -reps, reps);
    return start + r * replength;
}

// Repeat exactly reps times across each axis (e.g. 1 means ONLY one repeatition across the given axis, NOT 1 up and 1 down)
vec3 opFiniteRepeat2(in vec3 pos, in vec3 start, in vec3 reps, in vec3 replength){    
    vec3 m = mod(reps, 2); // 0 if even, 1 if odd
    vec3 m1 = vec3(1.0) - m; //1 if even , 0 if odd
    
    vec3 s = vec3(start+0.5*m1*replength);

    vec3 d = round((pos-s) / replength);

    vec3 r1 = (reps-m)*0.5;
    vec3 r = clamp(d, -r1, r1 - m1 ); //m - vec3(1.0) should be the same;

    return pos-s-r*replength;
}

vec3 s = vec3(0.0);
vec3 r = vec3(3.0, 5.0, 2.0);
vec3 rl = vec3(4.0, 2.0, 4.0);

phong sdfScene(in vec3 p){
    return  opUnion(
            opUnion(
                phong(phongSphere, sdfSphere(opInfiniteRepeat(p, 2*rl), 0.4)),
                phong(phongBox1, 
                    sdfBox(opFiniteRepeat2(p, s, r, rl), 
                            vec3(0.5, 0.1, 0.5) 
                        )
                    )
            ),
            opDifference(
                phong(phongSphere, sdfSphere(p-vec3(0.0, 5.0, 0.0), 0.25 + 0.25* (1+sin(u_time)))),
                phong(phongBox2, sdfBox(p-vec3(0.0, 5.0, 0.0), vec3(0.5)))
            )
        );
}
// END OF SDFs

vec3 sceneNormal(in vec3 p){
    vec3 smallstep = vec3(0.00001, 0.0, 0.0);
    float sdf = sdfScene(p).sdf;

    float gradient_x = sdfScene(p.xyz + smallstep.xyy).sdf - sdf;
    float gradient_y = sdfScene(p.xyz + smallstep.yxy).sdf - sdf;
    float gradient_z = sdfScene(p.xyz + smallstep.yyx).sdf - sdf;

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
            // ambient
            vec3 ambient = lightColor*dist.data.ambient;

            // diffuse
            lightDir = normalize(lightPos - pos);
            float diff = max(dot(sceneNormal(pos), lightDir), 0.0);
            vec3 diffuse = diff * dist.data.diffuse;

            // specular
            vec3 viewDir = normalize(u_camorigin - pos);
            vec3 reflectDir = reflect(-lightDir, sceneNormal(pos));

            float spec = pow(max(dot(viewDir, reflectDir), 0.0), dist.data.shininess);
            vec3 specular = lightColor * spec * dist.data.specular;

            return (vec3(0.1) * ambient + vec3(0.45) * diffuse +vec3(0.45) *  specular);
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
    FragColor = vec4(shaded_color, 1.0);
}
