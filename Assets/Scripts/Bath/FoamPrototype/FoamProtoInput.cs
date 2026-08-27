#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Yurufu.FoamPrototype
{
    /// <summary>
    /// WashPanel に実行時だけ足す入力コンポーネント。
    ///
    /// 【既存を壊さない理由】
    ///   EventSystem は同じ GameObject 上のハンドラを「すべて」呼ぶ。
    ///   既存の BathWashManager も IPointerDownHandler / IPointerUpHandler を実装しているが、
    ///   このコンポーネントを足しても既存の呼び出しは止まらない。
    ///   Input.mousePosition は監視せず、PointerEventData.position だけを使う。
    /// </summary>
    public class FoamProtoInput : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public Action<Vector2> OnDown;
        public Action<Vector2> OnMove;
        public Action          OnUp;

        public void OnPointerDown(PointerEventData e) => OnDown?.Invoke(e.position);
        public void OnDrag(PointerEventData e)        => OnMove?.Invoke(e.position);
        public void OnPointerUp(PointerEventData e)   => OnUp?.Invoke();
    }
}
#endif
