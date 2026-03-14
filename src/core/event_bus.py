"""
ALT_LAS Engine - Event Bus System
Ported from VirabisCore (C#) EventBus architecture.

Thread-safe, typed event system with priority-based dispatch,
filtering, and subscription management.

Reference: test_core/Virabis.Core/Events/
"""

import threading
import uuid
import logging
from dataclasses import dataclass, field
from datetime import datetime, timezone
from typing import Callable, Optional

logger = logging.getLogger(__name__)


class Event:
    """Base class for all events. All custom events should inherit from this."""

    def __init__(self, source: object = None):
        self.timestamp: datetime = datetime.now(timezone.utc)
        self.source: object = source

    def __repr__(self) -> str:
        return f"{self.__class__.__name__}(source={self.source})"


class EventSubscription:
    """Handle for managing an event subscription. Can be used to unsubscribe."""

    def __init__(self, subscription_id: uuid.UUID, unsubscribe_fn: Callable[[], None]):
        self._id = subscription_id
        self._unsubscribe_fn = unsubscribe_fn
        self._active = True

    @property
    def id(self) -> uuid.UUID:
        return self._id

    @property
    def is_active(self) -> bool:
        return self._active

    def dispose(self) -> None:
        """Unsubscribe from the event."""
        if self._active:
            self._unsubscribe_fn()
            self._active = False

    # Support context manager usage: with bus.subscribe(...) as sub: ...
    def __enter__(self):
        return self

    def __exit__(self, exc_type, exc_val, exc_tb):
        self.dispose()
        return False


@dataclass
class _Subscription:
    """Internal subscription record."""
    id: uuid.UUID
    handler: Callable[[Event], None]
    filter_fn: Optional[Callable[[Event], bool]]
    priority: int

    def try_handle(self, event: Event) -> bool:
        if self.handler is None:
            return False
        if self.filter_fn is not None and not self.filter_fn(event):
            return False
        self.handler(event)
        return True


class _SubscriptionList:
    """Thread-safe, priority-sorted subscription list."""

    def __init__(self):
        self._subscriptions: list[_Subscription] = []
        self._lock = threading.Lock()
        self._needs_sort = False

    @property
    def count(self) -> int:
        with self._lock:
            return len(self._subscriptions)

    def add(self, subscription: _Subscription) -> None:
        with self._lock:
            self._subscriptions.append(subscription)
            self._needs_sort = True

    def remove_by_id(self, subscription_id: uuid.UUID) -> None:
        with self._lock:
            self._subscriptions = [
                s for s in self._subscriptions if s.id != subscription_id
            ]

    def get_handlers(self) -> list[_Subscription]:
        with self._lock:
            if self._needs_sort:
                self._subscriptions.sort(key=lambda s: -s.priority)
                self._needs_sort = False
            return list(self._subscriptions)


class EventBus:
    """
    Thread-safe event bus for decoupled communication between systems.

    Ported from VirabisCore C# EventBus. Supports:
    - Type-based event routing
    - Priority-ordered handlers (higher priority fires first)
    - Event filtering
    - Subscription handles for easy unsubscribe
    - publish_and_check to know if any handler processed the event

    Usage:
        bus = EventBus()

        # Subscribe
        sub = bus.subscribe(DamageTakenEvent, on_damage)

        # Subscribe with priority (higher = called first)
        sub2 = bus.subscribe(DamageTakenEvent, on_damage_log, priority=-1)

        # Subscribe with filter
        sub3 = bus.subscribe(DamageTakenEvent, on_crit_damage,
                             filter_fn=lambda e: e.is_critical)

        # Publish
        bus.publish(DamageTakenEvent(target=enemy, damage=50))

        # Unsubscribe
        sub.dispose()
    """

    def __init__(self, enable_logging: bool = False):
        self._subscriptions: dict[type, _SubscriptionList] = {}
        self._subscription_index: dict[uuid.UUID, type] = {}
        self._lock = threading.Lock()
        self._enable_logging = enable_logging

    def subscribe(
        self,
        event_type: type,
        handler: Callable,
        priority: int = 0,
        filter_fn: Optional[Callable] = None,
    ) -> EventSubscription:
        """
        Subscribe to an event type.

        Args:
            event_type: The event class to listen for.
            handler: Callback function that receives the event.
            priority: Higher priority handlers fire first. Default 0.
            filter_fn: Optional predicate; handler only called when filter returns True.

        Returns:
            EventSubscription handle for unsubscribing.
        """
        if handler is None:
            raise ValueError("handler cannot be None")

        subscription_id = uuid.uuid4()

        with self._lock:
            if event_type not in self._subscriptions:
                self._subscriptions[event_type] = _SubscriptionList()
            sub_list = self._subscriptions[event_type]

        sub = _Subscription(
            id=subscription_id,
            handler=handler,
            filter_fn=filter_fn,
            priority=priority,
        )
        sub_list.add(sub)

        with self._lock:
            self._subscription_index[subscription_id] = event_type

        self._log(f"Subscribed to {event_type.__name__} (id: {subscription_id})")

        return EventSubscription(
            subscription_id, lambda: self._unsubscribe_by_id(subscription_id)
        )

    def subscribe_once(
        self,
        event_type: type,
        handler: Callable,
        priority: int = 0,
    ) -> EventSubscription:
        """Subscribe to an event type; automatically unsubscribes after first call."""
        subscription_holder: list[Optional[EventSubscription]] = [None]

        def wrapper(event):
            if subscription_holder[0] is not None:
                subscription_holder[0].dispose()
            handler(event)

        sub = self.subscribe(event_type, wrapper, priority=priority)
        subscription_holder[0] = sub
        return sub

    def unsubscribe(self, subscription: EventSubscription) -> None:
        """Unsubscribe using a subscription handle."""
        if not subscription.is_active:
            return
        subscription.dispose()

    def _unsubscribe_by_id(self, subscription_id: uuid.UUID) -> None:
        with self._lock:
            event_type = self._subscription_index.pop(subscription_id, None)
        if event_type is not None:
            sub_list = self._subscriptions.get(event_type)
            if sub_list is not None:
                sub_list.remove_by_id(subscription_id)
            self._log(f"Unsubscribed (id: {subscription_id})")

    def publish(self, event: Event) -> None:
        """Publish an event to all matching subscribers."""
        self.publish_and_check(event)

    def publish_and_check(self, event: Event) -> bool:
        """
        Publish an event and return True if any handler processed it.

        Args:
            event: The event instance to publish.

        Returns:
            True if at least one handler was called.
        """
        if event is None:
            raise ValueError("event cannot be None")

        event_type = type(event)
        has_handlers = False

        sub_list = self._subscriptions.get(event_type)
        if sub_list is not None:
            handlers = sub_list.get_handlers()
            for subscription in handlers:
                try:
                    if subscription.try_handle(event):
                        has_handlers = True
                except Exception as ex:
                    self._log(
                        f"Error in handler for {event_type.__name__}: {ex}",
                        level="error",
                    )

        self._log(f"Published {event_type.__name__} (handled: {has_handlers})")
        return has_handlers

    def clear_subscriptions(self, event_type: type) -> None:
        """Clear all subscriptions for a specific event type."""
        with self._lock:
            sub_list = self._subscriptions.pop(event_type, None)
            if sub_list is not None:
                for sub in sub_list.get_handlers():
                    self._subscription_index.pop(sub.id, None)
        self._log(f"Cleared subscriptions for {event_type.__name__}")

    def clear_all_subscriptions(self) -> None:
        """Clear all subscriptions for all event types."""
        with self._lock:
            self._subscriptions.clear()
            self._subscription_index.clear()
        self._log("Cleared all subscriptions")

    def get_subscriber_count(self, event_type: type) -> int:
        """Get the number of subscribers for a given event type."""
        sub_list = self._subscriptions.get(event_type)
        return sub_list.count if sub_list is not None else 0

    def _log(self, message: str, level: str = "debug") -> None:
        if self._enable_logging:
            log_fn = getattr(logger, level, logger.debug)
            log_fn(f"[EventBus] {message}")


# ---------------------------------------------------------------------------
# Global singleton (matches VirabisCore static Events class)
# ---------------------------------------------------------------------------

_global_bus: Optional[EventBus] = None
_global_lock = threading.Lock()


def get_event_bus() -> EventBus:
    """Get the global EventBus singleton."""
    global _global_bus
    if _global_bus is None:
        with _global_lock:
            if _global_bus is None:
                _global_bus = EventBus()
    return _global_bus


def set_event_bus(bus: EventBus) -> None:
    """Replace the global EventBus (useful for testing)."""
    global _global_bus
    with _global_lock:
        _global_bus = bus


def reset_event_bus() -> None:
    """Reset the global EventBus (clears all subscriptions)."""
    global _global_bus
    with _global_lock:
        if _global_bus is not None:
            _global_bus.clear_all_subscriptions()
        _global_bus = None


# ---------------------------------------------------------------------------
# Game Events (ported from VirabisCore GameEvents.cs)
# ---------------------------------------------------------------------------

class EntityCreatedEvent(Event):
    """Raised when an entity is created."""

    def __init__(self, entity, source=None):
        super().__init__(source=source)
        self.entity = entity


class EntityDestroyingEvent(Event):
    """Raised when an entity is about to be destroyed."""

    def __init__(self, entity, source=None):
        super().__init__(source=source)
        self.entity = entity


class DamageTakenEvent(Event):
    """Raised when an entity takes damage."""

    def __init__(
        self,
        target,
        attacker,
        damage: float,
        previous_health: float,
        current_health: float,
        is_critical: bool = False,
        is_stealth: bool = False,
        source=None,
    ):
        super().__init__(source=source)
        self.target = target
        self.attacker = attacker
        self.damage = damage
        self.previous_health = previous_health
        self.current_health = current_health
        self.is_critical = is_critical
        self.is_stealth = is_stealth

    @property
    def health_percent(self) -> float:
        max_hp = getattr(self.target, "max_hp", 0)
        return self.current_health / max_hp if max_hp > 0 else 0.0


class HealedEvent(Event):
    """Raised when an entity is healed."""

    def __init__(
        self, target, healer, amount: float,
        previous_health: float, current_health: float, source=None,
    ):
        super().__init__(source=source)
        self.target = target
        self.healer = healer
        self.amount = amount
        self.previous_health = previous_health
        self.current_health = current_health


class DeathEvent(Event):
    """Raised when an entity dies."""

    def __init__(
        self, entity, killer=None, final_damage: float = 0,
        was_critical: bool = False, source=None,
    ):
        super().__init__(source=source)
        self.entity = entity
        self.killer = killer
        self.final_damage = final_damage
        self.was_critical = was_critical


class RevivedEvent(Event):
    """Raised when an entity revives."""

    def __init__(self, entity, health_percent: float = 1.0, source=None):
        super().__init__(source=source)
        self.entity = entity
        self.health_percent = health_percent


class StateChangedEvent(Event):
    """Raised when an entity's state changes."""

    def __init__(
        self, entity, previous_state: str, new_state: str,
        previous_state_id: int = 0, new_state_id: int = 0,
        time_in_previous_state: float = 0.0, source=None,
    ):
        super().__init__(source=source)
        self.entity = entity
        self.previous_state = previous_state
        self.new_state = new_state
        self.previous_state_id = previous_state_id
        self.new_state_id = new_state_id
        self.time_in_previous_state = time_in_previous_state


class PreDamageEvent(Event):
    """
    Raised before damage is calculated.
    Handlers can modify base_damage, damage_multiplier, or cancel the event.
    """

    def __init__(self, attacker, target, base_damage: float, source=None):
        super().__init__(source=source)
        self.attacker = attacker
        self.target = target
        self.base_damage = base_damage
        self.damage_multiplier: float = 1.0
        self._is_cancelled = False

    @property
    def is_cancelled(self) -> bool:
        return self._is_cancelled

    def cancel(self) -> None:
        self._is_cancelled = True

    @property
    def final_damage(self) -> float:
        return self.base_damage * self.damage_multiplier


class AttackHitEvent(Event):
    """Raised when an attack hits (before damage is applied)."""

    def __init__(
        self, attacker, target, raw_damage: float,
        is_critical: bool = False, is_stealth: bool = False, source=None,
    ):
        super().__init__(source=source)
        self.attacker = attacker
        self.target = target
        self.raw_damage = raw_damage
        self.is_critical = is_critical
        self.is_stealth = is_stealth


class StatChangedEvent(Event):
    """Raised when an entity's stat changes."""

    def __init__(
        self, entity, stat_name: str, old_value: float,
        new_value: float, source=None,
    ):
        super().__init__(source=source)
        self.entity = entity
        self.stat_name = stat_name
        self.old_value = old_value
        self.new_value = new_value
