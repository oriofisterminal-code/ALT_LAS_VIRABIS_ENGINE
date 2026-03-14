# LYGIA Shader Library Adaptation

LYGIA kütüphanesinden ALT_LAS_ENGINE için adaptasyon edilecek fonksiyonlar.

## Kaynak
https://github.com/patriciogonzalezvivo/lygia

## Kullanım

LYGIA fonksiyonları `#include` direktifi ile kullanılabilir:

```glsl
#include "lygia/math/lerp.glsl"
#include "lygia/color/palette/spectral.glsl"
```

## Adaptasyon Edilecek Fonksiyonlar

### 1. Matematiksel Fonksiyonlar (`lygia/math/`)

#### Lerp (Linear Interpolation)
```glsl
// lygia/math/lerp.glsl
float lerp(float a, float b, float t) {
    return a + t * (b - a);
}

vec3 lerp(vec3 a, vec3 b, float t) {
    return a + t * (b - a);
}
```

#### Smoothstep Alternatifleri
```glsl
// lygia/math/smoothstep.glsl
float smoothstep(float edge0, float edge1, float x) {
    float t = clamp((x - edge0) / (edge1 - edge0), 0.0, 1.0);
    return t * t * (3.0 - 2.0 * t);
}
```

#### Rastgele Sayı Üretimi
```glsl
// lygia/math/random.glsl
float random(float n) {
    return fract(sin(n) * 43758.5453123);
}

float random(vec2 st) {
    return fract(sin(dot(st.xy, vec2(12.9898, 78.233))) * 43758.5453123);
}

vec2 random2(vec2 st) {
    st = vec2(dot(st, vec2(127.1, 311.7)),
              dot(st, vec2(269.5, 183.3)));
    return fract(sin(st) * 43758.5453123);
}
```

#### Noise Fonksiyonları
```glsl
// lygia/math/noise.glsl
float noise(vec2 st) {
    vec2 i = floor(st);
    vec2 f = fract(st);
    
    // Four corners in 2D of a tile
    float a = random(i);
    float b = random(i + vec2(1.0, 0.0));
    float c = random(i + vec2(0.0, 1.0));
    float d = random(i + vec2(1.0, 1.0));
    
    // Smooth interpolation
    vec2 u = f * f * (3.0 - 2.0 * f);
    
    return mix(a, b, u.x) +
           (c - a) * u.y * (1.0 - u.x) +
           (d - b) * u.x * u.y;
}

// Fractal Brownian Motion
float fbm(vec2 st) {
    float value = 0.0;
    float amplitude = 0.5;
    float frequency = 1.0;
    
    for (int i = 0; i < 6; i++) {
        value += amplitude * noise(st * frequency);
        frequency *= 2.0;
        amplitude *= 0.5;
    }
    
    return value;
}
```

---

### 2. Renk Fonksiyonları (`lygia/color/`)

#### Renk Uzayı Dönüşümleri
```glsl
// lygia/color/space/rgb2hsv.glsl
vec3 rgb2hsv(vec3 c) {
    vec4 K = vec4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    vec4 p = mix(vec4(c.bg, K.wz), vec4(c.gb, K.xy), step(c.b, c.g));
    vec4 q = mix(vec4(p.xyw, c.r), vec4(c.r, p.yzx), step(p.x, c.r));
    
    float d = q.x - min(q.w, q.y);
    float e = 1.0e-10;
    
    return vec3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
}

// lygia/color/space/hsv2rgb.glsl
vec3 hsv2rgb(vec3 c) {
    vec4 K = vec4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    vec3 p = abs(fract(c.xxx + K.xyz) * 6.0 - K.www);
    return c.z * mix(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
}
```

#### Luminance Hesaplama
```glsl
// lygia/color/luminance.glsl
float luminance(vec3 rgb) {
    return dot(rgb, vec3(0.2126, 0.7152, 0.0722));
}
```

#### Renk Paleti
```glsl
// lygia/color/palette/spectral.glsl
vec3 spectral(float t) {
    vec3 a = vec3(0.5, 0.5, 0.5);
    vec3 b = vec3(0.5, 0.5, 0.5);
    vec3 c = vec3(1.0, 1.0, 1.0);
    vec3 d = vec3(0.0, 0.33, 0.67);
    
    return a + b * cos(6.28318 * (c * t + d));
}
```

---

### 3. Işıklandırma Fonksiyonları (`lygia/lighting/`)

#### Phong Işıklandırma
```glsl
// lygia/lighting/phong.glsl
vec3 phong(vec3 normal, vec3 lightDir, vec3 viewDir, 
           vec3 ambient, vec3 diffuse, vec3 specular, 
           float shininess) {
    
    // Ambient
    vec3 ambientColor = ambient;
    
    // Diffuse
    float diff = max(dot(normal, lightDir), 0.0);
    vec3 diffuseColor = diff * diffuse;
    
    // Specular
    vec3 reflectDir = reflect(-lightDir, normal);
    float spec = pow(max(dot(viewDir, reflectDir), 0.0), shininess);
    vec3 specularColor = spec * specular;
    
    return ambientColor + diffuseColor + specularColor;
}
```

#### Basit PBR
```glsl
// lygia/lighting/pbr.glsl
vec3 pbr(vec3 normal, vec3 lightDir, vec3 viewDir,
         vec3 albedo, float metallic, float roughness) {
    
    vec3 N = normalize(normal);
    vec3 L = normalize(lightDir);
    vec3 V = normalize(viewDir);
    vec3 H = normalize(L + V);
    
    float NdotL = max(dot(N, L), 0.0);
    float NdotV = max(dot(N, V), 0.0);
    float NdotH = max(dot(N, H), 0.0);
    float VdotH = max(dot(V, H), 0.0);
    
    // Fresnel (Schlick approximation)
    vec3 F0 = mix(vec3(0.04), albedo, metallic);
    vec3 F = F0 + (1.0 - F0) * pow(1.0 - VdotH, 5.0);
    
    // Distribution (GGX)
    float alpha = roughness * roughness;
    float alpha2 = alpha * alpha;
    float denom = NdotH * NdotH * (alpha2 - 1.0) + 1.0;
    float D = alpha2 / (3.14159 * denom * denom);
    
    // Geometry (Smith)
    float k = (roughness + 1.0) * (roughness + 1.0) / 8.0;
    float G1L = NdotL / (NdotL * (1.0 - k) + k);
    float G1V = NdotV / (NdotV * (1.0 - k) + k);
    float G = G1L * G1V;
    
    // Specular
    vec3 specular = (D * F * G) / (4.0 * NdotV * NdotL + 0.001);
    
    // Diffuse
    vec3 diffuse = (1.0 - F) * (1.0 - metallic) * albedo / 3.14159;
    
    return (diffuse + specular) * NdotL;
}
```

---

### 4. Efekt Fonksiyonları (`lygia/effect/`)

#### Glow Efekti
```glsl
// lygia/effect/glow.glsl
vec3 glow(vec3 color, float intensity, float radius) {
    return color * intensity * (1.0 / (1.0 + radius * radius));
}
```

#### Vignette
```glsl
// lygia/effect/vignette.glsl
float vignette(vec2 uv, float radius, float softness) {
    vec2 center = uv - 0.5;
    float dist = length(center);
    return smoothstep(radius, radius - softness, dist);
}
```

#### Chromatic Aberration
```glsl
// lygia/effect/chroma.glsl
vec3 chromaticAberration(sampler2D tex, vec2 uv, float amount) {
    vec2 offset = vec2(amount, 0.0);
    float r = texture(tex, uv + offset).r;
    float g = texture(tex, uv).g;
    float b = texture(tex, uv - offset).b;
    return vec3(r, g, b);
}
```

---

## ALT_LAS_ENGINE Entegrasyonu

### Klasör Yapısı
```
Source/Shaders/
├── base.vert
├── base.frag
├── light.frag
├── glow.frag
├── water.frag
└── includes/
    ├── math/
    │   ├── lerp.glsl
    │   ├── noise.glsl
    │   └── random.glsl
    ├── color/
    │   ├── rgb2hsv.glsl
    │   └── palette.glsl
    └── lighting/
        ├── phong.glsl
        └── pbr.glsl
```

### Kullanım Örneği
```glsl
// glow.frag
#version 330 core

#include "includes/math/lerp.glsl"
#include "includes/color/palette.glsl"
#include "includes/effect/glow.glsl"

uniform float u_time;
uniform vec2 u_resolution;

void main() {
    vec2 uv = gl_FragCoord.xy / u_resolution;
    vec3 color = spectral(uv.x + u_time * 0.1);
    color = glow(color, 1.5, 0.5);
    gl_FragColor = vec4(color, 1.0);
}
```
