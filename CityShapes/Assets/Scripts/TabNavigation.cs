using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TabNavigation : MonoBehaviour
{
    [SerializeField] private EventSystem _EventSystem = default;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab) && Selectable.allSelectableCount > 0)
        {
            GameObject current = _EventSystem.currentSelectedGameObject;
            if (current)
            {
                if (current.TryGetComponent(out Selectable selectable))
                {
                    Selectable onRight = selectable.FindSelectableOnRight();
                    if (onRight != null)
                    {
                        _EventSystem.SetSelectedGameObject(onRight.gameObject);
                        return;
                    }
                    Selectable onDown = selectable.FindSelectableOnDown();
                    if (onDown != null)
                    {
                        _EventSystem.SetSelectedGameObject(onDown.gameObject);
                        return;
                    }
                }
            }
            _EventSystem.SetSelectedGameObject(GetTopSelectable().gameObject);
        }
        if (Input.GetKeyDown(KeyCode.Return))
        {
            GameObject current = _EventSystem.currentSelectedGameObject;
            if (current != null && current.TryGetComponent(out Button button)) 
            {
                button.OnSubmit(new BaseEventData(_EventSystem));
            }

            // this code would be nicer but for some reason TMP.Inputfields call their OnSubmit message already while Buttons do not,
            // so this code would invoke it twice.
             
            //ISubmitHandler handler = (ISubmitHandler)current.GetComponent(typeof(ISubmitHandler));
            //if (handler != null)
            //{
            //    handler.OnSubmit(new BaseEventData(_EventSystem));
            //}
        }
    }

    private Selectable GetTopSelectable()
    {
        Selectable topSelectable = null;
        foreach (Selectable selectable in Selectable.allSelectablesArray)
        {
            if (selectable.navigation.mode == Navigation.Mode.None)
            {
                continue;
            }

            if (topSelectable == null || selectable.transform.position.y > topSelectable.transform.position.y)
            {
                topSelectable = selectable;
            }
        }
        return topSelectable;
    }
}
