using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class CollectibleItemSetupMenu
{
    private const string ItemsFolder = "Assets/Items";

    [MenuItem("Tools/Setup Collectible Items System")]
    public static void SetupCollectibleItemsSystem()
    {
        // 1. Ensure Assets/Items folder exists
        if (!AssetDatabase.IsValidFolder(ItemsFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Items");
        }

        // 2. Ensure base coin material exists
        Material coinMat = CreateOrLoadCoinMaterial();

        // 3. Create the 4 Prefabs in Assets/Items/ with Kinematic Rigidbodies
        GameObject timeBonusPrefab = CreateCoinPrefab("TimeBonusCoin", CollectibleType.TimeBonus, 3, new Color(0.18f, 0.88f, 0.28f, 1f), coinMat);
        GameObject timePenaltyPrefab = CreateCoinPrefab("TimePenaltyCoin", CollectibleType.TimePenalty, 5, new Color(0.95f, 0.2f, 0.2f, 1f), coinMat);
        GameObject scoreBonusPrefab = CreateCoinPrefab("ScoreBonusCoin", CollectibleType.ScoreBonus, 500, new Color(1.0f, 0.84f, 0.1f, 1f), coinMat);
        GameObject scorePenaltyPrefab = CreateCoinPrefab("ScorePenaltyCoin", CollectibleType.ScorePenalty, 500, new Color(0.68f, 0.2f, 0.95f, 1f), coinMat);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 4. Create or Locate HUD Canvas with Timer in Top-Left and Score in Top-Right
        SetupHUD();

        // 5. Populate scene with instances under CollectibleItems parent in safe gameplay locations
        PopulateSceneInstances(timeBonusPrefab, timePenaltyPrefab, scoreBonusPrefab, scorePenaltyPrefab);

        // Mark active scene dirty and save
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[CollectibleItemSetup] Successfully configured 4 Prefabs in Assets/Items and placed instances under CollectibleItems in scene!");
    }

    private static Material CreateOrLoadCoinMaterial()
    {
        string matPath = $"{ItemsFolder}/M_Coin.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            mat = new Material(shader);
            mat.name = "M_Coin";
            mat.SetFloat("_Metallic", 0.85f);
            mat.SetFloat("_Smoothness", 0.75f);
            AssetDatabase.CreateAsset(mat, matPath);
        }
        return mat;
    }

    private static GameObject CreateCoinPrefab(string prefabName, CollectibleType type, int value, Color color, Material baseMat)
    {
        // Root GameObject
        GameObject root = new GameObject(prefabName);

        // SphereCollider trigger
        var col = root.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 0.55f;
        col.center = Vector3.zero;

        // Kinematic Rigidbody to guarantee Unity PhysX trigger callbacks
        var rb = root.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        // Visual child: flattened cylinder oriented vertically like a coin
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        visual.name = "CoinVisual";
        visual.transform.SetParent(root.transform, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        visual.transform.localScale = new Vector3(0.7f, 0.06f, 0.7f);

        // Remove primitive collider from child visual
        var childCol = visual.GetComponent<Collider>();
        if (childCol != null) Object.DestroyImmediate(childCol);

        var renderer = visual.GetComponent<MeshRenderer>();
        if (renderer != null && baseMat != null)
        {
            renderer.sharedMaterial = baseMat;
        }

        // Add CollectibleItem script on root
        var item = root.AddComponent<CollectibleItem>();
        item.ItemType = type;
        item.ItemValue = value;
        item.ItemColor = color;
        item.ItemVisual = renderer;

        // Apply visual color
        item.UpdateVisualColor();

        // Save as Prefab asset
        string prefabPath = $"{ItemsFolder}/{prefabName}.prefab";
        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);

        // Clean up scene temporary object
        Object.DestroyImmediate(root);

        Debug.Log($"[CollectibleItemSetup] Created Prefab: {prefabPath} (Type: {type}, Value: {value}, Color: {color})");
        return savedPrefab;
    }

    private static void PopulateSceneInstances(GameObject timeBonus, GameObject timePenalty, GameObject scoreBonus, GameObject scorePenalty)
    {
        // Locate or create parent GameObject 'CollectibleItems'
        GameObject parentGo = GameObject.Find("CollectibleItems");
        if (parentGo == null)
        {
            parentGo = new GameObject("CollectibleItems");
            Undo.RegisterCreatedObjectUndo(parentGo, "Create CollectibleItems parent");
        }

        // Clean up any existing children to prevent duplicates
        while (parentGo.transform.childCount > 0)
        {
            Object.DestroyImmediate(parentGo.transform.GetChild(0).gameObject);
        }

        // Place 21 ScoreBonusCoins (10,500 points total, enabling 10k win)
        // plus TimeBonusCoins and obstacles in safe central playground/garden locations
        var spawnList = new (GameObject prefab, Vector3 pos)[]
        {
            // Path from spawn (4.2, 0, 0) towards house (4.2, 0, 13.5)
            (scoreBonus, new Vector3(4.2f, 0.8f, 2.0f)),
            (scoreBonus, new Vector3(4.2f, 0.8f, 4.0f)),
            (scoreBonus, new Vector3(4.2f, 0.8f, 6.0f)),
            (scoreBonus, new Vector3(4.2f, 0.8f, 8.0f)),
            (scoreBonus, new Vector3(4.2f, 0.8f, 10.0f)),
            (scoreBonus, new Vector3(4.2f, 0.8f, 12.0f)),

            // Front porch and house doorway
            (scoreBonus, new Vector3(4.2f, 0.9f, 14.5f)),
            (scoreBonus, new Vector3(4.2f, 0.9f, 16.5f)), // Inside house interior

            // Left Garden path & flower beds
            (scoreBonus, new Vector3(1.5f, 0.8f, 3.5f)),
            (scoreBonus, new Vector3(1.5f, 0.8f, 6.5f)),
            (scoreBonus, new Vector3(1.5f, 0.8f, 9.5f)),
            (scoreBonus, new Vector3(1.5f, 0.8f, 12.5f)),
            (scoreBonus, new Vector3(-1.0f, 0.8f, 6.0f)),
            (scoreBonus, new Vector3(-1.0f, 0.8f, 10.0f)),

            // Right Garden path & flower beds
            (scoreBonus, new Vector3(7.0f, 0.8f, 3.5f)),
            (scoreBonus, new Vector3(7.0f, 0.8f, 6.5f)),
            (scoreBonus, new Vector3(7.0f, 0.8f, 9.5f)),
            (scoreBonus, new Vector3(7.0f, 0.8f, 12.5f)),
            (scoreBonus, new Vector3(9.5f, 0.8f, 6.0f)),
            (scoreBonus, new Vector3(9.5f, 0.8f, 10.0f)),
            (scoreBonus, new Vector3(4.2f, 0.8f, 18.5f)), // Backyard patio

            // Time Bonus Coins (+3s each)
            (timeBonus, new Vector3(0.0f, 0.8f, 4.0f)),
            (timeBonus, new Vector3(8.5f, 0.8f, 4.0f)),
            (timeBonus, new Vector3(0.0f, 0.8f, 13.0f)),
            (timeBonus, new Vector3(8.5f, 0.8f, 13.0f)),

            // Time Penalty Coins (-5s each)
            (timePenalty, new Vector3(3.0f, 0.8f, 7.0f)),
            (timePenalty, new Vector3(5.5f, 0.8f, 7.0f)),

            // Score Penalty Coins (-500 pts each)
            (scorePenalty, new Vector3(3.0f, 0.8f, 11.0f)),
            (scorePenalty, new Vector3(5.5f, 0.8f, 11.0f))
        };

        for (int i = 0; i < spawnList.Length; i++)
        {
            var (prefab, pos) = spawnList[i];
            if (prefab == null) continue;

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parentGo.transform);
            instance.transform.position = pos;
            instance.name = $"{prefab.name}_{i + 1:D2}";
            Undo.RegisterCreatedObjectUndo(instance, "Instantiate Collectible Item");
        }
    }

    private static void SetupHUD()
    {
        // Check if GameHUDCanvas already exists
        GameObject hudCanvasGo = GameObject.Find("GameHUDCanvas");
        if (hudCanvasGo == null)
        {
            hudCanvasGo = new GameObject("GameHUDCanvas");
            Undo.RegisterCreatedObjectUndo(hudCanvasGo, "Create GameHUDCanvas");
        }

        var canvas = hudCanvasGo.GetComponent<Canvas>() ?? hudCanvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        var scaler = hudCanvasGo.GetComponent<CanvasScaler>() ?? hudCanvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var raycaster = hudCanvasGo.GetComponent<GraphicRaycaster>() ?? hudCanvasGo.AddComponent<GraphicRaycaster>();

        // Top HUD Container Panel
        Transform hudContainer = hudCanvasGo.transform.Find("HUDContainer");
        GameObject containerGo;
        if (hudContainer == null)
        {
            containerGo = new GameObject("HUDContainer");
            containerGo.transform.SetParent(hudCanvasGo.transform, false);
            Undo.RegisterCreatedObjectUndo(containerGo, "Create HUDContainer");
        }
        else
        {
            containerGo = hudContainer.gameObject;
        }

        var containerRect = containerGo.GetComponent<RectTransform>() ?? containerGo.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0f, 1f);
        containerRect.anchorMax = new Vector2(1f, 1f);
        containerRect.pivot = new Vector2(0.5f, 1f);
        containerRect.anchoredPosition = new Vector2(0f, -20f);
        containerRect.sizeDelta = new Vector2(0f, 80f);

        // Timer Text placed in the TOP-LEFT corner (Requirement 1 & 6)
        Transform timerTransform = containerGo.transform.Find("TimerText");
        GameObject timerGo;
        if (timerTransform == null)
        {
            timerGo = new GameObject("TimerText");
            timerGo.transform.SetParent(containerGo.transform, false);
            Undo.RegisterCreatedObjectUndo(timerGo, "Create TimerText");
        }
        else
        {
            timerGo = timerTransform.gameObject;
        }

        var timerRect = timerGo.GetComponent<RectTransform>() ?? timerGo.AddComponent<RectTransform>();
        timerRect.anchorMin = new Vector2(0f, 0.5f);
        timerRect.anchorMax = new Vector2(0f, 0.5f);
        timerRect.pivot = new Vector2(0f, 0.5f);
        timerRect.anchoredPosition = new Vector2(40f, 0f);
        timerRect.sizeDelta = new Vector2(300f, 60f);

        var timerTmp = timerGo.GetComponent<TextMeshProUGUI>() ?? timerGo.AddComponent<TextMeshProUGUI>();
        timerTmp.text = "Time: 30s";
        timerTmp.fontSize = 36;
        timerTmp.fontStyle = FontStyles.Bold;
        timerTmp.color = Color.white;
        timerTmp.alignment = TextAlignmentOptions.MidlineLeft;

        // Score Text placed in the TOP-RIGHT corner
        Transform scoreTransform = containerGo.transform.Find("ScoreText");
        GameObject scoreGo;
        if (scoreTransform == null)
        {
            scoreGo = new GameObject("ScoreText");
            scoreGo.transform.SetParent(containerGo.transform, false);
            Undo.RegisterCreatedObjectUndo(scoreGo, "Create ScoreText");
        }
        else
        {
            scoreGo = scoreTransform.gameObject;
        }

        var scoreRect = scoreGo.GetComponent<RectTransform>() ?? scoreGo.AddComponent<RectTransform>();
        scoreRect.anchorMin = new Vector2(1f, 0.5f);
        scoreRect.anchorMax = new Vector2(1f, 0.5f);
        scoreRect.pivot = new Vector2(1f, 0.5f);
        scoreRect.anchoredPosition = new Vector2(-40f, 0f);
        scoreRect.sizeDelta = new Vector2(300f, 60f);

        var scoreTmp = scoreGo.GetComponent<TextMeshProUGUI>() ?? scoreGo.AddComponent<TextMeshProUGUI>();
        scoreTmp.text = "Score: 0";
        scoreTmp.fontSize = 36;
        scoreTmp.fontStyle = FontStyles.Bold;
        scoreTmp.color = new Color(1.0f, 0.88f, 0.2f, 1.0f); // Gold
        scoreTmp.alignment = TextAlignmentOptions.MidlineRight;

        // 1. Attach ScoreManager to HUD Canvas
        var scoreMgr = hudCanvasGo.GetComponent<ScoreManager>() ?? Undo.AddComponent<ScoreManager>(hudCanvasGo);
        var serializedScore = new SerializedObject(scoreMgr);
        serializedScore.FindProperty("scoreText").objectReferenceValue = scoreTmp;
        serializedScore.FindProperty("initialScore").intValue = 0;
        serializedScore.FindProperty("currentScore").intValue = 0;
        serializedScore.ApplyModifiedProperties();

        // 2. Attach GameTimer (30 seconds countdown)
        var gameTimer = hudCanvasGo.GetComponent<GameTimer>() ?? Undo.AddComponent<GameTimer>(hudCanvasGo);
        var serializedTimer = new SerializedObject(gameTimer);
        serializedTimer.FindProperty("timerText").objectReferenceValue = timerTmp;
        serializedTimer.FindProperty("initialTime").floatValue = 30f;
        serializedTimer.FindProperty("remainingTime").floatValue = 30f;
        serializedTimer.FindProperty("isTimerRunning").boolValue = true;
        serializedTimer.ApplyModifiedProperties();

        // 3. Attach GameStatsManager (bridge)
        var statsManager = hudCanvasGo.GetComponent<GameStatsManager>() ?? Undo.AddComponent<GameStatsManager>(hudCanvasGo);
        var serializedStats = new SerializedObject(statsManager);
        serializedStats.FindProperty("scoreText").objectReferenceValue = scoreTmp;
        serializedStats.FindProperty("timerText").objectReferenceValue = timerTmp;
        serializedStats.FindProperty("initialTime").floatValue = 30f;
        serializedStats.FindProperty("remainingTime").floatValue = 30f;
        serializedStats.FindProperty("initialScore").intValue = 0;
        serializedStats.FindProperty("currentScore").intValue = 0;
        serializedStats.FindProperty("isTimerRunning").boolValue = true;
        serializedStats.ApplyModifiedProperties();
    }
}
