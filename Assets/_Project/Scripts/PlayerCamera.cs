using System;
using UnityEngine;
public struct CameraInput
{
    public Vector2 Look;
}
public class PlayerCamera : MonoBehaviour
{
    private Vector3 _eulerAngles;
    [SerializeField] private float sensitivity = 0.1f;
    public void Initialize(Transform target)
    {
        transform.SetPositionAndRotation(target.position, target.rotation);
        transform.eulerAngles = _eulerAngles = target.eulerAngles;
    }
    public void UpdateRotation(CameraInput input) 
    {
        _eulerAngles += new Vector3(-input.Look.y, input.Look.x) * sensitivity;
        transform.eulerAngles = _eulerAngles;
    }
    public void UpdatePosition(Transform target)
    {
        transform.position = target.position;
    }

}
