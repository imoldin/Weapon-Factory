using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using WeaponFactory.Audio;
using WeaponFactory.Utilities;

namespace WeaponFactory.UI
{
    /// <summary>Press-down scale + click sound for every button.</summary>
    public class ButtonFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        Vector3 _base = Vector3.one;
        bool _init;

        void Init() { if (!_init) { _base = transform.localScale; _init = true; } }

        public void OnPointerDown(PointerEventData e)
        {
            Init();
            var b = GetComponent<Button>();
            if (b != null && !b.interactable) return;
            Tween.Kill(transform);
            Tween.Scale(transform, _base * 0.92f, 0.08f, Ease.OutQuad, 0f, true);
        }

        public void OnPointerUp(PointerEventData e)
        {
            Init();
            Tween.Kill(transform);
            Tween.Scale(transform, _base, 0.25f, Ease.OutBack, 0f, true);
        }

        public void OnPointerClick(PointerEventData e)
        {
            var b = GetComponent<Button>();
            if (b != null && !b.interactable)
            {
                AudioManager.Play(SfxId.Error, 0.5f);
                return;
            }
            AudioManager.Play(SfxId.UIClick, 0.7f);
        }
    }
}
