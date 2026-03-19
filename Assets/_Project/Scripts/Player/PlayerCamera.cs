using System;
using UnityEngine;
public struct CameraInput
{
    public Vector2 Look;
    public bool UseDeltaTime;
}
public class PlayerCamera : MonoBehaviour
{
    [SerializeField] private Transform target;

    [Header("Look")]
    [SerializeField] private float mouseSensitivity = 0.1f;
    [SerializeField] private float stickSensitivity = 180f;
    [SerializeField] private float rotationSharpness = 22f;
    [SerializeField] private float minPitch = -80f;
    [SerializeField] private float maxPitch = 80f;
    [Header("Follow")]
    [SerializeField] private bool smoothPosition = false;
    [SerializeField] private float positionSharpness = 28f;

    private float _yaw;
    private float _pitch;
    private Quaternion _currentRotation;
    private Vector3 _currentPosition;

    public void Initialize(Transform target)
    {
        this.target = target;

        var eulerAngles = transform.eulerAngles;
        _yaw = eulerAngles.y;
        _pitch = NormalizePitch(eulerAngles.x);
        _currentRotation = Quaternion.Euler(_pitch, _yaw, 0f);
        _currentPosition = target != null ? target.position : transform.position;

        transform.SetPositionAndRotation(_currentPosition, _currentRotation);
    }

    public void UpdateRotation(CameraInput input)
    {
        var sensitivity = input.UseDeltaTime
            ? stickSensitivity * Time.deltaTime
            : mouseSensitivity;

        _yaw += input.Look.x * sensitivity;
        _pitch = Mathf.Clamp(_pitch - input.Look.y * sensitivity, minPitch, maxPitch);
    }

    public void UpdatePosition(Transform target)
    {
        this.target = target;
    }

    public Quaternion GetRotation() => Quaternion.Euler(_pitch, _yaw, 0f);

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        var desiredRotation = GetRotation();
        var desiredPosition = target.position;
        var rotationT = 1f - Mathf.Exp(-rotationSharpness * Time.deltaTime);

        _currentRotation = Quaternion.Slerp(_currentRotation, desiredRotation, rotationT);

        if (smoothPosition)
        {
            var positionT = 1f - Mathf.Exp(-positionSharpness * Time.deltaTime);
            _currentPosition = Vector3.Lerp(_currentPosition, desiredPosition, positionT);
        }
        else
        {
            _currentPosition = desiredPosition;
        }

        transform.SetPositionAndRotation(_currentPosition, _currentRotation);
    }

    private static float NormalizePitch(float angle)
    {
        while (angle > 180f)
        {
            angle -= 360f;
        }

        return angle;
    }
}
