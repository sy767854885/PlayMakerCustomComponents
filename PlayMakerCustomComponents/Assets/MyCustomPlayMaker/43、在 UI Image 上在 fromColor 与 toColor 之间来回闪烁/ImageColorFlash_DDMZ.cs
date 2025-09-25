using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using HutongGames.PlayMaker;

[ActionCategory("UI")]
[HutongGames.PlayMaker.Tooltip("在 UI Image 上在 fromColor 与 toColor 之间来回闪烁。interval = 单程时间（from->to）。flashCount <= 0 表示无限循环。取消勾选 run 会立即中断并发送 stopEvent。")]
public class ImageColorFlash_DDMZ : FsmStateAction
{
    [RequiredField]
    [HutongGames.PlayMaker.Tooltip("目标 Image（可选择 Owner）")]
    public FsmOwnerDefault gameObject;

    [HutongGames.PlayMaker.Tooltip("起始颜色（未指定时使用进入时的颜色）")]
    public FsmColor fromColor;

    [HutongGames.PlayMaker.Tooltip("目标颜色（闪烁色）")]
    public FsmColor toColor;

    [RequiredField]
    [HutongGames.PlayMaker.Tooltip("单程时间：从 from -> to 所需时间（来回一次耗时 interval * 2）。")]
    public FsmFloat interval;

    [HutongGames.PlayMaker.Tooltip("来回闪烁次数。<=0 表示无限循环，直到 run 取消或调用 StopAction(). 每次 from->to->from 算一次。")]
    public FsmInt flashCount;

    [HutongGames.PlayMaker.Tooltip("勾选 = 运行。取消勾选时立即中断并发送 stopEvent。")]
    public FsmBool run;

    [HutongGames.PlayMaker.Tooltip("是否在完成或中断后恢复为进入 Action 时的颜色（若关闭则恢复为 fromColor）。")]
    public FsmBool restoreOriginalColor;

    [HutongGames.PlayMaker.Tooltip("完成（闪烁次数做完）时发送的事件。")]
    public FsmEvent finishEvent;

    [HutongGames.PlayMaker.Tooltip("被中断（run 变为 false）时发送的事件。")]
    public FsmEvent stopEvent;

    [HutongGames.PlayMaker.Tooltip("可选缓动曲线。若未设置或无法读取曲线则使用线性。")]
    public FsmAnimationCurve ease;

    // internal
    private Image _image;
    private Color _savedOriginalColor;
    private float _timer;
    private int _phase; // 0 = forward (from->to), 1 = backward (to->from)
    private int _doneCount;
    private bool _infinite;
    private bool _isStarted;

    // extracted curve (may be null)
    private AnimationCurve _easeCurve;

    public override void Reset()
    {
        gameObject = null;
        fromColor = new FsmColor { UseVariable = true }; // 未指定则用进入时颜色
        toColor = new FsmColor { UseVariable = false, Value = Color.red };
        interval = 0.2f;
        flashCount = new FsmInt { Value = 3 };
        run = new FsmBool { Value = true };
        restoreOriginalColor = new FsmBool { Value = true };
        finishEvent = null;
        stopEvent = null;

        // 初始化为新的 FsmAnimationCurve（不要尝试设置 UseVariable/Value）
        ease = new FsmAnimationCurve();

        _image = null;
        _timer = 0f;
        _phase = 0;
        _doneCount = 0;
        _infinite = false;
        _isStarted = false;
        _easeCurve = null;
    }

    public override void OnEnter()
    {
        var go = Fsm.GetOwnerDefaultTarget(gameObject);
        if (go == null)
        {
            Finish();
            return;
        }

        _image = go.GetComponent<Image>();
        if (_image == null)
        {
            LogWarning("ImageColorFlashAction: target GameObject has no Image component.");
            Finish();
            return;
        }

        // 保存进入时的颜色
        _savedOriginalColor = _image.color;

        // 如果用户没有在 inspector 指定 fromColor（IsNone 为 true），则用当前 image 颜色作为 from
        if (fromColor.IsNone)
            fromColor = new FsmColor { Value = _image.color, UseVariable = false };

        // 试图从 FsmAnimationCurve 提取 AnimationCurve（兼容不同 PlayMaker 版本）
        _easeCurve = TryExtractAnimationCurve(ease);

        // 保证 interval 非负
        if (interval.Value < 0f) interval.Value = 0f;

        _timer = 0f;
        _phase = 0; // 从 from -> to 开始
        _doneCount = 0;
        _infinite = (flashCount.Value <= 0);
        _isStarted = true;

        // 如果进入时 run 为 false，则立即中断
        if (!run.Value)
        {
            DoStop();
            return;
        }

        // 直接把图片设置为 fromColor 开始
        _image.color = fromColor.Value;
    }

    public override void OnUpdate()
    {
        if (!_isStarted || _image == null)
            return;

        // 检查 run 开关：取消勾选就中断
        if (!run.Value)
        {
            DoStop();
            return;
        }

        // 单程 duration
        float duration = Mathf.Max(0f, interval.Value);

        if (duration <= 0f)
        {
            // 直接跳过动画：立刻切换到目标，然后回到起点，算一次
            _image.color = (_phase == 0) ? toColor.Value : fromColor.Value;
            TogglePhaseAndCount();
            return;
        }

        _timer += Time.deltaTime;
        float t = Mathf.Clamp01(_timer / duration);
        float eval = t;

        // 使用提取到的 curve（如果有）
        if (_easeCurve != null)
        {
            eval = _easeCurve.Evaluate(t);
        }

        if (_phase == 0)
        {
            // from -> to
            _image.color = Color.LerpUnclamped(fromColor.Value, toColor.Value, eval);
        }
        else
        {
            // to -> from
            _image.color = Color.LerpUnclamped(toColor.Value, fromColor.Value, eval);
        }

        if (_timer >= duration)
        {
            // 一次单程完成，重置 timer 并切换相位
            _timer = 0f;
            TogglePhaseAndCount();
        }
    }

    private void TogglePhaseAndCount()
    {
        // phase 0 -> 1, or 1 -> 0
        _phase = (_phase == 0) ? 1 : 0;

        // 仅当完成一个完整来回（即从 phase 0 -> 1 -> 0）才算一次 doneCount
        if (_phase == 0)
        {
            _doneCount++;

            if (!_infinite && _doneCount >= flashCount.Value)
            {
                DoFinish();
            }
        }
    }

    private void DoFinish()
    {
        // 恢复颜色
        if (_image != null)
        {
            if (restoreOriginalColor.Value)
                _image.color = _savedOriginalColor;
            else
                _image.color = fromColor.Value;
        }

        if (finishEvent != null)
            Fsm.Event(finishEvent);

        Finish();
    }

    private void DoStop()
    {
        // 中断：恢复颜色
        if (_image != null)
        {
            if (restoreOriginalColor.Value)
                _image.color = _savedOriginalColor;
            else
                _image.color = fromColor.Value;
        }

        if (stopEvent != null)
            Fsm.Event(stopEvent);

        Finish();
    }

    public override void OnExit()
    {
        _isStarted = false;
    }

    /// <summary>
    /// 使用反射尝试从 FsmAnimationCurve 中取出 AnimationCurve（兼容不同 PlayMaker 版本）。
    /// 返回 null 表示未找到可用的曲线。
    /// </summary>
    private AnimationCurve TryExtractAnimationCurve(FsmAnimationCurve fsmCurve)
    {
        if (fsmCurve == null) return null;

        var t = fsmCurve.GetType();

        // 常见名字尝试：Property/Field "Value"
        var prop = t.GetProperty("Value", BindingFlags.Public | BindingFlags.Instance);
        if (prop != null && typeof(AnimationCurve).IsAssignableFrom(prop.PropertyType))
            return (AnimationCurve)prop.GetValue(fsmCurve, null);

        var field = t.GetField("Value", BindingFlags.Public | BindingFlags.Instance);
        if (field != null && typeof(AnimationCurve).IsAssignableFrom(field.FieldType))
            return (AnimationCurve)field.GetValue(fsmCurve);

        // 其他可能的命名："curve", "Curve", "animationCurve", "AnimationCurve"
        prop = t.GetProperty("curve", BindingFlags.Public | BindingFlags.Instance) ?? t.GetProperty("Curve", BindingFlags.Public | BindingFlags.Instance)
               ?? t.GetProperty("animationCurve", BindingFlags.Public | BindingFlags.Instance) ?? t.GetProperty("AnimationCurve", BindingFlags.Public | BindingFlags.Instance);
        if (prop != null && typeof(AnimationCurve).IsAssignableFrom(prop.PropertyType))
            return (AnimationCurve)prop.GetValue(fsmCurve, null);

        field = t.GetField("curve", BindingFlags.Public | BindingFlags.Instance) ?? t.GetField("Curve", BindingFlags.Public | BindingFlags.Instance)
                ?? t.GetField("animationCurve", BindingFlags.Public | BindingFlags.Instance) ?? t.GetField("AnimationCurve", BindingFlags.Public | BindingFlags.Instance);
        if (field != null && typeof(AnimationCurve).IsAssignableFrom(field.FieldType))
            return (AnimationCurve)field.GetValue(fsmCurve);

        // 无法找到，返回 null（使用线性）
        return null;
    }
}
