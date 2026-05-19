using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>URP post-processing + UI-атмосфера для Game Scene (критерий VFX and Postprocessing).</summary>
[DisallowMultipleComponent]
public class GameScenePresentation : MonoBehaviour
{
    const string VolumeRootName = "MedivoriaPostProcessVolume";

    [Header("URP Volume (камера)")]
    public bool enableUrpPostProcessing = true;
    [Range(0f, 0.45f)] public float vignetteIntensity = 0.28f;
    [Range(-0.3f, 0.3f)] public float postExposure = 0.08f;
    [Range(-20f, 20f)] public float contrast = 8f;
    [Range(-30f, 30f)] public float saturation = 6f;

    [Header("UI atmosphere")]
    public bool enableUiAtmosphere = true;

    Volume _volume;
    VolumeProfile _profile;

    public static void EnsureOnCamera(Camera camera, bool uiAtmosphere = true)
    {
        if (camera == null) return;
        var bootstrap = camera.GetComponent<GameScenePresentation>();
        if (bootstrap == null) bootstrap = camera.gameObject.AddComponent<GameScenePresentation>();
        bootstrap.enableUiAtmosphere = uiAtmosphere;
        bootstrap.Apply();
    }

    void Start() => Apply();

    public void Apply()
    {
        Camera cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        if (enableUrpPostProcessing) SetupUrpVolume(cam);
        if (enableUiAtmosphere) SetupUiAtmosphere();
    }

    void SetupUrpVolume(Camera cam)
    {
        var urp = cam.GetComponent<UniversalAdditionalCameraData>();
        if (urp == null) urp = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
        urp.renderPostProcessing = true;

        Transform existing = transform.Find(VolumeRootName);
        GameObject volumeGo = existing != null
            ? existing.gameObject
            : new GameObject(VolumeRootName);

        if (existing == null)
        {
            volumeGo.transform.SetParent(transform, false);
            _volume = volumeGo.AddComponent<Volume>();
            _volume.isGlobal = true;
            _volume.priority = 10f;
            _profile = ScriptableObject.CreateInstance<VolumeProfile>();
            _volume.profile = _profile;
        }
        else
        {
            _volume = volumeGo.GetComponent<Volume>();
            _profile = _volume != null ? _volume.profile : null;
            if (_profile == null)
            {
                _profile = ScriptableObject.CreateInstance<VolumeProfile>();
                _volume.profile = _profile;
            }
        }

        ConfigureVolumeOverrides();
    }

    void ConfigureVolumeOverrides()
    {
        if (_profile == null) return;

        if (!_profile.TryGet(out Vignette vignette))
            vignette = _profile.Add<Vignette>(true);
        vignette.active = true;
        vignette.intensity.Override(vignetteIntensity);
        vignette.smoothness.Override(0.45f);
        vignette.rounded.Override(true);

        if (!_profile.TryGet(out ColorAdjustments color))
            color = _profile.Add<ColorAdjustments>(true);
        color.active = true;
        color.postExposure.Override(postExposure);
        color.contrast.Override(contrast);
        color.saturation.Override(saturation);

        if (!_profile.TryGet(out Bloom bloom))
            bloom = _profile.Add<Bloom>(true);
        bloom.active = true;
        bloom.intensity.Override(0.12f);
        bloom.threshold.Override(1.1f);
    }

    void SetupUiAtmosphere()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        Transform bg = GameObject.Find("GameBackground")?.transform;
        if (canvas != null)
            MedievalAtmosphere.Ensure(canvas.transform, bg, true);
    }
}
