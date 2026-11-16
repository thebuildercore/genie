using Oculus.Interaction;
using UnityEngine;

public class CustomSnapInteractor : MonoBehaviour
{
    [SerializeField] private SnapInteractor _snapInteractor;
    [SerializeField, Range(0.001f, 0.1f)] private float _distanceThreshold = 0.03f;

    private void Start()
    {
        _snapInteractor.DistanceThreshold = _distanceThreshold;
    }
}