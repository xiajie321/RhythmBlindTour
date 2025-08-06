using UnityEngine;

namespace Views.UIManager
{
    public class UIManager
    {
        public static UIManager Instance { get; } = new UIManager();

        private Transform canvasTrans;

        private UIManager()
        {
            GameObject canvas = GameObject.Instantiate(Resources.Load<GameObject>("Prefabs/UI/Canvas"));
        }
    }
}