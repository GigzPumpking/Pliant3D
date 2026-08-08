using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ChangeCursor : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
   [SerializeField] private ModeOfCursor modeOfCursor;
   [SerializeField] private ClickSoundType clickSoundType;
   
   
   public void OnPointerEnter(PointerEventData eventData)
   {
      CursorController.Instance.SetToMode(modeOfCursor);
      CursorController.Instance.PlayHoverSound();
   }

   public void OnPointerExit(PointerEventData eventData)
   {
      CursorController.Instance.SetToMode(ModeOfCursor.Default);
   }

   public void OnPointerClick(PointerEventData eventData)
   {
      CursorController.Instance.PlayClickSound(clickSoundType);
   }
}
