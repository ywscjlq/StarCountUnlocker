using System;
using System.Collections.Generic;

using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace StarCountUnlocker
{
    [BepInPlugin("ywscjlq.star.count.unlocker", "StarCountUnlocker", "1.0.0")]
    [BepInProcess("DSPGAME.exe")]
    public class StarCountUnlockerPlugin : BaseUnityPlugin
    {
        public static int MaxStars = 1024;
        public static int MinStars = 1;

        // DLC 兼容: 自动检测到的游戏常量集合
        public static int[] DetectedConstants = null;
        // 调试: 已修补的方法列表
        public static List<string> PatchedMethods = new List<string>();

        void Awake()
        {
            // 从配置文件读取上限（BepInEx\config\star.count.unlocker.cfg）
            MaxStars = Config.Bind<int>("StarCount", "MaxStars", 1024,
                "Maximum number of stars (slider upper bound). Min=1, Max=1024 recommended.").Value;
            MinStars = Config.Bind<int>("StarCount", "MinStars", 1,
                "Minimum number of stars (slider lower bound).").Value;

            Logger.LogInfo("=== StarCountUnlocker v1.0 ===");
            var harm = new Harmony("star.count.unlocker");

            // DLC 兼容: 从游戏运行时自动检测关键常量
            DetectedConstants = DetectDspConstants();
            string constStr = "DSP constants: " + string.Join(", ", DetectedConstants);
            Logger.LogInfo("[SCU] " + constStr);

            // 修复1: 替换 OnStarCountSliderValueChange — 移除 20-80 硬编码
            var sliderMethod = AccessTools.Method("UIGalaxySelect:OnStarCountSliderValueChange", new Type[] { typeof(float) });
            if (sliderMethod != null)
            {
                harm.Patch(sliderMethod,
                    prefix: new HarmonyMethod(typeof(Patches), "OnStarCountSliderValueChange_Prefix"));
                Logger.LogInfo("[SCU] Fix #1: OnStarCountSliderValueChange Prefix (removed 20-80 hardcap)");
            }
            else { Logger.LogWarning("[SCU] OnStarCountSliderValueChange not found!"); }

            // 修复2: UI 打开后修改滑块范围 & 初始值
            var onOpenMethod = AccessTools.Method("UIGalaxySelect:_OnOpen");
            if (onOpenMethod != null)
            {
                harm.Patch(onOpenMethod,
                    postfix: new HarmonyMethod(typeof(Patches), "OnOpen_Postfix"));
                Logger.LogInfo("[SCU] Fix #2: _OnOpen Postfix (slider range + initial value)");
            }
            else { Logger.LogWarning("[SCU] _OnOpen not found!"); }

            // 修复3: DLC 兼容全局扫描 — 自动匹配所有包含关键常量的方法
            // 既匹配已知的 25700/25600，也匹配运行时检测到的自定义常量
            Type anyDSPType = AccessTools.TypeByName("GalaxyData");
            if (anyDSPType != null)
            {
                Assembly dspAsm = anyDSPType.Assembly;
                PatchedMethods.Clear();
                int totalPatched = 0;
                foreach (Type t in dspAsm.GetTypes())
                {
                    foreach (MethodInfo m in t.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                    {
                        if (m.DeclaringType != t) continue;
                        if (TryPatchConstants(harm, m)) totalPatched++;
                    }
                    foreach (ConstructorInfo c in t.GetConstructors(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                    {
                        if (TryPatchConstants(harm, c)) totalPatched++;
                    }
                }
                Logger.LogInfo("[SCU] Fix #3: Patched " + totalPatched + " methods with constants "
                    + string.Join(",", DetectedConstants != null ? DetectedConstants : new int[] { 25700, 25600 }));
            }

            Logger.LogInfo("=== Loaded MaxStars=" + MaxStars + " MinStars=" + MinStars + " ===");
        }

        // 快速检查 IL 中是否包含特定的 ldc.i4 常量值
        private static bool ContainsLdcI4Value(byte[] il, params int[] values)
        {
            if (il.Length < 5) return false;
            for (int i = 0; i <= il.Length - 5; i++)
            {
                if (il[i] == 0x20) // OpCodes.Ldc_I4 value follows
                {
                    int val = il[i + 1] | (il[i + 2] << 8) | (il[i + 3] << 16) | (il[i + 4] << 24);
                    for (int j = 0; j < values.Length; j++)
                    {
                        if (val == values[j]) return true;
                    }
                }
            }
            return false;
        }

        // 尝试对方法/构造函数应用常量替换转译器
        private static bool TryPatchConstants(Harmony harm, MethodBase m)
        {
            try
            {
                var body = m.GetMethodBody();
                if (body == null) return false;
                byte[] il = body.GetILAsByteArray();
                // 构建搜索常量集: 已知常量 + DLC 检测常量
                var lookFor = new List<int> { 25700, 25600 };
                if (DetectedConstants != null)
                    lookFor.AddRange(DetectedConstants);
                if (ContainsLdcI4Value(il, lookFor.ToArray()))
                {
                    harm.Patch(m, transpiler: new HarmonyMethod(typeof(Patches), "ReplaceMAX_ASTRO_COUNT"));
                    PatchedMethods.Add(m.DeclaringType.Name + "." + m.Name);
                    return true;
                }
            }
            catch (Exception e) { Debug.LogWarning("[SCU] TryPatchConstants failed for " + m.DeclaringType.Name + "." + m.Name + ": " + e.Message); }
            return false;
        }

        // DLC 兼容: 自动检测游戏中的最大恒星相关常量
        // 从 GalaxyData.astrosData 和 SectorModel 的构造函数 IL 中提取
        private static int[] DetectDspConstants()
        {
            HashSet<int> results = new HashSet<int>();
            Type galaxyType = AccessTools.TypeByName("GalaxyData");
            Type sectorType = AccessTools.TypeByName("SectorModel");
            Type[] typesToScan = new Type[] { galaxyType, sectorType };
            foreach (Type t in typesToScan)
            {
                if (t == null) continue;
                foreach (ConstructorInfo c in t.GetConstructors(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    try
                    {
                        MethodBody body = c.GetMethodBody();
                        if (body == null) continue;
                        byte[] il = body.GetILAsByteArray();
                        if (il == null || il.Length < 5) continue;
                        for (int i = 0; i <= il.Length - 5; i++)
                        {
                            if (il[i] == 0x20)
                            {
                                int val = il[i + 1] | (il[i + 2] << 8) | (il[i + 3] << 16) | (il[i + 4] << 24);
                                if (val > 10000 && val < 100000)
                                    results.Add(val);
                            }
                        }
                    }
                    catch { }
                }
            }
            if (results.Count == 0) return null;
            int[] arr = new int[results.Count];
            results.CopyTo(arr);
            return arr;
        }

        // 调试: F12 打印已修补方法列表
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F12))
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("=== StarCountUnlocker Debug ===");
                sb.AppendLine("MaxStars=" + MaxStars + " MinStars=" + MinStars);
                sb.AppendLine("CalcMaxAstro()=" + CalcMaxAstro());
                if (DetectedConstants != null)
                    sb.AppendLine("DetectedConstants: " + string.Join(", ", DetectedConstants));
                sb.AppendLine("Patched methods (" + PatchedMethods.Count + "):");
                foreach (string m in PatchedMethods)
                    sb.AppendLine("  " + m);
                sb.AppendLine("=== End ===");
                Debug.Log(sb.ToString());
                Logger.LogInfo(sb.ToString());
            }
        }

        public static int CalcMaxAstro()
        {
            return MaxStars * 100 + 200;
        }
    }

    public static class Patches
    {
        private static FieldInfo _fiSlider = null;
        private static FieldInfo _fiGameDesc = null;
        private static MethodInfo _miSetStarmapGalaxy = null;
        private static Type _uiType = null;
        private static bool _refInit = false;

        private static void EnsureReflection()
        {
            if (_refInit) return;
            _refInit = true;
            try
            {
                _uiType = AccessTools.TypeByName("UIGalaxySelect");
                if (_uiType == null) return;
                _fiSlider = AccessTools.Field(_uiType, "starCountSlider");
                _fiGameDesc = AccessTools.Field(_uiType, "gameDesc");
                _miSetStarmapGalaxy = AccessTools.Method(_uiType, "SetStarmapGalaxy");
            }
            catch (Exception e) { Debug.LogError("[SCU] EnsureReflection error: " + e); }
        }

        private static Slider GetSlider(object instance)
        {
            if (_fiSlider == null) return null;
            return _fiSlider.GetValue(instance) as Slider;
        }

        private static GameDesc GetGameDesc(object instance)
        {
            if (_fiGameDesc == null) return null;
            return _fiGameDesc.GetValue(instance) as GameDesc;
        }

        private static void CallSetStarmapGalaxy(object instance)
        {
            if (_miSetStarmapGalaxy == null) return;
            _miSetStarmapGalaxy.Invoke(instance, null);
        }

        // 修复1: 替换 OnStarCountSliderValueChange
        public static bool OnStarCountSliderValueChange_Prefix(object __instance)
        {
            EnsureReflection();
            try
            {
                Slider slider = GetSlider(__instance);
                if (slider == null) return false;
                float val = slider.value;
                int starCount = (int)(val + 0.1f);
                starCount = Mathf.Clamp(starCount, StarCountUnlockerPlugin.MinStars, StarCountUnlockerPlugin.MaxStars);
                GameDesc gameDesc = GetGameDesc(__instance);
                if (gameDesc == null) return false;
                if (starCount == gameDesc.starCount) return false;
                gameDesc.starCount = starCount;
                CallSetStarmapGalaxy(__instance);
                return false;
            }
            catch (Exception e)
            {
                Debug.LogError("[SCU] OnStarCountSliderValueChange_Prefix error: " + e);
                return false;
            }
        }

        // 修复2: _OnOpen 后置
        public static void OnOpen_Postfix(object __instance)
        {
            EnsureReflection();
            try
            {
                Slider slider = GetSlider(__instance);
                if (slider == null) { Debug.LogWarning("[SCU] starCountSlider not found in _OnOpen"); return; }
                slider.minValue = StarCountUnlockerPlugin.MinStars;
                slider.maxValue = StarCountUnlockerPlugin.MaxStars;
                Debug.Log("[SCU] Slider range set: [" + slider.minValue + ", " + slider.maxValue + "]");
                GameDesc gameDesc = GetGameDesc(__instance);
                if (gameDesc != null)
                {
                    int currentStarCount = gameDesc.starCount;
                    if (currentStarCount < StarCountUnlockerPlugin.MinStars || currentStarCount > StarCountUnlockerPlugin.MaxStars)
                    {
                        gameDesc.starCount = StarCountUnlockerPlugin.MaxStars;
                        Debug.Log("[SCU] Reset starCount to " + StarCountUnlockerPlugin.MaxStars);
                    }
                }
            }
            catch (Exception e) { Debug.LogError("[SCU] OnOpen_Postfix error: " + e); }
        }

        // 修复3: 通用 MAX_ASTRO_COUNT 相关常量替换转译器
        // 25700 = GalaxyData.astrosData / astrosFactory 原始大小
        // 25600 = SectorModel.galaxyAstroArr / starmapGalaxyAstroArr / ComputeBuffer 原始大小
        // 全部替换为 CalcMaxAstro() = MaxStars * 100 + 200
        public static IEnumerable<CodeInstruction> ReplaceMAX_ASTRO_COUNT(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction inst in instructions)
            {
                if (inst.opcode == OpCodes.Ldc_I4 && inst.operand is int)
                {
                    int v = (int)inst.operand;
                    if (v == 25700 || v == 25600)
                    {
                        inst.operand = StarCountUnlockerPlugin.CalcMaxAstro();
                    }
                }
                yield return inst;
            }
        }
    }
}
