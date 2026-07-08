using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class SceneTransitionPreviewWindow : EditorWindow
{
    private const string MaterialPath = "Assets/Resources/UI/SceneTransitionIrisNoiseMaterial.mat";
    private const string PreviewRootName = "SceneTransitionPreview_Temporary";
    private const string ProgressProperty = "_Progress";
    private const string ColorProperty = "_Color";
    private const string GridSizeProperty = "_GridSize";
    private const string TileDurationProperty = "_TileDuration";
    private const string WaveEndPaddingProperty = "_WaveEndPadding";
    private const string EdgeSoftnessProperty = "_EdgeSoftness";
    private const string FlipXStrengthProperty = "_FlipXStrength";
    private const string FlipYStrengthProperty = "_FlipYStrength";
    private const string PerspectiveProperty = "_Perspective";
    private const string TileScaleBoostProperty = "_TileScaleBoost";

    private Material _materialAsset;
    private Material _previewMaterial;
    private GameObject _previewRoot;
    private Image _previewImage;
    private Vector2 _scrollPosition;

    private float _progress;
    private Color _color = new(0.8313726f, 0.2627451f, 0.1843137f, 1f);
    private float _gridSize = 14f;
    private float _tileDuration = 0.34f;
    private float _waveEndPadding = 0.12f;
    private float _edgeSoftness = 0.025f;
    private float _flipXStrength = 0.55f;
    private float _flipYStrength = 0.7f;
    private float _perspective = 0.45f;
    private float _tileScaleBoost = 0.16f;
    private bool _isAutoPreviewing;
    private float _autoPreviewSpeed = 1f;
    private double _lastUpdateTime;

    [MenuItem("Tools/Scene Transition Preview")]
    public static void Open()
    {
        GetWindow<SceneTransitionPreviewWindow>("Transition Preview");
    }

    private void OnEnable()
    {
        LoadMaterialAsset();
        ReadValuesFromMaterial();
        EditorApplication.update += UpdateAutoPreview;
    }

    private void OnDisable()
    {
        EditorApplication.update -= UpdateAutoPreview;
        ClearPreview();
    }

    private void OnGUI()
    {
        DrawMaterialSection();
        DrawPreviewSection();

        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
        DrawShaderControls();
        EditorGUILayout.EndScrollView();
    }

    private void DrawMaterialSection()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("머티리얼", EditorStyles.boldLabel);

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUI.BeginChangeCheck();
            _materialAsset = (Material)EditorGUILayout.ObjectField("대상 머티리얼", _materialAsset, typeof(Material), false);
            if (EditorGUI.EndChangeCheck())
            {
                ReadValuesFromMaterial();
                RebuildPreviewMaterial();
            }

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("머티리얼 값 읽기", GUILayout.Height(28f)))
            {
                LoadMaterialAsset();
                ReadValuesFromMaterial();
                ApplyValuesToPreview();
            }

            if (GUILayout.Button("현재 값 머티리얼에 적용", GUILayout.Height(28f)))
            {
                ApplyValuesToMaterial();
            }

            EditorGUILayout.EndHorizontal();
        }
    }

    private void DrawPreviewSection()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("프리뷰", EditorStyles.boldLabel);

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("프리뷰 표시", GUILayout.Height(30f)))
            {
                EnsurePreview();
                ApplyValuesToPreview();
            }

            if (GUILayout.Button("프리뷰 제거", GUILayout.Height(30f)))
            {
                ClearPreview();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUI.BeginChangeCheck();
            _progress = EditorGUILayout.Slider("Progress", _progress, 0f, 1f);
            if (EditorGUI.EndChangeCheck())
            {
                ApplyValuesToPreview();
            }

            EditorGUILayout.BeginHorizontal();
            _isAutoPreviewing = GUILayout.Toggle(_isAutoPreviewing, "자동 재생", "Button", GUILayout.Height(26f));
            _autoPreviewSpeed = EditorGUILayout.Slider(_autoPreviewSpeed, 0.1f, 3f);
            EditorGUILayout.EndHorizontal();
        }
    }

    private void DrawShaderControls()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("셰이더 값", EditorStyles.boldLabel);

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUI.BeginChangeCheck();

            _color = EditorGUILayout.ColorField("Color", _color);
            _gridSize = EditorGUILayout.Slider("Grid Size", _gridSize, 4f, 32f);
            _tileDuration = EditorGUILayout.Slider("Tile Duration", _tileDuration, 0.05f, 0.9f);
            _waveEndPadding = EditorGUILayout.Slider("Wave End Padding", _waveEndPadding, 0f, 0.4f);
            _edgeSoftness = EditorGUILayout.Slider("Edge Softness", _edgeSoftness, 0.001f, 0.2f);
            _flipXStrength = EditorGUILayout.Slider("Flip X Strength", _flipXStrength, 0f, 1f);
            _flipYStrength = EditorGUILayout.Slider("Flip Y Strength", _flipYStrength, 0f, 1f);
            _perspective = EditorGUILayout.Slider("Perspective", _perspective, 0f, 1.5f);
            _tileScaleBoost = EditorGUILayout.Slider("Tile Scale Boost", _tileScaleBoost, 0f, 0.5f);

            if (EditorGUI.EndChangeCheck())
            {
                ApplyValuesToPreview();
            }
        }
    }

    private void LoadMaterialAsset()
    {
        if (_materialAsset != null)
        {
            return;
        }

        _materialAsset = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
    }

    private void ReadValuesFromMaterial()
    {
        if (_materialAsset == null)
        {
            return;
        }

        _color = GetMaterialColor(_materialAsset, ColorProperty, _color);
        _progress = GetMaterialFloat(_materialAsset, ProgressProperty, _progress);
        _gridSize = GetMaterialFloat(_materialAsset, GridSizeProperty, _gridSize);
        _tileDuration = GetMaterialFloat(_materialAsset, TileDurationProperty, _tileDuration);
        _waveEndPadding = GetMaterialFloat(_materialAsset, WaveEndPaddingProperty, _waveEndPadding);
        _edgeSoftness = GetMaterialFloat(_materialAsset, EdgeSoftnessProperty, _edgeSoftness);
        _flipXStrength = GetMaterialFloat(_materialAsset, FlipXStrengthProperty, _flipXStrength);
        _flipYStrength = GetMaterialFloat(_materialAsset, FlipYStrengthProperty, _flipYStrength);
        _perspective = GetMaterialFloat(_materialAsset, PerspectiveProperty, _perspective);
        _tileScaleBoost = GetMaterialFloat(_materialAsset, TileScaleBoostProperty, _tileScaleBoost);
    }

    private void ApplyValuesToMaterial()
    {
        if (_materialAsset == null)
        {
            EditorUtility.DisplayDialog("Transition Preview", "적용할 머티리얼이 없습니다.", "확인");
            return;
        }

        Undo.RecordObject(_materialAsset, "Apply Scene Transition Preview Values");
        SetMaterialValues(_materialAsset, applyProgress: false);
        EditorUtility.SetDirty(_materialAsset);
        AssetDatabase.SaveAssets();
    }

    private void EnsurePreview()
    {
        if (_previewRoot != null && _previewImage != null)
        {
            return;
        }

        ClearPreview();
        RebuildPreviewMaterial();

        _previewRoot = new GameObject(PreviewRootName);
        _previewRoot.hideFlags = HideFlags.HideAndDontSave;

        Canvas canvas = _previewRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;

        GameObject imageObject = new("PreviewImage");
        imageObject.hideFlags = HideFlags.HideAndDontSave;
        imageObject.transform.SetParent(_previewRoot.transform, false);

        RectTransform rectTransform = imageObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        _previewImage = imageObject.AddComponent<Image>();
        _previewImage.raycastTarget = false;
        _previewImage.color = Color.white;
        _previewImage.material = _previewMaterial;
    }

    private void RebuildPreviewMaterial()
    {
        if (_previewMaterial != null)
        {
            DestroyImmediate(_previewMaterial);
            _previewMaterial = null;
        }

        LoadMaterialAsset();
        if (_materialAsset == null)
        {
            return;
        }

        _previewMaterial = new Material(_materialAsset)
        {
            name = "SceneTransitionPreviewRuntime",
            hideFlags = HideFlags.HideAndDontSave
        };

        ApplyValuesToPreviewMaterial();
    }

    private void ApplyValuesToPreview()
    {
        if (_previewMaterial == null)
        {
            RebuildPreviewMaterial();
        }

        ApplyValuesToPreviewMaterial();

        if (_previewImage != null)
        {
            _previewImage.material = _previewMaterial;
            SceneView.RepaintAll();
        }
    }

    private void ApplyValuesToPreviewMaterial()
    {
        if (_previewMaterial == null)
        {
            return;
        }

        SetMaterialValues(_previewMaterial, applyProgress: true);
    }

    private void SetMaterialValues(Material material, bool applyProgress)
    {
        SetMaterialColor(material, ColorProperty, _color);
        SetMaterialFloat(material, ProgressProperty, applyProgress ? _progress : 0f);
        SetMaterialFloat(material, GridSizeProperty, _gridSize);
        SetMaterialFloat(material, TileDurationProperty, _tileDuration);
        SetMaterialFloat(material, WaveEndPaddingProperty, _waveEndPadding);
        SetMaterialFloat(material, EdgeSoftnessProperty, _edgeSoftness);
        SetMaterialFloat(material, FlipXStrengthProperty, _flipXStrength);
        SetMaterialFloat(material, FlipYStrengthProperty, _flipYStrength);
        SetMaterialFloat(material, PerspectiveProperty, _perspective);
        SetMaterialFloat(material, TileScaleBoostProperty, _tileScaleBoost);
    }

    private void ClearPreview()
    {
        if (_previewRoot != null)
        {
            DestroyImmediate(_previewRoot);
            _previewRoot = null;
            _previewImage = null;
        }

        if (_previewMaterial != null)
        {
            DestroyImmediate(_previewMaterial);
            _previewMaterial = null;
        }
    }

    private void UpdateAutoPreview()
    {
        if (!_isAutoPreviewing)
        {
            _lastUpdateTime = EditorApplication.timeSinceStartup;
            return;
        }

        EnsurePreview();

        double currentTime = EditorApplication.timeSinceStartup;
        float deltaTime = (float)(currentTime - _lastUpdateTime);
        _lastUpdateTime = currentTime;

        _progress = Mathf.Repeat(_progress + (deltaTime * _autoPreviewSpeed), 1f);
        ApplyValuesToPreview();
        Repaint();
    }

    private static float GetMaterialFloat(Material material, string propertyName, float fallback)
    {
        return material.HasProperty(propertyName) ? material.GetFloat(propertyName) : fallback;
    }

    private static Color GetMaterialColor(Material material, string propertyName, Color fallback)
    {
        return material.HasProperty(propertyName) ? material.GetColor(propertyName) : fallback;
    }

    private static void SetMaterialFloat(Material material, string propertyName, float value)
    {
        if (material.HasProperty(propertyName))
        {
            material.SetFloat(propertyName, value);
        }
    }

    private static void SetMaterialColor(Material material, string propertyName, Color value)
    {
        if (material.HasProperty(propertyName))
        {
            material.SetColor(propertyName, value);
        }
    }
}
