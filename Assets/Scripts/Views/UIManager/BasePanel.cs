using System;
using UnityEngine;
using UnityEngine.Events;

namespace Views.UIManager
{
    public abstract class BasePanel : MonoBehaviour
    {
        private CanvasGroup canvasGroup;
        private float alphaSpeed = 10f;
        public bool isShow; //是否显示

        private UnityAction hideCallback;

        protected virtual void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        }

        protected virtual void Update()
        {
            if (isShow && canvasGroup.alpha != 1)
            {
                canvasGroup.alpha += alphaSpeed * Time.deltaTime;
                if (canvasGroup.alpha >= 1) canvasGroup.alpha = 1;
            }
            else if (!isShow && canvasGroup.alpha != 0)
            {
                canvasGroup.alpha -= alphaSpeed * Time.deltaTime;
                if (canvasGroup.alpha <= 0) canvasGroup.alpha = 0;
                hideCallback.Invoke();
                hideCallback = null;
            }
        }

        public virtual  void ShowMe()
        {
            canvasGroup.alpha = 0;
            isShow = true;
        }

        public virtual void HideMe(UnityAction callback)
        {
            canvasGroup.alpha= 1;
            isShow = false;
            hideCallback = callback;
        }

        protected abstract void Init();
    }
}