# Virabis Test System

> "Bir test sistemi yazıyoruz, binlerce test değil." - Prof. Zeynep İlaç (Eczane Sahibi)

## Hızlı Başlangıç

### Basit Test (1 satır)

```csharp
[Fact] public void BasicDamage() => Test.Hit(10).ShouldDeal(10);
```

### Fluent Test

```csharp
[Fact]
public void CritDamage() =>
    Test.Damage()
        .WithDamage(10)
        .WithCrit(multiplier: 2f)
        .ShouldDeal(20);
```

### Property Test

```csharp
[Fact, Trait("Category", "Property")]
public void Property_DamageNeverNegative() =>
    Property.Damage_NeverNegative();
```

## TestData Fabrikası

```csharp
// Entity oluşturma
var player = TestData.Player();
var enemy = TestData.Enemy(health: 50);
var boss = TestData.Boss(health: 1000);
var aware = TestData.AwareEnemy();

// Player varyasyonları
var critPlayer = TestData.PlayerWithCrit(critMultiplier: 3f);

// HitContext oluşturma
var hit = TestData.Hit(player, enemy, damage: 10);
var stealth = TestData.StealthHit(damage: 15);
var multi = TestData.MultiHit(damagePerHit: 5, hitCount: 8);
```

## Test Builder

```csharp
// Temel
Test.Hit(10).ShouldDeal(10);

// Fluent API
Test.Damage()
    .AttackerIs(player)
    .TargetIs(enemy)
    .WithDamage(10)
    .WithHitCount(8)
    .AsStealth()
    .WithCrit(multiplier: 2.5f)
    .ShouldDeal(200);

// Assertion yöntemleri
.ShouldDeal(10)           // Tam hasar
.ShouldDealZero()         // Sıfır hasar
.ShouldLeaveHealth(90)    // Hedef canı
.ShouldKill()             // Öldürür
.ShouldNotKill()          // Öldürmez
.ShouldDealBetween(40, 100) // Aralık
```

## Property Testler

```csharp
// Built-in property testler
Property.Damage_NeverNegative();
Property.Health_NeverBelowZero();
Property.FriendlyFire_AlwaysZero();
Property.MultiHit_ScalesLinearly();
Property.Crit_IncreasesDamage();

// Tümünü çalıştır
Property.RunAll();
```

## Karşılaştırma: Eski vs Yeni

### Eski (23 satır)

```csharp
[Fact]
public void ProcessHit_BasicDamage_AppliesCorrectly()
{
    // Arrange
    var random = new DeterministicRandom(0.5);
    var system = new DamageSystem(random);
    var attacker = new Entity(TeamId.Player, maxHealth: 100);
    attacker.Stats.CritChance = 0.0f;
    var target = new Entity(TeamId.Enemy, maxHealth: 100);
    var context = new HitContext(attacker, target, baseDamage: 10);

    // Act
    float damage = system.ProcessHit(context);

    // Assert
    Assert.Equal(10f, damage, precision: 2);
    Assert.Equal(90f, target.Health.CurrentHealth, precision: 2);
}
```

### Yeni (3 satır)

```csharp
[Fact] public void BasicDamage() =>
    Test.Hit(10).ShouldDeal(10);
```

## Dosya Yapısı

```
TestSystem/
├── TestData.cs          # Fabrika metotları
├── TestBuilder.cs       # Fluent API
├── PropertyTest.cs      # Property test yardımcıları
└── DeterministicRandom.cs # Test için sabit random
```

## Uzman Tavsiyeleri

| Uzman | Tavsiye |
|--------|---------|
| Dr. Ahmet Teşhis | Test data'yı merkezi yap |
| Prof. Zeynep İlaç | Minimal doz, maksimum etki |
| Dr. Mehmet Ameliyat | Fazlalığı kes |
| Uzm. Elif Reçete | Tarif gibi basit olmalı |
| Dr. Can Kalp | Modüler yapı |
| Doç. Ayşe Temiz | Otomatik temizleme |
| Uzm. Ali Beyin | En kısa yol |
| Dr. Fatma Göz | Büyük resmi gör |
| Prof. Osman Hasta | Kullanıcı dostu |
| Uzm. Zeynep Röntgen | Gizli bugları bul |
