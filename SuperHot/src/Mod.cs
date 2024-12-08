using BoneLib;
using BoneLib.BoneMenu;

using MelonLoader;

using UnityEngine;

using Il2CppSLZ.Marrow;
using Il2CppSLZ.Marrow.Utilities;
using Il2CppSLZ.Marrow.SceneStreaming;
using Il2CppSLZ.Bonelab;

namespace SuperHot;

using Page = BoneLib.BoneMenu.Page;

public class SuperHotMod : MelonMod
{
    public const string Version = "1.1.0";

    private const float VelocityMultiplier = 0.2f;

    private const float VelocityPower = 4f;

    public static SuperHotMod Instance { get; private set; }

    public static bool IsEnabled { get; private set; }

    public static MelonPreferences_Category MelonPrefCategory { get; private set; }
    public static MelonPreferences_Entry<bool> MelonPrefEnabled { get; private set; }

    private static bool _preferencesSetup = false;
    private static float _lastTime = 1f;
    private static float _springVelocity;
    private static bool _wasUIOpen = false;

    public static Page MainPage { get; private set; }
    public static BoolElement BoneMenuEnabledElement { get; private set; }

    public override void OnInitializeMelon()
    {
        Instance = this;

        SetupMelonPrefs();
        SetupBoneMenu();
    }

    public static void SetupMelonPrefs()
    {
        MelonPrefCategory = MelonPreferences.CreateCategory("SuperHot");
        MelonPrefEnabled = MelonPrefCategory.CreateEntry("IsEnabled", true);

        IsEnabled = MelonPrefEnabled.Value;

        _preferencesSetup = true;
    }

    public static void SetupBoneMenu()
    {
        MainPage = Page.Root.CreatePage("SuperHot", Color.red);
        BoneMenuEnabledElement = MainPage.CreateBool("Mod Toggle", Color.yellow, IsEnabled, OnSetEnabled);
    }

    public static void OnSetEnabled(bool value)
    {
        IsEnabled = value;
        MelonPrefEnabled.Value = value;
        MelonPrefCategory.SaveToFile(false);

        if (!IsEnabled)
        {
            SetTimescale(1f);
        }
    }

    public override void OnPreferencesLoaded()
    {
        if (!_preferencesSetup)
        {
            return;
        }

        IsEnabled = MelonPrefEnabled.Value;
        BoneMenuEnabledElement.Value = IsEnabled;
    }

    private static void SetTimescale(float value)
    {
        if (value <= 0f)
        {
            return;
        }

        Time.timeScale = value;

        if (IsEnabled)
        {
            var hz = MarrowGame.xr.Display.GetRefreshRate();
            Time.fixedDeltaTime = Mathf.Clamp(value, 0.05f, 1f) / hz;
        }
    }

    public override void OnUpdate()
    {
        if (!IsEnabled)
        {
            return;
        }

        if (SceneStreamer.Session.Status == StreamStatus.LOADING)
        {
            return;
        }

        if (Time.timeScale <= 0f)
        {
            return;
        }

        if (Time.timeSinceLevelLoad <= 3f)
        {
            return;
        }

        if (Player.RigManager == null)
        {
            return;
        }

        if (UIRig.Instance.popUpMenu.m_IsActivated)
        {
            if (!_wasUIOpen)
            {
                SetTimescale(1f);
            }

            _wasUIOpen = true;

            return;
        }

        _wasUIOpen = false;

        var controllerRig = Player.ControllerRig;

        // Controllers
        float velocity = GetControllerVelocity(controllerRig.leftController) + GetControllerVelocity(controllerRig.rightController);

        // Pelvis
        velocity += Player.PhysicsRig.pelvisVelocity.magnitude * VelocityMultiplier;

        float time = Mathf.Clamp(Mathf.Pow(velocity, VelocityPower), 0.0001f, 1f);

        time = Mathf.SmoothDamp(_lastTime, time, ref _springVelocity, 0.25f, 10f, Time.unscaledDeltaTime);
        _lastTime = time;

        SetTimescale(time);
    }

    private static float GetControllerVelocity(BaseController controller)
    {
        float relative = controller.GetRelativeVelocityInWorld().magnitude * VelocityMultiplier;
        var thumbstick = controller.GetThumbStickAxis();
        relative += Mathf.Abs(thumbstick.x) + Mathf.Abs(thumbstick.y);
        return relative;
    }
}
