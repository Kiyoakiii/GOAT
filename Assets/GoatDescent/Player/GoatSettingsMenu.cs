using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor;

namespace GoatDescent
{
    [DefaultExecutionOrder(-300)]
    public sealed class GoatSettingsMenu : MonoBehaviour
    {
        public static bool IsOpen { get; private set; }
        public static bool BlocksInput => IsOpen || closedFrame == Time.frameCount;
        private static int closedFrame = -1;
        private UIDocument document;
        private PanelSettings panel;
        private VisualElement overlay;
        private Label viewMode;
        private Button changeView;
        private readonly List<Action> refreshGraphics = new List<Action>();
        private enum Page { Home, Settings, Controls, Network }
        private Page page;
        private VisualElement homePage, settingsPage, controlsPage, networkPage, menuPanel, frameLimitRow;
        private Button back, createGame, joinGame, leaveGame;
        private Label pageTitle, networkStatus, roomCode;
        private VisualElement roomInvite;
        private Button copyRoomCode;
        private float copiedCodeUntil;
        private TextField networkAddress;
        private GoatNetworkMenuSession networkSession;
        private Label frameLimitNote;
        private bool paused;
        private float previousTimeScale;
        private CursorLockMode previousLock;
        private bool previousVisible;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { IsOpen = false; closedFrame = -1; }

        private void Start()
        {
            panel = ScriptableObject.CreateInstance<PanelSettings>();
            panel.name = "Goat settings";
            panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panel.referenceResolution = new Vector2Int(1280, 720);
            panel.match = .5f;
            panel.sortingOrder = 1100;
            var node = new GameObject("Settings and controls overlay");
            node.transform.SetParent(transform, false);
            document = node.AddComponent<UIDocument>();
            document.panelSettings = panel;
            document.visualTreeAsset = Resources.Load<VisualTreeAsset>("GoatSettingsMenu");
            var root = document.rootVisualElement;
            root.styleSheets.Add(Resources.Load<StyleSheet>("GoatSettingsMenu"));
            root.AddToClassList("settings-document");
            foreach (var child in root.Children())
                if (child is TemplateContainer) child.AddToClassList("settings-template");
            root.style.unityFontDefinition = FontDefinition.FromFont(Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));
            root.pickingMode = PickingMode.Ignore;
            overlay = root.Q("settingsOverlay");
            viewMode = root.Q<Label>("viewMode");
            changeView = root.Q<Button>("changeView");
            changeView.clicked += () => { Follow()?.TogglePerspective(); RefreshView(); };
            root.Q<Button>("resume").clicked += Close;
            root.Q<Button>("openSkins").clicked += () => { Close(); GetComponent<GoatSkinMenu>()?.Open(); };
            menuPanel = root.Q("mainPanel");
            homePage = root.Q("homePage");
            settingsPage = root.Q("settingsPage");
            controlsPage = root.Q("controlsPage");
            networkPage = root.Q("networkPage");
            pageTitle = root.Q<Label>("pageTitle");
            back = root.Q<Button>("back");
            back.clicked += () => SelectPage(Page.Home);
            root.Q<Button>("openSettings").clicked += () => SelectPage(Page.Settings);
            root.Q<Button>("openControls").clicked += () => SelectPage(Page.Controls);
            root.Q<Button>("openNetwork").clicked += () => SelectPage(Page.Network);
            root.Query<Button>().ForEach(button => { button.focusable = false; button.tabIndex = -1; });
            BuildControls(root.Q<ScrollView>("controls"));
            BuildGraphics(root.Q<ScrollView>("graphics"));
            root.Q<Button>("resetGraphics").clicked += () => { GoatGraphicsSettings.RestoreDefaults(); RefreshGraphics(); };
            BindNetwork(root);
            SelectPage(Page.Home);
        }

        private static ThirdPersonGoatCamera Follow() => Camera.main ? Camera.main.GetComponent<ThirdPersonGoatCamera>() : null;
        private void Update()
        {
            if (!document || GoatSkinMenu.IsOpen) return;
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (!IsOpen) Open();
                else if (page != Page.Home) SelectPage(Page.Home);
                else Close();
            }
            if (!IsOpen) return;
            SyncPause();
            if (Input.GetKeyDown(KeyCode.F5)) Follow()?.TogglePerspective();
            RefreshView();
            if (page == Page.Network) RefreshNetwork();
        }
        public void Open()
        {
            if (IsOpen || !document || GoatSkinMenu.IsOpen) return;
            previousLock = Cursor.lockState;
            previousVisible = Cursor.visible;
            IsOpen = true;
            SyncPause();
            LocalGoatPair.Instance?.Active?.GetComponent<GoatController>()?.ClearInput();
            overlay.RemoveFromClassList("settings-hidden");
            RefreshView();
            SelectPage(Page.Home);
            UnlockCursor();
        }
        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            closedFrame = Time.frameCount;
            GoatGraphicsSettings.Flush();
            networkAddress?.Blur();
            if (overlay != null) overlay.AddToClassList("settings-hidden");
            if (paused) { Time.timeScale = previousTimeScale; paused = false; }
            Cursor.lockState = previousLock;
            Cursor.visible = previousVisible;
        }
        private void LateUpdate() { if (IsOpen) UnlockCursor(); }
        private static void UnlockCursor() { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        private void RefreshView()
        {
            bool first = Follow() && Follow().IsFirstPerson;
            viewMode.text = first ? "От первого лица" : "От третьего лица";
            changeView.text = first ? "Переключить на третье лицо · F5" : "Переключить на первое лицо · F5";
        }
        private void SyncPause()
        {
            bool shouldPause = !LocalGoatPair.Instance || !LocalGoatPair.Instance.NetworkMode;
            if (shouldPause && !paused) { previousTimeScale = Time.timeScale; Time.timeScale = 0f; paused = true; }
            else if (!shouldPause && paused) { Time.timeScale = previousTimeScale; paused = false; }
        }
        private void SelectPage(Page value)
        {
            page = value;
            homePage.EnableInClassList("settings-hidden", page != Page.Home);
            settingsPage.EnableInClassList("settings-hidden", page != Page.Settings);
            controlsPage.EnableInClassList("settings-hidden", page != Page.Controls);
            networkPage.EnableInClassList("settings-hidden", page != Page.Network);
            back.EnableInClassList("settings-hidden", page == Page.Home);
            menuPanel.EnableInClassList("menu-home", page == Page.Home);
            menuPanel.EnableInClassList("menu-network", page == Page.Network);
            pageTitle.text = page == Page.Home ? "МЕНЮ" : page == Page.Settings ? "НАСТРОЙКИ"
                : page == Page.Controls ? "УПРАВЛЕНИЕ" : "ИГРА ПО СЕТИ";
            if (page == Page.Settings) RefreshGraphics();
            if (page == Page.Network) RefreshNetwork();
        }
        private void BindNetwork(VisualElement root)
        {
            networkStatus = root.Q<Label>("networkStatus");
            networkAddress = root.Q<TextField>("networkAddress");
            networkAddress.maxLength = 12;
            networkAddress.SetValueWithoutNotify(PlayerPrefs.GetString("Goat.Network.RoomCode", ""));
            roomInvite = root.Q("roomInvite");
            roomCode = root.Q<Label>("roomCode");
            copyRoomCode = root.Q<Button>("copyRoomCode");
            copyRoomCode.clicked += () =>
            {
                if (!networkSession || string.IsNullOrEmpty(networkSession.RoomCode)) return;
                GUIUtility.systemCopyBuffer = networkSession.RoomCode;
                copiedCodeUntil = Time.unscaledTime + 2f;
                RefreshNetwork();
            };
            createGame = root.Q<Button>("createGame");
            joinGame = root.Q<Button>("joinGame");
            leaveGame = root.Q<Button>("leaveGame");
            createGame.clicked += () => { if (networkSession) networkSession.Host(); RefreshNetwork(); };
            joinGame.clicked += () =>
            {
                if (!networkSession) return;
                string address = networkAddress.value.Trim().Replace(" ", "").ToUpperInvariant();
                if (address.Length == 0) return;
                PlayerPrefs.SetString("Goat.Network.RoomCode", address); PlayerPrefs.Save();
                networkAddress.SetValueWithoutNotify(address);
                networkSession.Join(address); RefreshNetwork();
            };
            leaveGame.clicked += () => { if (networkSession) networkSession.Disconnect(); SyncPause(); RefreshNetwork(); };
            networkAddress.RegisterValueChangedCallback(_ => RefreshNetwork());
        }
        private void RefreshNetwork()
        {
            if (!networkSession) networkSession = FindFirstObjectByType<GoatNetworkMenuSession>();
            bool busy = networkSession && networkSession.Busy;
            networkStatus.text = networkSession ? networkSession.Status : "Сетевая игра будет доступна после загрузки уровня";
            createGame.SetEnabled(networkSession && !busy);
            joinGame.SetEnabled(networkSession && !busy && !string.IsNullOrWhiteSpace(networkAddress.value));
            networkAddress.SetEnabled(!busy);
            leaveGame.EnableInClassList("settings-hidden", !busy);
            string code = networkSession ? networkSession.RoomCode : "";
            roomInvite.EnableInClassList("settings-hidden", string.IsNullOrEmpty(code));
            roomCode.text = code;
            copyRoomCode.text = Time.unscaledTime < copiedCodeUntil ? "Код скопирован" : "Скопировать код";
        }
        private void RefreshGraphics()
        {
            foreach (var refresh in refreshGraphics) refresh();
            frameLimitRow.SetEnabled(!GoatGraphicsSettings.VSync);
            frameLimitNote.text = GoatGraphicsSettings.VSync
                ? "VSync включён: частоту кадров определяет монитор. Отключи VSync для своего лимита."
                : "Ограничение FPS действует в собранной игре. Редактор Unity управляет своей частотой кадров.";
        }
        private static VisualElement GraphicsRow(VisualElement parent, string title, string description)
        {
            var row = new VisualElement(); row.AddToClassList("graphics-row");
            var text = new VisualElement(); text.AddToClassList("graphics-copy");
            var name = new Label(title); name.AddToClassList("graphics-label"); text.Add(name);
            var note = new Label(description); note.AddToClassList("graphics-note"); text.Add(note);
            row.Add(text); parent.Add(row); return row;
        }
        private void AddSlider(VisualElement parent, string title, string description, int min, int max,
            Func<int> read, Action<int> write, string suffix)
        {
            var row = GraphicsRow(parent, title, description); row.AddToClassList("graphics-slider-row");
            var field = new VisualElement(); field.AddToClassList("graphics-range");
            var slider = new SliderInt(min, max) { name = suffix == "°" ? "firstPersonFov" : "brightness" };
            slider.AddToClassList("graphics-slider"); slider.pageSize = 1;
            var value = new Label(); value.AddToClassList("graphics-number");
            field.Add(slider); field.Add(value); row.Add(field);
            refreshGraphics.Add(() => { slider.SetValueWithoutNotify(read()); value.text = read() + suffix; });
            slider.RegisterValueChangedCallback(evt => { write(evt.newValue); value.text = read() + suffix; });
        }
        private VisualElement AddChoice(VisualElement parent, string title, string description, string[] options,
            Func<int> read, Action<int> write)
        {
            var row = GraphicsRow(parent, title, description);
            var field = new VisualElement(); field.AddToClassList("graphics-choice");
            var previous = new Button { text = "‹", tooltip = "Предыдущее значение" };
            var next = new Button { text = "›", tooltip = "Следующее значение" };
            previous.AddToClassList("graphics-arrow"); next.AddToClassList("graphics-arrow");
            var value = new Label(); value.AddToClassList("graphics-choice-value");
            field.Add(previous); field.Add(value); field.Add(next); row.Add(field);
            refreshGraphics.Add(() => value.text = options[Mathf.Clamp(read(), 0, options.Length - 1)]);
            previous.clicked += () => { write((read() + options.Length - 1) % options.Length); RefreshGraphics(); };
            next.clicked += () => { write((read() + 1) % options.Length); RefreshGraphics(); };
            return row;
        }
        private void BuildGraphics(VisualElement parent)
        {
            Section(parent, "КАМЕРА И ИЗОБРАЖЕНИЕ");
            AddSlider(parent, "Обзор от первого лица · FOV", "70° — уже, 90° — шире, 120° — максимум. Третье лицо имеет свой обзор.",
                GoatGraphicsSettings.MinFirstPersonFov, GoatGraphicsSettings.MaxFirstPersonFov,
                () => GoatGraphicsSettings.FirstPersonFov, GoatGraphicsSettings.SetFirstPersonFov, "°");
            AddSlider(parent, "Яркость", "100% — исходное изображение. Подстрой так, чтобы детали в тени были видны.",
                50, 150, () => GoatGraphicsSettings.Brightness, GoatGraphicsSettings.SetBrightness, "%");
            AddChoice(parent, "Цветокоррекция", "Художественный контраст, насыщенность, оттенок и виньетка.",
                new[] { "Выкл.", "Вкл." }, () => GoatGraphicsSettings.ColorEffects ? 1 : 0,
                value => GoatGraphicsSettings.SetColorEffects(value != 0));
            Section(parent, "КАЧЕСТВО ГРАФИКИ");
            string[] names = QualitySettings.names;
            string[] translated = { "Минимальное", "Низкое", "Среднее", "Высокое", "Очень высокое", "Ультра" };
            AddChoice(parent, "Общее качество", "Готовый набор настроек. Тени, текстуры и сглаживание можно изменить отдельно.",
                names.Length == translated.Length ? translated : names,
                () => GoatGraphicsSettings.Quality, GoatGraphicsSettings.SetQuality);
            AddChoice(parent, "Тени", "Разрешение теней. Низкое значение уменьшает нагрузку на видеокарту.",
                new[] { "Выкл.", "Низкие", "Средние", "Высокие", "Максимум" },
                () => GoatGraphicsSettings.Shadows, GoatGraphicsSettings.SetShadows);
            AddChoice(parent, "Текстуры", "Детализация поверхностей и объём видеопамяти.",
                new[] { "Полные", "½ размера", "¼ размера" },
                () => GoatGraphicsSettings.TextureLimit, GoatGraphicsSettings.SetTextureLimit);
            AddChoice(parent, "Сглаживание · MSAA", "Сглаживает края объектов. Влияет на нагрузку на видеокарту.",
                new[] { "Выкл.", "2×", "4×", "8×" },
                () => Mathf.Max(0, Array.IndexOf(GoatGraphicsSettings.AntialiasingLevels, GoatGraphicsSettings.Antialiasing)),
                value => GoatGraphicsSettings.SetAntialiasing(GoatGraphicsSettings.AntialiasingLevels[value]));
            Section(parent, "ЧАСТОТА КАДРОВ");
            AddChoice(parent, "Вертикальная синхронизация", "Убирает разрывы изображения и привязывает FPS к частоте монитора.",
                new[] { "Выкл.", "Вкл." }, () => GoatGraphicsSettings.VSync ? 1 : 0,
                value => GoatGraphicsSettings.SetVSync(value != 0));
            frameLimitRow = AddChoice(parent, "Лимит FPS", "Меньше нагрев и нагрузка при выбранном ограничении.",
                new[] { "Без лимита", "30", "60", "90", "120", "144", "165", "240" },
                () => Mathf.Max(0, Array.IndexOf(GoatGraphicsSettings.FrameLimits, GoatGraphicsSettings.FrameLimit)),
                value => GoatGraphicsSettings.SetFrameLimit(GoatGraphicsSettings.FrameLimits[value]));
            frameLimitNote = new Label(); frameLimitNote.AddToClassList("settings-note"); parent.Add(frameLimitNote);
            RefreshGraphics();
        }
        private void OnApplicationPause(bool value) { if (value) GoatGraphicsSettings.Flush(); }
        private void OnApplicationQuit() => GoatGraphicsSettings.Flush();
        private static void Section(VisualElement parent, string title)
        {
            var label = new Label(title); label.AddToClassList("settings-section"); parent.Add(label);
        }
        private static void Row(VisualElement parent, string key, string description)
        {
            var row = new VisualElement(); row.AddToClassList("settings-row");
            var binding = new Label(key); binding.AddToClassList("settings-key");
            var text = new Label(description); text.AddToClassList("settings-description");
            row.Add(binding); row.Add(text); parent.Add(row);
        }
        private static void BuildControls(VisualElement parent)
        {
            Section(parent, "ДВИЖЕНИЕ И КАМЕРА");
            Row(parent, "W A S D", "Двигаться относительно камеры. На спине напарника A / D помогают балансировать.");
            Row(parent, "Мышь", "Осматриваться и поворачивать камеру.");
            Row(parent, "Колесо мыши", "Приближать и отдалять камеру от третьего лица.");
            Row(parent, "Пробел", "Прыгнуть. Удерживай и отпусти для заряженного прыжка. Ещё одно нажатие в воздухе — двойной прыжок.");
            Row(parent, "Left Shift", "Держаться за скалу. W / S — вверх и вниз, A / D — вдоль стены.");
            Row(parent, "Shift + Пробел", "Оттолкнуться от скалы, пока держишься за неё.");
            Row(parent, "Ctrl", "Притормаживать на склоне.");
            Row(parent, "Left Alt", "Короткий усиленный упор копытами. Требуется восстановление запаса.");
            Row(parent, "F5", "Менять вид от первого и третьего лица. Выбор сохраняется.");
            Section(parent, "НАПАРНИК И СПАСЕНИЕ");
            Row(parent, "F · нажать", "Толкнуть ближайшего козла.");
            Row(parent, "F · удерживать", "Сцепиться рогами, удерживать или поймать падающего напарника. Отпусти, чтобы разомкнуть хват.");
            Row(parent, "B · обоим", "Дать копыто друг другу. Оба должны нажать B рядом: совместный хват рогами станет крепче на 10 секунд.");
            Row(parent, "K", "Залезть на спину напарника или слезть. Пробел — спрыгнуть, F — держаться пассажиру.");
            Row(parent, "Tab", "Переключиться между двумя козлами в локальной игре.");
            Section(parent, "ПРЕДМЕТЫ И ПРИЁМЫ");
            Row(parent, "I", "Открыть или закрыть сундук с припасами рядом.");
            Row(parent, "1 / 2 / 3", "Выбрать страховку / ледоруб / парашют. В открытом сундуке — взять соответствующий предмет.");
            Row(parent, "U", "Использовать выбранный предмет.");
            Row(parent, "H", "Поставить карабин и временную страховочную верёвку.");
            Row(parent, "E · у воды", "Пить 3 секунды; ещё одно нажатие отменяет действие. Чистая вода лечит до 100 HP. Загрязнённая мочой снимает 25 HP и сбрасывает шерсть: она отрастает за 10 минут. Стоять в луже безопасно.");
            Row(parent, "Ледоруб · 2, U", "При касании скалы заземляет накопленный от одиночества заряд. Полная разрядка занимает около двух секунд.");
            Row(parent, "G", "Ударить задним копытом с разворота и отогнать орла.");
            Row(parent, "E · у сундука", "Открыть сундук безумия. Если рядом также вода, E отвечает за питьё.");
            Section(parent, "АНИМАЦИИ И ДУРАЧЕСТВА");
            Row(parent, "Q", "Поесть траву.");
            Row(parent, "P", "Пописять. Попадание струи в воду загрязняет её, но само по себе не сбрасывает шерсть.");
            Row(parent, "C", "Покакать. Завершённое действие восстанавливает 10 HP.");
            Row(parent, "J", "Притвориться мёртвым / встать. Движение, Пробел или F тоже прерывают притворство.");
            Row(parent, "T", "Показать удивление.");
            Row(parent, "V", "Проиграть всю последовательность анимаций.");
            Row(parent, "Z / X", "Проиграть первую / вторую роль дуэтной анимации.");
            Section(parent, "МЕНЮ И ВОЗВРАТ");
            Row(parent, "Esc", "Открыть меню. В разделах — вернуться назад, в главном меню — продолжить игру. Локальная игра на паузе; онлайн продолжается.");
            Row(parent, "M", "Открыть или закрыть выбор скина.");
            Row(parent, "R", "Возродиться на текущей точке возврата.");
            Row(parent, "Backspace", "Вернуться на старт и сбросить контрольную точку. В локальной игре / у хоста.");
            Row(parent, "Левая кнопка мыши", "Вернуть управление камерой, когда курсор освобождён и меню закрыто.");
            var debug = new Foldout { text = "Тестовые сценарии", value = false }; debug.AddToClassList("settings-debug"); parent.Add(debug);
            Row(debug, "F6", "Вернуть пару на стартовую площадку.");
            Row(debug, "F7", "Поставить козлов у края для проверки толчка.");
            Row(debug, "F8", "Сценарий спасения напарника у края.");
            Row(debug, "F9", "Сценарий: напарник висит над пропастью.");
            Row(debug, "F10", "Перейти к хрупкому выступу.");
            Row(debug, "F11", "Перейти к сценарию с катящимся камнем.");
            Row(debug, "F12", "Вызвать атаку орла. F6–F11 доступны в локальной игре.");
        }
        private void OnDisable() => Close();
        private void OnDestroy() { Close(); if (panel) Destroy(panel); }
    }
}
