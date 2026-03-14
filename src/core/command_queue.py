"""
ALT_LAS Engine - Command Queue
Thread-safe command queue for external AI control.
Engine checks queue each frame and processes commands.
"""

import threading
import uuid
from typing import Optional
from collections import deque


class Command:
    """A single command to be executed by the engine."""

    def __init__(self, tool: str, args: dict):
        self.id = str(uuid.uuid4())[:8]
        self.tool = tool
        self.args = args
        self.result: Optional[dict] = None
        self.done = threading.Event()

    def set_result(self, result: dict) -> None:
        self.result = result
        self.done.set()

    def wait(self, timeout: float = 5.0) -> Optional[dict]:
        self.done.wait(timeout=timeout)
        return self.result


class CommandQueue:
    """Thread-safe command queue for AI-to-engine communication."""

    def __init__(self, max_size: int = 1000):
        self._queue: deque[Command] = deque(maxlen=max_size)
        self._lock = threading.Lock()
        self._results: dict[str, dict] = {}
        self._results_lock = threading.Lock()

    def push(self, tool: str, args: dict) -> Command:
        cmd = Command(tool, args)
        with self._lock:
            self._queue.append(cmd)
        return cmd

    def pop(self) -> Optional[Command]:
        with self._lock:
            if self._queue:
                return self._queue.popleft()
        return None

    def pop_all(self) -> list[Command]:
        with self._lock:
            commands = list(self._queue)
            self._queue.clear()
        return commands

    def size(self) -> int:
        with self._lock:
            return len(self._queue)

    def is_empty(self) -> bool:
        with self._lock:
            return len(self._queue) == 0

    def clear(self) -> None:
        with self._lock:
            self._queue.clear()


# Global command queue instance
_command_queue: Optional[CommandQueue] = None
_queue_lock = threading.Lock()


def get_command_queue() -> CommandQueue:
    """Get or create the global command queue."""
    global _command_queue
    with _queue_lock:
        if _command_queue is None:
            _command_queue = CommandQueue()
        return _command_queue
