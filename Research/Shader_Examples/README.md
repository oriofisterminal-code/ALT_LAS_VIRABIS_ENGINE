# Godot Shaders Örnekleri

Godot Shaders'dan ilham alınacak glow, water ve light shader örnekleri.

## Kaynak
https://godotshaders.com

---

## 1. Glow Shader

### Godot Shader (Referans)
```glsl
// Godot Glow Shader
shader_type canvas_item;

uniform float glow_intensity : hint_range(0.0, 5.0) = 1.5;
uniform vec4 glow_color : hint_color = vec4(1.0, 0.8, 0.2, 1.0);
uniform float pulse_speed : hint_range(0.0, 10.0) = 2.0;

void fragment() {
    vec4 tex_color = texture(TEXTURE, UV);
    
    // Pulse efekti
    float pulse = sin(TIME * pulse_speed) * 0.5 + 0.5;
    float intensity = glow_intensity * (0.7 + 0.3 * pulse);
    
    // Glow hesaplama
    vec3 glow = glow_color.rgb * intensity;
    
    // Alpha'ya göre glow
    float alpha = tex_color.a;
    vec3 final_color = mix(tex_color.rgb, tex_color.rgb + glow, alpha);
    
    COLOR = vec4(final_color, alpha);
}
```

### ALT_LAS_ENGINE Adaptasyonu
```glsl
// glow.frag - ALT_LAS_ENGINE
#version 330 core

uniform sampler2D u_texture;
uniform float u_time;
uniform float u_intensity;
uniform vec3 u_glowColor;
uniform float u_pulseSpeed;

in vec2 v_uv;
out vec4 fragColor;

void main() {
    vec4 texColor = texture(u_texture, v_uv);
    
    // Pulse efekti
    float pulse = sin(u_time * u_pulseSpeed) * 0.5 + 0.5;
    float intensity = u_intensity * (0.7 + 0.3 * pulse);
    
    // Glow hesaplama
    vec3 glow = u_glowColor * intensity;
    vec3 finalColor = texColor.rgb + glow * texColor.a;
    
    fragColor = vec4(finalColor, texColor.a);
}
```

### Python Wrapper
```python
# Source/Effects/glow.py
from dataclasses import dataclass

@dataclass
class GlowConfig:
    intensity: float = 1.5
    color: tuple = (1.0, 0.8, 0.2)
    pulse_speed: float = 2.0

class GlowEffect:
    def __init__(self, shader_manager, config: GlowConfig = None):
        self.config = config or GlowConfig()
        self.shader = shader_manager.load("glow")
        
    def apply(self, target, time):
        self.shader.set_uniform("u_intensity", self.config.intensity)
        self.shader.set_uniform("u_glowColor", self.config.color)
        self.shader.set_uniform("u_pulseSpeed", self.config.pulse_speed)
        self.shader.set_uniform("u_time", time)
        
        self.shader.render(target)
```

---

## 2. Water Shader

### Godot Shader (Referans)
```glsl
// Godot Water Shader
shader_type canvas_item;

uniform float wave_speed : hint_range(0.0, 5.0) = 1.0;
uniform float wave_height : hint_range(0.0, 0.5) = 0.05;
uniform vec2 wave_direction = vec2(1.0, 0.5);
uniform vec4 water_color : hint_color = vec4(0.1, 0.4, 0.8, 0.8);
uniform sampler2D noise_texture;

void fragment() {
    vec2 uv = UV;
    
    // Wave distortion
    vec2 wave_uv = uv + TIME * wave_speed * wave_direction * 0.1;
    float noise1 = texture(noise_texture, wave_uv).r;
    float noise2 = texture(noise_texture, wave_uv * 2.0 + 0.5).r;
    
    // UV distorsiyonu
    vec2 distorted_uv = uv;
    distorted_uv.x += sin(uv.y * 20.0 + TIME * wave_speed + noise1 * 3.0) * wave_height;
    distorted_uv.y += cos(uv.x * 20.0 + TIME * wave_speed + noise2 * 3.0) * wave_height;
    
    // Renk
    vec4 tex_color = texture(TEXTURE, distorted_uv);
    
    // Dalga rengi
    float wave = sin(uv.x * 30.0 + TIME * wave_speed) * 0.5 + 0.5;
    wave *= sin(uv.y * 30.0 + TIME * wave_speed * 1.3) * 0.5 + 0.5;
    
    vec3 color = mix(water_color.rgb, water_color.rgb * 1.3, wave);
    color = mix(color, tex_color.rgb, tex_color.a);
    
    COLOR = vec4(color, water_color.a);
}
```

### ALT_LAS_ENGINE Adaptasyonu
```glsl
// water.frag - ALT_LAS_ENGINE
#version 330 core

uniform sampler2D u_texture;
uniform sampler2D u_noiseTexture;
uniform float u_time;
uniform float u_waveSpeed;
uniform float u_waveHeight;
uniform vec2 u_waveDirection;
uniform vec4 u_waterColor;

in vec2 v_uv;
out vec4 fragColor;

void main() {
    vec2 uv = v_uv;
    
    // Wave distortion
    vec2 wave_uv = uv + u_time * u_waveSpeed * u_waveDirection * 0.1;
    float noise1 = texture(u_noiseTexture, wave_uv).r;
    float noise2 = texture(u_noiseTexture, wave_uv * 2.0 + 0.5).r;
    
    // UV distorsiyonu
    vec2 distorted_uv = uv;
    distorted_uv.x += sin(uv.y * 20.0 + u_time * u_waveSpeed + noise1 * 3.0) * u_waveHeight;
    distorted_uv.y += cos(uv.x * 20.0 + u_time * u_waveSpeed + noise2 * 3.0) * u_waveHeight;
    
    // Texture okuma
    vec4 texColor = texture(u_texture, distorted_uv);
    
    // Dalga efekti
    float wave = sin(uv.x * 30.0 + u_time * u_waveSpeed) * 0.5 + 0.5;
    wave *= sin(uv.y * 30.0 + u_time * u_waveSpeed * 1.3) * 0.5 + 0.5;
    
    // Final renk
    vec3 color = mix(u_waterColor.rgb, u_waterColor.rgb * 1.3, wave);
    color = mix(color, texColor.rgb, texColor.a);
    
    fragColor = vec4(color, u_waterColor.a);
}
```

---

## 3. Light/Point Light Shader

### Godot Shader (Referans)
```glsl
// Godot Point Light Shader
shader_type canvas_item;

uniform vec2 light_position;
uniform vec3 light_color : hint_color = vec3(1.0, 0.9, 0.7);
uniform float light_radius : hint_range(50.0, 500.0) = 200.0;
uniform float light_intensity : hint_range(0.0, 5.0) = 1.5;
uniform float flicker_amount : hint_range(0.0, 1.0) = 0.1;
uniform float flicker_speed : hint_range(0.0, 20.0) = 5.0;

void fragment() {
    vec4 tex_color = texture(TEXTURE, UV);
    
    // Işık pozisyonunu screen space'e çevir
    vec2 screen_pos = FRAGCOORD.xy;
    
    // Mesafe hesaplama
    float dist = distance(screen_pos, light_position);
    
    // Flicker efekti
    float flicker = sin(TIME * flicker_speed) * flicker_amount + 1.0;
    
    // Attenuation
    float attenuation = 1.0 - smoothstep(0.0, light_radius, dist);
    attenuation *= light_intensity * flicker;
    
    // Işık rengini uygula
    vec3 lit = tex_color.rgb + light_color * attenuation * tex_color.a;
    
    COLOR = vec4(lit, tex_color.a);
}
```

### ALT_LAS_ENGINE Adaptasyonu
```glsl
// light.frag - ALT_LAS_ENGINE
#version 330 core

uniform sampler2D u_texture;
uniform vec2 u_resolution;
uniform vec2 u_lightPosition;
uniform vec3 u_lightColor;
uniform float u_lightRadius;
uniform float u_lightIntensity;
uniform float u_flickerAmount;
uniform float u_flickerSpeed;
uniform float u_time;

in vec2 v_uv;
out vec4 fragColor;

void main() {
    vec4 texColor = texture(u_texture, v_uv);
    
    // Screen space pozisyonu
    vec2 screenPos = v_uv * u_resolution;
    
    // Mesafe hesaplama
    float dist = distance(screenPos, u_lightPosition);
    
    // Flicker efekti
    float flicker = sin(u_time * u_flickerSpeed) * u_flickerAmount + 1.0;
    
    // Attenuation
    float attenuation = 1.0 - smoothstep(0.0, u_lightRadius, dist);
    attenuation *= u_lightIntensity * flicker;
    
    // Işık uygula
    vec3 lit = texColor.rgb + u_lightColor * attenuation * texColor.a;
    
    fragColor = vec4(lit, texColor.a);
}
```

---

## 4. Post-Processing: Bloom

### ALT_LAS_ENGINE Bloom Shader
```glsl
// bloom.frag
#version 330 core

uniform sampler2D u_texture;
uniform vec2 u_resolution;
uniform float u_bloomThreshold;
uniform float u_bloomIntensity;
uniform float u_bloomRadius;

in vec2 v_uv;
out vec4 fragColor;

// Gaussian blur
vec3 blur(sampler2D tex, vec2 uv, vec2 direction) {
    vec3 result = vec3(0.0);
    vec2 texelSize = 1.0 / u_resolution;
    
    float weights[5] = float[](0.227027, 0.1945946, 0.1216216, 0.054054, 0.016216);
    
    for (int i = -2; i <= 2; i++) {
        vec2 offset = direction * texelSize * float(i) * u_bloomRadius;
        result += texture(tex, uv + offset).rgb * weights[abs(i)];
    }
    
    return result;
}

void main() {
    vec4 texColor = texture(u_texture, v_uv);
    
    // Bright pixels'ı tespit et
    float brightness = dot(texColor.rgb, vec3(0.2126, 0.7152, 0.0722));
    
    if (brightness > u_bloomThreshold) {
        // Bloom için blur uygula
        vec3 bloomColor = blur(u_texture, v_uv, vec2(1.0, 0.0));
        bloomColor += blur(u_texture, v_uv, vec2(0.0, 1.0));
        bloomColor *= 0.5 * u_bloomIntensity;
        
        fragColor = vec4(texColor.rgb + bloomColor, texColor.a);
    } else {
        fragColor = texColor;
    }
}
```

---

## 5. Particle Shader

### ALT_LAS_ENGINE Particle Shader
```glsl
// particle.frag
#version 330 core

uniform sampler2D u_texture;
uniform float u_time;
uniform float u_particleSize;
uniform vec3 u_particleColor;
uniform float u_fadeSpeed;

in vec2 v_uv;
in float v_lifetime;
out vec4 fragColor;

void main() {
    vec2 center = v_uv - 0.5;
    float dist = length(center);
    
    // Dairesel parçacık
    if (dist > 0.5) discard;
    
    // Lifetime'a göre fade
    float alpha = 1.0 - (v_lifetime * u_fadeSpeed);
    alpha *= smoothstep(0.5, 0.0, dist);
    
    // Renk
    vec3 color = u_particleColor;
    
    // Glow efekti
    float glow = exp(-dist * 4.0) * 0.5;
    color += glow;
    
    fragColor = vec4(color, alpha);
}
```

---

## Terminal ASCII Shader Alternatifleri

### ASCII Glow
```python
# Source/Effects/ascii_glow.py

class ASCIIGlow:
    """Terminal için ASCII glow efekti"""
    
    GLOW_CHARS = " .:;+=xX$&"
    
    def __init__(self, intensity=1.5):
        self.intensity = intensity
    
    def apply(self, char, brightness):
        """Glow uygula"""
        # Parlaklığı artır
        bright = min(1.0, brightness * self.intensity)
        
        # Karakteri seç
        index = int(bright * (len(self.GLOW_CHARS) - 1))
        return self.GLOW_CHARS[index]
```

### ASCII Water Effect
```python
# Source/Effects/ascii_water.py

import math

class ASCIIWater:
    """Terminal için ASCII su efekti"""
    
    WATER_CHARS = " ~≈∼〜〰"
    
    def __init__(self, wave_speed=1.0, wave_height=2):
        self.wave_speed = wave_speed
        self.wave_height = wave_height
    
    def get_char(self, x, y, time):
        """Dalga karakteri"""
        wave = math.sin(x * 0.3 + time * self.wave_speed) 
        wave += math.sin(y * 0.2 + time * self.wave_speed * 1.3)
        wave = (wave + 2) / 4  # Normalize 0-1
        
        index = int(wave * (len(self.WATER_CHARS) - 1))
        return self.WATER_CHARS[index]
```

---

## Kaynaklar

- Godot Shaders: https://godotshaders.com
- Shadertoy: https://www.shadertoy.com
- GLSL Sandbox: https://glslsandbox.com
