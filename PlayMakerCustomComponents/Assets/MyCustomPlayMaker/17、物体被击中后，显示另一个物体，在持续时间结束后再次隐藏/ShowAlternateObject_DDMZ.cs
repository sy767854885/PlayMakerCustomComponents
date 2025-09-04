using UnityEngine;
using HutongGames.PlayMaker;
using System.Collections;

[ActionCategory("Custom/Hit")]
[HutongGames.PlayMaker.Tooltip("物体被击中后，立即显示另一个物体，并在持续时间结束后自动隐藏，但不阻塞后续逻辑。")]
public class ShowAlternateObject_DDMZ : FsmStateAction
{
    [RequiredField]
    [HutongGames.PlayMaker.Tooltip("要显示的另一个物体")]
    public FsmGameObject alternateObject;

    [HutongGames.PlayMaker.Tooltip("显示持续的时间（秒）。小于等于0则立即隐藏。")]
    public FsmFloat duration;

    public override void Reset()
    {
        alternateObject = null;
        duration = 0f;
    }

    public override void OnEnter()
    {
        if (alternateObject != null && alternateObject.Value != null)
        {
            alternateObject.Value.SetActive(true);

            // 开启延时隐藏
            if (duration.Value > 0f)
            {
                Fsm.Owner.StartCoroutine(HideAfterDelay(alternateObject.Value, duration.Value));
            }
            else
            {
                alternateObject.Value.SetActive(false);
            }
        }

        // 立即让 FSM 往下走
        Finish();
    }

    private IEnumerator HideAfterDelay(GameObject go, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (go != null) go.SetActive(false);
    }
}
