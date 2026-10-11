# GOAT — спуск по скале

## Документация

Карта проекта — как устроены гора, трасса, лаборатория столбов и куда крутить настройки:
**[Docs/Architecture.md](Docs/Architecture.md)**. Исторические заметки: `Docs/ProjectState.md` (снимок до рефакторинга, пути устарели).

## Быстрый запуск

Откройте сцену `Assets/GoatDescent/Scenes/GoatPlayground.unity` в Unity и нажмите Play. Это единственная сцена, включённая в Build Settings; весь мир строится кодом при запуске.

Настройки горы и трассы — окно **Tools → GOAT → Mountain Editor** (сохраняются в EditorPrefs, есть живое превью).

## Основная гора

Модули игровой горы в `Assets/GoatDescent/`:

- `Terrain/` — ядро: генератор горы, трасса (`MountainRoute`), высотное поле, эрозия, материалы, шейдеры (`Terrain/Shaders/`).
- `Bootstrap/` — `MountainSceneBuilder`: собирает сцену, пересобирает рельеф методом `RebuildTerrain`.
- `Route/` и `Decor/` — пропсы трассы (лёд, осыпающиеся платформы, ветер) и растительность.
- `Player/` — контроллер козы, сцепление, баланс, камера, прогресс маршрута.

Трасса генерируется первой, рельеф строится вокруг неё: под площадками — полки, между ними — провалы.

## Дополнительные сцены генератора

Модуль `Assets/GoatDescent/Vista/` — лаборатория (не в Build Settings): виста «туманные столбы» и биомный генератор мира. Для просмотра откройте `ProceduralWorldMilestone.unity`, `ProceduralWorldGroundcoverPreview.unity` или `PineTreeArtPreview.unity` из `Assets/GoatDescent/Vista/Scenes/`. Полигон одного столба: `Assets/GoatDescent/Scenes/Testing/SinglePillarPlayground.unity`. Инструмент: **Tools → Procedural World → Open Generator**.

## Модели мира

Исходный Blender-пак моделей создаётся командой из корня проекта:

    blender --background --python Tools/Blender/generate_world_asset_pack.py

Результат кладётся в `Assets/Art/Generated/` (каталог в `.gitignore`, пересоздаётся скриптом).
