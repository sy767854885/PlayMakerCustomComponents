using UnityEngine;
using UnityEngine.UI;
using HutongGames.PlayMaker;
using DG.Tweening;

[ActionCategory("DOTween")]
[HutongGames.PlayMaker.Tooltip("使用 DOTween 将 UI Graphic 颜色从一个颜色过渡到另一个颜色，支持 Ease 或 AnimationCurve，完成时发送事件。")]
public class DOVirtual_ImageColorChange : FsmStateAction
{
    [RequiredField]
    [HutongGames.PlayMaker.Tooltip("要作用的 GameObject（包含 Image、RawImage 或 Text 组件）。")]
    public FsmOwnerDefault gameObject;

    [HutongGames.PlayMaker.Tooltip("是否使用当前颜色作为起始色（False 时使用 From Color）。")]
    public FsmBool useCurrentAsFrom;

    [HutongGames.PlayMaker.Tooltip("起始颜色（当 useCurrentAsFrom 为 false 时使用）。")]
    public FsmColor fromColor;

    [RequiredField]
    [HutongGames.PlayMaker.Tooltip("目标颜色（最终颜色）。")]
    public FsmColor toColor;

    [RequiredField]
    [HutongGames.PlayMaker.Tooltip("持续时间（秒）。")]
    public FsmFloat duration;

    [HutongGames.PlayMaker.Tooltip("是否使用自定义 AnimationCurve（优先于 Ease 设置）。")]
    public FsmBool useAnimationCurve;

    [HutongGames.PlayMaker.Tooltip("自定义曲线（只有在 useAnimationCurve 为 true 时有效）。")]
    public FsmAnimationCurve animationCurve;

    [HutongGames.PlayMaker.Tooltip("DOTween 的 Ease 类型（当 useAnimationCurve 为 false 时生效）。")]
    public Ease ease = Ease.Linear;

    [HutongGames.PlayMaker.Tooltip("是否忽略 Time.timeScale（true 使用独立更新）。")]
    public FsmBool ignoreTimeScale;

    [HutongGames.PlayMaker.Tooltip("每帧更新时发送的事件（可选）。")]
    public FsmEvent updateEvent;

    [HutongGames.PlayMaker.Tooltip("完成后发送的事件（可选）。")]
    public FsmEvent finishEvent;

    [HutongGames.PlayMaker.Tooltip("离开状态时是否回退到进入时颜色（默认 false）。")]
    public FsmBool revertOnExit;

    private Graphic _graphic;
    private Color _originalColor;
    private Tween _tween;
    private bool _hasOriginalColor;

    public override void Reset()
    {
        gameObject = null;
        useCurrentAsFrom = new FsmBool { Value = true };
        fromColor = new FsmColor { Value = Color.white };
        toColor = new FsmColor { Value = Color.white };
        duration = new FsmFloat { Value = 1f };
        useAnimationCurve = new FsmBool { Value = false };
        animationCurve = new FsmAnimationCurve();
        animationCurve.curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        ease = Ease.Linear;
        ignoreTimeScale = new FsmBool { Value = false };
        updateEvent = null;
        finishEvent = null;
        revertOnExit = new FsmBool { Value = false };
    }

    public override void OnEnter()
    {
        var go = Fsm.GetOwnerDefaultTarget(gameObject);
        if (go == null)
        {
            LogError("GameObject is null.");
            Finish();
            return;
        }

        _graphic = go.GetComponent<Graphic>();
        if (_graphic == null)
        {
            LogError("No UI Graphic (Image/RawImage/Text) found on GameObject.");
            Finish();
            return;
        }

        _originalColor = _graphic.color;
        _hasOriginalColor = true;

        Color startColor = useCurrentAsFrom.Value ? _graphic.color : fromColor.Value;
        Color endColor = toColor.Value;

        if (!useCurrentAsFrom.Value)
        {
            _graphic.color = startColor;
        }

        _tween = DOTween.To(
            () => _graphic.color,
            c => { if (_graphic != null) _graphic.color = c; },
            endColor,
            duration.Value
        );

        if (ignoreTimeScale != null && ignoreTimeScale.Value)
            _tween.SetUpdate(true);

        if (useAnimationCurve != null && useAnimationCurve.Value && animationCurve != null && animationCurve.curve != null)
        {
            _tween.SetEase(animationCurve.curve);
        }
        else
        {
            _tween.SetEase(ease);
        }

        if (updateEvent != null)
        {
            _tween.OnUpdate(() =>
            {
                Fsm.Event(updateEvent);
            });
        }

        _tween.OnComplete(() =>
        {
            if (finishEvent != null)
            {
                Fsm.Event(finishEvent);
            }
            Finish();
        });
    }

    public override void OnExit()
    {
        if (_tween != null)
        {
            _tween.Kill();
            _tween = null;
        }

        if (revertOnExit != null && revertOnExit.Value && _hasOriginalColor && _graphic != null)
        {
            _graphic.color = _originalColor;
        }
    }
}
