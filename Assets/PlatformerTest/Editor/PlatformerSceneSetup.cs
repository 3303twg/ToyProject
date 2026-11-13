using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class PlatformerSceneSetup
{
    const string PrefKey = "AstarTest.PlatformerTest.NewScene1.SetupV1";
    const string ScenePath = "Assets/Scenes/New Scene 1.unity";
    const string RootFolder = "Assets/PlatformerTest";
    const string SpritePath = RootFolder + "/Square.png";
    const string MaterialPath = RootFolder + "/SpriteUnlit.mat";
    const string PhysicsMaterialPath = RootFolder + "/PlayerNoFriction.physicsMaterial2D";

    static int attempts;
    static bool sceneHooked;

    static PlatformerSceneSetup()
    {
        EditorApplication.delayCall += SetupIfNeeded;
    }

    [MenuItem("Tools/Setup 2D Platformer Test")]
    static void SetupFromMenu()
    {
        EditorPrefs.SetBool(PrefKey, false);
        attempts = 0;
        SetupIfNeeded();
    }

    static void SetupIfNeeded()
    {
        if (EditorPrefs.GetBool(PrefKey, false))
            return;

        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.delayCall += SetupIfNeeded;
            return;
        }

        if (attempts++ > 40)
        {
            Debug.LogWarning("[PlatformerTest] Setup gave up waiting for New Scene 1.");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        bool isTarget = scene.IsValid() && (scene.path == ScenePath || scene.name == "New Scene 1");
        if (!isTarget)
        {
            HookSceneChange();
            return;
        }

        if (GameObject.Find("Player") != null)
        {
            EditorPrefs.SetBool(PrefKey, true);
            return;
        }

        Sprite sprite = GetOrCreateSprite();
        Material material = GetOrCreateMaterial();
        PhysicsMaterial2D playerMaterial = GetOrCreatePhysicsMaterial();
        if (sprite == null || material == null)
        {
            Debug.LogError("[PlatformerTest] Could not create the square sprite.");
            EditorApplication.delayCall += SetupIfNeeded;
            return;
        }

        var map = new GameObject("TestMap");
        Undo.RegisterCreatedObjectUndo(map, "Create 2D platformer test");

        Color ground = new Color(0.22f, 0.24f, 0.28f);
        Color platform = new Color(0.35f, 0.72f, 0.42f);
        Color step = new Color(0.85f, 0.72f, 0.28f);

        CreateBlock("GroundLeft", sprite, material, map.transform, new Vector3(-7f, -1f, 0f), new Vector2(12f, 1f), ground);
        CreateBlock("GroundRight", sprite, material, map.transform, new Vector3(7.5f, -1f, 0f), new Vector2(12f, 1f), ground);
        CreateBlock("GapStep", sprite, material, map.transform, new Vector3(0.25f, -0.15f, 0f), new Vector2(1.6f, 0.35f), step);
        CreateBlock("PlatformLow", sprite, material, map.transform, new Vector3(3.6f, 0.9f, 0f), new Vector2(2.4f, 0.4f), platform);
        CreateBlock("PlatformMid", sprite, material, map.transform, new Vector3(6.6f, 2.3f, 0f), new Vector2(2.2f, 0.4f), platform);
        CreateBlock("PlatformHigh", sprite, material, map.transform, new Vector3(9.8f, 3.7f, 0f), new Vector2(2.2f, 0.4f), platform);
        CreateBlock("Wall", sprite, material, map.transform, new Vector3(13.2f, 0.6f, 0f), new Vector2(0.6f, 3.2f), ground);

        GameObject player = CreateBlock("Player", sprite, material, null, new Vector3(-8f, 1.2f, 0f), new Vector2(0.7f, 1.15f), new Color(0.25f, 0.78f, 0.95f));
        player.GetComponent<SpriteRenderer>().sortingOrder = 2;
        var collider = player.GetComponent<BoxCollider2D>();
        collider.sharedMaterial = playerMaterial;

        var body = player.AddComponent<Rigidbody2D>();
        body.gravityScale = 3f;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        player.AddComponent<PlatformerController>();

        Camera camera = Camera.main;
        if (camera != null)
        {
            Undo.RecordObject(camera, "Setup 2D camera");
            Undo.RecordObject(camera.transform, "Setup 2D camera");
            camera.orthographic = true;
            camera.orthographicSize = 4.5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.53f, 0.74f, 0.93f);
            camera.transform.position = new Vector3(-8f, 2f, -10f);
            camera.transform.rotation = Quaternion.identity;
            camera.nearClipPlane = 0.1f;

            if (camera.GetComponent<UniversalAdditionalCameraData>() == null)
                camera.gameObject.AddComponent<UniversalAdditionalCameraData>();

            var follow = camera.GetComponent<CameraFollow2D>();
            if (follow == null)
                follow = Undo.AddComponent<CameraFollow2D>(camera.gameObject);
            follow.target = player.transform;
            follow.offset = new Vector3(0f, 1.2f, -10f);
            EditorUtility.SetDirty(follow);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        EditorPrefs.SetBool(PrefKey, true);
        Debug.Log("[PlatformerTest] New Scene 1 setup complete. Move with A/D or arrows, jump with Space / W / Up.");
    }

    static void HookSceneChange()
    {
        if (sceneHooked)
            return;

        sceneHooked = true;
        EditorSceneManager.activeSceneChangedInEditMode += (_, next) =>
        {
            if (next.path == ScenePath)
                EditorApplication.delayCall += SetupIfNeeded;
        };
    }

    static GameObject CreateBlock(string name, Sprite sprite, Material material, Transform parent, Vector3 position, Vector2 size, Color color)
    {
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        if (parent != null)
            go.transform.SetParent(parent, true);
        go.transform.position = position;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);

        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sharedMaterial = material;
        renderer.color = color;
        go.AddComponent<BoxCollider2D>();
        return go;
    }

    static Sprite GetOrCreateSprite()
    {
        const string builtin = "Packages/com.unity.2d.sprite/Editor/ObjectMenuCreation/DefaultAssets/Textures/v2/Square.png";
        Sprite builtinSprite = AssetDatabase.LoadAssetAtPath<Sprite>(builtin);
        if (builtinSprite != null)
            return builtinSprite;

        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
        if (existing != null)
            return existing;

        EnsureFolder();
        if (!File.Exists(SpritePath))
        {
            var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var pixels = new Color[16];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.white;
            texture.SetPixels(pixels);
            texture.Apply();
            File.WriteAllBytes(SpritePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(SpritePath, ImportAssetOptions.ForceUpdate);
        }

        var importer = AssetImporter.GetAtPath(SpritePath) as TextureImporter;
        if (importer == null)
            return null;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 4f;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
    }

    static Material GetOrCreateMaterial()
    {
        const string builtin = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";
        Material builtinMaterial = AssetDatabase.LoadAssetAtPath<Material>(builtin);
        if (builtinMaterial != null)
            return builtinMaterial;

        Material existing = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (existing != null)
            return existing;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader == null)
            return null;

        EnsureFolder();
        var material = new Material(shader);
        AssetDatabase.CreateAsset(material, MaterialPath);
        return material;
    }

    static PhysicsMaterial2D GetOrCreatePhysicsMaterial()
    {
        PhysicsMaterial2D existing = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(PhysicsMaterialPath);
        if (existing != null)
            return existing;

        EnsureFolder();
        var material = new PhysicsMaterial2D("PlayerNoFriction")
        {
            friction = 0f,
            bounciness = 0f
        };
        AssetDatabase.CreateAsset(material, PhysicsMaterialPath);
        return material;
    }

    static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder(RootFolder))
            AssetDatabase.CreateFolder("Assets", "PlatformerTest");
    }
}
