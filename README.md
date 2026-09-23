# Arkanoid

Классический 2D Arkanoid на Unity. Проект создаётся как законченное портфолио-приложение с понятной архитектурой, автоматическими тестами и обоснованным применением VContainer, Addressables, Decorator, Composite и Chain of Responsibility.

Проект находится на начальном этапе разработки. Подробный порядок работ и текущий статус находятся в [Development Plan](<Development Plan/README.md>).

## Требования

- Unity `6000.3.18f1`;
- Addressables `2.9.1`;
- целевые платформы первой версии — Android и iOS, портретная ориентация.

## Запуск в Unity Editor

1. Добавить корневую папку проекта в Unity Hub.
2. Открыть проект в Unity `6000.3.18f1` и дождаться импорта assets и компиляции scripts.
3. Открыть сцену `Assets/Content/Scenes/Bootstrap.unity`.
4. Нажать Play.

## Проверка

В Unity Test Runner выбрать PlayMode и запустить `BootstrapSmokeTests`. Тест проверяет переход между сценами, сроки жизни VContainer scopes и инициализацию Addressables и DOTween. Обычный запуск `Bootstrap` через Play проверяет автоматический старт отдельно: Test Runner создаёт корневой scope до начала тела теста.
