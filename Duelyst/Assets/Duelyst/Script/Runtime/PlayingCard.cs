using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PlayingCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    private RectTransform selectingArrowRect;

    private Material outline;
    private Image image;
    private Animator animator;

    private void Awake()
    {
        selectingArrowRect = Functions.GetRootGameObject(Functions.NAME_OBJCANVAS).FindChildGameObject(Functions.NAME_SELECTINGARROW).GetComponent<RectTransform>();
        outline = Functions.outline;
        image = GetComponent<Image>();
        animator = GetComponent<Animator>();
    }

    public void OnPointerEnter(PointerEventData ped)
    {
        image.material = outline;
    }

    public void OnPointerExit(PointerEventData ped)
    {
        image.material = null;
    }

    public void OnBeginDrag(PointerEventData ped)
    {
        //드래그 시작
        selectingArrowRect.gameObject.SetActive(true);
        selectingArrowRect.position =  Camera.main.ScreenToViewportPoint(transform.position);
    }

    public void OnDrag(PointerEventData ped)
    {

    }

    public void OnEndDrag(PointerEventData ped)
    {
        //레이캐스트로 타겟 가져오기
        GameObject raycastTarget = ped.pointerCurrentRaycast.gameObject;
        if (raycastTarget != null)
        {
            Debug.Log(raycastTarget.tag);
            if (raycastTarget.tag == Functions.TAG_ENEMY)
            {
                //공격
            }
            else if (raycastTarget.tag == Functions.TAG_PLACE)
            {
                //아무도 있지 않을 때
                //
                //

                //이동
                StartCoroutine(Move(raycastTarget.transform.GetChild(0).GetComponent<RectTransform>()));
            }
        }

        //드래그 종료
        selectingArrowRect.gameObject.SetActive(false);
    }

    public void OnPointerClick(PointerEventData ped)
    {
        if (ped.button == PointerEventData.InputButton.Right)
        {
            //카드 상세보기
            UIManager.Instance.ShowPlayerCardDetail(image.sprite, animator.runtimeAnimatorController);
        }
    }

    public IEnumerator Move(RectTransform distRect)
    {
        Vector3 sourcePos = transform.position;
        float timer = 1f;
        const int FRAME = 60;
        float term = (float)1f / FRAME;

        transform.SetParent(distRect.transform.parent.parent);

        while (timer >= 0)
        {
            transform.position = Vector3.Lerp(sourcePos, distRect.position, 1 - timer);

            yield return new WaitForSeconds(term);
            timer -= term;
        }
    }
}