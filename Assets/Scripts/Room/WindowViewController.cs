using UnityEngine;
using UnityEngine.Rendering;

public class WindowViewController : MonoBehaviour
{
    public enum TimeOfDay { Day, Evening, Night }

    [System.Serializable]
    public class TimeOfDaySetting
    {
        public Texture2D windowTexture;
        public Color ambientColor = Color.white;
        public float ambientIntensity = 1f;
        public Color sunLightColor = Color.white;
        public float sunLightIntensity = 1f;
        public float roomLightIntensity = 1.5f;
        public Color roomLightColor = new Color(1f, 0.95f, 0.85f);
        public float fillLightIntensity = 0.5f;
        public Color fillLightColor = Color.white;
        public float moonLightIntensity = 0f;

        [Header("BookShelfLight")]
        public bool bookShelfLightEnabled = false;
        public Color bookShelfLightColor = new Color(1f, 0.733f, 0.486f);
        public float bookShelfLightIntensity = 0.5f;

        [Header("CharacterLights")]
        public Color characterKeyColor = Color.white;
        public float characterKeyIntensity = 0.5f;
        public Color characterRimColor = Color.white;
        public float characterRimIntensity = 0.6f;
        public Color characterFillColor = Color.white;
        public float characterFillIntensity = 0.12f;
    }

    [SerializeField] private MeshRenderer viewPlaneRenderer;
    [SerializeField] private Light sunLight;
    [SerializeField] private Light roomLight;
    [SerializeField] private Light fillLight;
    [SerializeField] private Light moonLight;
    [SerializeField] private Light bookShelfLight;
    [SerializeField] private Light characterKeyLight;
    [SerializeField] private Light characterRimLight;
    [SerializeField] private Light characterFillLight;
    [SerializeField] private Camera mainCamera;

    [SerializeField] private TimeOfDaySetting daySetting = new TimeOfDaySetting
    {
        ambientColor       = new Color(1.0f, 0.95f, 0.85f),
        ambientIntensity   = 1.0f,
        sunLightColor      = new Color(1.0f, 0.98f, 0.9f),
        sunLightIntensity  = 1.0f,
        roomLightIntensity = 1.5f,
        roomLightColor     = new Color(1.0f, 0.95f, 0.85f),
        fillLightIntensity = 0.5f,
        moonLightIntensity = 0f,
    };

    [SerializeField] private TimeOfDaySetting eveningSetting = new TimeOfDaySetting
    {
        ambientColor       = new Color(0.957f, 0.933f, 0.902f), // #F4EEE6
        ambientIntensity   = 0.18f,
        sunLightColor      = new Color(1.0f, 0.816f, 0.651f),   // #FFD0A6
        sunLightIntensity  = 0.25f,
        roomLightIntensity = 0.25f,
        roomLightColor     = new Color(1.0f, 0.953f, 0.878f),   // #FFF3E0
        fillLightIntensity = 0.06f,
        fillLightColor     = new Color(0.937f, 0.910f, 0.878f), // #EFE8E0
        moonLightIntensity = 0f,
    };

    [SerializeField] private TimeOfDaySetting nightSetting = new TimeOfDaySetting
    {
        ambientColor       = new Color(0.4f,   0.36f,  0.3f),
        ambientIntensity   = 1.0f,
        sunLightColor      = new Color(0.8f,   0.75f,  0.6f),
        sunLightIntensity  = 0.3f,
        roomLightIntensity = 5.0f,
        roomLightColor     = new Color(1.0f,   0.711f, 0.203f),
        fillLightIntensity = 0.5f,
        moonLightIntensity = 0.35f,
    };

    public enum ForceTimeOfDay { Auto, Day, Evening, Night }

    [Header("デバッグ・開発用")]
    [SerializeField] private ForceTimeOfDay forcedTimeOfDay = ForceTimeOfDay.Auto;

    private TimeOfDay _currentTimeOfDay = (TimeOfDay)(-1);
    private float _checkInterval = 60f;
    private float _timeSinceLastCheck = 60f;

    private void Start()
    {
        CheckAndApplyTimeOfDay();
    }

    private void Update()
    {
        _timeSinceLastCheck += Time.deltaTime;
        if (_timeSinceLastCheck < _checkInterval) return;
        _timeSinceLastCheck = 0f;
        CheckAndApplyTimeOfDay();
    }

    private void CheckAndApplyTimeOfDay()
    {
        if (forcedTimeOfDay != ForceTimeOfDay.Auto)
        {
            var forced = forcedTimeOfDay switch
            {
                ForceTimeOfDay.Day     => TimeOfDay.Day,
                ForceTimeOfDay.Evening => TimeOfDay.Evening,
                ForceTimeOfDay.Night   => TimeOfDay.Night,
                _                      => TimeOfDay.Day,
            };
            if (_currentTimeOfDay != forced)
            {
                _currentTimeOfDay = forced;
                SetTimeOfDay(forced);
            }
            return;
        }

        var now = System.DateTime.Now;
        int hour = now.Hour;

        TimeOfDay newTime;
        if (hour >= 6 && hour <= 16)
            newTime = TimeOfDay.Day;
        else if (hour >= 17 && hour <= 18)
            newTime = TimeOfDay.Evening;
        else
            newTime = TimeOfDay.Night;

        if (newTime == _currentTimeOfDay) return;

        _currentTimeOfDay = newTime;
        SetTimeOfDay(newTime);
    }

    /// <summary>
    /// いまの時間帯を、もう一度いまのライトへ適用し直す。
    /// 家具を置き換えたあとに呼ぶ。
    /// </summary>
    public void ReapplyCurrentTimeOfDay()
    {
        if ((int)_currentTimeOfDay < 0) { CheckAndApplyTimeOfDay(); return; }
        SetTimeOfDay(_currentTimeOfDay);
    }

    /// <summary>
    /// ナイトスタンドのライト（Prefab 内の BookShelfLight）を差し替える。
    ///
    /// 【なぜ必要か】2026/8/30（U-6）
    ///   ライトは家具の Prefab の中にあり、ナイトスタンドの種類ごとに別々の実体を持つ。
    ///   もようがえで家具を置き換えると Instantiate され直すため、
    ///   Inspector で1個を結線しておく方式では追従できない（結線した実体は消える）。
    ///   そこで RoomFurnitureApplier が、家具を置いた直後にここへ渡す。
    ///
    /// 【時間帯の判定はここ1箇所のまま】
    ///   受け取った直後に、いまの時間帯をそのライトへ適用する。
    ///   呼び出し側は「昼か夜か」を知らなくてよい（CLAUDE.md §18）。
    ///
    /// 家具を外したときは null が渡される。
    /// </summary>
    public void SetBookShelfLight(Light light)
    {
        bookShelfLight = light;
        if (light == null) return;
        ReapplyCurrentTimeOfDay();
    }

    public void SetTimeOfDay(TimeOfDay time)
    {
        TimeOfDaySetting setting = time switch
        {
            TimeOfDay.Day     => daySetting,
            TimeOfDay.Evening => eveningSetting,
            TimeOfDay.Night   => nightSetting,
            _                 => null,
        };

        if (setting == null) return;

        if (viewPlaneRenderer != null && setting.windowTexture != null)
            viewPlaneRenderer.material.mainTexture = setting.windowTexture;

        RenderSettings.ambientMode      = AmbientMode.Flat;
        RenderSettings.ambientLight     = setting.ambientColor * setting.ambientIntensity;
        RenderSettings.ambientIntensity = setting.ambientIntensity;

        if (sunLight != null)
        {
            sunLight.gameObject.SetActive(setting.sunLightIntensity > 0f);
            sunLight.color     = setting.sunLightColor;
            sunLight.intensity = setting.sunLightIntensity;
        }

        if (roomLight != null)
        {
            roomLight.gameObject.SetActive(setting.roomLightIntensity > 0f);
            roomLight.color     = setting.roomLightColor;
            roomLight.intensity = setting.roomLightIntensity;
        }

        if (fillLight != null)
        {
            fillLight.gameObject.SetActive(setting.fillLightIntensity > 0f);
            fillLight.color     = setting.fillLightColor;
            fillLight.intensity = setting.fillLightIntensity;
        }

        if (moonLight != null)
        {
            moonLight.gameObject.SetActive(setting.moonLightIntensity > 0f);
            moonLight.intensity = setting.moonLightIntensity;
        }

        if (bookShelfLight != null)
        {
            bookShelfLight.gameObject.SetActive(setting.bookShelfLightEnabled);
            if (setting.bookShelfLightEnabled)
            {
                bookShelfLight.color     = setting.bookShelfLightColor;
                bookShelfLight.intensity = setting.bookShelfLightIntensity;
            }
        }

        if (characterKeyLight != null)
        {
            characterKeyLight.gameObject.SetActive(setting.characterKeyIntensity > 0f);
            characterKeyLight.color     = setting.characterKeyColor;
            characterKeyLight.intensity = setting.characterKeyIntensity;
        }

        if (characterRimLight != null)
        {
            characterRimLight.gameObject.SetActive(setting.characterRimIntensity > 0f);
            characterRimLight.color     = setting.characterRimColor;
            characterRimLight.intensity = setting.characterRimIntensity;
        }

        if (characterFillLight != null)
        {
            characterFillLight.gameObject.SetActive(setting.characterFillIntensity > 0f);
            characterFillLight.color     = setting.characterFillColor;
            characterFillLight.intensity = setting.characterFillIntensity;
        }
    }
}
