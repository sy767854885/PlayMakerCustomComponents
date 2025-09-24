using UnityEngine;
using HutongGames.PlayMaker;

[ActionCategory("Custom/FSM")]
[HutongGames.PlayMaker.Tooltip("启用或关闭指定的 PlayMakerFSM 脚本。")]
public class SetFsmEnabled_DDMZ : FsmStateAction
{
    [RequiredField]
    [HutongGames.PlayMaker.Tooltip("要操作的 PlayMakerFSM 组件")]
    [ObjectType(typeof(PlayMakerFSM))]
    public FsmObject targetFsm;

    [HutongGames.PlayMaker.Tooltip("是否启用 FSM（勾选=启用，未勾选=关闭）")]
    public FsmBool enable;

    public override void Reset()
    {
        targetFsm = null;
        enable = true;
    }

    public override void OnEnter()
    {
        DoSetFsmEnabled();
        Finish(); // 立即结束，不阻塞流程
    }

    private void DoSetFsmEnabled()
    {
        if (targetFsm == null || targetFsm.Value == null) return;

        var fsm = targetFsm.Value as PlayMakerFSM;
        if (fsm != null)
        {
            fsm.enabled = enable.Value;
        }
    }
}
