using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class BeaconSystem : MonoBehaviour
{
    [SerializeField] Transform player;
    [SerializeField] Color beaconAColor = new Color(0.30f, 0.75f, 1.00f, 1f);
    [SerializeField] Color beaconBColor = new Color(1.00f, 0.45f, 0.30f, 1f);
    [SerializeField] Color riftColor    = new Color(0.65f, 0.30f, 1.00f, 0.75f);
    [SerializeField] float riftHeight = 12f;
    [SerializeField] int beaconSortingOrder = 50;
    [SerializeField] int riftSortingOrder = 5;

    Sprite beaconSpriteA; // треугольник
    Sprite beaconSpriteB; // ромб
    Sprite riftSprite;
    GameObject beaconA;
    GameObject beaconB;
    GameObject rift;
    FoldSnapshot snapshot;

    // Состояние для HUD: можно ли сейчас поставить новый маяк.
    public bool BeaconAPlaced => beaconA != null;
    public bool BeaconBPlaced => beaconB != null;
    public bool RiftActive    => rift != null;
    public bool CanPlaceBeaconA => beaconA == null && rift == null;
    public bool CanPlaceBeaconB => beaconB == null && rift == null;

    class FoldSnapshot
    {
        public List<GameObject> hidden = new();                    // Foldable, попавшие в зону — скрыты
        public List<GameObject> shifted = new();                   // Foldable правее зоны — сдвинуты на shiftWidth
        public List<(GameObject go, float shift)> pushed = new();  // Shiftable — у каждого свой сдвиг (может быть < shiftWidth, если объект стоял внутри зоны)
        public float shiftWidth;
    }

    void Awake()
    {
        beaconSpriteA = MakeTriangleSprite(32, 32f);
        beaconSpriteB = MakeDiamondSprite(32, 32f);
        riftSprite    = MakeRectSprite(4, 64, 32f);
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null || player == null) return;

        if (kb.qKey.wasPressedThisFrame) ToggleBeacon(ref beaconA, "BeaconA", beaconAColor, beaconSpriteA);
        if (kb.eKey.wasPressedThisFrame) ToggleBeacon(ref beaconB, "BeaconB", beaconBColor, beaconSpriteB);
        if (kb.fKey.wasPressedThisFrame)
        {
            if (rift != null) Unfold();
            else              TryFold();
        }
    }

    void ToggleBeacon(ref GameObject beacon, string name, Color color, Sprite sprite)
    {
        if (beacon != null)
        {
            Destroy(beacon);
            beacon = null;
            return;
        }
        // Пока активен разлом — новые маки ставить нельзя: сначала F, чтобы свернуть разлом обратно.
        if (rift != null) return;
        beacon = new GameObject(name);
        beacon.transform.position = new Vector3(player.position.x, player.position.y, 0f);
        var sr = beacon.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = beaconSortingOrder;
    }

    void TryFold()
    {
        if (beaconA == null || beaconB == null) return;

        Vector3 a = beaconA.transform.position;
        Vector3 b = beaconB.transform.position;
        float leftX  = Mathf.Min(a.x, b.x);
        float rightX = Mathf.Max(a.x, b.x);
        float width  = rightX - leftX;
        if (width < 0.0001f) return;

        var snap = new FoldSnapshot { shiftWidth = width };

        var foldables = Object.FindObjectsByType<Foldable>(FindObjectsSortMode.None);
        foreach (var f in foldables)
        {
            float x = f.transform.position.x;
            if (x >= leftX && x <= rightX)
            {
                f.gameObject.SetActive(false);
                snap.hidden.Add(f.gameObject);
            }
            else if (x > rightX)
            {
                var p = f.transform.position;
                p.x -= width;
                f.transform.position = p;
                snap.shifted.Add(f.gameObject);
            }
        }

        // Shiftable: переезжают, но не прячутся.
        // Внутри зоны → подтягиваются к leftX (шву); правее зоны → сдвигаются на width, как Foldable.
        var shiftables = Object.FindObjectsByType<Shiftable>(FindObjectsSortMode.None);
        Debug.Log($"[Shiftable] TryFold: zone=[{leftX:F2},{rightX:F2}] width={width:F2}, найдено Shiftable в сцене: {shiftables.Length}");
        foreach (var s in shiftables)
        {
            float x = s.transform.position.x;
            if (x < leftX)
            {
                Debug.Log($"[Shiftable] {s.name} x={x:F2} левее leftX — не двигаем");
                continue;
            }
            float shift = Mathf.Min(width, x - leftX);
            var p = s.transform.position;
            p.x -= shift;
            s.transform.position = p;
            snap.pushed.Add((s.gameObject, shift));
            Debug.Log($"[Shiftable] {s.name}: x {x:F2} → {p.x:F2} (shift={shift:F2})");
        }

        // Shiftable — ограничители: если из-за сдвига игрок поменял сторону относительно
        // препятствия, прижимаем его обратно к той же стороне, что и до свёртки.
        EnforceShiftableBarriers(snap.pushed);

        SpawnRift(leftX, (a.y + b.y) * 0.5f);
        snapshot = snap;

        Destroy(beaconA);
        Destroy(beaconB);
        beaconA = null;
        beaconB = null;
    }

    void Unfold()
    {
        if (rift != null)
        {
            Destroy(rift);
            rift = null;
        }
        if (snapshot == null) return;

        foreach (var go in snapshot.hidden)
        {
            if (go != null) go.SetActive(true);
        }
        foreach (var go in snapshot.shifted)
        {
            if (go == null) continue;
            var p = go.transform.position;
            p.x += snapshot.shiftWidth;
            go.transform.position = p;
        }
        foreach (var entry in snapshot.pushed)
        {
            if (entry.go == null) continue;
            var p = entry.go.transform.position;
            p.x += entry.shift;
            entry.go.transform.position = p;
        }

        snapshot = null;
    }

    /// <summary>
    /// Полный сброс состояния разлома: разворачивает активный разлом обратно
    /// и убирает поставленные маяки. Нужен для «дырки в полу»/респауна —
    /// чтобы игрок начинал уровень с чистого состояния.
    /// </summary>
    public void ResetAll()
    {
        if (rift != null) Unfold();

        if (beaconA != null) { Destroy(beaconA); beaconA = null; }
        if (beaconB != null) { Destroy(beaconB); beaconB = null; }
    }

    void EnforceShiftableBarriers(List<(GameObject go, float shift)> shifted)
    {
        Debug.Log($"[Shiftable] EnforceShiftableBarriers: {shifted.Count} shifted entries");

        if (player == null) { Debug.LogWarning("[Shiftable] player == null — пропуск"); return; }
        var playerColl = player.GetComponent<Collider2D>();
        var rb = player.GetComponent<Rigidbody2D>();
        if (playerColl == null) Debug.LogWarning("[Shiftable] У игрока нет Collider2D — буду использовать позицию transform");
        if (rb == null)         Debug.LogWarning("[Shiftable] У игрока нет Rigidbody2D — пушу через transform.position");

        float originalPlayerX = playerColl != null
            ? playerColl.bounds.center.x
            : player.position.x;
        Debug.Log($"[Shiftable] originalPlayerX = {originalPlayerX:F3}");

        const float margin = 0.02f;

        foreach (var entry in shifted)
        {
            if (entry.go == null) { Debug.Log("[Shiftable] entry.go == null — пропуск"); continue; }
            if (entry.shift <= 0f) { Debug.Log($"[Shiftable] {entry.go.name}: shift={entry.shift} ≤ 0 — пропуск"); continue; }

            if (!TryGetWorldBoundsX(entry.go, out var sb, out string boundsSource))
            {
                Debug.LogWarning($"[Shiftable] {entry.go.name}: НЕ нашёл ни Collider2D, ни SpriteRenderer — пропуск. Барьер не сработает.");
                continue;
            }

            Bounds pb;
            if (playerColl != null) pb = playerColl.bounds;
            else
            {
                var pp = player.position;
                pb = new Bounds(pp, new Vector3(0.5f, 0.5f, 0f));
            }

            Debug.Log($"[Shiftable] {entry.go.name}: shift={entry.shift:F2}, bounds via {boundsSource}, " +
                      $"sb.X=[{sb.min.x:F2},{sb.max.x:F2}] sb.Y=[{sb.min.y:F2},{sb.max.y:F2}] " +
                      $"pb.X=[{pb.min.x:F2},{pb.max.x:F2}] pb.Y=[{pb.min.y:F2},{pb.max.y:F2}]");

            // Диагностика рассогласования спрайт↔коллайдер у Shiftable.
            var dbgSr  = entry.go.GetComponentInChildren<SpriteRenderer>();
            var dbgCol = entry.go.GetComponentInChildren<Collider2D>();
            if (dbgSr != null && dbgCol != null)
            {
                float dx = dbgCol.bounds.center.x - dbgSr.bounds.center.x;
                if (Mathf.Abs(dx) > 0.05f)
                    Debug.LogWarning($"[Shiftable] {entry.go.name}: спрайт.center.x={dbgSr.bounds.center.x:F3}, " +
                                     $"коллайдер.center.x={dbgCol.bounds.center.x:F3} — рассогласование {dx:F3}! " +
                                     $"Поправь Collider2D.offset или иерархию, иначе коллайдер блокирует не там, где видно стену.");
            }

            if (pb.max.y < sb.min.y || pb.min.y > sb.max.y)
            {
                Debug.Log($"[Shiftable] {entry.go.name}: Y-диапазоны не пересекаются — пропуск (игрок выше/ниже)");
                continue;
            }

            float oldCenter = sb.center.x + entry.shift;
            Debug.Log($"[Shiftable] {entry.go.name}: oldCenter={oldCenter:F3}, originalPlayerX={originalPlayerX:F3} ⇒ " +
                      (originalPlayerX < oldCenter ? "игрок был СЛЕВА" :
                       originalPlayerX > oldCenter ? "игрок был СПРАВА" : "по центру"));

            if (originalPlayerX < oldCenter)
            {
                float targetMax = sb.min.x - margin;
                if (pb.max.x > targetMax)
                {
                    float dx = targetMax - pb.max.x;
                    Debug.Log($"[Shiftable] {entry.go.name}: ПУШ влево на {dx:F3} (targetMax={targetMax:F3}, pb.max.x={pb.max.x:F3})");
                    ShiftPlayer(dx, rb);
                }
                else
                {
                    Debug.Log($"[Shiftable] {entry.go.name}: пуш не нужен — pb.max.x={pb.max.x:F3} ≤ targetMax={targetMax:F3}");
                }
            }
            else if (originalPlayerX > oldCenter)
            {
                float targetMin = sb.max.x + margin;
                if (pb.min.x < targetMin)
                {
                    float dx = targetMin - pb.min.x;
                    Debug.Log($"[Shiftable] {entry.go.name}: ПУШ вправо на {dx:F3} (targetMin={targetMin:F3}, pb.min.x={pb.min.x:F3})");
                    ShiftPlayer(dx, rb);
                }
                else
                {
                    Debug.Log($"[Shiftable] {entry.go.name}: пуш не нужен — pb.min.x={pb.min.x:F3} ≥ targetMin={targetMin:F3}");
                }
            }
        }

        float finalPlayerX = playerColl != null ? playerColl.bounds.center.x : player.position.x;
        Debug.Log($"[Shiftable] финал: player.x={finalPlayerX:F3} (был {originalPlayerX:F3}, сдвиг {finalPlayerX - originalPlayerX:F3})");
    }

    // Берём визуальные bounds — то, что игрок видит как стену.
    // SpriteRenderer приоритетнее Collider2D: если у объекта коллайдер с offset, важна именно
    // визуальная сторона свёртки (иначе спрайт «телепортируется» сквозь игрока, а коллайдер — нет,
    // и логика «не пускать» не срабатывает).
    static bool TryGetWorldBoundsX(GameObject go, out Bounds bounds, out string source)
    {
        var sr = go.GetComponent<SpriteRenderer>();
        if (sr != null) { bounds = sr.bounds; source = "SpriteRenderer (self)"; return true; }
        var srChild = go.GetComponentInChildren<SpriteRenderer>();
        if (srChild != null) { bounds = srChild.bounds; source = $"SpriteRenderer (child: {srChild.name})"; return true; }
        var col = go.GetComponent<Collider2D>();
        if (col != null) { bounds = col.bounds; source = "Collider2D (self)"; return true; }
        var colChild = go.GetComponentInChildren<Collider2D>();
        if (colChild != null) { bounds = colChild.bounds; source = $"Collider2D (child: {colChild.name})"; return true; }
        bounds = default;
        source = "none";
        return false;
    }

    void ShiftPlayer(float dx, Rigidbody2D rb)
    {
        if (rb != null)
        {
            rb.position = new Vector2(rb.position.x + dx, rb.position.y);
        }
        else
        {
            var p = player.position;
            p.x += dx;
            player.position = p;
        }
    }

    void SpawnRift(float x, float y)
    {
        rift = new GameObject("Rift");
        rift.transform.position = new Vector3(x, y, 0f);
        rift.transform.localScale = new Vector3(1f, riftHeight, 1f);
        var sr = rift.AddComponent<SpriteRenderer>();
        sr.sprite = riftSprite;
        sr.color = riftColor;
        sr.sortingOrder = riftSortingOrder;
    }

    static Sprite MakeTriangleSprite(int size, float ppu)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        var px = new Color32[size * size];

        float h = size - 1;
        float cx = h * 0.5f;
        float borderPx = 2f;
        float sideNorm = Mathf.Sqrt(h * h + cx * cx);

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            // Треугольник остриём вверх: основание по нижней грани, апекс по центру верхней.
            bool insideLeft  = x * h >= y * cx;
            bool insideRight = (h - x) * h >= y * (h - cx);
            Color32 col;
            if (!insideLeft || !insideRight)
            {
                col = new Color32(0, 0, 0, 0);
            }
            else
            {
                float dLeft   = Mathf.Abs(x * h - y * cx) / sideNorm;
                float dRight  = Mathf.Abs((h - x) * h - y * (h - cx)) / sideNorm;
                float dBottom = y;
                float minD = Mathf.Min(dBottom, Mathf.Min(dLeft, dRight));
                col = (minD <= borderPx) ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 200);
            }
            px[y * size + x] = col;
        }
        tex.SetPixels32(px);
        tex.Apply();
        // Pivot у центроида треугольника (y = 1/3 высоты), чтобы маяк визуально «стоял» на позиции игрока.
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 1f / 3f), ppu);
    }

    static Sprite MakeDiamondSprite(int size, float ppu)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        var px = new Color32[size * size];
        float c = (size - 1) * 0.5f;
        float radius = c;
        float border = radius - 2f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float d = Mathf.Abs(x - c) + Mathf.Abs(y - c);
            Color32 col;
            if (d > radius)        col = new Color32(0, 0, 0, 0);
            else if (d > border)   col = new Color32(255, 255, 255, 255);
            else                   col = new Color32(255, 255, 255, 200);
            px[y * size + x] = col;
        }
        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), ppu);
    }

    static Sprite MakeRectSprite(int w, int h, float ppu)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        var px = new Color32[w * h];
        for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, 255);
        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), ppu);
    }
}
