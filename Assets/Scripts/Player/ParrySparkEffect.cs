using UnityEngine;

/// <summary>
/// Procedurally builds a ZZZ-style weapon-clash sparkle entirely in code.
/// No prefab configuration needed — call Spawn() from PlayerController.ParryImpact().
///
/// Three layers:
///   1. Streaking sparks  — stretched particles burst radially, white→yellow, fade+shrink
///   2. Ring flash        — single disc expands fast then fades (the "impact bloom")
///   3. Glint stars       — 5 billboard sparks that pulse then dissolve
///
/// All layers use useUnscaledTime = true so they remain visible during hitstop (timeScale=0).
/// </summary>
public class ParrySparkEffect : MonoBehaviour
{
    // ── Entry point ───────────────────────────────────────────────────────────

    /// <summary>
    /// Spawns the sparkle at <paramref name="worldPos"/>, oriented along <paramref name="clashDir"/>.
    /// Reads the player's SpriteRenderer sorting layer so particles always render above the player.
    /// </summary>
    public static void Spawn(Vector3 worldPos, Vector2 clashDir, SpriteRenderer playerSprite = null)
    {
        if (clashDir == Vector2.zero) clashDir = Vector2.right;

        // Resolve sorting layer — use the dedicated "Effects" layer if it exists,
        // otherwise fall back to the player sprite's own layer + offset so we always
        // render on top.
        int sortLayerID  = SortingLayer.NameToID("Effects");
        int sortLayerIdx = SortingLayer.GetLayerValueFromID(sortLayerID);
        // SortingLayer.NameToID returns 0 if the layer doesn't exist; 0 == "Default".
        // Verify the layer actually exists by name.
        if (!SortingLayer.IsValid(sortLayerID) || SortingLayer.IDToName(sortLayerID) != "Effects")
        {
            // Fall back: use player sprite's layer with a positive order offset
            if (playerSprite != null)
            {
                sortLayerID = playerSprite.sortingLayerID;
            }
            else
            {
                // Last resort — "Characters" or Default
                sortLayerID = SortingLayer.NameToID("Characters");
                if (!SortingLayer.IsValid(sortLayerID))
                    sortLayerID = 0;
            }
        }

        var root = new GameObject("[VFX] ParrySpark");
        float angle = Mathf.Atan2(clashDir.y, clashDir.x) * Mathf.Rad2Deg;
        root.transform.SetPositionAndRotation(worldPos, Quaternion.Euler(0f, 0f, angle));

        var effect = root.AddComponent<ParrySparkEffect>();
        effect._sortLayerID = sortLayerID;
        effect.Build();
    }

    // ── Internal state ────────────────────────────────────────────────────────

    private int _sortLayerID;

    // ── Lifetime ──────────────────────────────────────────────────────────────

    private void Build()
    {
        Material addMat = BuildAdditiveMaterial();
        AddSparks(addMat);
        AddRingFlash(addMat);
        AddGlintStars(addMat);
        Destroy(gameObject, 1.2f);
    }

    // ── Material ──────────────────────────────────────────────────────────────

    private static Material BuildAdditiveMaterial()
    {
        // Try shaders in order of likelihood for Built-in RP in Unity 6
        string[] candidates =
        {
            "Mobile/Particles/Additive",     // Built-in RP, always included
            "Particles/Additive",            // Legacy — may be present
            "Legacy Shaders/Particles/Additive",
            "Unlit/Color",                   // Non-additive fallback — still visible
            "Sprites/Default",
        };

        UnityEngine.Shader sh = null;
        foreach (var name in candidates)
        {
            sh = UnityEngine.Shader.Find(name);
            if (sh != null) break;
        }

        if (sh == null)
        {
            Debug.LogError("[ParrySpark] No suitable shader found. Check your render pipeline.");
            return new Material(UnityEngine.Shader.Find("Hidden/InternalErrorShader"));
        }

        var mat = new Material(sh);
        // Force additive blending on shaders that expose blend-mode properties
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        mat.SetInt("_ZWrite",   0);
        // Keep it unlit / self-illuminated
        mat.color = Color.white;
        return mat;
    }

    // ── Shared renderer setup ─────────────────────────────────────────────────

    private void ApplyRenderer(ParticleSystemRenderer r, Material mat, int orderInLayer = 0)
    {
        r.material            = mat;
        r.sortingLayerID      = _sortLayerID;   // Correct layer — renders above player
        r.sortingOrder        = orderInLayer;    // Fine-tune draw order within the layer
        r.minParticleSize     = 0f;
        r.maxParticleSize     = 2f;             // Prevent Unity from clamping large particles
    }

    // ── Layer 1: Streaking sparks ─────────────────────────────────────────────

    private void AddSparks(Material mat)
    {
        var child = new GameObject("Sparks");
        child.transform.SetParent(transform, false);

        var ps = child.AddComponent<ParticleSystem>();
        var r  = ps.GetComponent<ParticleSystemRenderer>();

        var main = ps.main;
        main.duration           = 0.35f;
        main.loop               = false;
        main.useUnscaledTime    = true;   // plays during hitstop (timeScale=0)
        main.startLifetime      = new ParticleSystem.MinMaxCurve(0.15f, 0.4f);
        main.startSpeed         = new ParticleSystem.MinMaxCurve(4f, 10f);
        main.startSize          = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);  // bigger — easier to see
        main.startColor         = new ParticleSystem.MinMaxGradient(
            new Color(1f, 1f, 0.85f, 1f), new Color(1f, 0.85f, 0.25f, 1f));
        main.gravityModifier    = 0f;
        main.simulationSpace    = ParticleSystemSimulationSpace.World;
        main.stopAction         = ParticleSystemStopAction.None; // root Destroy handles cleanup

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 28) });

        var shape = ps.shape;
        shape.enabled               = true;
        shape.shapeType             = ParticleSystemShapeType.Circle;
        shape.radius                = 0.05f;
        shape.radiusThickness       = 0f;
        shape.randomDirectionAmount = 0.3f;

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.radial  = new ParticleSystem.MinMaxCurve(3f);

        // Color: white-yellow → transparent
        var col = ps.colorOverLifetime;
        col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(1f, 1f, 0.8f),  0f),
                new GradientColorKey(new Color(1f, 0.85f, 0.2f), 0.4f),
                new GradientColorKey(Color.white,              1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f,   0f),
                new GradientAlphaKey(0.9f, 0.2f),
                new GradientAlphaKey(0f,   1f)
            });
        col.color = new ParticleSystem.MinMaxGradient(grad);

        var sizeLife = ps.sizeOverLifetime;
        sizeLife.enabled = true;
        var sc = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(0.3f, 0.7f),
            new Keyframe(1f, 0f));
        sizeLife.size = new ParticleSystem.MinMaxCurve(1f, sc);

        r.renderMode    = ParticleSystemRenderMode.Stretch;
        r.velocityScale = 0.22f;
        r.lengthScale   = 1.8f;
        ApplyRenderer(r, mat, orderInLayer: 2);

        ps.Play();
    }

    // ── Layer 2: Ring flash ───────────────────────────────────────────────────

    private void AddRingFlash(Material mat)
    {
        var child = new GameObject("RingFlash");
        child.transform.SetParent(transform, false);

        var ps = child.AddComponent<ParticleSystem>();
        var r  = ps.GetComponent<ParticleSystemRenderer>();

        var main = ps.main;
        main.duration           = 0.2f;
        main.loop               = false;
        main.useUnscaledTime    = true;
        main.startLifetime      = new ParticleSystem.MinMaxCurve(0.25f);
        main.startSpeed         = new ParticleSystem.MinMaxCurve(0f);
        main.startSize          = new ParticleSystem.MinMaxCurve(0.4f);
        main.startColor         = new Color(1f, 0.97f, 0.65f, 0.9f);
        main.gravityModifier    = 0f;
        main.simulationSpace    = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });

        var shape = ps.shape;
        shape.enabled = false;

        var sizeLife = ps.sizeOverLifetime;
        sizeLife.enabled = true;
        var sc = new AnimationCurve(
            new Keyframe(0f,   0.05f,  0f, 35f),
            new Keyframe(0.2f, 1.2f,   8f,  0f),
            new Keyframe(1f,   1.6f, 0.3f,  0f));
        sizeLife.size = new ParticleSystem.MinMaxCurve(1f, sc);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(1f, 0.97f, 0.75f), 0f),
                new GradientColorKey(Color.white,                  0.25f),
                new GradientColorKey(new Color(1f, 0.9f, 0.5f),  1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0.9f, 0f),
                new GradientAlphaKey(0.5f, 0.35f),
                new GradientAlphaKey(0f,   1f)
            });
        col.color = new ParticleSystem.MinMaxGradient(g);

        r.renderMode = ParticleSystemRenderMode.Billboard;
        ApplyRenderer(r, mat, orderInLayer: 1);

        ps.Play();
    }

    // ── Layer 3: Glint stars ──────────────────────────────────────────────────

    private void AddGlintStars(Material mat)
    {
        var child = new GameObject("GlintStars");
        child.transform.SetParent(transform, false);

        var ps = child.AddComponent<ParticleSystem>();
        var r  = ps.GetComponent<ParticleSystemRenderer>();

        var main = ps.main;
        main.duration           = 0.45f;
        main.loop               = false;
        main.useUnscaledTime    = true;
        main.startLifetime      = new ParticleSystem.MinMaxCurve(0.25f, 0.5f);
        main.startSpeed         = new ParticleSystem.MinMaxCurve(0.5f, 3f);
        main.startSize          = new ParticleSystem.MinMaxCurve(0.2f, 0.55f);  // bigger
        main.startRotation      = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        main.startColor         = new ParticleSystem.MinMaxGradient(
            new Color(1f, 1f, 1f, 1f), new Color(1f, 0.92f, 0.45f, 1f));
        main.gravityModifier    = 0f;
        main.simulationSpace    = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 6) });

        var shape = ps.shape;
        shape.enabled         = true;
        shape.shapeType       = ParticleSystemShapeType.Circle;
        shape.radius          = 0.1f;
        shape.radiusThickness = 0f;

        var sizeLife = ps.sizeOverLifetime;
        sizeLife.enabled = true;
        var sc = new AnimationCurve(
            new Keyframe(0f,    0.1f),
            new Keyframe(0.25f, 1.0f),
            new Keyframe(1f,    0f));
        sizeLife.size = new ParticleSystem.MinMaxCurve(1f, sc);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(Color.white,                 0f),
                new GradientColorKey(new Color(1f, 0.95f, 0.4f), 0.4f),
                new GradientColorKey(Color.white,                 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f,    0f),
                new GradientAlphaKey(0.85f, 0.5f),
                new GradientAlphaKey(0f,    1f)
            });
        col.color = new ParticleSystem.MinMaxGradient(g);

        var rotLife = ps.rotationOverLifetime;
        rotLife.enabled = true;
        rotLife.z = new ParticleSystem.MinMaxCurve(-60f * Mathf.Deg2Rad, 60f * Mathf.Deg2Rad);

        r.renderMode = ParticleSystemRenderMode.Billboard;
        ApplyRenderer(r, mat, orderInLayer: 3);

        ps.Play();
    }
}
