# glslViewer Terminal Integration

glslViewer araç setinin ALT_LAS_ENGINE terminal modülüne entegrasyonu için referanslar.

## Kaynak
https://github.com/patriciogonzalezvivo/glslViewer

---

## glslViewer Özellikleri

### Temel Kullanım
```bash
# Tek shader dosyası
glslViewer shader.frag

# Vertex + Fragment shader
glslViewer shader.vert shader.frag

# Texture ile
glslViewer shader.frag texture.png

# Uniform parametreler
glslViewer shader.frag -u_time 1.0 -u_resolution 800,600
```

### Konsol Komutları
```
# Uniform değerleri ayarla
u_time,TIME 0.5
u_resolution,vec2,800,600
u_color,vec3,1.0,0.5,0.2

# Texture yükle
texture,texture.png,0

# Ekran görüntüsü al
screenshot,output.png
```

---

## ALT_LAS_ENGINE Entegrasyonu

### 1. Shader Loader

```python
# Source/Terminal/shader_loader.py

import os
import subprocess
from dataclasses import dataclass
from typing import Dict, Optional

@dataclass
class ShaderConfig:
    vertex_path: str = None
    fragment_path: str = None
    textures: Dict[str, str] = None
    uniforms: Dict[str, any] = None
    
class ShaderLoader:
    """Shader dosyalarını yükleyip yöneten sınıf"""
    
    def __init__(self, shader_dir: str = "Source/Shaders"):
        self.shader_dir = shader_dir
        self.cache = {}
        
    def load(self, name: str, config: ShaderConfig = None) -> 'Shader':
        """Shader yükle"""
        if name in self.cache:
            return self.cache[name]
            
        # Shader dosyalarını bul
        if config is None:
            config = self._auto_detect(name)
            
        # Shader oluştur
        shader = Shader(
            vertex_path=config.vertex_path,
            fragment_path=config.fragment_path
        )
        
        # Texture'ları yükle
        if config.textures:
            for uniform_name, tex_path in config.textures.items():
                shader.load_texture(uniform_name, tex_path)
                
        # Uniform'ları ayarla
        if config.uniforms:
            for name, value in config.uniforms.items():
                shader.set_uniform(name, value)
                
        self.cache[name] = shader
        return shader
        
    def _auto_detect(self, name: str) -> ShaderConfig:
        """Shader dosyalarını otomatik bul"""
        config = ShaderConfig()
        
        # Vertex shader
        vert_path = os.path.join(self.shader_dir, f"{name}.vert")
        if os.path.exists(vert_path):
            config.vertex_path = vert_path
            
        # Fragment shader
        frag_path = os.path.join(self.shader_dir, f"{name}.frag")
        if os.path.exists(frag_path):
            config.fragment_path = frag_path
            
        return config
```

### 2. Terminal Shader Manager

```python
# Source/Terminal/shader_manager.py

import subprocess
import threading
import time
from queue import Queue

class TerminalShaderManager:
    """Terminal için shader yönetimi"""
    
    def __init__(self, width: int = 80, height: int = 24):
        self.width = width
        self.height = height
        self.shaders = {}
        self.active_shader = None
        self.command_queue = Queue()
        self._running = False
        
    def start(self):
        """Shader manager'ı başlat"""
        self._running = True
        self._thread = threading.Thread(target=self._render_loop)
        self._thread.daemon = True
        self._thread.start()
        
    def stop(self):
        """Shader manager'ı durdur"""
        self._running = False
        if self._thread:
            self._thread.join()
            
    def load_shader(self, name: str, frag_path: str, vert_path: str = None):
        """Shader yükle"""
        self.shaders[name] = {
            'fragment': frag_path,
            'vertex': vert_path,
            'uniforms': {}
        }
        
    def set_uniform(self, shader_name: str, uniform_name: str, value):
        """Uniform değer ayarla"""
        if shader_name in self.shaders:
            self.shaders[shader_name]['uniforms'][uniform_name] = value
            
    def activate(self, name: str):
        """Shader'ı aktif et"""
        if name in self.shaders:
            self.active_shader = name
            
    def render(self) -> str:
        """Mevcut shader'ı render et ve ASCII döndür"""
        if not self.active_shader:
            return ""
            
        shader = self.shaders[self.active_shader]
        
        # Render komutunu kuyruğa ekle
        result_queue = Queue()
        self.command_queue.put(('render', result_queue))
        
        return result_queue.get()
        
    def _render_loop(self):
        """Render döngüsü"""
        while self._running:
            try:
                cmd = self.command_queue.get(timeout=0.1)
                if cmd[0] == 'render':
                    result = self._do_render()
                    cmd[1].put(result)
            except:
                pass
                
    def _do_render(self) -> str:
        """Gerçek render işlemi"""
        if not self.active_shader:
            return ""
            
        shader = self.shaders[self.active_shader]
        
        # glslViewer'ı çalıştır (eğer mevcutsa)
        # veya fallback olarak basit ASCII render
        return self._ascii_fallback()
        
    def _ascii_fallback(self) -> str:
        """ASCII fallback render"""
        lines = []
        for y in range(self.height):
            line = ""
            for x in range(self.width):
                # Basit gradient
                brightness = (x + y) / (self.width + self.height)
                char = self._brightness_to_char(brightness)
                line += char
            lines.append(line)
        return "\n".join(lines)
        
    def _brightness_to_char(self, brightness: float) -> str:
        """Parlaklık değerini ASCII karaktere çevir"""
        chars = " .:-=+*#%@"
        index = int(brightness * (len(chars) - 1))
        return chars[max(0, min(index, len(chars) - 1))]
```

### 3. Hot Reload Desteği

```python
# Source/Terminal/hot_reload.py

import os
import time
from watchdog.observers import Observer
from watchdog.events import FileSystemEventHandler

class ShaderWatcher(FileSystemEventHandler):
    """Shader dosya değişikliklerini izle"""
    
    def __init__(self, callback):
        self.callback = callback
        self.last_modified = {}
        
    def on_modified(self, event):
        if event.is_directory:
            return
            
        # Dosya uzantısını kontrol et
        if event.src_path.endswith(('.frag', '.vert', '.glsl')):
            # Debounce
            now = time.time()
            if event.src_path in self.last_modified:
                if now - self.last_modified[event.src_path] < 0.5:
                    return
                    
            self.last_modified[event.src_path] = now
            
            # Callback'i çağır
            self.callback(event.src_path)

class HotReloadManager:
    """Hot reload yönetimi"""
    
    def __init__(self, shader_manager):
        self.shader_manager = shader_manager
        self.observer = Observer()
        self.watchers = {}
        
    def watch(self, path: str):
        """Klasörü izle"""
        if path not in self.watchers:
            watcher = ShaderWatcher(self._on_shader_change)
            self.observer.schedule(watcher, path, recursive=True)
            self.watchers[path] = watcher
            
    def start(self):
        """İzlemeyi başlat"""
        self.observer.start()
        
    def stop(self):
        """İzlemeyi durdur"""
        self.observer.stop()
        self.observer.join()
        
    def _on_shader_change(self, filepath: str):
        """Shader değiştiğinde"""
        print(f"Shader changed: {filepath}")
        
        # Shader'ı yeniden yükle
        shader_name = os.path.basename(filepath).split('.')[0]
        if shader_name in self.shader_manager.shaders:
            self.shader_manager.load_shader(
                shader_name,
                self.shader_manager.shaders[shader_name]['fragment'],
                self.shader_manager.shaders[shader_name]['vertex']
            )
            print(f"Reloaded shader: {shader_name}")
```

### 4. Uniform Sistemi

```python
# Source/Terminal/uniforms.py

from dataclasses import dataclass
from typing import Union, List, Tuple
import numpy as np

@dataclass
class Uniform:
    """Shader uniform değişkeni"""
    name: str
    type: str  # float, vec2, vec3, vec4, mat4, sampler2D
    value: Union[float, List, Tuple, np.ndarray, str]
    
class UniformManager:
    """Uniform yönetimi"""
    
    def __init__(self):
        self.uniforms = {}
        self.time = 0.0
        self.resolution = (80, 24)
        
    def set(self, name: str, value, uniform_type: str = None):
        """Uniform değer ayarla"""
        # Tip otomatik tespit
        if uniform_type is None:
            uniform_type = self._detect_type(value)
            
        self.uniforms[name] = Uniform(name, uniform_type, value)
        
    def get(self, name: str):
        """Uniform değer al"""
        return self.uniforms.get(name)
        
    def _detect_type(self, value) -> str:
        """Değer tipini tespit et"""
        if isinstance(value, float) or isinstance(value, int):
            return 'float'
        elif isinstance(value, (list, tuple)):
            if len(value) == 2:
                return 'vec2'
            elif len(value) == 3:
                return 'vec3'
            elif len(value) == 4:
                return 'vec4'
        elif isinstance(value, np.ndarray):
            if value.shape == (2,):
                return 'vec2'
            elif value.shape == (3,):
                return 'vec3'
            elif value.shape == (4, 4):
                return 'mat4'
        elif isinstance(value, str):
            return 'sampler2D'
        return 'unknown'
        
    def update_time(self, delta: float):
        """Zaman güncelle"""
        self.time += delta
        self.uniforms['u_time'] = Uniform('u_time', 'float', self.time)
        
    def set_resolution(self, width: int, height: int):
        """Çözünürlük ayarla"""
        self.resolution = (width, height)
        self.uniforms['u_resolution'] = Uniform('u_resolution', 'vec2', [width, height])
        
    def get_all(self) -> dict:
        """Tüm uniform'ları döndür"""
        return {name: u.value for name, u in self.uniforms.items()}
        
    def to_glsl_command(self) -> str:
        """GLSL komutuna çevir"""
        lines = []
        for name, uniform in self.uniforms.items():
            if uniform.type == 'float':
                lines.append(f"u_{name},float,{uniform.value}")
            elif uniform.type == 'vec2':
                lines.append(f"u_{name},vec2,{uniform.value[0]},{uniform.value[1]}")
            elif uniform.type == 'vec3':
                lines.append(f"u_{name},vec3,{uniform.value[0]},{uniform.value[1]},{uniform.value[2]}")
            elif uniform.type == 'vec4':
                lines.append(f"u_{name},vec4,{uniform.value[0]},{uniform.value[1]},{uniform.value[2]},{uniform.value[3]}")
        return '\n'.join(lines)
```

### 5. Terminal Render Context

```python
# Source/Terminal/context.py

import sys
import tty
import termios
from contextlib import contextmanager

class TerminalContext:
    """Terminal context yönetimi"""
    
    def __init__(self):
        self.original_settings = None
        self.width = 80
        self.height = 24
        
    def enter_raw_mode(self):
        """Raw mode'a geç"""
        self.original_settings = termios.tcgetattr(sys.stdin)
        tty.setraw(sys.stdin)
        
    def exit_raw_mode(self):
        """Normal mode'a dön"""
        if self.original_settings:
            termios.tcsetattr(sys.stdin, termios.TCSADRAIN, self.original_settings)
            
    def get_size(self) -> tuple:
        """Terminal boyutunu al"""
        try:
            import shutil
            size = shutil.get_terminal_size()
            self.width = size.columns
            self.height = size.lines
            return (self.width, self.height)
        except:
            return (self.width, self.height)
            
    def clear(self):
        """Ekranı temizle"""
        print('\033[2J\033[H', end='')
        
    def set_cursor(self, x: int, y: int):
        """İmleci ayarla"""
        print(f'\033[{y};{x}H', end='')
        
    def hide_cursor(self):
        """İmleci gizle"""
        print('\033[?25l', end='')
        
    def show_cursor(self):
        """İmleci göster"""
        print('\033[?25h', end='')
        
    def set_color(self, fg: int = None, bg: int = None):
        """Renk ayarla (256 color)"""
        codes = []
        if fg is not None:
            codes.append(f'38;5;{fg}')
        if bg is not None:
            codes.append(f'48;5;{bg}')
        if codes:
            print(f'\033[{";".join(codes)}m', end='')
            
    def reset_color(self):
        """Rengi sıfırla"""
        print('\033[0m', end='')
        
    @contextmanager
    def raw_mode(self):
        """Context manager for raw mode"""
        self.enter_raw_mode()
        try:
            yield
        finally:
            self.exit_raw_mode()
            
    @contextmanager
    def alternate_buffer(self):
        """Alternate buffer kullan"""
        print('\033[?1049h', end='')  # Alternate buffer
        try:
            yield
        finally:
            print('\033[?1049l', end='')  # Main buffer
```

---

## Entegrasyon Örneği

```python
# main.py - Terminal shader demo

from Source.Terminal.shader_manager import TerminalShaderManager
from Source.Terminal.uniforms import UniformManager
from Source.Terminal.context import TerminalContext
from Source.Terminal.hot_reload import HotReloadManager
import time

def main():
    # Terminal context
    ctx = TerminalContext()
    ctx.get_size()
    
    # Shader manager
    shader_mgr = TerminalShaderManager(ctx.width, ctx.height)
    
    # Uniform manager
    uniforms = UniformManager()
    uniforms.set_resolution(ctx.width, ctx.height)
    
    # Shader'ları yükle
    shader_mgr.load_shader("glow", "Source/Shaders/glow.frag")
    shader_mgr.load_shader("water", "Source/Shaders/water.frag")
    
    # Hot reload
    hot_reload = HotReloadManager(shader_mgr)
    hot_reload.watch("Source/Shaders")
    hot_reload.start()
    
    # Ana döngü
    with ctx.alternate_buffer():
        with ctx.raw_mode():
            ctx.hide_cursor()
            
            shader_mgr.activate("glow")
            
            running = True
            last_time = time.time()
            
            while running:
                # Delta time
                now = time.time()
                delta = now - last_time
                last_time = now
                
                # Uniform güncelle
                uniforms.update_time(delta)
                
                # Shader uniform'larını ayarla
                for name, value in uniforms.get_all().items():
                    shader_mgr.set_uniform("glow", name, value)
                
                # Render
                ctx.clear()
                output = shader_mgr.render()
                print(output, end='', flush=True)
                
                # Input kontrol
                if sys.stdin in select.select([sys.stdin], [], [], 0)[0]:
                    char = sys.stdin.read(1)
                    if char == 'q' or char == '\x1b':  # q or ESC
                        running = False
                    elif char == '1':
                        shader_mgr.activate("glow")
                    elif char == '2':
                        shader_mgr.activate("water")
                        
                time.sleep(1/30)  # 30 FPS
                
    # Temizlik
    hot_reload.stop()
    ctx.show_cursor()

if __name__ == "__main__":
    main()
```

---

## Performans İpuçları

1. **Delta Time**: Frame'ler arası zamanı hesapla
2. **Frame Limiting**: FPS sınırlaması uygula
3. **Partial Updates**: Sadece değişen bölgeleri güncelle
4. **Buffer Caching**: Render buffer'ını cache'le
5. **Unicode Optimization**: Tek byte karakterler kullan

## Bağımlılıklar

```txt
# requirements.txt
watchdog>=2.0.0
numpy>=1.20.0
```
