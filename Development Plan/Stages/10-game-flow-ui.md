# Этап 10. Полный игровой flow и UI

[Назад: этап 9](09-addressable-levels.md) · [К индексу](../README.md) · [Далее: этап 11](11-feedback-audio.md)

**Результат:** приложение воспринимается как законченная небольшая игра, а не тестовая сцена.

## Задачи

- [ ] `P10.1` Реализовать state flow: `Boot → MainMenu → Loading → Ready → Playing → Paused/LifeLost → LevelComplete → Loading/RunComplete → MainMenu`.
- [ ] `P10.2` Сделать Main Menu: Play, Quit; в Editor Quit не генерирует ошибку.
- [ ] `P10.3` Доработать HUD: score, lives, level, combo, активные modifiers.
- [ ] `P10.4` Сделать overlays: Ready/Launch, Pause, Level Complete, Game Over, Victory, Loading Error.
- [ ] `P10.5` Настроить navigation и focus для keyboard/gamepad UI.
- [ ] `P10.6` Гарантировать, что UI только отображает state и отправляет commands, но не определяет правила победы/поражения.
- [ ] `P10.7` Проверить повторные входы в gameplay: не остаются старые subscriptions, tweens, cancellation sources и session state.
- [ ] `P10.8` Добавить PlayMode smoke-test полного flow хотя бы через один минимальный test level.

## Gate 10

- [ ] Игру можно пройти от запуска приложения до Victory и вернуться в меню.
- [ ] Все экраны доступны без мыши.
- [ ] Pause/restart/menu работают из каждого разрешённого состояния.
