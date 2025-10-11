using UnityEngine;
using HutongGames.PlayMaker;

[ActionCategory("Custom/FSM")]
[HutongGames.PlayMaker.Tooltip("通过 GameObject 或子物体找到指定名字的 FSM，并返回 PlayMakerFSM 对象")]
public class GetFSMByName_DDMZ : FsmStateAction
{
    [RequiredField]
    [HutongGames.PlayMaker.Tooltip("要搜索的 GameObject")]
    public FsmOwnerDefault targetGameObject;

    [RequiredField]
    [HutongGames.PlayMaker.Tooltip("FSM 的名字")]
    public FsmString fsmName;

    [HutongGames.PlayMaker.Tooltip("找到的 PlayMakerFSM 对象")]
    [UIHint(UIHint.Variable)]
    public FsmObject storeFsm;

    [HutongGames.PlayMaker.Tooltip("是否在子物体中搜索")]
    public bool searchChildren = true;

    public override void Reset()
    {
        targetGameObject = null;
        fsmName = "";
        storeFsm = null;
        searchChildren = true;
    }

    public override void OnEnter()
    {
        GameObject go = Fsm.GetOwnerDefaultTarget(targetGameObject);
        if (go == null)
        {
            storeFsm.Value = null;
            Finish();
            return;
        }

        PlayMakerFSM fsm = FindFSM(go, fsmName.Value);
        storeFsm.Value = fsm;
        Finish();
    }

    private PlayMakerFSM FindFSM(GameObject go, string name)
    {
        // 先查自己
        PlayMakerFSM fsm = go.GetComponent<PlayMakerFSM>();
        if (fsm != null && fsm.FsmName == name)
        {
            return fsm;
        }

        if (searchChildren)
        {
            // 查所有子物体
            foreach (Transform child in go.transform)
            {
                fsm = FindFSM(child.gameObject, name);
                if (fsm != null) return fsm;
            }
        }

        return null;
    }
}
