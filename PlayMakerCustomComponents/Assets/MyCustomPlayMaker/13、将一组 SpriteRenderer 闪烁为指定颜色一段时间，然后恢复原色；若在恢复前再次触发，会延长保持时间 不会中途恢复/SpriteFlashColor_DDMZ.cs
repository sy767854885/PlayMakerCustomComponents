using UnityEngine;
using HutongGames.PlayMaker;
using System.Collections.Generic;

[ActionCategory("Custom/Visual")]
[HutongGames.PlayMaker.Tooltip("将一组 SpriteRenderer 闪烁为指定颜色一段时间，然后恢复原色；若在恢复前再次触发，会延长保持时间（不会中途恢复）。支持多个物体和可选包含子物体。")]
public class SpriteFlashColor_DDMZ : FsmStateAction
{
    [RequiredField]
    [UIHint(UIHint.Variable)]
    [HutongGames.PlayMaker.Tooltip("FsmArray（类型为 GameObject）：要处理的目标物体集合。每个物体上将查找 SpriteRenderer；可选向下遍历子物体。")]
    [ArrayEditor(VariableType.GameObject)]
    public FsmArray targetObjects;

    [HutongGames.PlayMaker.Tooltip("闪烁成的目标颜色（例如命中变红）。")]
    public FsmColor flashColor;

    [HutongGames.PlayMaker.Tooltip("颜色保持的时间（秒）。如果在倒计时结束前再次触发，会从当前时间重新计时，不会在中途恢复原色。")]
    public FsmFloat duration;

    [HutongGames.PlayMaker.Tooltip("是否在子物体上继续查找 SpriteRenderer。")]
    public FsmBool includeChildren;

    [HutongGames.PlayMaker.Tooltip("是否使用 UnscaledTime（不受 Time.timeScale 影响）。")]
    public FsmBool useUnscaledTime;

    private static readonly List<SpriteRenderer> _cache = new List<SpriteRenderer>();

    public override void Reset()
    {
        targetObjects = null;
        flashColor = Color.red;
        duration = 0.15f;
        includeChildren = false;
        useUnscaledTime = false;
    }

    public override void OnEnter()
    {
        if (targetObjects == null || targetObjects.Length == 0)
        {
            Finish();
            return;
        }

        // 校验 FsmArray 类型
        if (targetObjects.ElementType != VariableType.GameObject)
        {
            Debug.LogWarning("SpriteFlashColor_DDMZ: targetObjects 必须是 GameObject 类型的 FsmArray。");
            Finish();
            return;
        }

        for (int i = 0; i < targetObjects.Length; i++)
        {
            var go = targetObjects.Get(i) as GameObject;
            if (go == null) continue;

            _cache.Clear();
            if (includeChildren.Value)
            {
                go.GetComponentsInChildren(true, _cache);
            }
            else
            {
                var sr = go.GetComponent<SpriteRenderer>();
                if (sr != null) _cache.Add(sr);
            }

            foreach (var sr in _cache)
            {
                if (sr == null) continue;
                var helper = sr.GetComponent<SpriteFlashRuntime_DDMZ>();
                if (helper == null) helper = sr.gameObject.AddComponent<SpriteFlashRuntime_DDMZ>();
                helper.Trigger(sr, flashColor.Value, Mathf.Max(0f, duration.Value), useUnscaledTime.Value);
            }
        }

        Finish(); // 本次触发即完成
    }
}

/// <summary>
/// 运行时辅助组件：管理单个 SpriteRenderer 的“命中变色”逻辑，
/// 使用 endTime 累进方式保证在连续命中时不提前恢复原色。
/// </summary>
public class SpriteFlashRuntime_DDMZ : MonoBehaviour
{
    private bool _hasOriginal;
    private Color _originalColor;
    private float _endTime;
    private bool _useUnscaled;
    private Coroutine _routine;

    public void Trigger(SpriteRenderer sr, Color flashColor, float duration, bool useUnscaled)
    {
        if (sr == null) return;

        // 记录原色（只在第一次触发时记录）
        if (!_hasOriginal)
        {
            _originalColor = sr.color;
            _hasOriginal = true;
        }

        // 立即设为命中颜色
        sr.color = flashColor;

        _useUnscaled = useUnscaled;

        float now = useUnscaled ? Time.unscaledTime : Time.time;
        // 将截止时间设置为“当前时间 + 时长”。
        _endTime = now + duration;

        if (_routine == null)
        {
            _routine = StartCoroutine(Run(sr));
        }
    }

    private System.Collections.IEnumerator Run(SpriteRenderer sr)
    {
        while (true)
        {
            float now = _useUnscaled ? Time.unscaledTime : Time.time;
            if (now >= _endTime) break;
            yield return null;
        }

        // 恢复原色
        if (sr != null)
        {
            sr.color = _originalColor;
        }

        // 清理状态，等待下次触发
        _hasOriginal = false;
        _routine = null;
    }
}