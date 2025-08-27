using UnityEngine;
using HutongGames.PlayMaker;

[ActionCategory("Custom")]
[HutongGames.PlayMaker.Tooltip("查找第一个处于指定Layer的GameObject；Layer可用编号或名称。")]
public class FindGameObjectByLayer_DDMZ : FsmStateAction
{
    [HutongGames.PlayMaker.Tooltip("优先使用Layer名称（非空时生效）；例如：Default、UI、Player")]
    public FsmString layerName;

    [HutongGames.PlayMaker.Tooltip("Layer编号（当Layer名称为空或无效时使用）")]
    public FsmInt layer;

    [HutongGames.PlayMaker.Tooltip("是否包含未激活( inactive )物体")]
    public FsmBool includeInactive;

    [HutongGames.PlayMaker.Tooltip("存储找到的GameObject（只返回第一个匹配项）")]
    [UIHint(UIHint.Variable)]
    public FsmGameObject storeResult;

    public override void Reset()
    {
        layerName = "";
        layer = 0;
        includeInactive = false;
        storeResult = null;
    }

    public override void OnEnter()
    {
        // 解析 Layer（名称优先）
        int targetLayer = -1;
        string ln = layerName.Value;
        if (!string.IsNullOrEmpty(ln))
        {
            targetLayer = LayerMask.NameToLayer(ln);
        }
        if (targetLayer < 0)
        {
            targetLayer = layer.Value; // 回退到编号
        }

        // 如果Layer仍然非法，直接结束
        if (targetLayer < 0 || targetLayer > 31)
        {
            storeResult.Value = null;
            Finish();
            return;
        }

        // 获取场景中所有GameObject
        GameObject[] allObjects;
#if UNITY_2022_2_OR_NEWER
        allObjects = Object.FindObjectsByType<GameObject>(
            includeInactive.Value ? FindObjectsInactive.Include : FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);
#else
        allObjects = Object.FindObjectsOfType<GameObject>(includeInactive.Value);
#endif

        GameObject found = null;

        for (int i = 0; i < allObjects.Length; i++)
        {
            var go = allObjects[i];
            if (go == null) continue;
            if (go.layer != targetLayer) continue;

            found = go;
            break; // 只取第一个
        }

        storeResult.Value = found;
        Finish();
    }
}
