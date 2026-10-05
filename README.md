# Arkanoid

Классический 2D Arkanoid на Unity. Проект создаётся как законченное портфолио-приложение с понятной архитектурой, автоматическими тестами и обоснованным применением VContainer, Addressables, Decorator, Composite и Chain of Responsibility.

Playable slice и композиция через VContainer завершены; текущая работа — расчёт счёта в этапе 5. Подробный порядок работ и статус находятся в [Development Plan](<Development Plan/README.md>).

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

В Unity Test Runner выбрать PlayMode и запустить `BootstrapSmokeTests`, `LevelCompletionSmokeTests` и `LifeLossSmokeTests`. Первый тест проверяет переход между сценами, сроки жизни VContainer scopes, общий счёт и модификаторы в дочерних scopes, сброс счёта при полном restart, разрешение основных моделей и entry points, а также инициализацию Addressables и DOTween. Остальные проверяют победу с одним начислением за уничтожение и начислением последнего блока до завершения уровня, а также три потери жизни со сбросом комбо и сохранением total. Обычный запуск `Bootstrap` через Play проверяет автоматический старт отдельно: Test Runner создаёт корневой scope до начала тела теста.

В EditMode `ScoreServiceTests` проверяет накопление рассчитанных очков, публикацию обновлённого total, нулевое начисление и сохранение счёта при ошибках. Правила base, combo и double проверяются отдельными тестами калькуляторов и композиции.

Для HUD назначить новый текст TextMeshPro в поле `Combo Text` компонента `GameplayHudView`; шаги находятся в [P5.9](<Development Plan/Stages/05-score-decorator.md#назначение-inspector-пользователем>). При старте видны `Score: 0` и `Combo: 0`, уничтожение увеличивает оба значения, потеря жизни сбрасывает только комбо, Restart начинает счёт заново. `ComboModelTests` также проверяет события изменения комбо.

PlayMode-класс `GameplayScoreIntegrationTests` проверяет реальную серию уничтожений, потерю жизни и следующий ×1, а также отклонение отрицательного входа в `ScoreService` из gameplay scope с последующим корректным начислением. Для сценариев нужны минимум три активных `Basic` в gameplay. Отдельные правила калькуляторов этими тестами не дублируются.
