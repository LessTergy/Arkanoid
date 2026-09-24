# Этап 1. Изолированная механика платформы

[Назад: этап 0](00-foundation.md) · [К индексу](../README.md) · [Далее: этап 2](02-ball.md)

**Результат:** платформа предсказуемо двигается в ограниченном поле без мяча, блоков и игрового состояния.

## Задачи

- [x] `P1.1` Настроить orthographic camera и игровую область под портретные 9:16; определить единицы поля и безопасные границы.
- [x] `P1.2` Создать слои `Paddle`, `Ball`, `Brick`, `Wall`, `Pickup`, `DeathZone` и collision matrix с минимально нужными взаимодействиями.
- [x] `P1.3` Создать prefab `Paddle` с `Rigidbody2D`, collider и простым SpriteRenderer.
- [x] `P1.4` Создать `PaddleConfig` со speed и width; границы движения получать из `PlayfieldCamera.WorldBounds`.
- [x] `P1.5` Реализовать `IPlayerInput` adapter над Input System для сенсорного ввода с keyboard/gamepad fallback в Editor. Gameplay-код не должен читать устройства напрямую.
- [x] `P1.6` Реализовать `PaddleMovement`: получать намерение игрока через `IPlayerInput`, двигаться в physics tick и ограничивать центр платформы границами поля с учётом половины её ширины.
- [ ] `P1.7` Проверить одинаковую скорость при разных frame rates и отсутствие дрожания у границ.
- [ ] `P1.8` Добавить тест чистой функции расчёта/clamp новой позиции.

## Геометрия поля

- Референс для арта и UI — `1080 × 1920 px` (9:16). Игровые спрайты импортируются с `Pixels Per Unit = 100`; реальное разрешение устройства не меняет gameplay-координаты.
- Логическое поле имеет размер `10.8 × 19.2` Unity units. Камера находится в `(0, 1, -10)` и при 9:16 использует `Orthographic Size = 1920 / (2 × 100) = 9.6`.
- Границы поля в мировых координатах: `x ∈ [-5.4, 5.4]`, `y ∈ [-8.6, 10.6]`. Для центра платформы допустимы `x ∈ [left + width/2, right - width/2]`.
- `PlayfieldCamera` увеличивает видимую область при другом соотношении сторон, сохраняя логическое поле полностью видимым. Дополнительное пространство камеры не расширяет gameplay-границы.
- Вырезы экрана и системные панели учитываются при размещении UI через `Screen.safeArea` на этапе 10; границы движения платформы задаёт логическое поле.
- `PaddleConfig` хранится в `Assets/Content/Data/Paddle`: начальные `speed = 8` units/s и `width = 2` units. Ширина соответствует текущему `BoxCollider2D`; `PaddleMovement` использует `PlayfieldCamera.WorldBounds` и половину `width` для ограничения центра.

## Ввод платформы

- `IPlayerInput.Move` возвращает мировую X-координату пальца при удержании касания либо направление `[-1, 1]` от клавиатуры/геймпада. `PaddleMovement` перемещается к цели с ограничением скорости из `PaddleConfig` в `FixedUpdate` через `Rigidbody2D.MovePosition`.
- `InputSystem_Actions` содержит touch actions `TouchPosition` и `TouchPress`; первое касание также даёт `Launch`. `Pause` пока доступен через keyboard/gamepad; экранная кнопка появится с UI на этапе 10.
- `InputSystemPlayerInput` находится на `GameplayLifetimeScope`, зарегистрирован как `IPlayerInput` и получает камеру и ссылки `InputActionReference` через Inspector. `PaddleMovement`, `PaddleConfig` и `PlayfieldCamera` зарегистрированы через прямые ссылки в `GameplayLifetimeScope`.

## Слои и контакты 2D Physics

- Разрешённые пары: `Ball–Paddle`, `Ball–Brick`, `Ball–Wall`, `Ball–DeathZone`, `Pickup–Paddle`, `Pickup–DeathZone`.
- `Pickup` и `DeathZone` используют trigger-коллайдеры. `Paddle` ограничивается границами поля программно, поэтому контакт с `Wall` ему не нужен.
- Все остальные пары с участием новых слоёв отключены; взаимодействия между стандартными слоями Unity сохранены. При создании соответствующих prefab и объектов назначать им одноимённые слои.

## Gate 1

- [ ] Платформа управляется касанием; keyboard/gamepad доступны для проверки в Editor.
- [ ] Не выходит за левую и правую границы.
- [ ] Поведение не зависит заметно от render frame rate.
