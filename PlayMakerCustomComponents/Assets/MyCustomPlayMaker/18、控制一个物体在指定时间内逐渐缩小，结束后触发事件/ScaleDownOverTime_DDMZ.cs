using UnityEngine;
using HutongGames.PlayMaker;

[ActionCategory("Custom/Transform")]
[HutongGames.PlayMaker.Tooltip("控制一个物体在指定时间内逐渐缩小，结束后触发事件。")]
public class ScaleDownOverTime_DDMZ : FsmStateAction
{
    [RequiredField]
    [HutongGames.PlayMaker.Tooltip("要缩小的目标物体")]
    public FsmOwnerDefault gameObject;

    [HutongGames.PlayMaker.Tooltip("缩小到的最终缩放值（通常是 0,0,0 表示完全消失）")]
    public FsmVector3 targetScale;

    [HutongGames.PlayMaker.Tooltip("缩小所用的时间（秒）")]
    public FsmFloat duration;

    [HutongGames.PlayMaker.Tooltip("缩小完成后要触发的事件")]
    public FsmEvent finishEvent;

    private Vector3 _startScale;
    private float _timer;
    private GameObject _go;

    public override void Reset()
    {
        gameObject = null;
        targetScale = new FsmVector3 { UseVariable = false };
        duration = 1f;
        finishEvent = null;
        _timer = 0f;
        _go = null;
    }

    public override void OnEnter()
    {
        _go = Fsm.GetOwnerDefaultTarget(gameObject);
        if (_go == null)
        {
            Finish();
            return;
        }

        _startScale = _go.transform.localScale;
        _timer = 0f;
    }

    public override void OnUpdate()
    {
        if (_go == null) return;

        _timer += Time.deltaTime;
        float t = Mathf.Clamp01(_timer / duration.Value);

        _go.transform.localScale = Vector3.Lerp(_startScale, targetScale.Value, t);

        if (t >= 1f)
        {
            if (finishEvent != null)
            {
                Fsm.Event(finishEvent);
            }
            Finish();
        }
    }
}
