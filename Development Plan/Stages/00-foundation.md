# Этап 0. Требования и основание проекта

[К индексу](../README.md) · [Scope](../SCOPE.md) · [Архитектура](../ARCHITECTURE.md) · [Далее: этап 1](01-paddle.md)

**Результат:** проект открывается без ошибок, имеет понятную структуру, зависимости и минимальный composition root.

## Задачи

- [x] `P0.1` Подтвердить и зафиксировать решения: текущая платформа, управление, число уровней, визуальный scope и обязательный набор паттернов.
- [x] `P0.2` Создать короткий корневой `README.md` проекта с целью, версией Unity и инструкцией запуска; расширять его по мере разработки.
- [x] `P0.3` Исправить Player Settings `productName` с `Arcanoid` на `Arkanoid`. Не переименовывать папку проекта и solution без отдельной необходимости.
- [x] `P0.4` Создать структуру `Assets/Content` из архитектурного документа и перенести собственные assets из template-папок, сохраняя `.meta`.
- [x] `P0.5` Создать asmdef-файлы `Arkanoid.Core`, `Arkanoid.Runtime`, `Arkanoid.Tests.EditMode`, `Arkanoid.Tests.PlayMode` и проверить направление references.
- [x] `P0.6` Удалить из будущего gameplay Input Actions лишний template-набор. Создать actions: `Move`, `Launch`, `Pause`, `Confirm`, `Cancel`.
- [x] `P0.7` Настроить keyboard и gamepad bindings. Не добавлять touch bindings до решения о mobile scope.
- [x] `P0.8` Проверить DOTween Setup, активные модули Sprite/UI/Physics2D и compile symbols. Не изменять сторонние исходники.
- [x] `P0.9` Установить совместимую с Unity 6.3 версию Addressables через Package Manager и зафиксировать фактическую версию в корневом README проекта.
- [x] `P0.10` Создать пустые `Bootstrap` и `Gameplay` scenes; добавить `Bootstrap` первой сценой Build Settings.
- [x] `P0.11` Создать `AppLifetimeScope` и `GameplayLifetimeScope` с одной простой тестовой регистрацией; проверить создание и disposal scope без ошибок.
- [x] `P0.12` Добавить один проходящий EditMode smoke-test, чтобы проверить корректность test assemblies.
- [ ] `P0.13` Создать prefab `AppLifetimeScope` и назначить его Project root scope через `VContainerSettings`; убрать scene-instance из `Bootstrap`. Зарегистрировать prefab `SceneNavigator` с сериализуемыми `SceneReference` на `Bootstrap` и `Gameplay` и один раз загрузить `Gameplay` в режиме `Single`. Проверить в Unity Editor, что app scope переживает смену сцены, а `GameplayLifetimeScope` автоматически становится дочерним.
- [ ] `P0.14` Добавить PlayMode smoke-test запуска из `Bootstrap`: дождаться загрузки `Gameplay`, проверить наличие обоих scopes, успешную инициализацию Addressables и DOTween и отсутствие ошибок при завершении.
- [ ] `P0.15` Сделать baseline commit/tag, от которого можно сравнивать последующие изменения.

## Gate 0

- [ ] Проект компилируется с нулём ошибок.
- [ ] `Bootstrap` запускает `Gameplay`.
- [ ] Test Runner видит EditMode и PlayMode assemblies.
- [ ] Addressables и DOTween проходят базовую инициализацию.

После прохождения Gate обновить статус в [индексе](../README.md) и перейти к [этапу 1](01-paddle.md).
