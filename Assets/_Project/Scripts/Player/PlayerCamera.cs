using System;
using UnityEngine;
public struct CameraInput
{
    public Vector2 Look;
}
public class PlayerCamera : MonoBehaviour
{
    [SerializeField] private Transform target;

    private Vector3 _eulerAngles;
    [SerializeField] private float sensitivity = 0.1f;
    public void Initialize(Transform target)
    {
        this.target = target;
        transform.position = target.position;
        transform.eulerAngles = _eulerAngles = target.eulerAngles;
    }
    public void UpdateRotation(CameraInput input)
    {
        _eulerAngles += new Vector3(-input.Look.y, input.Look.x) * sensitivity;
        transform.eulerAngles = _eulerAngles;
    }
    public void UpdatePosition(Transform target)
    {
        if (target == null) return;
        transform.position = target.position;
    }

    private Vector3 _lastPos;

private void LateUpdate()
{
    if (target == null) return;

    transform.position = target.position;
    transform.rotation = Quaternion.Euler(_eulerAngles);

   /* if (_lastPos != Vector3.zero && transform.position != _lastPos)
    {
        Debug.LogWarning($"[Camera Drift] Nova posição: {transform.position} (esperado: {target.position})");
    }*/
    _lastPos = transform.position;
}
}
