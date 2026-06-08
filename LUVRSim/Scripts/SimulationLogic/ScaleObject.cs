using UnityEngine;

public class ScaleObject : MonoBehaviour
{
    private Vector3 _initialScale;
    private Vector3 _initialPosition;
    private Vector3 _grabStart;

    private string _activeHandleName;

    public void OnHandleGrabbed(Transform handle)
    {
        // Store initial values
        _initialScale = transform.localScale;
        _initialPosition = transform.position;
        _grabStart = handle.position;
        _activeHandleName = handle.name;
        
        if (_activeHandleName == "ColliderX")
        {
            handle.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezePositionZ;
        }
        else if (_activeHandleName == "ColliderY")
        {
            handle.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezePositionX;
        }
        else if (_activeHandleName == "ColliderZ")
        {
            handle.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionY;
        }
        
    }

    public void OnHandleReleased(Transform handle)
    {
        // Reset active handle
        _activeHandleName = null;
    }

    private void Update()
    {
        if (string.IsNullOrEmpty(_activeHandleName)) return;

        // Find the axis to scale
        Vector3 grabDelta = _grabStart - transform.position;
        Vector3 scaleDelta = Vector3.zero;

        if (_activeHandleName == "ColliderX")
        {
            scaleDelta.x = grabDelta.x;
        }
        else if (_activeHandleName == "ColliderY")
        {
            scaleDelta.y = grabDelta.y;
        }
        else if (_activeHandleName == "ColliderZ")
        {
            scaleDelta.z = grabDelta.z;
        }

        // Apply scaling
        Vector3 newScale = _initialScale + scaleDelta;
        transform.localScale = Vector3.Max(newScale, Vector3.one * 0.1f); // Prevent negative or too small scales

        // Adjust position to keep the opposite side fixed
        Vector3 positionDelta = scaleDelta * 0.5f;
        transform.position = _initialPosition + positionDelta;
    }
}
