using System;
using System.IO;
using TMPro;
using UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GameDev.Editor
{
    public static class MenuSetupMenu
    {
        private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
        private const string StageSelectionScenePath = "Assets/Scenes/StageSelection.unity";
        private const string Stage1ScenePath = "Assets/Starter Assets/Sample/ThirdPersonController/Playground.unity";
        private const string Stage2ScenePath = "Assets/Starter Assets/Sample/ThirdPersonController/Playground 2.unity";

        private static TMP_FontAsset defaultFontAsset;

        [MenuItem("Tools/Setup Main Menu and Stage Selection")]
        public static void SetupAllMenuSystems()
        {
            try
            {
                EnsureDirectoryExists("Assets/Scenes");

                LoadDefaultFont();

                // 1. Build and save MainMenu.unity
                CreateMainMenuScene();

                // 2. Build and save StageSelection.unity
                CreateStageSelectionScene();

                // 3. Configure EditorBuildSettings.scenes
                ConfigureBuildSettings();

                // Return to MainMenu scene so it is active and ready in the Editor
                EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);

                Debug.Log("<color=green>[MenuSetup] Successfully created MainMenu and StageSelection scenes and configured Build Settings!</color>");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MenuSetup] Failed to set up menu systems: {ex}");
            }
        }

        private static void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                AssetDatabase.Refresh();
            }
        }

        private static void LoadDefaultFont()
        {
            defaultFontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            if (defaultFontAsset == null)
            {
                defaultFontAsset = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            }
        }

        #region Main Menu Creation

        private static void CreateMainMenuScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Camera
            var camObj = new GameObject("Main Camera");
            var cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.10f, 0.15f, 1f);
            camObj.AddComponent<AudioListener>();

            // 2. EventSystem
            CreateEventSystem();

            // 3. Canvas
            var canvasObj = CreateCanvas("MainMenuCanvas");
            var canvasRt = canvasObj.GetComponent<RectTransform>();

            // Manager
            var manager = canvasObj.AddComponent<MainMenuManager>();

            // Background
            CreateBackground(canvasRt, new Color(0.06f, 0.08f, 0.12f, 1f));

            // Game Title
            CreateText(canvasRt, "TitleText", "PRAC EXAM ADVENTURE", 64, FontStyles.Bold,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -140f), new Vector2(900f, 120f), TextAlignmentOptions.Center,
                new Color(0.95f, 0.95f, 1f, 1f));

            // Subtitle
            CreateText(canvasRt, "SubtitleText", "Third Person Platformer", 26, FontStyles.Normal,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -220f), new Vector2(600f, 50f), TextAlignmentOptions.Center,
                new Color(0.6f, 0.7f, 0.85f, 1f));

            // Button Container in center
            var btnContainer = CreateUIObject("MenuButtonsContainer", canvasRt);
            btnContainer.anchorMin = new Vector2(0.5f, 0.5f);
            btnContainer.anchorMax = new Vector2(0.5f, 0.5f);
            btnContainer.pivot = new Vector2(0.5f, 0.5f);
            btnContainer.anchoredPosition = new Vector2(0f, -50f);
            btnContainer.sizeDelta = new Vector2(360f, 300f);

            var vlg = btnContainer.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.spacing = 24f;
            vlg.childControlWidth = false;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = false;
            vlg.childForceExpandHeight = false;

            // Start Button
            var startBtn = CreateButton(btnContainer, "StartButton", "Start", new Vector2(340f, 65f),
                new Color(0.18f, 0.55f, 0.35f, 1f), new Color(0.24f, 0.70f, 0.45f, 1f), 28);

            // Option Button
            var optionBtn = CreateButton(btnContainer, "OptionButton", "Option", new Vector2(340f, 65f),
                new Color(0.25f, 0.38f, 0.58f, 1f), new Color(0.35f, 0.50f, 0.75f, 1f), 28);

            // Exit Button
            var exitBtn = CreateButton(btnContainer, "ExitButton", "Exit", new Vector2(340f, 65f),
                new Color(0.58f, 0.25f, 0.28f, 1f), new Color(0.75f, 0.32f, 0.36f, 1f), 28);

            // Options Panel
            var optionsPanel = CreateOptionsPanel(canvasRt, out var optionsBackBtn);

            // Wire SerializedFields in MainMenuManager via SerializedObject
            var so = new SerializedObject(manager);
            so.FindProperty("stageSelectionSceneName").stringValue = "StageSelection";
            so.FindProperty("optionsPanel").objectReferenceValue = optionsPanel;
            so.FindProperty("startButton").objectReferenceValue = startBtn;
            so.FindProperty("optionButton").objectReferenceValue = optionBtn;
            so.FindProperty("exitButton").objectReferenceValue = exitBtn;
            so.FindProperty("optionsBackButton").objectReferenceValue = optionsBackBtn;
            so.ApplyModifiedProperties();

            // Hook up persistent OnClick events in Inspector
            UnityEventTools.AddPersistentListener(startBtn.onClick, manager.OnStartClicked);
            UnityEventTools.AddPersistentListener(optionBtn.onClick, manager.OnOptionClicked);
            UnityEventTools.AddPersistentListener(exitBtn.onClick, manager.OnExitClicked);
            UnityEventTools.AddPersistentListener(optionsBackBtn.onClick, manager.OnCloseOptionsClicked);

            // Save Scene
            EditorSceneManager.SaveScene(scene, MainMenuScenePath);
            Debug.Log($"[MenuSetup] Saved scene: {MainMenuScenePath}");
        }

        private static GameObject CreateOptionsPanel(RectTransform parent, out Button backButton)
        {
            var panelObj = CreateUIObject("OptionsPanel", parent);
            panelObj.anchorMin = new Vector2(0f, 0f);
            panelObj.anchorMax = new Vector2(1f, 1f);
            panelObj.offsetMin = Vector2.zero;
            panelObj.offsetMax = Vector2.zero;

            // Modal Dimmer Background
            var dimmer = panelObj.gameObject.AddComponent<Image>();
            dimmer.color = new Color(0f, 0f, 0f, 0.75f);

            // Modal Content Window
            var windowObj = CreateUIObject("OptionsWindow", panelObj);
            windowObj.anchorMin = new Vector2(0.5f, 0.5f);
            windowObj.anchorMax = new Vector2(0.5f, 0.5f);
            windowObj.pivot = new Vector2(0.5f, 0.5f);
            windowObj.anchoredPosition = Vector2.zero;
            windowObj.sizeDelta = new Vector2(700f, 500f);

            var winImg = windowObj.gameObject.AddComponent<Image>();
            winImg.color = new Color(0.12f, 0.15f, 0.22f, 0.98f);

            // Outline
            var winOutline = windowObj.gameObject.AddComponent<Outline>();
            winOutline.effectColor = new Color(0.35f, 0.45f, 0.65f, 0.8f);
            winOutline.effectDistance = new Vector2(2f, -2f);

            // Options Header
            CreateText(windowObj, "OptionsTitle", "OPTIONS", 44, FontStyles.Bold,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -50f), new Vector2(500f, 60f), TextAlignmentOptions.Center,
                new Color(0.95f, 0.95f, 1f, 1f));

            // Placeholder Content Text
            CreateText(windowObj, "PlaceholderInfo",
                "Game Settings\n\n- Master Volume: 100%\n- Music Volume: 80%\n- SFX Volume: 100%\n- Resolution: 1920 x 1080 (Full Screen)\n- Quality: High\n\n(Placeholder settings panel — can be expanded)",
                22, FontStyles.Normal,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 10f), new Vector2(580f, 260f), TextAlignmentOptions.Center,
                new Color(0.75f, 0.82f, 0.92f, 1f));

            // Options Back Button
            backButton = CreateButton(windowObj, "OptionsBackButton", "Back", new Vector2(220f, 55f),
                new Color(0.25f, 0.35f, 0.50f, 1f), new Color(0.35f, 0.48f, 0.68f, 1f), 24);
            var backRt = backButton.GetComponent<RectTransform>();
            backRt.anchorMin = new Vector2(0.5f, 0f);
            backRt.anchorMax = new Vector2(0.5f, 0f);
            backRt.pivot = new Vector2(0.5f, 0f);
            backRt.anchoredPosition = new Vector2(0f, 35f);

            // Initially inactive
            panelObj.gameObject.SetActive(false);

            return panelObj.gameObject;
        }

        #endregion

        #region Stage Selection Creation

        private static void CreateStageSelectionScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Camera
            var camObj = new GameObject("Main Camera");
            var cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.10f, 0.15f, 1f);
            camObj.AddComponent<AudioListener>();

            // 2. EventSystem
            CreateEventSystem();

            // 3. Canvas
            var canvasObj = CreateCanvas("StageSelectionCanvas");
            var canvasRt = canvasObj.GetComponent<RectTransform>();

            // Manager
            var manager = canvasObj.AddComponent<StageSelectionManager>();

            // Background
            CreateBackground(canvasRt, new Color(0.06f, 0.08f, 0.12f, 1f));

            // Title: SELECT STAGE at top center
            CreateText(canvasRt, "SelectStageTitle", "SELECT STAGE", 64, FontStyles.Bold,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -140f), new Vector2(900f, 120f), TextAlignmentOptions.Center,
                new Color(0.95f, 0.95f, 1f, 1f));

            // Subtitle
            CreateText(canvasRt, "SelectStageSubtitle", "Choose your character and environment", 26, FontStyles.Normal,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -220f), new Vector2(700f, 50f), TextAlignmentOptions.Center,
                new Color(0.6f, 0.7f, 0.85f, 1f));

            // Stage selection buttons container (centered)
            var stageContainer = CreateUIObject("StageButtonsContainer", canvasRt);
            stageContainer.anchorMin = new Vector2(0.5f, 0.5f);
            stageContainer.anchorMax = new Vector2(0.5f, 0.5f);
            stageContainer.pivot = new Vector2(0.5f, 0.5f);
            stageContainer.anchoredPosition = new Vector2(0f, -40f);
            stageContainer.sizeDelta = new Vector2(550f, 320f);

            var vlg = stageContainer.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.spacing = 30f;
            vlg.childControlWidth = false;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = false;
            vlg.childForceExpandHeight = false;

            // Stage 1 - Capsule Player
            var stage1Btn = CreateButton(stageContainer, "Stage1Button", "Stage 1 - Capsule Player",
                new Vector2(500f, 85f),
                new Color(0.18f, 0.45f, 0.65f, 1f), new Color(0.25f, 0.60f, 0.85f, 1f), 26);

            // Stage 2 - Humanoid Player
            var stage2Btn = CreateButton(stageContainer, "Stage2Button", "Stage 2 - Humanoid Player",
                new Vector2(500f, 85f),
                new Color(0.55f, 0.35f, 0.15f, 1f), new Color(0.75f, 0.48f, 0.22f, 1f), 26);

            // Back Button anchored to bottom-left corner with padding
            var backBtn = CreateButton(canvasRt, "BackButton", "Back", new Vector2(200f, 60f),
                new Color(0.35f, 0.35f, 0.40f, 1f), new Color(0.48f, 0.48f, 0.55f, 1f), 24);
            var backRt = backBtn.GetComponent<RectTransform>();
            backRt.anchorMin = new Vector2(0f, 0f);
            backRt.anchorMax = new Vector2(0f, 0f);
            backRt.pivot = new Vector2(0f, 0f);
            backRt.anchoredPosition = new Vector2(60f, 60f);

            // Wire SerializedFields in StageSelectionManager
            var so = new SerializedObject(manager);
            so.FindProperty("stage1SceneName").stringValue = "Playground";
            so.FindProperty("stage2SceneName").stringValue = "Playground 2";
            so.FindProperty("mainMenuSceneName").stringValue = "MainMenu";
            so.FindProperty("stage1Button").objectReferenceValue = stage1Btn;
            so.FindProperty("stage2Button").objectReferenceValue = stage2Btn;
            so.FindProperty("backButton").objectReferenceValue = backBtn;
            so.ApplyModifiedProperties();

            // Hook up persistent OnClick events in Inspector
            UnityEventTools.AddPersistentListener(stage1Btn.onClick, manager.LoadStage1);
            UnityEventTools.AddPersistentListener(stage2Btn.onClick, manager.LoadStage2);
            UnityEventTools.AddPersistentListener(backBtn.onClick, manager.LoadMainMenu);

            // Save Scene
            EditorSceneManager.SaveScene(scene, StageSelectionScenePath);
            Debug.Log($"[MenuSetup] Saved scene: {StageSelectionScenePath}");
        }

        #endregion

        #region Build Settings Configuration

        private static void ConfigureBuildSettings()
        {
            var requiredScenes = new[]
            {
                new EditorBuildSettingsScene(MainMenuScenePath, true),
                new EditorBuildSettingsScene(StageSelectionScenePath, true),
                new EditorBuildSettingsScene(Stage1ScenePath, true),
                new EditorBuildSettingsScene(Stage2ScenePath, true)
            };

            EditorBuildSettings.scenes = requiredScenes;
            AssetDatabase.SaveAssets();

            Debug.Log("[MenuSetup] EditorBuildSettings configured with 4 scenes:");
            for (int i = 0; i < EditorBuildSettings.scenes.Length; i++)
            {
                Debug.Log($"  [{i}] {EditorBuildSettings.scenes[i].path} (enabled={EditorBuildSettings.scenes[i].enabled})");
            }
        }

        #endregion

        #region UI Helpers

        private static void CreateEventSystem()
        {
            var esObj = new GameObject("UI_EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<InputSystemUIInputModule>();
        }

        private static GameObject CreateCanvas(string name)
        {
            var canvasObj = new GameObject(name, typeof(RectTransform));
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();
            return canvasObj;
        }

        private static RectTransform CreateUIObject(string name, RectTransform parent)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            var rt = obj.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            return rt;
        }

        private static void CreateBackground(RectTransform parent, Color color)
        {
            var bgObj = CreateUIObject("Background", parent);
            bgObj.anchorMin = Vector2.zero;
            bgObj.anchorMax = Vector2.one;
            bgObj.offsetMin = Vector2.zero;
            bgObj.offsetMax = Vector2.zero;

            var img = bgObj.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
        }

        private static TextMeshProUGUI CreateText(RectTransform parent, string name, string content,
            float fontSize, FontStyles style,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 anchoredPos, Vector2 sizeDelta, TextAlignmentOptions alignment, Color color)
        {
            var textObj = CreateUIObject(name, parent);
            textObj.anchorMin = anchorMin;
            textObj.anchorMax = anchorMax;
            textObj.pivot = pivot;
            textObj.anchoredPosition = anchoredPos;
            textObj.sizeDelta = sizeDelta;

            var tmp = textObj.gameObject.AddComponent<TextMeshProUGUI>();
            if (defaultFontAsset != null) tmp.font = defaultFontAsset;
            tmp.text = content;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.alignment = alignment;
            tmp.color = color;
            tmp.raycastTarget = false;

            return tmp;
        }

        private static Button CreateButton(RectTransform parent, string name, string label,
            Vector2 size, Color normalColor, Color highlightedColor, float fontSize)
        {
            var btnObj = CreateUIObject(name, parent);
            btnObj.sizeDelta = size;

            var img = btnObj.gameObject.AddComponent<Image>();
            img.color = normalColor;

            var outline = btnObj.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 1f, 1f, 0.25f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            var btn = btnObj.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;

            var cb = btn.colors;
            cb.normalColor = normalColor;
            cb.highlightedColor = highlightedColor;
            cb.pressedColor = normalColor * 0.75f;
            cb.selectedColor = highlightedColor;
            cb.disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            cb.colorMultiplier = 1f;
            btn.colors = cb;

            // Child Text
            var textObj = CreateUIObject("Text (TMP)", btnObj);
            textObj.anchorMin = Vector2.zero;
            textObj.anchorMax = Vector2.one;
            textObj.offsetMin = Vector2.zero;
            textObj.offsetMax = Vector2.zero;

            var tmp = textObj.gameObject.AddComponent<TextMeshProUGUI>();
            if (defaultFontAsset != null) tmp.font = defaultFontAsset;
            tmp.text = label;
            tmp.fontSize = fontSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.raycastTarget = false;

            return btn;
        }

        #endregion
    }
}
