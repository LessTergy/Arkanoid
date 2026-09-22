# Этап 11. Feedback, DOTween и звук

[Назад: этап 10](10-game-flow-ui.md) · [К индексу](../README.md) · [Далее: этап 12](12-quality.md)

**Результат:** каждое важное событие читается визуально и на слух, но presentation не управляет правилами.

## Задачи

- [ ] `P11.1` Составить таблицу feedback events: paddle hit, brick damaged, shield broken, brick destroyed, pickup spawned/collected, life lost, level complete.
- [ ] `P11.2` Добавить короткий scale/color tween при попадании в блок.
- [ ] `P11.3` Добавить отдельный shield-break feedback, визуально отличимый от damage.
- [ ] `P11.4` Добавить pickup spawn/collect tween и HUD punch при изменении score/lives.
- [ ] `P11.5` Добавить частицы только для событий, где они улучшают читаемость.
- [ ] `P11.6` Создать `AudioService` и простой набор SFX; не вызывать `AudioSource` из Core.
- [ ] `P11.7` Связать lifecycle твинов с владельцем через `SetLink` или явный `Kill`. Для `Sequence` управлять самой sequence.
- [ ] `P11.8` Не использовать `SetAutoKill(false)` без фактического повторного использования.
- [ ] `P11.9` Если tween ожидается через UniTask, использовать корректную семантику ожидания завершения и cancellation; не делать `async void`.
- [ ] `P11.10` Проверить, что уничтожение уровня во время анимаций не создаёт callbacks в уничтоженные objects.

## Gate 11

- [ ] Игрок различает damage, shield и destruction без чтения Console.
- [ ] Отключение presentation/звука не ломает gameplay result.
- [ ] При смене уровня нет DOTween warnings и обращений к destroyed objects.
