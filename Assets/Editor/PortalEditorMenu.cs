#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

static class PortalEditorMenu
{
    const string ForegroundLayerName = "Foreground";

    // ---------- Portal creation ----------

    // Ctrl+Alt+Shift+P
    [MenuItem("Tools/Portals/Create Portal Pair %&#p")]
    static void CreatePair()
    {
        var sv = SceneView.lastActiveSceneView;
        Vector3 center = sv != null ? new Vector3(sv.pivot.x, sv.pivot.y, 0f) : Vector3.zero;

        var a = CreatePortal("Portal_A", center + Vector3.left * 3f);
        var b = CreatePortal("Portal_B", center + Vector3.right * 3f);

        a.linkedPortal = b;
        b.linkedPortal = a;
        EditorUtility.SetDirty(a);
        EditorUtility.SetDirty(b);

        Undo.RegisterCreatedObjectUndo(a.gameObject, "Create Portal Pair");
        Undo.RegisterCreatedObjectUndo(b.gameObject, "Create Portal Pair");

        Selection.objects = new Object[] { a.gameObject, b.gameObject };
        if (sv != null) sv.FrameSelected();
    }

    // Ctrl+Alt+Shift+L
    [MenuItem("Tools/Portals/Link Selected Pair %&#l")]
    static void LinkSelected()
    {
        var sel = Selection.gameObjects;
        Portal a = null, b = null;
        foreach (var go in sel)
        {
            var p = go.GetComponent<Portal>();
            if (p == null) continue;
            if (a == null) a = p;
            else if (b == null) { b = p; break; }
        }
        if (a == null || b == null)
        {
            Debug.LogWarning("Выдели в иерархии ровно два объекта с компонентом Portal.");
            return;
        }
        Undo.RecordObject(a, "Link Portals");
        Undo.RecordObject(b, "Link Portals");
        a.linkedPortal = b;
        b.linkedPortal = a;
        EditorUtility.SetDirty(a);
        EditorUtility.SetDirty(b);
        Debug.Log($"Linked {a.name} ↔ {b.name}", a);
    }

    [MenuItem("Tools/Portals/Add Reveal Vision Controller to Scene")]
    static void AddController()
    {
        var existing = Object.FindFirstObjectByType<RevealVisionController>();
        if (existing != null)
        {
            Selection.activeObject = existing.gameObject;
            Debug.Log("RevealVisionController уже есть в сцене.", existing);
            return;
        }
        var go = new GameObject("RevealVisionController");
        go.AddComponent<RevealVisionController>();
        Undo.RegisterCreatedObjectUndo(go, "Add Reveal Vision Controller");
        Selection.activeObject = go;
    }

    // ---------- Foreground (immune to desaturation) ----------

    [MenuItem("Tools/Portals/Setup Foreground Camera Stack")]
    static void SetupForegroundStack()
    {
        int layer = EnsureLayer(ForegroundLayerName);
        if (layer < 0)
        {
            Debug.LogError("Нет свободного слота для слоя 'Foreground' в Tags & Layers. Освободи один и повтори.");
            return;
        }

        var mainCam = Camera.main;
        if (mainCam == null)
        {
            mainCam = Object.FindFirstObjectByType<Camera>();
            if (mainCam == null)
            {
                Debug.LogError("В сцене нет ни одной камеры.");
                return;
            }
        }

        int fgBit = 1 << layer;

        Undo.RecordObject(mainCam, "Setup Foreground Stack");
        mainCam.cullingMask &= ~fgBit;

        var mainData = mainCam.GetUniversalAdditionalCameraData();
        Undo.RecordObject(mainData, "Setup Foreground Stack");
        mainData.renderType = CameraRenderType.Base;
        mainData.renderPostProcessing = true;

        Transform overlayT = mainCam.transform.Find("ForegroundOverlayCamera");
        Camera overlay;
        if (overlayT != null) overlay = overlayT.GetComponent<Camera>();
        else
        {
            var overlayGO = new GameObject("ForegroundOverlayCamera");
            overlayGO.transform.SetParent(mainCam.transform, false);
            overlayGO.transform.localPosition = Vector3.zero;
            overlayGO.transform.localRotation = Quaternion.identity;
            overlay = overlayGO.AddComponent<Camera>();
            Undo.RegisterCreatedObjectUndo(overlayGO, "Create Overlay Camera");
        }

        Undo.RecordObject(overlay, "Setup Overlay");
        overlay.orthographic = mainCam.orthographic;
        overlay.orthographicSize = mainCam.orthographicSize;
        overlay.fieldOfView = mainCam.fieldOfView;
        overlay.nearClipPlane = mainCam.nearClipPlane;
        overlay.farClipPlane = mainCam.farClipPlane;
        overlay.clearFlags = CameraClearFlags.Depth;
        overlay.cullingMask = fgBit;

        var overlayData = overlay.GetUniversalAdditionalCameraData();
        Undo.RecordObject(overlayData, "Setup Overlay");
        overlayData.renderType = CameraRenderType.Overlay;
        overlayData.renderPostProcessing = false;

        if (!mainData.cameraStack.Contains(overlay))
        {
            mainData.cameraStack.Add(overlay);
        }

        EditorUtility.SetDirty(mainCam);
        EditorUtility.SetDirty(mainData);
        EditorUtility.SetDirty(overlay);
        EditorUtility.SetDirty(overlayData);

        Debug.Log($"Готово. Слой '{ForegroundLayerName}' = {layer}. " +
                  $"Теперь выдели кота/порталы и вызови Tools → Portals → Mark Selected As Foreground.", mainCam);
    }

    [MenuItem("Tools/Portals/Mark Selected As Foreground")]
    static void MarkSelectedAsForeground()
    {
        int layer = LayerMask.NameToLayer(ForegroundLayerName);
        if (layer < 0)
        {
            Debug.LogError("Слой 'Foreground' ещё не создан. Сначала запусти Tools → Portals → Setup Foreground Camera Stack.");
            return;
        }
        if (Selection.gameObjects.Length == 0)
        {
            Debug.LogWarning("Выдели в иерархии хотя бы один GameObject.");
            return;
        }
        foreach (var go in Selection.gameObjects)
        {
            Undo.RegisterFullObjectHierarchyUndo(go, "Set Foreground Layer");
            SetLayerRecursive(go, layer);
        }
        Debug.Log($"Помечено как Foreground: {Selection.gameObjects.Length} объект(ов).");
    }

    [MenuItem("Tools/Portals/Set Selected Sprites To Unlit (fix чёрных спрайтов)")]
    static void SetSelectedToUnlit()
    {
        if (Selection.gameObjects.Length == 0)
        {
            Debug.LogWarning("Выдели объект(ы) со SpriteRenderer.");
            return;
        }
        var unlit = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
        if (unlit == null)
        {
            Debug.LogError("Не нашёл встроенный материал Sprites-Default.mat.");
            return;
        }
        int changed = 0;
        foreach (var go in Selection.gameObjects)
        {
            var renderers = go.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var sr in renderers)
            {
                Undo.RecordObject(sr, "Set Unlit Material");
                sr.sharedMaterial = unlit;
                EditorUtility.SetDirty(sr);
                changed++;
            }
        }
        Debug.Log($"Переведено на Sprites/Default (unlit): {changed} SpriteRenderer.");
    }

    static void SetLayerRecursive(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform)
            SetLayerRecursive(child.gameObject, layer);
    }

    static int EnsureLayer(string name)
    {
        int existing = LayerMask.NameToLayer(name);
        if (existing >= 0) return existing;

        var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (assets == null || assets.Length == 0) return -1;
        var tagManager = new SerializedObject(assets[0]);
        var layersProp = tagManager.FindProperty("layers");
        if (layersProp == null || !layersProp.isArray) return -1;

        for (int i = 8; i < 32; i++)
        {
            var sp = layersProp.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(sp.stringValue))
            {
                sp.stringValue = name;
                tagManager.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.SaveAssets();
                return i;
            }
        }
        return -1;
    }

    // ---------- Helpers ----------

    static Portal CreatePortal(string name, Vector3 pos)
    {
        var go = new GameObject(name);
        go.transform.position = pos;

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1.2f, 2.2f);

        var visualGO = new GameObject("Visual");
        visualGO.transform.SetParent(go.transform, false);
        var sr = visualGO.AddComponent<SpriteRenderer>();
        sr.sprite = BuildPortalSprite();
        sr.color = new Color(0.55f, 0.9f, 1f, 0.85f);
        visualGO.transform.localScale = new Vector3(1.2f, 2.2f, 1f);

        int fg = LayerMask.NameToLayer(ForegroundLayerName);
        if (fg >= 0)
        {
            go.layer = fg;
            visualGO.layer = fg;
        }

        return go.AddComponent<Portal>();
    }

    static Sprite cachedSprite;
    static Sprite BuildPortalSprite()
    {
        if (cachedSprite != null) return cachedSprite;
        var tex = new Texture2D(8, 8, TextureFormat.RGBA32, false);
        var px = new Color[64];
        for (int i = 0; i < 64; i++) px[i] = Color.white;
        tex.SetPixels(px);
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        cachedSprite = Sprite.Create(tex, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f), 8);
        cachedSprite.name = "PortalDefault";
        return cachedSprite;
    }
}
#endif
