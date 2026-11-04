# AI Harness (GoatDescent.AgentHarness)

Слой, через который ИИ-агент управляет игрой, снимает её состояние и проверяет собственные изменения.
Это не второй набор игровых правил: команды идут через тот же remote-API ввода, которым пользуется сеть
(`GoatController.SetRemoteControl/SetRemoteMove/SetRemoteMountainInput`, `GoatJumpController.NetworkSpaceDown`,
`GoatCliffGrip.SetRemoteInput`, `GoatInteraction.TryShove/TryGrab`, `RespawnController`).

## Запуск

```
"/mnt/c/Program Files/Unity/Hub/Editor/6000.3.22f1/Editor/Unity.exe" -batchmode \
  -executeMethod GoatDescent.EditorTools.AgentHarnessCI.Run \
  -projectPath "C:\Users\Вадим\Unity Projects\GOAT" \
  -logFile "C:\Users\Вадим\Unity Projects\GOAT\harness.log"
```

`-quit` не указывать: метод сам входит в Play Mode и завершает редактор через `EditorApplication.Exit`
(0 — всё зелёное, 1 — есть провалы). Один запуск редактора прогоняет все сценарии.

Переменные окружения (необязательно): `GOAT_HARNESS_SCENARIOS`, `GOAT_HARNESS_SCENE`, `GOAT_HARNESS_OUT`.

Результат: `Library/AgentHarness/report.json` + строки `HARNESS_*` в логе.

## Формат сценария

```json
{
  "id": "platform:12",
  "seed": 7,
  "timeout": 30,
  "tolerance": 0.25,
  "repeat": false,
  "commands": [ { "type": "walk", "x": 0, "y": 0, "z": 1, "seconds": 0.35 } ],
  "checks": [ { "type": "progress", "op": ">=", "value": 0.1 } ]
}
```

`id` одновременно задаёт стартовую точку: `summit`/`pair_platform`, `pair_shove`, `pair_rescue`, `gap`,
`fragile`, `rock`, `platform:<индекс>`, `jump:<индекс>` — всё сводится к штатным телепортам и `LocalGoatPair.ResetScenario`.

Команды: `walk` (x,y,z + seconds), `stop`, `wait`, `brake` (value 0/1), `super` (value 0/1),
`grip` (value, x=вбок, y=вверх), `jump` (seconds — удержание заряда), `shove`, `grab`, `release`,
`respawn`, `teleport` (value — индекс опоры). Поле `target`: `a` (по умолчанию) или `b`.

Проверки (`type`, `op` = `>=`/`>`/`<=`/`<`/`==`/`!=`, `value`): `alive`, `dead`, `grounded`, `gripping`,
`linked`, `completed`, `progress`, `progress_gain`, `speed`, `max_speed`, `slope`, `hooves`, `position_y`,
`falls`, `distance`, `airborne`, `fell`, `slipped`, `samples`.

`repeat: true` прогоняет сценарий дважды и сравнивает финальные позиции с допуском `tolerance`
(по умолчанию 0.35 м) — это проверка воспроизводимости.

## Слои

- `GameCommand.cs`, `GameSnapshot.cs`, `ScenarioReport.cs` — DTO (сериализуются `JsonUtility`).
- `IAgentGame.cs` — контракт `Ready/Reset/Execute/Snapshot`.
- `GoatAgentAdapter.cs` — адаптер к реальным компонентам козла.
- `ScenarioRunner.cs` — пошаговый прогон (время инжектируется, поэтому тестируется без Play Mode).
- `HarnessEvaluator.cs` — метрики, операторы сравнения, сборка результата.
- `Scenarios/default.json` — стартовый набор: обычное действие, прыжок, повторяемость.
- `Assets/GoatDescent/Editor/AgentHarnessCI.cs` — batch-точка входа.
- `Assets/Tests/EditMode/AgentHarness/` — быстрые EditMode-тесты (без физики).
