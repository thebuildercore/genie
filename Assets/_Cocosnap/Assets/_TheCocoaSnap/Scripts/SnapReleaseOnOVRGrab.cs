//using UnityEngine;
//using Oculus.Interaction;

///// <summary>
///// Attach to every Physical Cube alongside Grabbable (ISDK).
///// Distinguishes between a HAND grab and a SNAP grab by checking
///// the identifier on the PointerEvent. Only releases the snap when
///// a real hand grab (HandGrab or RayGrab) is detected.
///// </summary>
//[RequireComponent(typeof(Grabbable))]
//[RequireComponent(typeof(SnapInteractor))]
//[RequireComponent(typeof(Rigidbody))]
//public class SnapReleaseOnGrab : MonoBehaviour
//{
//    [Tooltip("Nudge distance to exit the snap trigger collider on grab (meters).")]
//    public float snapExitOffset = 0.05f;

//    private Grabbable _grabbable;
//    private SnapInteractor _snapInteractor;
//    private Rigidbody _rb;
//    private SnapInteractable _lastSnappedSlot = null;

//    // Track the identifier of the hand that grabbed us
//    // so we can match it on Unselect
//    private int _handGrabIdentifier = -1;

//    private void Awake()
//    {
//        _grabbable = GetComponent<Grabbable>();
//        _snapInteractor = GetComponent<SnapInteractor>();
//        _rb = GetComponent<Rigidbody>();
//    }

//    private void OnEnable()
//    {
//        _grabbable.WhenPointerEventRaised += OnPointerEvent;
//    }

//    private void OnDisable()
//    {
//        _grabbable.WhenPointerEventRaised -= OnPointerEvent;
//    }

//    private void OnPointerEvent(PointerEvent evt)
//    {
//        switch (evt.Type)
//        {
//            case PointerEventType.Select:
//                // Check if this is a hand/ray grab, not a snap event
//                // SnapInteractor raises events with its own identifier
//                // Hand grabs come from HandGrabInteractor or RayInteractor
//                if (IsHandGrab(evt))
//                {
//                    _handGrabIdentifier = evt.Identifier;
//                    OnGrabStarted();
//                }
//                break;

//            case PointerEventType.Unselect:
//                // Only respond to the same hand that grabbed us
//                if (evt.Identifier == _handGrabIdentifier)
//                {
//                    _handGrabIdentifier = -1;
//                    OnGrabEnded();
//                }
//                break;
//        }
//    }

//    private bool IsHandGrab(PointerEvent evt)
//    {
//        // The event's data object tells us who raised it.
//        // SnapInteractor raises events too — we exclude those
//        // by checking if the sender is NOT a SnapInteractor.
//        if (evt.Data == null) return false;

//        // If the interactor raising this event is a SnapInteractor, ignore it
//        var senderObject = evt.Data as MonoBehaviour;
//        if (senderObject != null)
//        {
//            // If the sender is our own SnapInteractor, this is a snap event — skip
//            if (senderObject.GetComponent<SnapInteractor>() != null)
//                return false;

//            // If the sender GameObject is this same object, it's our own snap — skip
//            if (senderObject.gameObject == this.gameObject)
//                return false;
//        }

//        return true;
//    }

//    private void OnGrabStarted()
//    {
//        Debug.Log($"[SnapRelease] {gameObject.name} — HAND grab detected, releasing snap.");

//        // Step 1: Cache snapped slot before unselecting
//        if (_snapInteractor != null && _snapInteractor.HasSelectedInteractable)
//        {
//            _lastSnappedSlot = _snapInteractor.SelectedInteractable as SnapInteractable;
//            _snapInteractor.Unselect();
//        }

//        // Step 2: Disable SnapInteractor so SDK stops re-selecting
//        if (_snapInteractor != null)
//            _snapInteractor.enabled = false;

//        // Step 3: Disable grid slot SnapInteractable + neutralise its Rigidbody
//        if (_lastSnappedSlot != null)
//        {
//            var slotRb = _lastSnappedSlot.GetComponent<Rigidbody>();
//            if (slotRb != null)
//                slotRb.isKinematic = false;

//            _lastSnappedSlot.enabled = false;
//        }

//        // Step 4: Fully unlock this cube's Rigidbody
//        if (_rb != null)
//        {
//            _rb.constraints = RigidbodyConstraints.None;
//            _rb.isKinematic = false;
//            _rb.useGravity = false;
//            _rb.linearVelocity = Vector3.zero;
//            _rb.angularVelocity = Vector3.zero;
//        }

//        // Step 5: Nudge cube out of snap trigger so it can't re-snap this frame
//        transform.position += Vector3.up * snapExitOffset;
//    }

//    private void OnGrabEnded()
//    {
//        Debug.Log($"[SnapRelease] {gameObject.name} — hand released, re-arming snap.");

//        // Restore grid slot
//        if (_lastSnappedSlot != null)
//        {
//            var slotRb = _lastSnappedSlot.GetComponent<Rigidbody>();
//            if (slotRb != null)
//                slotRb.isKinematic = true;

//            _lastSnappedSlot.enabled = true;
//            _lastSnappedSlot = null;
//        }

//        // Re-enable SnapInteractor so cube can snap again
//        if (_snapInteractor != null)
//            _snapInteractor.enabled = true;

//        // Restore normal physics
//        if (_rb != null)
//        {
//            _rb.constraints = RigidbodyConstraints.None;
//            _rb.isKinematic = false;
//            _rb.useGravity = true;
//        }
//    }
//}