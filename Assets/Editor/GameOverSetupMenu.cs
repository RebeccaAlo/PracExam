using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class GameOverSetupMenu
{
    [MenuItem("Tools/Setup Game Over System")]
    public static void SetupGameOverSystem()
    {
        // 1. Locate Player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            player = GameObject.Find("PlayerArmature");
        }

        if (player == null)
        {
            Debug.LogError("[GameOverSetup] Could not find Player or PlayerArmature in the scene!");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(player, "Setup Player Fall Detector");

        // 2. Add / Configure PlayerFallDetector on Player
        var fallDetector = player.GetComponent<PlayerFallDetector>();
        if (fallDetector == null)
        {
            fallDetector = Undo.AddComponent<PlayerFallDetector>(player);
        }
        fallDetector.FallThreshold = -10f;

        // Ensure Player has a Rigidbody (Kinematic if CharacterController is present to avoid physics fighting)
        var rb = player.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = Undo.AddComponent<Rigidbody>(player);
            var cc = player.GetComponent<CharacterController>();
            if (cc != null)
            {
                rb.isKinematic = true;
            }
            else
            {
                rb.isKinematic = false;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                rb.constraints = RigidbodyConstraints.FreezeRotation;
            }
        }

        // 3. Ensure EventSystem exists
        var eventSystem = Object.FindAnyObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            var eventSystemGo = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            eventSystemGo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            eventSystemGo.AddComponent<StandaloneInputModule>();
#endif
            Undo.RegisterCreatedObjectUndo(eventSystemGo, "Create EventSystem");
        }

        // 4. Create or Locate GameOverCanvas
        GameObject canvasGo = GameObject.Find("GameOverCanvas");
        if (canvasGo == null)
        {
            canvasGo = new GameObject("GameOverCanvas");
            Undo.RegisterCreatedObjectUndo(canvasGo, "Create GameOverCanvas");
        }

        var canvas = canvasGo.GetComponent<Canvas>();
        if (canvas == null) canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var raycaster = canvasGo.GetComponent<GraphicRaycaster>();
        if (raycaster == null) canvasGo.AddComponent<GraphicRaycaster>();

        // 5. Build GAME OVER Panel
        GameObject gameOverPanelGo = CreateOrFindChild(canvasGo, "GameOverPanel");
        ConfigurePanelRect(gameOverPanelGo, new Color(0.08f, 0.04f, 0.04f, 0.88f));

        GameObject gameOverTextGo = CreateOrFindChild(gameOverPanelGo, "GameOverText");
        ConfigureText(gameOverTextGo, "GAME OVER", 72, FontStyles.Bold, new Color(0.95f, 0.2f, 0.2f, 1f), new Vector2(0f, 90f), new Vector2(800f, 120f));

        GameObject gameOverScoreGo = CreateOrFindChild(gameOverPanelGo, "FinalScoreText");
        var gameOverScoreTmp = ConfigureText(gameOverScoreGo, "Final Score: 0", 36, FontStyles.Bold, new Color(1f, 0.88f, 0.2f, 1f), new Vector2(0f, 15f), new Vector2(600f, 60f));

        GameObject gameOverBtnGo = CreateOrFindChild(gameOverPanelGo, "RestartButton");
        var gameOverBtn = ConfigureButton(gameOverBtnGo, "Restart", new Vector2(0f, -60f), new Color(0.18f, 0.55f, 0.95f, 1f));

        // 6. Build WIN Panel
        GameObject winPanelGo = CreateOrFindChild(canvasGo, "WinPanel");
        ConfigurePanelRect(winPanelGo, new Color(0.04f, 0.08f, 0.04f, 0.88f));

        GameObject winTextGo = CreateOrFindChild(winPanelGo, "WinText");
        ConfigureText(winTextGo, "WIN", 76, FontStyles.Bold, new Color(0.2f, 0.95f, 0.35f, 1f), new Vector2(0f, 90f), new Vector2(800f, 120f));

        GameObject winScoreGo = CreateOrFindChild(winPanelGo, "FinalScoreText");
        var winScoreTmp = ConfigureText(winScoreGo, "Final Score: 10,000", 36, FontStyles.Bold, new Color(1f, 0.88f, 0.2f, 1f), new Vector2(0f, 15f), new Vector2(600f, 60f));

        GameObject winBtnGo = CreateOrFindChild(winPanelGo, "RestartButton");
        var winBtn = ConfigureButton(winBtnGo, "Restart", new Vector2(0f, -60f), new Color(0.2f, 0.75f, 0.3f, 1f));

        // 7. Attach & Configure GameOverManager
        var manager = canvasGo.GetComponent<GameOverManager>();
        if (manager == null) manager = Undo.AddComponent<GameOverManager>(canvasGo);

        var serializedMgr = new SerializedObject(manager);
        serializedMgr.FindProperty("gameOverPanel").objectReferenceValue = gameOverPanelGo;
        serializedMgr.FindProperty("winPanel").objectReferenceValue = winPanelGo;
        serializedMgr.FindProperty("gameOverScoreText").objectReferenceValue = gameOverScoreTmp;
        serializedMgr.FindProperty("winScoreText").objectReferenceValue = winScoreTmp;
        serializedMgr.FindProperty("gameOverRestartButton").objectReferenceValue = gameOverBtn;
        serializedMgr.FindProperty("winRestartButton").objectReferenceValue = winBtn;
        serializedMgr.FindProperty("player").objectReferenceValue = player;
        serializedMgr.FindProperty("playerRigidbody").objectReferenceValue = rb;
        serializedMgr.FindProperty("fallDetector").objectReferenceValue = fallDetector;
        serializedMgr.FindProperty("autoDetectMovementComponents").boolValue = true;
        serializedMgr.FindProperty("manageCursor").boolValue = true;
        serializedMgr.FindProperty("lockCursorOnRestart").boolValue = true;
        serializedMgr.FindProperty("targetWinScore").intValue = 10000;
        serializedMgr.FindProperty("initialPosition").vector3Value = player.transform.position;
        serializedMgr.FindProperty("initialRotation").quaternionValue = player.transform.rotation;
        serializedMgr.ApplyModifiedProperties();

        // Connect fallDetector reference to manager
        var serializedDetector = new SerializedObject(fallDetector);
        serializedDetector.FindProperty("gameOverManager").objectReferenceValue = manager;
        serializedDetector.FindProperty("initialPosition").vector3Value = player.transform.position;
        serializedDetector.FindProperty("initialRotation").quaternionValue = player.transform.rotation;
        serializedDetector.ApplyModifiedProperties();

        // 8. Hide both panels initially
        gameOverPanelGo.SetActive(false);
        winPanelGo.SetActive(false);

        // Mark scene dirty and save
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[GameOverSetup] Successfully created WIN and GAME OVER panels and wired LevelManager!");
    }

    private static GameObject CreateOrFindChild(GameObject parent, string name)
    {
        Transform child = parent.transform.Find(name);
        if (child != null)
        {
            if (child is RectTransform) return child.gameObject;
            Object.DestroyImmediate(child.gameObject);
        }

        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent.transform, false);
        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        return go;
    }

    private static void ConfigurePanelRect(GameObject panelGo, Color color)
    {
        var panelRect = panelGo.GetComponent<RectTransform>() ?? panelGo.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        var panelImage = panelGo.GetComponent<Image>() ?? panelGo.AddComponent<Image>();
        panelImage.color = color;
    }

    private static TextMeshProUGUI ConfigureText(GameObject go, string text, float fontSize, FontStyles style, Color color, Vector2 pos, Vector2 size)
    {
        var rect = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;

        var tmp = go.GetComponent<TextMeshProUGUI>() ?? go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        return tmp;
    }

    private static Button ConfigureButton(GameObject btnGo, string label, Vector2 pos, Color baseColor)
    {
        var rect = btnGo.GetComponent<RectTransform>() ?? btnGo.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = new Vector2(240f, 65f);

        var img = btnGo.GetComponent<Image>() ?? btnGo.AddComponent<Image>();
        img.color = baseColor;

        var btn = btnGo.GetComponent<Button>() ?? btnGo.AddComponent<Button>();
        btn.targetGraphic = img;

        var colors = btn.colors;
        colors.highlightedColor = baseColor * 1.2f;
        colors.pressedColor = baseColor * 0.8f;
        btn.colors = colors;

        GameObject labelGo = CreateOrFindChild(btnGo, "Text (TMP)");
        var labelRect = labelGo.GetComponent<RectTransform>() ?? labelGo.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        var labelTmp = labelGo.GetComponent<TextMeshProUGUI>() ?? labelGo.AddComponent<TextMeshProUGUI>();
        labelTmp.text = label;
        labelTmp.fontSize = 28;
        labelTmp.fontStyle = FontStyles.Bold;
        labelTmp.alignment = TextAlignmentOptions.Center;
        labelTmp.color = Color.white;

        return btn;
    }
}
