# TinyRenderer Render Pipeline Reference

TinyRenderer'dan render pipeline mimarisi için referans notları.

## Kaynak
https://github.com/ssloy/tinyrenderer

## Genel Bakış

TinyRenderer, ~500 satır kod ile bir yazılım render motoru oluşturmayı öğreten kapsamlı bir kurstur. OpenGL'nin çalışma prensiplerini anlamak için ideal bir kaynaktır.

## Ders İçerikleri

### Lesson 0: Başlangıç
- Görüntü dosyası oluşturma (TGA format)
- Temel çizim fonksiyonları

### Lesson 1: Bresenham Algoritması
- İki nokta arası çizgi çizme
- Verimli integer hesaplama

### Lesson 2: Triangle Rasterization
- Üçgen doldurma algoritması
- Bounding box hesaplama
- Z-buffer konsepti

### Lesson 3: Hidden Face Removal
- Back-face culling
- Derinlik sıralaması

### Lesson 4: Perspective Projection
- Model-View-Projection matrisleri
- 3D'den 2D'ye dönüşüm

### Lesson 5: Camera Movement
- Kamera transformasyonları
- Look-at matrisi

### Lesson 6: Shader Pipeline
- Vertex shader
- Fragment shader
- Gouraud shading

### Lesson 7: Shadow Mapping
- Shadow buffer
- Işık perspektifi

---

## Render Pipeline Mimarisi

### 1. Vertex Processing

```python
# Vertex Shader Mantığı
def vertex_shader(vertex, mvp_matrix, model_matrix):
    # Clip space koordinatları
    clip_coords = mvp_matrix @ vertex.position
    
    # World space pozisyonu
    world_pos = model_matrix @ vertex.position
    
    # Normal transformasyonu
    normal = model_matrix.inverse().transpose() @ vertex.normal
    
    return {
        'gl_Position': clip_coords,
        'worldPos': world_pos,
        'normal': normalize(normal),
        'uv': vertex.uv
    }
```

### 2. Primitive Assembly

```python
def primitive_assembly(vertices):
    """Üçgen oluşturma"""
    triangles = []
    for i in range(0, len(vertices), 3):
        triangle = Triangle(
            vertices[i],
            vertices[i+1],
            vertices[i+2]
        )
        triangles.append(triangle)
    return triangles
```

### 3. Rasterization

```python
def rasterize_triangle(triangle, width, height):
    """Üçgen rasterizasyonu"""
    # Bounding box
    min_x = max(0, int(min(v.x for v in triangle.vertices)))
    max_x = min(width-1, int(max(v.x for v in triangle.vertices)))
    min_y = max(0, int(min(v.y for v in triangle.vertices)))
    max_y = min(height-1, int(max(v.y for v in triangle.vertices)))
    
    fragments = []
    for y in range(min_y, max_y + 1):
        for x in range(min_x, max_x + 1):
            # Barycentric koordinatlar
            bc = barycentric(triangle, x, y)
            if all(b >= 0 for b in bc):
                # Interpolasyon
                z = interpolate_z(triangle, bc)
                fragments.append(Fragment(x, y, z, bc))
    
    return fragments
```

### 4. Fragment Processing

```python
def fragment_shader(fragment, material, lights):
    """Fragment shader"""
    # Interpolasyon
    uv = interpolate_uv(fragment.barycentric)
    normal = interpolate_normal(fragment.barycentric)
    
    # Texture okuma
    albedo = material.diffuse_texture.sample(uv)
    
    # Işıklandırma
    color = vec3(0, 0, 0)
    for light in lights:
        # Diffuse
        ndotl = max(0, dot(normal, light.direction))
        color += albedo * light.color * ndotl
        
        # Specular
        view_dir = normalize(camera_pos - fragment.world_pos)
        reflect_dir = reflect(-light.direction, normal)
        spec = pow(max(dot(view_dir, reflect_dir), 0), material.shininess)
        color += light.color * spec * material.specular
    
    return color
```

### 5. Output Merger

```python
def output_merger(fragment, color, depth_buffer, frame_buffer):
    """Derinlik testi ve framebuffer yazma"""
    x, y = fragment.x, fragment.y
    
    # Depth test
    if fragment.z < depth_buffer[y][x]:
        depth_buffer[y][x] = fragment.z
        frame_buffer[y][x] = color
```

---

## ALT_LAS_ENGINE Entegrasyonu

### Pipeline Sınıfı

```python
# Source/Rendering/pipeline.py

class RenderPipeline:
    def __init__(self, width, height):
        self.width = width
        self.height = height
        self.depth_buffer = [[float('inf')] * width for _ in range(height)]
        self.frame_buffer = [[vec3(0, 0, 0)] * width for _ in range(height)]
        
    def render(self, mesh, camera, lights):
        # 1. Vertex processing
        vertices = self.vertex_process(mesh.vertices, camera)
        
        # 2. Primitive assembly
        triangles = self.assemble_primitives(vertices, mesh.indices)
        
        # 3. Clipping
        triangles = self.clip(triangles)
        
        # 4. Rasterization
        for triangle in triangles:
            fragments = self.rasterize(triangle)
            
            # 5. Fragment processing
            for fragment in fragments:
                color = self.fragment_process(fragment, lights)
                
                # 6. Output merger
                self.merge_output(fragment, color)
        
        return self.frame_buffer
```

### Vertex Formatı

```python
# Source/Rendering/vertex.py

from dataclasses import dataclass
from numpy import array

@dataclass
class Vertex:
    position: array      # vec3
    normal: array        # vec3
    uv: array           # vec2
    color: array = None  # vec4 (optional)
    
    def lerp(self, other, t):
        """İki vertex arası interpolasyon"""
        return Vertex(
            position=self.position * (1-t) + other.position * t,
            normal=self.normal * (1-t) + other.normal * t,
            uv=self.uv * (1-t) + other.uv * t,
            color=self.color * (1-t) + other.color * t if self.color else None
        )
```

### Matris İşlemleri

```python
# Source/Rendering/matrix.py

import numpy as np

def perspective(fov, aspect, near, far):
    """Perspektif projeksiyon matrisi"""
    f = 1.0 / np.tan(fov / 2)
    return np.array([
        [f/aspect, 0, 0, 0],
        [0, f, 0, 0],
        [0, 0, (far+near)/(near-far), 2*far*near/(near-far)],
        [0, 0, -1, 0]
    ])

def look_at(eye, target, up):
    """Look-at matrisi"""
    forward = normalize(target - eye)
    right = normalize(np.cross(forward, up))
    up = np.cross(right, forward)
    
    return np.array([
        [right[0], right[1], right[2], -np.dot(right, eye)],
        [up[0], up[1], up[2], -np.dot(up, eye)],
        [-forward[0], -forward[1], -forward[2], np.dot(forward, eye)],
        [0, 0, 0, 1]
    ])

def model_matrix(position, rotation, scale):
    """Model transformasyon matrisi"""
    # Rotation (Euler angles)
    rx = rotation_x(rotation[0])
    ry = rotation_y(rotation[1])
    rz = rotation_z(rotation[2])
    rot = rz @ ry @ rx
    
    # Scale
    s = np.diag([scale[0], scale[1], scale[2], 1])
    
    # Translation
    t = np.array([
        [1, 0, 0, position[0]],
        [0, 1, 0, position[1]],
        [0, 0, 1, position[2]],
        [0, 0, 0, 1]
    ])
    
    return t @ rot @ s
```

---

## Terminal-Optimized Render

Terminal için optimize edilmiş render pipeline:

```python
# Source/Rendering/terminal_renderer.py

class TerminalRenderer:
    """Terminal için optimize render sistemi"""
    
    # ASCII karakter gradyanı (koyudan aydınlığa)
    GRADIENT = " .:-=+*#%@"
    
    def __init__(self, width, height):
        self.width = width
        self.height = height
        self.buffer = [[' ' for _ in range(width)] for _ in range(height)]
        self.depth_buffer = [[float('inf')] * width for _ in range(height)]
        
    def clear(self):
        """Buffer'ı temizle"""
        self.buffer = [[' ' for _ in range(self.width)] for _ in range(self.height)]
        self.depth_buffer = [[float('inf')] * self.width for _ in range(self.height)]
        
    def render(self, meshes, camera, lights):
        """Render işlemi"""
        self.clear()
        
        for mesh in meshes:
            self.render_mesh(mesh, camera, lights)
        
        return self.buffer
    
    def to_string(self):
        """Buffer'ı string'e çevir"""
        return '\n'.join(''.join(row) for row in self.buffer)
    
    def luminance_to_char(self, luminance):
        """Luminance değerini ASCII karaktere çevir"""
        index = int(luminance * (len(self.GRADIENT) - 1))
        return self.GRADIENT[max(0, min(index, len(self.GRADIENT) - 1))]
```

---

## Performans İpuçları

1. **Backface Culling**: Görünmeyen yüzleri erken ele
2. **Frustum Culling**: Görüş alanı dışındaki objeleri atla
3. **LOD (Level of Detail)**: Uzak objeler için basitleştirilmiş mesh kullan
4. **Instance Rendering**: Aynı objeyi tekrar eden draw call'ları birleştir
5. **Occlusion Culling**: Arkada kalan objeleri render etme

## Terminal İçin Ek Optimizasyonlar

1. **Düşük çözünürlük**: Terminal karakterleri için düşük çözünürlük yeterli
2. **Basit shading**: ASCII karakterleri için basit shading
3. **Cache**: Hesaplanan değerleri cache'le
4. **Dirty Regions**: Sadece değişen bölgeleri güncelle
