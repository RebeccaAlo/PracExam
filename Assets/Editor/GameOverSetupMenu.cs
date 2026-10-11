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
                rb.isKinematic = true; // Kinematic prevents conflict with CharacterController
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

        // 4. Create or Locate Canvas
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

        // 5. Create Game Over Panel
        Transform panelTransform = canvasGo.transform.Find("GameOverPanel");
        GameObject panelGo;
        if (panelTransform == null)
        {
            panelGo = new GameObject("GameOverPanel");
            panelGo.transform.SetParent(canvasGo.transform, false);
            Undo.RegisterCreatedObjectUndo(panelGo, "Create GameOverPanel");
        }
        else
        {
            panelGo = panelTransform.gameObject;
        }

        var panelRect = panelGo.GetComponent<RectTransform>();
        if (panelRect == null) panelRect = panelGo.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        var panelImage = panelGo.GetComponent<Image>();
        if (panelImage == null) panelImage = panelGo.AddComponent<Image>();
        panelImage.color = new Color(0.05f, 0.05f, 0.08f, 0.85f); // Elegant dark overlay

        // 6. Create GAME OVER Text
        Transform textTransform = panelGo.transform.Find("GameOverText");
        GameObject textGo;
        if (textTransform == null)
        {
            textGo = new GameObject("GameOverText");
            textGo.transform.SetParent(panelGo.transform, false);
            Undo.RegisterCreatedObjectUndo(textGo, "Create GameOverText");
        }
        else
        {
            textGo = textTransform.gameObject;
        }

        var textRect = textGo.GetComponent<RectTransform>();
        if (textRect == null) textRect = textGo.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = new Vector2(0f, 75f);
        textRect.sizeDelta = new Vector2(800f, 140f);

        var tmpText = textGo.GetComponent<TextMeshProUGUI>();
        if (tmpText == null) tmpText = textGo.AddComponent<TextMeshProUGUI>();
        tmpText.text = "GAME OVER";
        tmpText.fontSize = 72;
        tmpText.fontStyle = FontStyles.Bold;
        tmpText.alignment = TextAlignmentOptions.Center;
        tmpText.color = new Color(0.95f, 0.2f, 0.2f, 1.0f); // Bold red

        // 7. Create Restart Button
        Transform buttonTransform = panelGo.transform.Find("RestartButton");
        GameObject buttonGo;
        if (buttonTransform == null)
        {
            buttonGo = new GameObject("RestartButton");
            buttonGo.transform.SetParent(panelGo.transform, false);
            Undo.RegisterCreatedObjectUndo(buttonGo, "Create RestartButton");
        }
        else
        {
            buttonGo = buttonTransform.gameObject;
        }

        var btnRect = buttonGo.GetComponent<RectTransform>();
        if (btnRect == null) btnRect = buttonGo.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.5f, 0.5f);
        btnRect.anchorMax = new Vector2(0.5f, 0.5f);
        btnRect.pivot = new Vector2(0.5f, 0.5f);
        btnRect.anchoredPosition = new Vector2(0f, -50f);
        btnRect.sizeDelta = new Vector2(240f, 65f);

        var btnImage = buttonGo.GetComponent<Image>();
        if (btnImage == null) btnImage = buttonGo.AddComponent<Image>();
        btnImage.color = new Color(0.18f, 0.55f, 0.95f, 1.0f); // Clean blue button

        var btnComp = buttonGo.GetComponent<Button>();
        if (btnComp == null) btnComp = buttonGo.AddComponent<Button>();
        btnComp.targetGraphic = btnImage;
        var colors = btnComp.colors;
        colors.highlightedColor = new Color(0.3f, 0.68f, 1.0f, 1.0f);
        colors.pressedColor = new Color(0.12f, 0.42f, 0.78f, 1.0f);
        btnComp.colors = colors;

        // Button label
        Transform btnLabelTransform = buttonGo.transform.Find("Text (TMP)");
        GameObject btnLabelGo;
        if (btnLabelTransform == null)
        {
            btnLabelGo = new GameObject("Text (TMP)");
            btnLabelGo.transform.SetParent(buttonGo.transform, false);
            Undo.RegisterCreatedObjectUndo(btnLabelGo, "Create Button Label");
        }
        else
        {
            btnLabelGo = btnLabelTransform.gameObject;
        }

        var btnLabelRect = btnLabelGo.GetComponent<RectTransform>();
        if (btnLabelRect == null) btnLabelRect = btnLabelGo.AddComponent<RectTransform>();
        btnLabelRect.anchorMin = Vector2.zero;
        btnLabelRect.anchorMax = Vector2.one;
        btnLabelRect.offsetMin = Vector2.zero;
        btnLabelRect.offsetMax = Vector2.zero;

        var btnTmp = btnLabelGo.GetComponent<TextMeshProUGUI>();
        if (btnTmp == null) btnTmp = btnLabelGo.AddComponent<TextMeshProUGUI>();
        btnTmp.text = "Restart";
        btnTmp.fontSize = 28;
        btnTmp.fontStyle = FontStyles.Bold;
        btnTmp.alignment = TextAlignmentOptions.Center;
        btnTmp.color = Color.white;

        // 8. Attach / Configure GameOverManager on Canvas
        var manager = canvasGo.GetComponent<GameOverManager>();
        if (manager == null) manager = Undo.AddComponent<GameOverManager>(canvasGo);

        // Serialize fields via SerializedObject to ensure Inspector references are properly saved
        var serializedMgr = new SerializedObject(manager);
        serializedMgr.FindProperty("gameOverPanel").objectReferenceValue = panelGo;
        serializedMgr.FindProperty("restartButton").objectReferenceValue = btnComp;
        serializedMgr.FindProperty("player").objectReferenceValue = player;
        serializedMgr.FindProperty("playerRigidbody").objectReferenceValue = rb;
        serializedMgr.FindProperty("fallDetector").objectReferenceValue = fallDetector;
        serializedMgr.FindProperty("autoDetectMovementComponents").boolValue = true;
        serializedMgr.FindProperty("manageCursor").boolValue = true;
        serializedMgr.FindProperty("lockCursorOnRestart").boolValue = true;
        serializedMgr.FindProperty("initialPosition").vector3Value = player.transform.position;
        serializedMgr.FindProperty("initialRotation").quaternionValue = player.transform.rotation;
        serializedMgr.ApplyModifiedProperties();

        // Also connect fallDetector reference to manager
        var serializedDetector = new SerializedObject(fallDetector);
        serializedDetector.FindProperty("gameOverManager").objectReferenceValue = manager;
        serializedDetector.FindProperty("initialPosition").vector3Value = player.transform.position;
        serializedDetector.FindProperty("initialRotation").quaternionValue = player.transform.rotation;
        serializedDetector.ApplyModifiedProperties();

        // 9. Hide panel initially
        panelGo.SetActive(false);

        // Mark scene dirty and save
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[GameOverSetup] Successfully created and wired Game Over & Restart System!");
    }
}
