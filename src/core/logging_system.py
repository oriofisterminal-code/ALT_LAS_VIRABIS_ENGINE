"""
ALT_LAS Engine - Logging & Debug System
v0.3.1 Implementation

Features:
- LogLevel: 6 severity levels (DEBUG → CRITICAL)
- ErrorCode: Categorized error codes (E001-E399)
- LogManager: Console + File logging with security masking
- LoadingScreen: Progress bar + ASCII art + Tips

Authors:
- Mehmet (Log seviyeleri)
- Feyza (Hata kodları)
- Deniz (Renkler, ASCII art)
- Ayşe (Dosya arşivleme)
- Burak (Güvenlik maskeleme)
- Kemal & Selin (Loading screen)
"""

import os
import sys
import re
import time
import json
import threading
from datetime import datetime
from enum import Enum, auto
from typing import Optional, Callable, List, Dict, Any
from pathlib import Path
from dataclasses import dataclass, field


# ============================================================================
# LOG LEVELS (Mehmet - Backend/Muhasebeci)
# ============================================================================

class LogLevel(Enum):
    """
    Log severity levels for ALT_LAS Engine.
    
    Each level has a numeric value for comparison and filtering.
    Higher values indicate more severe issues.
    """
    DEBUG = 10      # Geliştirici detayları - sadece development'te
    INFO = 20       # Normal işlemler - genel bilgi
    SUCCESS = 25    # Başarılı işlemler - yeşil, pozitif feedback
    WARNING = 30    # Dikkat gerekli - potansiyel sorun
    ERROR = 40      # Hata oluştu - işlem başarısız
    CRITICAL = 50   # Sistem çöküyor - acil müdahale gerekli
    
    @classmethod
    def from_string(cls, level_str: str) -> 'LogLevel':
        """Convert string to LogLevel."""
        mapping = {
            'debug': cls.DEBUG,
            'info': cls.INFO,
            'success': cls.SUCCESS,
            'warning': cls.WARNING,
            'error': cls.ERROR,
            'critical': cls.CRITICAL
        }
        return mapping.get(level_str.lower(), cls.INFO)


# ============================================================================
# ERROR CODES (Feyza - Test/Kalite Kontrol)
# ============================================================================

class ErrorCode(Enum):
    """
    Categorized error codes for ALT_LAS Engine.
    
    Categories:
    - E001-E099: GPU/Rendering errors
    - E100-E199: Texture/Sprite errors
    - E200-E299: File/Data errors
    - E300-E399: System/Runtime errors
    """
    # GPU HATALARI (E001-E099)
    E001_GPU_INIT_FAILED = "GPU başlatılamadı - OpenGL context oluşturulamadı"
    E002_SHADER_COMPILE_ERROR = "Shader derleme hatası - GLSL syntax error"
    E003_OPENGL_CONTEXT_LOST = "OpenGL context kaybı - GPU reset gerekebilir"
    E004_FRAMEBUFFER_INCOMPLETE = "Framebuffer tamamlanmamış - Attachment eksik"
    E005_VERTEX_ARRAY_ERROR = "Vertex array hatası - Buffer oluşturulamadı"
    E006_TEXTURE_BIND_FAILED = "Texture bind hatası - Slot kullanılamıyor"
    E007_SHADER_LINK_ERROR = "Shader link hatası - Vertex/Fragment uyumsuz"
    E008_GPU_MEMORY_EXCEEDED = "GPU bellek aşıldı - Asset azaltın"
    E009_VSYNC_ERROR = "VSync hatası - Refresh rate ayarlanamadı"
    E010_DISPLAY_MODE_ERROR = "Display mode hatası - Çözünürlük desteklenmiyor"
    
    # RENDER HATALARI (E100-E199)
    E100_TEXTURE_LOAD_FAILED = "Texture yüklenemedi - Dosya bozuk veya format hatalı"
    E101_SPRITE_NOT_FOUND = "Sprite bulunamadı - ID mevcut değil"
    E102_FRAME_BUFFER_ERROR = "Frame buffer hatası - Render target oluşturulamadı"
    E103_LAYER_ERROR = "Layer hatası - Geçersiz layer ID"
    E104_CAMERA_ERROR = "Kamera hatası - View matrix oluşturulamadı"
    E105_LIGHT_ERROR = "Işık hatası - Maximum ışık sayısı aşıldı"
    E106_PARTICLE_ERROR = "Partikül hatası - Buffer overflow"
    E107_SHADER_NOT_FOUND = "Shader bulunamadı - Dosya mevcut değil"
    E108_ATLAS_ERROR = "Atlas hatası - Koordinat hesaplanamadı"
    E109_VIEWPORT_ERROR = "Viewport hatası - Geçersiz boyutlar"
    E110_EFFECT_ERROR = "Effect hatası - Post-process başarısız"
    
    # DOSYA HATALARI (E200-E299)
    E200_FILE_NOT_FOUND = "Dosya bulunamadı - Path kontrol edin"
    E201_PARSE_ERROR = "Parse hatası - JSON/veri formatı geçersiz"
    E202_PERMISSION_DENIED = "İzin reddedildi - Dosya erişim yetkisi yok"
    E203_CORRUPT_FILE = "Dosya bozuk - Checksum doğrulama başarısız"
    E204_INVALID_ENCODING = "Geçersiz encoding - UTF-8 bekleniyor"
    E205_DIRECTORY_ERROR = "Dizin hatası - Oluşturulamadı veya erişilemedi"
    E206_SAVE_LOAD_ERROR = "Save/Load hatası - Slot geçersiz"
    E207_ASSET_VERSION_ERROR = "Asset versiyon hatası - Güncelleme gerekli"
    E208_MAP_LOAD_ERROR = "Harita yüklenemedi - Format desteklenmiyor"
    E209_DIALOGUE_ERROR = "Diyalog hatası - Script parse edilemedi"
    E210_CHARACTER_ERROR = "Karakter hatası - Entity tanımı geçersiz"
    
    # SİSTEM HATALARI (E300-E399)
    E300_MEMORY_ALLOC_FAILED = "Bellek tahsis hatası - Yetersiz RAM"
    E301_THREAD_ERROR = "Thread hatası - Worker oluşturulamadı"
    E302_TIMEOUT_ERROR = "Timeout hatası - İşlem zaman aşımına uğradı"
    E303_API_ERROR = "API hatası - HTTP request başarısız"
    E304_MCP_ERROR = "MCP hatası - Tool execution failed"
    E305_INPUT_ERROR = "Input hatası - Cihaz erişilemiyor"
    E306_AUDIO_ERROR = "Audio hatası - Ses cihazı kullanılamıyor"
    E307_STATE_ERROR = "State hatası - Geçersiz sahne geçişi"
    E308_CONFIG_ERROR = "Config hatası - Ayar dosyası geçersiz"
    E309_CALLBACK_ERROR = "Callback hatası - Handler exception"
    E310_INIT_ERROR = "Init hatası - Modül başlatılamadı"
    
    def get_category(self) -> str:
        """Get error category from code number."""
        code_num = int(self.name.split('_')[0][1:])
        if 1 <= code_num <= 99:
            return "GPU"
        elif 100 <= code_num <= 199:
            return "RENDER"
        elif 200 <= code_num <= 299:
            return "DOSYA"
        else:
            return "SİSTEM"
    
    def get_severity(self) -> LogLevel:
        """Determine log level from error type."""
        critical_codes = [1, 2, 3, 8, 300, 310]
        error_codes = [100, 101, 102, 200, 201, 206, 303, 304, 307]
        
        code_num = int(self.name.split('_')[0][1:])
        
        if code_num in critical_codes:
            return LogLevel.CRITICAL
        elif code_num in error_codes:
            return LogLevel.ERROR
        else:
            return LogLevel.WARNING


# ============================================================================
# ANSI COLORS & EMOJIS (Deniz - Frontend/Grafik Tasarımcı)
# ============================================================================

class ANSIColor:
    """ANSI color codes for terminal output."""
    # Reset
    RESET = '\033[0m'
    
    # Basic colors
    BLACK = '\033[30m'
    RED = '\033[31m'
    GREEN = '\033[32m'
    YELLOW = '\033[33m'
    BLUE = '\033[34m'
    MAGENTA = '\033[35m'
    CYAN = '\033[36m'
    WHITE = '\033[37m'
    
    # Bright colors
    BRIGHT_RED = '\033[91m'
    BRIGHT_GREEN = '\033[92m'
    BRIGHT_YELLOW = '\033[93m'
    BRIGHT_BLUE = '\033[94m'
    BRIGHT_MAGENTA = '\033[95m'
    BRIGHT_CYAN = '\033[96m'
    BRIGHT_WHITE = '\033[97m'
    
    # Background colors
    BG_RED = '\033[41m'
    BG_GREEN = '\033[42m'
    BG_YELLOW = '\033[43m'
    BG_BLUE = '\033[44m'
    
    # Styles
    BOLD = '\033[1m'
    DIM = '\033[2m'
    ITALIC = '\033[3m'
    UNDERLINE = '\033[4m'
    BLINK = '\033[5m'
    
    @classmethod
    def supports_color(cls) -> bool:
        """Check if terminal supports ANSI colors."""
        # Check for common color-supporting terminals
        if os.getenv('NO_COLOR'):
            return False
        if os.getenv('COLORTERM') in ('truecolor', '24bit'):
            return True
        if os.getenv('TERM') in ('xterm-256color', 'screen-256color', 'xterm', 'screen'):
            return True
        return sys.stdout.isatty()


class LogStyle:
    """Log styling configuration for each level."""
    
    STYLES = {
        LogLevel.DEBUG: {
            'color': ANSIColor.CYAN,
            'emoji': '🔍',
            'prefix': 'DBG',
            'style': ANSIColor.DIM
        },
        LogLevel.INFO: {
            'color': ANSIColor.BLUE,
            'emoji': 'ℹ️',
            'prefix': 'INF',
            'style': ''
        },
        LogLevel.SUCCESS: {
            'color': ANSIColor.GREEN,
            'emoji': '✅',
            'prefix': 'SUC',
            'style': ANSIColor.BOLD
        },
        LogLevel.WARNING: {
            'color': ANSIColor.YELLOW,
            'emoji': '⚠️',
            'prefix': 'WRN',
            'style': ANSIColor.BOLD
        },
        LogLevel.ERROR: {
            'color': ANSIColor.RED,
            'emoji': '❌',
            'prefix': 'ERR',
            'style': ANSIColor.BOLD
        },
        LogLevel.CRITICAL: {
            'color': ANSIColor.WHITE,
            'emoji': '🔥',
            'prefix': 'CRT',
            'style': ANSIColor.BG_RED + ANSIColor.BOLD
        }
    }
    
    @classmethod
    def format(cls, level: LogLevel, message: str, use_color: bool = True) -> str:
        """Format a log message with styling."""
        style = cls.STYLES.get(level, cls.STYLES[LogLevel.INFO])
        
        timestamp = datetime.now().strftime('%H:%M:%S.%f')[:-3]
        
        if use_color and ANSIColor.supports_color():
            color = style['color']
            prefix = style['prefix']
            emoji = style['emoji']
            text_style = style['style']
            reset = ANSIColor.RESET
            
            return f"{text_style}{color}[{timestamp}] {emoji} {prefix}{reset} {message}"
        else:
            prefix = style['prefix']
            emoji = style['emoji']
            return f"[{timestamp}] {emoji} {prefix} {message}"


# ============================================================================
# SECURITY MASKING (Burak - Güvenlik/Dedektif)
# ============================================================================

class SecurityMasker:
    """
    Security patterns for masking sensitive data in logs.
    
    Masks:
    - IP addresses
    - API keys
    - Passwords
    - Credit card numbers
    - Email addresses
    - Phone numbers
    """
    
    # Masking patterns
    PATTERNS = {
        # IP addresses (IPv4)
        'ipv4': (re.compile(r'\b(\d{1,3})\.(\d{1,3})\.(\d{1,3})\.(\d{1,3})\b'), r'\1.\2.***.***'),
        
        # API keys (common formats)
        'api_key': (re.compile(r'(api[_-]?key|apikey|token)[\s]*[=:][\s]*["\']?([a-zA-Z0-9_-]{8,})["\']?', re.I), r'\1=***MASKED***'),
        
        # Generic secrets
        'secret': (re.compile(r'(secret|password|passwd|pwd|credential)[\s]*[=:][\s]*["\']?([^\s"\']+)["\']?', re.I), r'\1=***MASKED***'),
        
        # Bearer tokens
        'bearer': (re.compile(r'Bearer\s+[a-zA-Z0-9_-]+\.[a-zA-Z0-9_-]+\.[a-zA-Z0-9_-]+'), 'Bearer ***MASKED***'),
        
        # Credit card numbers
        'credit_card': (re.compile(r'\b(\d{4})[-\s]?(\d{4})[-\s]?(\d{4})[-\s]?(\d{4})\b'), r'\1-****-****-\4'),
        
        # Email addresses
        'email': (re.compile(r'\b([a-zA-Z0-9._%+-]+)@([a-zA-Z0-9.-]+\.[a-zA-Z]{2,})\b'), r'***@\2'),
        
        # Phone numbers (Turkish format)
        'phone_tr': (re.compile(r'\b(0)?(\d{3})[\s-]?(\d{3})[\s-]?(\d{2})[\s-]?(\d{2})\b'), r'0\2-***-**-\5'),
        
        # JWT tokens
        'jwt': (re.compile(r'eyJ[a-zA-Z0-9_-]*\.eyJ[a-zA-Z0-9_-]*\.[a-zA-Z0-9_-]*'), '***JWT_MASKED***'),
        
        # Connection strings
        'connection_string': (re.compile(r'(mysql|postgres|mongodb|redis)://([^:]+):([^@]+)@'), r'\1://\2:***@'),
        
        # AWS keys
        'aws_key': (re.compile(r'(AKIA|ASIA|ABIA)[0-9A-Z]{16}'), '***AWS_KEY_MASKED***'),
    }
    
    @classmethod
    def mask(cls, text: str) -> str:
        """Apply all security masks to text."""
        masked_text = text
        
        for pattern_name, (pattern, replacement) in cls.PATTERNS.items():
            masked_text = pattern.sub(replacement, masked_text)
        
        return masked_text
    
    @classmethod
    def add_custom_pattern(cls, name: str, pattern: re.Pattern, replacement: str) -> None:
        """Add a custom masking pattern."""
        cls.PATTERNS[name] = (pattern, replacement)


# ============================================================================
# FILE LOGGING (Ayşe - DB/Kütüphaneci)
# ============================================================================

@dataclass
class LogFileConfig:
    """Configuration for file logging."""
    log_dir: str = "logs"
    max_size_mb: float = 10.0
    max_files: int = 30  # Keep 30 days of logs
    encoding: str = "utf-8"
    include_date: bool = True
    rotation_count: int = 0


class FileRotatingHandler:
    """
    Rotating file handler for log persistence.
    
    Features:
    - 10MB rotation (configurable)
    - 30-day retention (configurable)
    - Date-based filenames
    - Automatic cleanup of old files
    """
    
    def __init__(self, config: Optional[LogFileConfig] = None):
        self.config = config or LogFileConfig()
        self._current_file: Optional[Path] = None
        self._current_size: int = 0
        self._lock = threading.Lock()
        self._ensure_log_dir()
    
    def _ensure_log_dir(self) -> None:
        """Ensure log directory exists."""
        log_path = Path(self.config.log_dir)
        if not log_path.exists():
            log_path.mkdir(parents=True, exist_ok=True)
    
    def _get_log_filename(self) -> Path:
        """Get current log filename with date."""
        date_str = datetime.now().strftime('%Y-%m-%d')
        return Path(self.config.log_dir) / f"altlas_{date_str}.log"
    
    def _should_rotate(self) -> bool:
        """Check if current file needs rotation."""
        if not self._current_file or not self._current_file.exists():
            return False
        
        size_mb = self._current_file.stat().st_size / (1024 * 1024)
        return size_mb >= self.config.max_size_mb
    
    def _rotate_file(self) -> None:
        """Rotate current log file."""
        if not self._current_file or not self._current_file.exists():
            return
        
        # Generate rotated filename
        timestamp = datetime.now().strftime('%Y%m%d_%H%M%S')
        rotated_name = f"{self._current_file.stem}_{timestamp}.log"
        rotated_path = self._current_file.parent / rotated_name
        
        # Rename current file
        self._current_file.rename(rotated_path)
        self.config.rotation_count += 1
        
        # Cleanup old files
        self._cleanup_old_files()
    
    def _cleanup_old_files(self) -> None:
        """Remove old log files beyond retention period."""
        log_path = Path(self.config.log_dir)
        log_files = sorted(log_path.glob("altlas_*.log"), key=lambda f: f.stat().st_mtime)
        
        # Remove files beyond max_files
        while len(log_files) > self.config.max_files:
            oldest = log_files.pop(0)
            oldest.unlink()
    
    def write(self, message: str) -> None:
        """Write a message to the log file."""
        with self._lock:
            log_file = self._get_log_filename()
            
            # Check for rotation (different day or size exceeded)
            if self._current_file != log_file or self._should_rotate():
                if self._current_file and self._current_file.exists():
                    self._rotate_file()
                self._current_file = log_file
                self._current_size = 0
            
            # Write to file (no colors, plain text)
            plain_message = SecurityMasker.mask(message)
            with open(log_file, 'a', encoding=self.config.encoding) as f:
                f.write(plain_message + '\n')
            
            self._current_size += len(message.encode(self.config.encoding))


# ============================================================================
# LOG MANAGER (Main Controller)
# ============================================================================

@dataclass
class LogEntry:
    """A single log entry."""
    timestamp: datetime
    level: LogLevel
    message: str
    error_code: Optional[ErrorCode] = None
    context: Dict[str, Any] = field(default_factory=dict)
    
    def to_dict(self) -> Dict[str, Any]:
        """Convert to dictionary for JSON serialization."""
        return {
            'timestamp': self.timestamp.isoformat(),
            'level': self.level.name,
            'message': self.message,
            'error_code': self.error_code.name if self.error_code else None,
            'context': self.context
        }


class LogManager:
    """
    Central logging system for ALT_LAS Engine.
    
    Features:
    - Console output with colors and emojis
    - File logging with rotation
    - Security masking
    - Error callbacks
    - Context tracking
    - Thread-safe operations
    """
    
    _instance: Optional['LogManager'] = None
    _initialized: bool = False
    
    def __new__(cls, *args, **kwargs):
        if cls._instance is None:
            cls._instance = super().__new__(cls)
        return cls._instance
    
    def __init__(
        self,
        log_level: LogLevel = LogLevel.INFO,
        enable_file: bool = True,
        enable_console: bool = True,
        log_dir: str = "logs"
    ):
        if self._initialized:
            return
        
        self._initialized = True
        self._min_level = log_level
        self._enable_file = enable_file
        self._enable_console = enable_console
        self._lock = threading.Lock()
        
        # Callbacks
        self._error_callbacks: List[Callable[[LogEntry], None]] = []
        self._level_callbacks: Dict[LogLevel, List[Callable[[LogEntry], None]]] = {
            level: [] for level in LogLevel
        }
        
        # File handler
        self._file_handler: Optional[FileRotatingHandler] = None
        if enable_file:
            config = LogFileConfig(log_dir=log_dir)
            self._file_handler = FileRotatingHandler(config)
        
        # Context tracking
        self._context: Dict[str, Any] = {}
        
        # Statistics
        self._stats = {
            'total_logs': 0,
            'by_level': {level.name: 0 for level in LogLevel}
        }
    
    @property
    def min_level(self) -> LogLevel:
        """Get minimum log level."""
        return self._min_level
    
    @min_level.setter
    def min_level(self, level: LogLevel) -> None:
        """Set minimum log level."""
        self._min_level = level
    
    def set_context(self, **kwargs) -> None:
        """Set logging context (will be included in all logs)."""
        with self._lock:
            self._context.update(kwargs)
    
    def clear_context(self) -> None:
        """Clear logging context."""
        with self._lock:
            self._context.clear()
    
    def add_error_callback(self, callback: Callable[[LogEntry], None]) -> None:
        """Add a callback for error-level logs."""
        self._error_callbacks.append(callback)
    
    def add_level_callback(self, level: LogLevel, callback: Callable[[LogEntry], None]) -> None:
        """Add a callback for specific level logs."""
        self._level_callbacks[level].append(callback)
    
    def _log(
        self,
        level: LogLevel,
        message: str,
        error_code: Optional[ErrorCode] = None,
        **context
    ) -> None:
        """Internal logging method."""
        # Check minimum level
        if level.value < self._min_level.value:
            return
        
        # Create entry
        entry = LogEntry(
            timestamp=datetime.now(),
            level=level,
            message=message,
            error_code=error_code,
            context={**self._context, **context}
        )
        
        # Update stats
        with self._lock:
            self._stats['total_logs'] += 1
            self._stats['by_level'][level.name] += 1
        
        # Format for output
        formatted = LogStyle.format(level, message)
        
        # Console output
        if self._enable_console:
            print(formatted)
        
        # File output
        if self._enable_file and self._file_handler:
            # Plain format for file
            file_format = f"[{entry.timestamp.strftime('%Y-%m-%d %H:%M:%S')}] [{level.name}] {message}"
            if error_code:
                file_format += f" ({error_code.name})"
            if context:
                file_format += f" | {context}"
            self._file_handler.write(file_format)
        
        # Callbacks
        if level in (LogLevel.ERROR, LogLevel.CRITICAL):
            for callback in self._error_callbacks:
                try:
                    callback(entry)
                except Exception:
                    pass  # Don't let callback errors propagate
        
        for callback in self._level_callbacks.get(level, []):
            try:
                callback(entry)
            except Exception:
                pass
    
    # Convenience methods
    def debug(self, message: str, **context) -> None:
        """Log debug message."""
        self._log(LogLevel.DEBUG, message, **context)
    
    def info(self, message: str, **context) -> None:
        """Log info message."""
        self._log(LogLevel.INFO, message, **context)
    
    def success(self, message: str, **context) -> None:
        """Log success message."""
        self._log(LogLevel.SUCCESS, message, **context)
    
    def warning(self, message: str, error_code: Optional[ErrorCode] = None, **context) -> None:
        """Log warning message."""
        self._log(LogLevel.WARNING, message, error_code, **context)
    
    def error(self, message: str, error_code: Optional[ErrorCode] = None, **context) -> None:
        """Log error message."""
        self._log(LogLevel.ERROR, message, error_code, **context)
    
    def critical(self, message: str, error_code: Optional[ErrorCode] = None, **context) -> None:
        """Log critical message."""
        self._log(LogLevel.CRITICAL, message, error_code, **context)
    
    def log_error(self, error_code: ErrorCode, message: str = "", **context) -> None:
        """Log with error code (determines level automatically)."""
        level = error_code.get_severity()
        full_message = f"[{error_code.name}] {error_code.value}"
        if message:
            full_message += f" - {message}"
        self._log(level, full_message, error_code, **context)
    
    def get_stats(self) -> Dict[str, Any]:
        """Get logging statistics."""
        return self._stats.copy()
    
    @classmethod
    def get_instance(cls) -> 'LogManager':
        """Get the singleton instance."""
        if cls._instance is None:
            cls._instance = cls()
        return cls._instance


# ============================================================================
# LOADING SCREEN (Kemal & Selin - GPU & UX)
# ============================================================================

class LoadingScreen:
    """
    Loading screen with progress bar, ASCII art, and tips.
    
    Features:
    - ASCII art ALT_LAS banner
    - Progress bar with percentage
    - Random tips
    - Error handling
    - Both terminal and window mode support
    """
    
    # ASCII Art by Deniz
    ASCII_BANNER = """
    ╔═══════════════════════════════════════════════════════════╗
    ║                                                           ║
    ║     █████╗ ███╗   ██╗ █████╗ ██████╗  ██████╗██╗  ██╗    ║
    ║    ██╔══██╗████╗  ██║██╔══██╗██╔══██╗██╔════╝██║  ██║    ║
    ║    ███████║██╔██╗ ██║███████║██████╔╝██║     ███████║    ║
    ║    ██╔══██║██║╚██╗██║██╔══██║██╔══██╗██║     ██╔══██║    ║
    ║    ██║  ██║██║ ╚████║██║  ██║██║  ██║╚██████╗██║  ██║    ║
    ║    ╚═╝  ╚═╝╚═╝  ╚═══╝╚═╝  ╚═╝╚═╝  ╚═╝ ╚═════╝╚═╝  ╚═╝    ║
    ║                                                           ║
    ║               E N G I N E  v0.3.1                        ║
    ║                                                           ║
    ╚═══════════════════════════════════════════════════════════╝
    """
    
    # Tips by Selin
    TIPS = [
        "💡 İpucu: F1 tuşu ile debug modunu açabilirsiniz.",
        "💡 İpucu: Terminal mode AI/MCP sistemi için optimize edilmiştir.",
        "💡 İpucu: Shader'lar src/render/ klasöründe bulunur.",
        "💡 İpucu: Haritalar JSON formatında assets/maps/ altında.",
        "💡 İpucu: API endpoint'leri src/api/handlers/ içinde tanımlı.",
        "💡 İpucu: MCP tools ile oyunu AI ile kontrol edebilirsiniz.",
        "💡 İpucu: Window mode SDL2 veya GLFW kullanabilir.",
        "💡 İpucu: Battle sistem Undertale tarzında tasarlanmıştır.",
        "💡 İpucu: Log dosyaları logs/ klasöründe saklanır.",
        "💡 İpucu: Texture'lar PNG formatında olmalıdır.",
        "💡 İpucu: Dialogues JSON script formatında çalışır.",
        "💡 İpucu: GPU shader'ları GLSL ile yazılır.",
        "💡 İpucu: Save sistemi src/core/save.py içinde.",
        "💡 İpucu: Yeni sahneler BaseScene'den türetilmeli.",
        "💡 İpucu: Entity'ler entity.py base class'ını kullanır.",
    ]
    
    def __init__(self, use_color: bool = True):
        self._progress: float = 0.0
        self._current_step: str = "Başlatılıyor..."
        self._start_time: float = 0.0
        self._errors: List[str] = []
        self._use_color = use_color and ANSIColor.supports_color()
        self._log = LogManager.get_instance()
    
    def start(self) -> None:
        """Start the loading screen."""
        self._start_time = time.time()
        self._clear_screen()
        self._render()
    
    def update(self, progress: float, step: str) -> None:
        """Update loading progress."""
        self._progress = min(1.0, max(0.0, progress))
        self._current_step = step
        self._render()
    
    def add_error(self, error: str) -> None:
        """Add an error message."""
        self._errors.append(error)
        self._log.error(error)
    
    def finish(self, success: bool = True) -> None:
        """Finish loading and show result."""
        if success:
            self._progress = 1.0
            self._current_step = "Tamamlandı!"
            self._render()
            time.sleep(0.5)
            self._clear_screen()
            self._log.success("Engine başarıyla yüklendi!")
        else:
            self._current_step = "Hata oluştu!"
            self._render()
            self._show_errors()
    
    def _clear_screen(self) -> None:
        """Clear the terminal screen."""
        if sys.stdout.isatty():
            sys.stdout.write('\033[2J\033[H')
            sys.stdout.flush()
    
    def _render(self) -> None:
        """Render the loading screen."""
        self._clear_screen()
        
        # Banner
        if self._use_color:
            banner = ANSIColor.BRIGHT_CYAN + self.ASCII_BANNER + ANSIColor.RESET
        else:
            banner = self.ASCII_BANNER
        print(banner)
        
        # Progress bar
        self._render_progress_bar()
        
        # Current step
        if self._use_color:
            step_color = ANSIColor.BRIGHT_WHITE if self._progress < 1.0 else ANSIColor.BRIGHT_GREEN
            print(f"\n{step_color}► {self._current_step}{ANSIColor.RESET}")
        else:
            print(f"\n► {self._current_step}")
        
        # Errors (if any)
        if self._errors:
            self._render_errors()
        
        # Tip
        import random
        tip = random.choice(self.TIPS)
        if self._use_color:
            print(f"\n{ANSIColor.DIM}{tip}{ANSIColor.RESET}")
        else:
            print(f"\n{tip}")
        
        # Elapsed time
        elapsed = time.time() - self._start_time
        print(f"\n⏱️  Geçen süre: {elapsed:.1f}s")
    
    def _render_progress_bar(self) -> None:
        """Render the progress bar."""
        width = 50
        filled = int(width * self._progress)
        empty = width - filled
        percent = int(self._progress * 100)
        
        if self._use_color:
            if self._progress < 0.5:
                bar_color = ANSIColor.YELLOW
            elif self._progress < 1.0:
                bar_color = ANSIColor.CYAN
            else:
                bar_color = ANSIColor.GREEN
            
            bar = f"[{bar_color}{'█' * filled}{ANSIColor.DIM}{'░' * empty}{ANSIColor.RESET}] {percent}%"
        else:
            bar = f"[{'█' * filled}{'░' * empty}] {percent}%"
        
        print(f"\n{bar}")
    
    def _render_errors(self) -> None:
        """Render error messages."""
        if self._use_color:
            print(f"\n{ANSIColor.BRIGHT_RED}⚠️  Hatalar:{ANSIColor.RESET}")
            for error in self._errors[-3:]:  # Show last 3 errors
                print(f"  {ANSIColor.RED}• {error}{ANSIColor.RESET}")
        else:
            print("\n⚠️  Hatalar:")
            for error in self._errors[-3:]:
                print(f"  • {error}")
    
    def _show_errors(self) -> None:
        """Show detailed error information."""
        if self._errors:
            self._log.critical(f"Yükleme başarısız! {len(self._errors)} hata oluştu.")
            for i, error in enumerate(self._errors, 1):
                self._log.error(f"  {i}. {error}")


# ============================================================================
# CONVENIENCE FUNCTIONS
# ============================================================================

# Global log instance
_log: Optional[LogManager] = None


def get_log() -> LogManager:
    """Get the global LogManager instance."""
    global _log
    if _log is None:
        _log = LogManager()
    return _log


def init_logging(
    level: LogLevel = LogLevel.INFO,
    enable_file: bool = True,
    enable_console: bool = True,
    log_dir: str = "logs"
) -> LogManager:
    """Initialize the logging system."""
    global _log
    _log = LogManager(
        log_level=level,
        enable_file=enable_file,
        enable_console=enable_console,
        log_dir=log_dir
    )
    return _log


# Quick access functions
def debug(msg: str, **ctx) -> None: get_log().debug(msg, **ctx)
def info(msg: str, **ctx) -> None: get_log().info(msg, **ctx)
def success(msg: str, **ctx) -> None: get_log().success(msg, **ctx)
def warning(msg: str, code: Optional[ErrorCode] = None, **ctx) -> None: get_log().warning(msg, code, **ctx)
def error(msg: str, code: Optional[ErrorCode] = None, **ctx) -> None: get_log().error(msg, code, **ctx)
def critical(msg: str, code: Optional[ErrorCode] = None, **ctx) -> None: get_log().critical(msg, code, **ctx)


# ============================================================================
# MODULE TEST
# ============================================================================

if __name__ == "__main__":
    # Test the logging system
    print("=" * 60)
    print("ALT_LAS Engine Logging System Test")
    print("=" * 60)
    
    # Initialize logging
    log = init_logging(level=LogLevel.DEBUG, log_dir="test_logs")
    
    # Test all log levels
    log.debug("Debug mesajı - geliştirici detayları")
    log.info("Info mesajı - normal işlem")
    log.success("Success mesajı - başarılı işlem!")
    log.warning("Warning mesajı - dikkat gerekli", ErrorCode.E009_VSYNC_ERROR)
    log.error("Error mesajı - bir hata oluştu", ErrorCode.E100_TEXTURE_LOAD_FAILED)
    log.critical("Critical mesajı - sistem hatası!", ErrorCode.E001_GPU_INIT_FAILED)
    
    # Test error code logging
    log.log_error(ErrorCode.E201_PARSE_ERROR, "config.json dosyası bozuk")
    
    # Test security masking
    test_secret = "API Key: api_key=sk-1234567890abcdef, Password: secret=mypassword123"
    print(f"\nOriginal: {test_secret}")
    print(f"Masked:   {SecurityMasker.mask(test_secret)}")
    
    # Test loading screen
    print("\n" + "=" * 60)
    print("Loading Screen Test")
    print("=" * 60)
    
    loader = LoadingScreen()
    loader.start()
    
    steps = [
        (0.1, "GPU başlatılıyor..."),
        (0.2, "Shader'lar yükleniyor..."),
        (0.4, "Texture'lar yükleniyor..."),
        (0.6, "Haritalar yükleniyor..."),
        (0.8, "Sahneler oluşturuluyor..."),
        (0.9, "API server başlatılıyor..."),
        (1.0, "Tamamlandı!"),
    ]
    
    for progress, step in steps:
        time.sleep(0.3)
        loader.update(progress, step)
    
    loader.finish()
    
    # Show stats
    print("\n" + "=" * 60)
    print("Log İstatistikleri:")
    stats = log.get_stats()
    print(f"  Toplam log: {stats['total_logs']}")
    for level, count in stats['by_level'].items():
        if count > 0:
            print(f"  {level}: {count}")
