using UnityEngine;
using HutongGames.PlayMaker;
using System.Collections;

[ActionCategory("Custom/Hit")]
[HutongGames.PlayMaker.Tooltip("物体被击中后立即显示另一个物体，并在持续时间结束后自动隐藏；不阻塞后续逻辑。多次进入会刷新计时（最后一次生效）。")]
public class ShowAlternateObject_DDMZ : FsmStateAction
{
    [RequiredField]
    [HutongGames.PlayMaker.Tooltip("要显示/隐藏的物体")]
    public FsmGameObject alternateObject;

    [HutongGames.PlayMaker.Tooltip("显示持续时间（秒）。≤0 则立刻隐藏。")]
    public FsmFloat duration;

    [HutongGames.PlayMaker.Tooltip("离开本状态时是否取消计时（避免在别的状态被异步隐藏）")]
    public FsmBool cancelOnExit;

    private int _version;            // 每次 OnEnter 递增，用于“最后一次”判定
    private Coroutine _hideRoutine;  // 当前延时隐藏的协程句柄

    public override void Reset()
    {
        alternateObject = null;
        duration = 0f;
        cancelOnExit = true;
        _version = 0;
        _hideRoutine = null;
    }

    public override void OnEnter()
    {
        var go = alternateObject != null ? alternateObject.Value : null;
        if (go == null)
        {
            Finish();
            return;
        }

        // 立即显示
        go.SetActive(true);

        // 取消旧协程，避免叠加导致时间错乱
        if (_hideRoutine != null)
        {
            Fsm.Owner.StopCoroutine(_hideRoutine);
            _hideRoutine = null;
        }

        // 仅让“最后一次进入”的隐藏生效
        var myVersion = ++_version;

        if (duration.Value > 0f)
        {
            _hideRoutine = Fsm.Owner.StartCoroutine(HideAfterDelay(go, duration.Value, myVersion));
        }
        else
        {
            go.SetActive(false);
        }

        // 立刻让 FSM 往下执行
        Finish();
    }

    private IEnumerator HideAfterDelay(GameObject go, float delay, int myVersion)
    {
        yield return new WaitForSeconds(delay);

        // 如果期间又进入过一次，本次作废
        if (myVersion != _version) yield break;

        if (go != null) go.SetActive(false);
        _hideRoutine = null;
    }

    public override void OnExit()
    {
        if (_hideRoutine != null && cancelOnExit.Value)
        {
            Fsm.Owner.StopCoroutine(_hideRoutine);
            _hideRoutine = null;
        }
    }
}
