using UnityEngine;
using HutongGames.PlayMaker;
using System.Collections.Generic;

[ActionCategory("Custom/Detection")]
[HutongGames.PlayMaker.Tooltip("优化版：2D/3D射线检测物体是否穿过指定层级，每颗子弹每帧只做一次射线检测，穿过不同层触发事件，并返回碰到的物体。")]
public class RaycastPassThroughReturnObject : FsmStateAction
{
    [RequiredField]
    public FsmOwnerDefault gameObject;

    [HutongGames.PlayMaker.Tooltip("是否使用2D物理检测，否则使用3D")]
    public FsmBool use2D;

    [System.Serializable]
    public class LayerEventEntry
    {
        [HutongGames.PlayMaker.Tooltip("可穿透层级")]
        public FsmInt layer;

        [HutongGames.PlayMaker.Tooltip("穿过该层触发事件")]
        public FsmEvent passThroughEvent;
    }

    [HutongGames.PlayMaker.Tooltip("层级与事件映射数组")]
    public LayerEventEntry[] layerEventList;

    [HutongGames.PlayMaker.Tooltip("碰到的物体输出")]
    public FsmGameObject hitObject;

    private Vector3 lastPosition;
    private int combinedLayerMask;

    public override void OnEnter()
    {
        var go = Fsm.GetOwnerDefaultTarget(gameObject);
        if (go != null)
        {
            lastPosition = go.transform.position;
        }

        // 合并所有层级为一次检测的LayerMask
        combinedLayerMask = 0;
        if (layerEventList != null)
        {
            foreach (var entry in layerEventList)
            {
                if (entry != null)
                {
                    combinedLayerMask |= 1 << entry.layer.Value;
                }
            }
        }
    }

    public override void OnUpdate()
    {
        var go = Fsm.GetOwnerDefaultTarget(gameObject);
        if (go == null) return;

        Vector3 currentPosition = go.transform.position;

        if (use2D.Value)
        {
            // 2D射线检测一次
            RaycastHit2D hit = Physics2D.Linecast(lastPosition, currentPosition, combinedLayerMask);
            if (hit.collider != null)
            {
                hitObject.Value = hit.collider.gameObject;  // 返回碰到的物体
                TriggerEventByLayer(hit.collider.gameObject.layer);
            }
        }
        else
        {
            // 3D射线检测一次
            if (Physics.Linecast(lastPosition, currentPosition, out RaycastHit hit3D, combinedLayerMask))
            {
                hitObject.Value = hit3D.collider.gameObject; // 返回碰到的物体
                TriggerEventByLayer(hit3D.collider.gameObject.layer);
            }
        }

        lastPosition = currentPosition;
    }

    private void TriggerEventByLayer(int layer)
    {
        if (layerEventList == null) return;

        foreach (var entry in layerEventList)
        {
            if (entry != null && entry.layer.Value == layer && entry.passThroughEvent != null)
            {
                Fsm.Event(entry.passThroughEvent);
                break; // 一次只触发最先匹配事件
            }
        }
    }
}
