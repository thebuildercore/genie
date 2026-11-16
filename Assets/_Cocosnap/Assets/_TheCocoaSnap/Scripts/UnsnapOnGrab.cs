using UnityEngine;
using Oculus.Interaction;

public class UnsnapOnGrab : MonoBehaviour
{
    private SnapInteractor _snapInteractor;
    private Grabbable _grabbable;

    void Start()
    {
        _snapInteractor = GetComponent<SnapInteractor>();
        _grabbable = GetComponent<Grabbable>();

        if (_grabbable != null)
        {
            _grabbable.WhenPointerEventRaised += OnPointerEvent;
        }
    }

    private void OnPointerEvent(PointerEvent evt)
    {
        if (evt.Type == PointerEventType.Select)
        {
            if (_snapInteractor != null && _snapInteractor.HasSelectedInteractable)
            {
                _snapInteractor.Unselect();
            }
        }
    }

    void OnDestroy()
    {
        if (_grabbable != null)
        {
            _grabbable.WhenPointerEventRaised -= OnPointerEvent;
        }
    }
}