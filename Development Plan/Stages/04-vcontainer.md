# Этап 4. Стабилизировать композицию через VContainer

[Назад: этап 3](03-playable-slice.md) · [К индексу](../README.md) · [Далее: этап 5](05-score-decorator.md)

**Результат:** работающий vertical slice собран через понятные scopes и явные зависимости.

## Задачи

- [ ] `P4.1` Выписать фактические lifetime всех созданных объектов: app, gameplay session, level, scene object.
- [ ] `P4.2` Зарегистрировать app services в `AppLifetimeScope` с обоснованными `Singleton`/`Scoped` lifetimes.
- [ ] `P4.3` Зарегистрировать session services и scene components в `GameplayLifetimeScope`.
- [ ] `P4.4` Перенести orchestration из случайных `Start/Awake` в один-два entry point/presenter-класса.
- [ ] `P4.5` Внедрять обычные C#-зависимости через constructor; scene MonoBehaviour — через регистрацию component и injection method только при необходимости.
- [ ] `P4.6` Создать узкие factories для динамических ball/brick/pickup instances. Скрыть `IObjectResolver` внутри factory implementations.
- [ ] `P4.7` Убедиться, что gameplay-классы не вызывают `Resolve`, `Find*`, `GameObject.Find` и не используют статические singleton instances.
- [ ] `P4.8` Добавить composition smoke-test или PlayMode-test, который строит scope и разрешает основные entry points.

## Gate 4

- [ ] Поведение Gate 3 не изменилось.
- [ ] По composition root видно, где и как создаётся каждая крупная система.
- [ ] Уничтожение gameplay scope освобождает session objects и отписки.
