using UnityEngine;
using HutongGames.PlayMaker;

[ActionCategory("Custom/Variable Sync")]
[HutongGames.PlayMaker.Tooltip("向另一个FSM发送一个字符串变量（变量名和值），接收端若存在同名变量则更新。")]
public class SendStringVariable_DDMZ : FsmStateAction
{
    [HutongGames.PlayMaker.Tooltip("目标物体")]
    public FsmGameObject targetGameObject;

    [HutongGames.PlayMaker.Tooltip("目标FSM名称（留空取第一个）")]
    public FsmString fsmName;

    [HutongGames.PlayMaker.Tooltip("要发送的变量（必须是字符串变量）")]
    [UIHint(UIHint.FsmString)]
    public FsmString sourceVariable;

    public override void Reset()
    {
        targetGameObject = null;
        fsmName = "";
        sourceVariable = null;
    }

    public override void OnEnter()
    {
        DoSend();
        Finish();
    }

    private void DoSend()
    {
        if (targetGameObject.Value == null || string.IsNullOrEmpty(sourceVariable.Name))
            return;

        // 找目标FSM
        PlayMakerFSM[] fsms = targetGameObject.Value.GetComponents<PlayMakerFSM>();
        if (fsms == null || fsms.Length == 0) return;

        PlayMakerFSM targetFsm = null;
        if (string.IsNullOrEmpty(fsmName.Value))
        {
            targetFsm = fsms[0];
        }
        else
        {
            foreach (var f in fsms)
            {
                if (f.FsmName == fsmName.Value)
                {
                    targetFsm = f;
                    break;
                }
            }
        }

        if (targetFsm == null) return;

        // 在目标FSM里查找同名变量
        var targetVar = targetFsm.FsmVariables.GetFsmString(sourceVariable.Name);
        if (targetVar != null)
        {
            targetVar.Value = sourceVariable.Value;
        }
    }
}
