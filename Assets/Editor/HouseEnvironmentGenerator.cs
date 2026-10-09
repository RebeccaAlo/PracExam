using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.ProBuilder;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.Rendering;

public static class HouseEnvironmentGenerator
{
    private const string MaterialsFolder = "Assets/Materials/HouseEnvironment";

    [MenuItem("Tools/ProBuilder/Generate House Environment", priority = 1)]
    public static void GenerateEnvironment()
    {
        // 1. Clean existing root if present
        GameObject existingRoot = GameObject.Find("HouseEnvironment");
        if (existingRoot != null)
        {
            Undo.DestroyObjectImmediate(existingRoot);
        }

        // 2. Setup materials
        EnsureMaterialsFolder();
        Material matGrass = GetOrCreateMaterial("Mat_Grass.mat", new Color(0.32f, 0.65f, 0.22f), 0.1f);
        Material matWall = GetOrCreateMaterial("Mat_Wall_White.mat", new Color(0.96f, 0.95f, 0.92f), 0.15f);
        Material matFloor = GetOrCreateMaterial("Mat_Floor_Wood.mat", new Color(0.72f, 0.53f, 0.35f), 0.3f);
        Material matRoof = GetOrCreateMaterial("Mat_Roof_Brown.mat", new Color(0.50f, 0.24f, 0.14f), 0.15f);
        Material matDoor = GetOrCreateMaterial("Mat_Door_Wood.mat", new Color(0.38f, 0.20f, 0.10f), 0.35f);
        Material matDoorKnob = GetOrCreateMaterial("Mat_Door_Knob.mat", new Color(0.95f, 0.80f, 0.20f), 0.85f);
        Material matWindowFrame = GetOrCreateMaterial("Mat_Window_Frame.mat", new Color(0.28f, 0.18f, 0.10f), 0.25f);
        Material matWindowGlass = GetOrCreateMaterial("Mat_Window_Glass.mat", new Color(0.42f, 0.75f, 0.95f), 0.9f);
        Material matFence = GetOrCreateMaterial("Mat_Fence_Wood.mat", new Color(0.68f, 0.50f, 0.33f), 0.15f);
        Material matStone = GetOrCreateMaterial("Mat_Stone_Pathway.mat", new Color(0.55f, 0.56f, 0.55f), 0.2f);
        Material matSoil = GetOrCreateMaterial("Mat_Garden_Soil.mat", new Color(0.25f, 0.18f, 0.12f), 0.05f);
        Material matBark = GetOrCreateMaterial("Mat_Tree_Bark.mat", new Color(0.35f, 0.22f, 0.14f), 0.1f);
        Material matLeaves = GetOrCreateMaterial("Mat_Tree_Leaves.mat", new Color(0.20f, 0.52f, 0.18f), 0.1f);
        Material matLeavesLight = GetOrCreateMaterial("Mat_Tree_LeavesLight.mat", new Color(0.28f, 0.60f, 0.22f), 0.1f);
        Material matFlowerStem = GetOrCreateMaterial("Mat_Flower_Stem.mat", new Color(0.25f, 0.58f, 0.20f), 0.1f);
        Material matFlowerRed = GetOrCreateMaterial("Mat_Flower_Red.mat", new Color(0.92f, 0.18f, 0.18f), 0.3f);
        Material matFlowerYellow = GetOrCreateMaterial("Mat_Flower_Yellow.mat", new Color(0.98f, 0.82f, 0.15f), 0.3f);
        Material matFlowerPurple = GetOrCreateMaterial("Mat_Flower_Purple.mat", new Color(0.68f, 0.25f, 0.88f), 0.3f);
        Material matFlowerPink = GetOrCreateMaterial("Mat_Flower_Pink.mat", new Color(0.95f, 0.45f, 0.65f), 0.3f);

        // 3. Create Root Hierarchy
        GameObject rootGo = new GameObject("HouseEnvironment");
        Undo.RegisterCreatedObjectUndo(rootGo, "Generate House Environment");

        // 4. Garden Lawn Ground
        GameObject gardenGroup = CreateChild(rootGo, "Garden");
        CreateProBuilderCube("Lawn", gardenGroup, new Vector3(0f, -0.1f, 1f), new Vector3(26f, 0.2f, 26f), matGrass);

        // 5. House Construction
        GameObject houseGroup = CreateChild(rootGo, "House");
        Vector3 houseCenter = new Vector3(0f, 0f, 4f);

        // 5a. Floor
        CreateProBuilderCube("Floor", houseGroup, houseCenter + new Vector3(0f, 0.1f, 0f), new Vector3(6.4f, 0.2f, 6.4f), matFloor);

        // 5b. Walls (Grouped)
        GameObject wallsGroup = CreateChild(houseGroup, "Walls");
        // Back Wall
        CreateProBuilderCube("Wall_Back", wallsGroup, houseCenter + new Vector3(0f, 1.7f, 3.05f), new Vector3(6.4f, 3.2f, 0.3f), matWall);
        // Left Wall
        CreateProBuilderCube("Wall_Left", wallsGroup, houseCenter + new Vector3(-3.05f, 1.7f, 0f), new Vector3(0.3f, 3.2f, 6.4f), matWall);
        // Right Wall
        CreateProBuilderCube("Wall_Right", wallsGroup, houseCenter + new Vector3(3.05f, 1.7f, 0f), new Vector3(0.3f, 3.2f, 6.4f), matWall);
        // Front Wall (Left section)
        CreateProBuilderCube("Wall_Front_Left", wallsGroup, houseCenter + new Vector3(-1.95f, 1.7f, -3.05f), new Vector3(2.5f, 3.2f, 0.3f), matWall);
        // Front Wall (Right section)
        CreateProBuilderCube("Wall_Front_Right", wallsGroup, houseCenter + new Vector3(1.95f, 1.7f, -3.05f), new Vector3(2.5f, 3.2f, 0.3f), matWall);
        // Front Wall (Lintel above door)
        CreateProBuilderCube("Wall_Front_Lintel", wallsGroup, houseCenter + new Vector3(0f, 2.85f, -3.05f), new Vector3(1.4f, 0.9f, 0.3f), matWall);

        // Ceiling / Attic base
        CreateProBuilderCube("Ceiling", houseGroup, houseCenter + new Vector3(0f, 3.4f, 0f), new Vector3(6.6f, 0.2f, 6.6f), matWall);

        // 5c. Roof (Triangular Gable Roof using ProBuilder Prism)
        GameObject roofGroup = CreateChild(houseGroup, "Roof");
        CreateProBuilderPrism("Roof_Main", roofGroup, houseCenter + new Vector3(0f, 4.6f, 0f), new Vector3(7.2f, 2.2f, 7.2f), matRoof);
        CreateProBuilderCube("Roof_Trim_Front", roofGroup, houseCenter + new Vector3(0f, 3.52f, -3.6f), new Vector3(7.3f, 0.15f, 0.25f), matRoof);
        CreateProBuilderCube("Roof_Trim_Back", roofGroup, houseCenter + new Vector3(0f, 3.52f, 3.6f), new Vector3(7.3f, 0.15f, 0.25f), matRoof);

        // Chimney on Roof
        GameObject chimneyGroup = CreateChild(roofGroup, "Chimney");
        CreateProBuilderCube("Chimney_Column", chimneyGroup, houseCenter + new Vector3(1.8f, 5.0f, 1.0f), new Vector3(0.75f, 2.2f, 0.75f), matStone);
        CreateProBuilderCube("Chimney_Cap", chimneyGroup, houseCenter + new Vector3(1.8f, 6.15f, 1.0f), new Vector3(0.95f, 0.15f, 0.95f), matRoof);

        // 5d. Front Door
        GameObject doorGroup = CreateChild(houseGroup, "FrontDoor");
        CreateProBuilderCube("Door_Frame", doorGroup, houseCenter + new Vector3(0f, 1.25f, -3.10f), new Vector3(1.4f, 2.4f, 0.25f), matWindowFrame);
        CreateProBuilderCube("Door_Panel", doorGroup, houseCenter + new Vector3(0f, 1.2f, -3.12f), new Vector3(1.2f, 2.3f, 0.18f), matDoor);
        CreateProBuilderSphere("Door_Knob", doorGroup, houseCenter + new Vector3(0.45f, 1.15f, -3.24f), 0.07f, matDoorKnob);
        CreateProBuilderCube("Door_Step", doorGroup, houseCenter + new Vector3(0f, 0.1f, -3.6f), new Vector3(2.0f, 0.2f, 0.9f), matStone);

        // 5e. Windows (Front & Side)
        GameObject windowsGroup = CreateChild(houseGroup, "Windows");
        // Front Left Window (prominently visible on front wall)
        CreateWindow("Window_FrontLeft", windowsGroup, houseCenter + new Vector3(-1.95f, 1.8f, -3.22f), new Vector3(1.25f, 1.35f, 0.22f), matWindowFrame, matWindowGlass, false);
        // Front Right Window (prominently visible on front wall)
        CreateWindow("Window_FrontRight", windowsGroup, houseCenter + new Vector3(1.95f, 1.8f, -3.22f), new Vector3(1.25f, 1.35f, 0.22f), matWindowFrame, matWindowGlass, false);
        // Left Wall Window
        CreateWindow("Window_LeftSide", windowsGroup, houseCenter + new Vector3(-3.22f, 1.8f, 0f), new Vector3(0.22f, 1.35f, 1.6f), matWindowFrame, matWindowGlass, true);
        // Right Wall Window
        CreateWindow("Window_RightSide", windowsGroup, houseCenter + new Vector3(3.22f, 1.8f, 0f), new Vector3(0.22f, 1.35f, 1.6f), matWindowFrame, matWindowGlass, true);

        // 6. Fence Boundary (with front gate opening)
        GameObject fenceGroup = CreateChild(rootGo, "Fence");
        BuildFence(fenceGroup, matFence);

        // 7. Stone Pathway (From front gate to front door)
        GameObject pathGroup = CreateChild(gardenGroup, "Pathway");
        BuildStonePathway(pathGroup, matStone);

        // 8. Garden Elements: Flower Beds & Trees
        GameObject flowerBedsGroup = CreateChild(gardenGroup, "FlowerBeds");
        // Left Flower Bed
        BuildFlowerBed("FlowerBed_Left", flowerBedsGroup, new Vector3(-3.6f, 0f, -0.8f), new Vector3(3.6f, 0.25f, 1.6f),
            matSoil, matFlowerStem, new[] { matFlowerRed, matFlowerYellow, matFlowerPink });
        // Right Flower Bed
        BuildFlowerBed("FlowerBed_Right", flowerBedsGroup, new Vector3(3.6f, 0f, -0.8f), new Vector3(3.6f, 0.25f, 1.6f),
            matSoil, matFlowerStem, new[] { matFlowerPurple, matFlowerYellow, matFlowerRed });

        // Trees
        GameObject treesGroup = CreateChild(gardenGroup, "Trees");
        BuildTree("Tree_FrontLeft", treesGroup, new Vector3(-6.8f, 0f, -4.5f), 2.2f, 1.3f, matBark, matLeaves);
        BuildTree("Tree_FrontRight", treesGroup, new Vector3(6.8f, 0f, -4.5f), 2.4f, 1.4f, matBark, matLeavesLight);
        BuildTree("Tree_BackLeft", treesGroup, new Vector3(-7.2f, 0f, 8.5f), 2.8f, 1.6f, matBark, matLeavesLight);
        BuildTree("Tree_BackRight", treesGroup, new Vector3(7.2f, 0f, 8.5f), 2.6f, 1.5f, matBark, matLeaves);
        BuildTree("Tree_SideLeft", treesGroup, new Vector3(-7.8f, 0f, 2.5f), 2.3f, 1.3f, matBark, matLeaves);
        BuildTree("Tree_SideRight", treesGroup, new Vector3(7.8f, 0f, 2.5f), 2.5f, 1.4f, matBark, matLeavesLight);

        // 9. Ensure Environment Lighting & Camera
        EnsureLightingAndCamera(rootGo);

        Selection.activeGameObject = rootGo;
        UnityEditor.EditorUtility.SetDirty(rootGo);
        Debug.Log("<color=green>[HouseEnvironmentGenerator]</color> 3D House Environment with ProBuilder successfully generated!");
    }

    [MenuItem("Tools/ProBuilder/Clear House Environment", priority = 2)]
    public static void ClearEnvironment()
    {
        GameObject existingRoot = GameObject.Find("HouseEnvironment");
        if (existingRoot != null)
        {
            Undo.DestroyObjectImmediate(existingRoot);
            Debug.Log("[HouseEnvironmentGenerator] HouseEnvironment cleared.");
        }
    }

    // =========================================================================
    // Construction Helpers
    // =========================================================================

    private static void BuildFence(GameObject parent, Material matFence)
    {
        float minX = -9.5f;
        float maxX = 9.5f;
        float minZ = -8.5f;
        float maxZ = 10.5f;
        float postHeight = 1.15f;
        float postSize = 0.2f;

        GameObject postsGroup = CreateChild(parent, "Posts");
        GameObject railsGroup = CreateChild(parent, "Rails");

        // Helper to place post
        void PlacePost(Vector3 pos, string name)
        {
            CreateProBuilderCube(name, postsGroup, pos + new Vector3(0f, postHeight * 0.5f, 0f), new Vector3(postSize, postHeight, postSize), matFence);
        }

        // Helper to place rail between two points
        void PlaceRail(Vector3 from, Vector3 to, string name)
        {
            Vector3 mid = (from + to) * 0.5f;
            float length = Vector3.Distance(from, to);
            Vector3 diff = to - from;

            Vector3 railSize;
            if (Mathf.Abs(diff.x) > Mathf.Abs(diff.z))
                railSize = new Vector3(length, 0.08f, 0.08f);
            else
                railSize = new Vector3(0.08f, 0.08f, length);

            CreateProBuilderCube(name + "_Top", railsGroup, mid + new Vector3(0f, 0.85f, 0f), railSize, matFence);
            CreateProBuilderCube(name + "_Bottom", railsGroup, mid + new Vector3(0f, 0.45f, 0f), railSize, matFence);
        }

        // North boundary (Back)
        float step = 2.0f;
        int countX = Mathf.RoundToInt((maxX - minX) / step);
        float actualStepX = (maxX - minX) / countX;
        for (int i = 0; i <= countX; i++)
        {
            float x = minX + i * actualStepX;
            PlacePost(new Vector3(x, 0f, maxZ), $"Post_Back_{i}");
            if (i < countX)
            {
                PlaceRail(new Vector3(x, 0f, maxZ), new Vector3(x + actualStepX, 0f, maxZ), $"Rail_Back_{i}");
            }
        }

        // West boundary (Left)
        int countZ = Mathf.RoundToInt((maxZ - minZ) / step);
        float actualStepZ = (maxZ - minZ) / countZ;
        for (int i = 0; i <= countZ; i++)
        {
            float z = minZ + i * actualStepZ;
            PlacePost(new Vector3(minX, 0f, z), $"Post_Left_{i}");
            if (i < countZ)
            {
                PlaceRail(new Vector3(minX, 0f, z), new Vector3(minX, 0f, z + actualStepZ), $"Rail_Left_{i}");
            }
        }

        // East boundary (Right)
        for (int i = 0; i <= countZ; i++)
        {
            float z = minZ + i * actualStepZ;
            PlacePost(new Vector3(maxX, 0f, z), $"Post_Right_{i}");
            if (i < countZ)
            {
                PlaceRail(new Vector3(maxX, 0f, z), new Vector3(maxX, 0f, z + actualStepZ), $"Rail_Right_{i}");
            }
        }

        // South boundary (Front) with Gate Opening at center (between X = -1.5 and X = 1.5)
        float gateHalfWidth = 1.5f;

        // Front Left Section: minX to -gateHalfWidth
        int frontSideCount = Mathf.RoundToInt((-gateHalfWidth - minX) / step);
        float frontStep = (-gateHalfWidth - minX) / frontSideCount;
        for (int i = 0; i <= frontSideCount; i++)
        {
            float x = minX + i * frontStep;
            PlacePost(new Vector3(x, 0f, minZ), $"Post_FrontLeft_{i}");
            if (i < frontSideCount)
            {
                PlaceRail(new Vector3(x, 0f, minZ), new Vector3(x + frontStep, 0f, minZ), $"Rail_FrontLeft_{i}");
            }
        }

        // Front Right Section: gateHalfWidth to maxX
        for (int i = 0; i <= frontSideCount; i++)
        {
            float x = gateHalfWidth + i * frontStep;
            PlacePost(new Vector3(x, 0f, minZ), $"Post_FrontRight_{i}");
            if (i < frontSideCount)
            {
                PlaceRail(new Vector3(x, 0f, minZ), new Vector3(x + frontStep, 0f, minZ), $"Rail_FrontRight_{i}");
            }
        }

        // Decorative Gate Posts flanking opening
        PlacePost(new Vector3(-gateHalfWidth, 0f, minZ), "GatePost_Left");
        PlacePost(new Vector3(gateHalfWidth, 0f, minZ), "GatePost_Right");
        CreateProBuilderSphere("GatePostCap_Left", postsGroup, new Vector3(-gateHalfWidth, postHeight + 0.08f, minZ), 0.12f, matFence);
        CreateProBuilderSphere("GatePostCap_Right", postsGroup, new Vector3(gateHalfWidth, postHeight + 0.08f, minZ), 0.12f, matFence);
    }

    private static void BuildStonePathway(GameObject parent, Material matStone)
    {
        // Path from Z = -8.2f (gate) to Z = 0.5f (house porch step)
        float startZ = -8.0f;
        float endZ = 0.4f;
        int steps = 9;
        float stepZ = (endZ - startZ) / (steps - 1);

        for (int i = 0; i < steps; i++)
        {
            float z = startZ + i * stepZ;
            // Slight natural alternating width and offset for flagstone feel
            float width = 1.5f + ((i % 2 == 0) ? 0.1f : -0.1f);
            float length = 0.65f;
            float offsetX = (i % 2 == 0) ? 0.03f : -0.03f;
            CreateProBuilderCube($"Pathway_Stone_{i + 1:D2}", parent, new Vector3(offsetX, 0.03f, z), new Vector3(width, 0.06f, length), matStone);
        }
    }

    private static void CreateWindow(string name, GameObject parent, Vector3 pos, Vector3 size, Material matFrame, Material matGlass, bool isSideWindow)
    {
        GameObject winGo = CreateChild(parent, name);
        float borderThick = 0.08f;
        float depth = isSideWindow ? size.x : size.z;

        if (!isSideWindow)
        {
            // Front / Back facing window (aligned along X-Y)
            // Top border
            CreateProBuilderCube(name + "_FrameTop", winGo, pos + new Vector3(0f, (size.y - borderThick) * 0.5f, 0f), new Vector3(size.x, borderThick, depth), matFrame);
            // Bottom border / Window Sill (slightly wider & deeper)
            CreateProBuilderCube(name + "_FrameBottom", winGo, pos + new Vector3(0f, -(size.y - borderThick) * 0.5f, -0.03f), new Vector3(size.x + 0.14f, borderThick + 0.02f, depth + 0.08f), matFrame);
            // Left border
            CreateProBuilderCube(name + "_FrameLeft", winGo, pos + new Vector3(-(size.x - borderThick) * 0.5f, 0f, 0f), new Vector3(borderThick, size.y, depth), matFrame);
            // Right border
            CreateProBuilderCube(name + "_FrameRight", winGo, pos + new Vector3((size.x - borderThick) * 0.5f, 0f, 0f), new Vector3(borderThick, size.y, depth), matFrame);

            // Exposed Glass Pane
            CreateProBuilderCube(name + "_Glass", winGo, pos, new Vector3(size.x - borderThick * 2f, size.y - borderThick * 2f, 0.04f), matGlass);

            // Mullions / Crossbars
            CreateProBuilderCube(name + "_MullionH", winGo, pos + new Vector3(0f, 0f, -0.01f), new Vector3(size.x - borderThick * 2f, 0.04f, 0.05f), matFrame);
            CreateProBuilderCube(name + "_MullionV", winGo, pos + new Vector3(0f, 0f, -0.01f), new Vector3(0.04f, size.y - borderThick * 2f, 0.05f), matFrame);
        }
        else
        {
            // Side facing window (aligned along Z-Y)
            // Top border
            CreateProBuilderCube(name + "_FrameTop", winGo, pos + new Vector3(0f, (size.y - borderThick) * 0.5f, 0f), new Vector3(depth, borderThick, size.z), matFrame);
            // Bottom border / Window Sill
            float xOffset = pos.x < 0 ? -0.03f : 0.03f;
            CreateProBuilderCube(name + "_FrameBottom", winGo, pos + new Vector3(xOffset, -(size.y - borderThick) * 0.5f, 0f), new Vector3(depth + 0.08f, borderThick + 0.02f, size.z + 0.14f), matFrame);
            // Front border
            CreateProBuilderCube(name + "_FrameFront", winGo, pos + new Vector3(0f, 0f, -(size.z - borderThick) * 0.5f), new Vector3(depth, size.y, borderThick), matFrame);
            // Back border
            CreateProBuilderCube(name + "_FrameBack", winGo, pos + new Vector3(0f, 0f, (size.z - borderThick) * 0.5f), new Vector3(depth, size.y, borderThick), matFrame);

            // Exposed Glass Pane
            CreateProBuilderCube(name + "_Glass", winGo, pos, new Vector3(0.04f, size.y - borderThick * 2f, size.z - borderThick * 2f), matGlass);

            // Mullions / Crossbars
            CreateProBuilderCube(name + "_MullionH", winGo, pos + new Vector3(xOffset * 0.3f, 0f, 0f), new Vector3(0.05f, 0.04f, size.z - borderThick * 2f), matFrame);
            CreateProBuilderCube(name + "_MullionV", winGo, pos + new Vector3(xOffset * 0.3f, 0f, 0f), new Vector3(0.05f, size.y - borderThick * 2f, 0.04f), matFrame);
        }
    }

    private static void BuildFlowerBed(string name, GameObject parent, Vector3 pos, Vector3 size, Material matSoil, Material matStem, Material[] petalMaterials)
    {
        GameObject bedGo = CreateChild(parent, name);
        // Soil Mound
        CreateProBuilderCube(name + "_Soil", bedGo, pos + new Vector3(0f, size.y * 0.5f, 0f), size, matSoil);

        // Flowers planted in grid
        int rows = 2;
        int cols = 4;
        float startX = pos.x - size.x * 0.35f;
        float stepX = (size.x * 0.7f) / (cols - 1);
        float startZ = pos.z - size.z * 0.28f;
        float stepZ = (size.z * 0.56f) / (rows - 1);

        int flowerIdx = 0;
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                float fx = startX + c * stepX + UnityEngine.Random.Range(-0.06f, 0.06f);
                float fz = startZ + r * stepZ + UnityEngine.Random.Range(-0.06f, 0.06f);
                float baseHeight = pos.y + size.y;

                Material petalMat = petalMaterials[(flowerIdx++) % petalMaterials.Length];
                BuildFlower($"Flower_{flowerIdx}", bedGo, new Vector3(fx, baseHeight, fz), matStem, petalMat);
            }
        }
    }

    private static void BuildFlower(string name, GameObject parent, Vector3 basePos, Material matStem, Material matPetal)
    {
        GameObject flGo = CreateChild(parent, name);
        float stemHeight = 0.32f;
        // Stem
        CreateProBuilderCylinder(name + "_Stem", flGo, basePos + new Vector3(0f, stemHeight * 0.5f, 0f), 0.025f, stemHeight, 6, matStem);

        // Blossom Center / Petals
        Vector3 blossomPos = basePos + new Vector3(0f, stemHeight + 0.06f, 0f);
        CreateProBuilderSphere(name + "_Petals", flGo, blossomPos, 0.09f, matPetal);
    }

    private static void BuildTree(string name, GameObject parent, Vector3 pos, float trunkHeight, float foliageRadius, Material matBark, Material matLeaves)
    {
        GameObject treeGo = CreateChild(parent, name);

        // Trunk
        float trunkRadius = 0.22f;
        CreateProBuilderCylinder(name + "_Trunk", treeGo, pos + new Vector3(0f, trunkHeight * 0.5f, 0f), trunkRadius, trunkHeight, 10, matBark);

        // Canopy - Stylized low-poly cluster of spheres
        Vector3 canopyBase = pos + new Vector3(0f, trunkHeight + foliageRadius * 0.6f, 0f);
        CreateProBuilderSphere(name + "_FoliageMain", treeGo, canopyBase, foliageRadius, matLeaves);
        CreateProBuilderSphere(name + "_FoliageTop", treeGo, canopyBase + new Vector3(0f, foliageRadius * 0.65f, 0f), foliageRadius * 0.75f, matLeaves);
        CreateProBuilderSphere(name + "_FoliageL", treeGo, canopyBase + new Vector3(-foliageRadius * 0.4f, -foliageRadius * 0.2f, 0f), foliageRadius * 0.65f, matLeaves);
        CreateProBuilderSphere(name + "_FoliageR", treeGo, canopyBase + new Vector3(foliageRadius * 0.4f, -foliageRadius * 0.2f, 0f), foliageRadius * 0.65f, matLeaves);
        CreateProBuilderSphere(name + "_FoliageF", treeGo, canopyBase + new Vector3(0f, -foliageRadius * 0.2f, foliageRadius * 0.4f), foliageRadius * 0.65f, matLeaves);
    }

    private static void EnsureLightingAndCamera(GameObject root)
    {
        // Directional Light
        Light dirLight = Object.FindAnyObjectByType<Light>();
        if (dirLight == null || dirLight.type != LightType.Directional)
        {
            GameObject lightGo = new GameObject("Directional Light");
            lightGo.transform.SetParent(root.transform);
            lightGo.transform.position = new Vector3(0f, 15f, 0f);
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            Light light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1.0f, 0.96f, 0.88f);
            light.intensity = 1.2f;
            light.shadows = LightShadows.Soft;
        }

        // Camera setup
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject camGo = new GameObject("Main Camera");
            camGo.transform.SetParent(root.transform);
            cam = camGo.AddComponent<Camera>();
            camGo.tag = "MainCamera";
            camGo.AddComponent<AudioListener>();
        }

        // Position camera to frame the entire house, pathway, garden, and fence
        cam.transform.position = new Vector3(0f, 6.0f, -13.0f);
        cam.transform.rotation = Quaternion.Euler(18f, 0f, 0f);
    }

    // =========================================================================
    // ProBuilder Primitive Generation
    // =========================================================================

    private static GameObject CreateProBuilderCube(string name, GameObject parent, Vector3 localPos, Vector3 size, Material mat)
    {
        ProBuilderMesh pbMesh = ShapeGenerator.GenerateCube(PivotLocation.Center, size);
        ConfigureProBuilderObject(pbMesh, name, parent, localPos, Quaternion.identity, mat);
        return pbMesh.gameObject;
    }

    private static GameObject CreateProBuilderPrism(string name, GameObject parent, Vector3 localPos, Vector3 size, Material mat)
    {
        ProBuilderMesh pbMesh = ShapeGenerator.GeneratePrism(PivotLocation.Center, size);
        ConfigureProBuilderObject(pbMesh, name, parent, localPos, Quaternion.identity, mat);
        return pbMesh.gameObject;
    }

    private static GameObject CreateProBuilderCylinder(string name, GameObject parent, Vector3 localPos, float radius, float height, int axisDivisions, Material mat)
    {
        ProBuilderMesh pbMesh = ShapeGenerator.GenerateCylinder(PivotLocation.Center, axisDivisions, radius, height, 0, -1);
        ConfigureProBuilderObject(pbMesh, name, parent, localPos, Quaternion.identity, mat);
        return pbMesh.gameObject;
    }

    private static GameObject CreateProBuilderSphere(string name, GameObject parent, Vector3 localPos, float radius, Material mat)
    {
        // GenerateIcosahedron produces a low-poly stylized spherical mesh
        ProBuilderMesh pbMesh = ShapeGenerator.GenerateIcosahedron(PivotLocation.Center, radius, 1, true, false);
        ConfigureProBuilderObject(pbMesh, name, parent, localPos, Quaternion.identity, mat);
        return pbMesh.gameObject;
    }

    private static void ConfigureProBuilderObject(ProBuilderMesh pbMesh, string name, GameObject parent, Vector3 localPos, Quaternion localRot, Material mat)
    {
        GameObject go = pbMesh.gameObject;
        go.name = name;
        if (parent != null)
        {
            go.transform.SetParent(parent.transform, false);
        }
        go.transform.localPosition = localPos;
        go.transform.localRotation = localRot;

        // Apply material
        if (mat != null)
        {
            pbMesh.SetMaterial(pbMesh.faces, mat);
        }

        // Rebuild mesh & colliders
        pbMesh.ToMesh();
        pbMesh.Refresh();
        EditorMeshUtility.Optimize(pbMesh);
    }

    private static GameObject CreateChild(GameObject parent, string name)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent.transform, false);
        return child;
    }

    // =========================================================================
    // Material Creation Helpers
    // =========================================================================

    private static void EnsureMaterialsFolder()
    {
        if (!Directory.Exists(MaterialsFolder))
        {
            Directory.CreateDirectory(MaterialsFolder);
            AssetDatabase.Refresh();
        }
    }

    private static Material GetOrCreateMaterial(string fileName, Color color, float smoothness)
    {
        string fullPath = $"{MaterialsFolder}/{fileName}";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(fullPath);
        if (mat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Mobile/Diffuse");

            mat = new Material(shader) { name = Path.GetFileNameWithoutExtension(fileName) };
            SetMaterialProperties(mat, color, smoothness);
            AssetDatabase.CreateAsset(mat, fullPath);
        }
        else
        {
            SetMaterialProperties(mat, color, smoothness);
            UnityEditor.EditorUtility.SetDirty(mat);
        }
        return mat;
    }

    private static void SetMaterialProperties(Material mat, Color color, float smoothness)
    {
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);

        // If translucent/glass
        if (color.a < 1.0f)
        {
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1); // 1 = Transparent in URP
            mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = (int)RenderQueue.Transparent;
        }
    }
}
