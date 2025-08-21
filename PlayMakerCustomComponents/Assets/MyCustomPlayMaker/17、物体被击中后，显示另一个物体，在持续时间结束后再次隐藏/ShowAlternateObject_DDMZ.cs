using UnityEngine;
using HutongGames.PlayMaker;

[ActionCategory("Custom/Hit")]
[HutongGames.PlayMaker.Tooltip("物体被击中后，显示另一个物体，在持续时间结束后再次隐藏。")]
public class ShowAlternateObject_DDMZ : FsmStateAction
{
    [RequiredField]
    [HutongGames.PlayMaker.Tooltip("要显示的另一个物体")]
    public FsmGameObject alternateObject;

    [HutongGames.PlayMaker.Tooltip("显示持续的时间（秒）。小于等于0则立即隐藏。")]
    public FsmFloat duration;

    private float _timer;

    public override void Reset()
    {
        alternateObject = null;
        duration = 0f;
        _timer = 0f;
    }

    public override void OnEnter()
    {
        if (alternateObject != null && alternateObject.Value != null)
        {
            alternateObject.Value.SetActive(true);
        }

        _timer = duration.Value;

        if (_timer <= 0f)
        {
            EndAndHide();
        }
    }

    public override void OnUpdate()
    {
        _timer -= Time.deltaTime;

        if (_timer <= 0f)
        {
            EndAndHide();
        }
    }

    private void EndAndHide()
    {
        if (alternateObject != null && alternateObject.Value != null)
        {
            alternateObject.Value.SetActive(false);
        }
        Finish();
    }
}
