# GOAT — спуск по скале

## Быстрый запуск

Откройте сцену Assets/GoatDescent/Scenes/GoatPlayground.unity в Unity и нажмите Play. Это единственная сцена, включённая в Build Settings.

## Основная гора

Игровая гора, маршрут и декор находятся в Assets/GoatDescent/Scripts/World. Компонент MountainSceneBuilder пересобирает рельеф и декор методом RebuildTerrain. Параметры можно менять в окне Tools → GOAT → Mountain Editor; окно хранит настройки в EditorPrefs.

Механики козы находятся в Assets/GoatDescent/Scripts/Player. Сцена использует процедурно собираемую гору и игровые системы движения, сцепления с поверхностью и баланса на склоне.

## Дополнительные сцены генератора

Модуль Assets/ProceduralWorld содержит отдельный генератор биомного ландшафта и сцены предпросмотра. Для просмотра откройте ProceduralWorldMilestone.unity, ProceduralWorldGroundcoverPreview.unity или PineTreeArtPreview.unity из Assets/ProceduralWorld/Scenes. Эти сцены не включены в Build Settings.

## Модели мира

Исходный Blender-пак моделей создаётся командой из корня проекта:

    blender --background --python Tools/Blender/generate_world_asset_pack.py
