# Контрольные точки и продолжение проекта

[К индексу плана](README.md)

## Milestones

| Milestone | После этапа | Демонстрируемый результат |
|---|---:|---|
| Foundation | 0 | Чистый проект, scopes, tests, зависимости |
| Mechanics prototype | 2 | Платформа и качественный отскок мяча |
| Playable MVP | 3 | Победа, поражение, жизни, простые блоки |
| Architecture pass | 4 | Явные lifetimes и DI через VContainer |
| Patterns complete | 8 | Decorator, Chain of Responsibility и Composite применены и обоснованы реальными механиками |
| Content complete | 10 | Три Addressable-уровня и полный flow |
| Release candidate | 12 | Тесты, стабильность, отсутствие утечек |
| Portfolio release | 13 | Build, README и видео |

Ориентир для одного опытного разработчика при готовых простых assets: 8–12 полноценных рабочих дней. Если создание визуала и звука выполняется с нуля, лучше планировать ещё 2–4 дня. Срок не должен сокращаться за счёт отскоков мяча, тестов загрузки или release documentation — это наиболее показательные части работы.

## Порядок коммитов

Рекомендуется один небольшой осмысленный commit на завершённую задачу или компактную группу задач:

```text
chore: initialize project structure and test assemblies
feat: add constrained paddle movement
feat: add stable ball launch and paddle bounce
feat: complete minimal game session flow
refactor: compose gameplay services with VContainer
feat: add composable score modifiers
feat: process brick hits through handler chain
feat: apply grouped bonus effects
feat: load level content with Addressables
test: cover gameplay rules and level lifecycle
docs: add architecture and verification guide
```

Не объединять весь проект в один итоговый commit: история изменений сама является частью демонстрации инженерного процесса.

## Идеи только после Release Gate

- управление платформой мышью и touch;
- multi-ball с корректным условием потери жизни;
- временные эффекты с duration/stacking policy;
- object pooling для pickups/particles;
- level selection и локальный progress save;
- accessibility options: reduced motion, volume controls, high-contrast palette;
- WebGL build;
- editor tooling для проверки LevelDefinition.

Каждое расширение начинается с отдельного мини-ТЗ, acceptance criteria и оценки влияния на существующие правила.
