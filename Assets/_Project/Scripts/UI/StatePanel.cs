using Reloj.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Reloj.UI
{
    public class StatePanel : MonoBehaviour
    {
        [SerializeField] private GameState visibleIn;
        [SerializeField] private Selectable firstSelected;

        public GameState VisibleIn => visibleIn;

        public virtual void Show()
        {
            gameObject.SetActive(true);
            Select();
        }

        public virtual void Hide() => gameObject.SetActive(false);

        public void Select()
        {
            var es = EventSystem.current;
            var first = GetFirstSelectable();
            if (es == null || first == null) return;
            es.SetSelectedGameObject(null);
            es.SetSelectedGameObject(first.gameObject);
        }

        protected virtual Selectable GetFirstSelectable() => firstSelected;
    }
}