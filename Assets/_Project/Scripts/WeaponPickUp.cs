using UnityEngine;
using DG.Tweening;

public class WeaponPickUp : MonoBehaviour, IInteractable
{
    public GameObject weaponPrefab;
    [SerializeField] private WeaponHandler _playerWeapon;
    [SerializeField] private InteractableOutlineTarget outlineTarget;
    [Header("Animation")]
    [SerializeField] private float floatHeight = 0.2f;
    [SerializeField] private float floatDuration = 1.2f;
    [SerializeField] private float rotationDuration = 3f;
    [SerializeField] private Vector3 rotationAxis = Vector3.up;

    private Vector3 _initialLocalPosition;
    private Tween _floatTween;
    private Tween _rotationTween;

    private void Awake()
    {
        outlineTarget ??= GetComponent<InteractableOutlineTarget>();
        if (outlineTarget == null)
        {
            outlineTarget = gameObject.AddComponent<InteractableOutlineTarget>();
        }

        _initialLocalPosition = transform.localPosition;
    }

    private void OnEnable()
    {
        StartIdleAnimation();
    }

    private void OnDisable()
    {
        StopIdleAnimation();
    }

    private void OnDestroy()
    {
        StopIdleAnimation();
    }

    public void OnInteract()
    {
        _playerWeapon = FindFirstObjectByType<WeaponHandler>();

        if (_playerWeapon != null && weaponPrefab != null)
        {
            GameObject weaponInstance = Instantiate(weaponPrefab);
            var weapon = weaponInstance.GetComponentInChildren<WeaponBase>(true);

            if (weapon != null)
            {
                _playerWeapon.EquipWeapon(weapon);
                Debug.Log($"Weapon {weapon.name} given to the player.");
                StopIdleAnimation();
                outlineTarget?.SetHighlighted(false);
                gameObject.SetActive(false);
                Destroy(this);
            }
            else
            {
                Debug.LogWarning("Weapon prefab doesn't have WeaponBase.");
                Destroy(weaponInstance);
            }
        }
        else
        {
            Debug.LogWarning("Weapon pickup failed because player weapon handler or weapon prefab is missing.", this);
        }
    }

    private void StartIdleAnimation()
    {
        StopIdleAnimation();

        _floatTween = transform
            .DOLocalMoveY(_initialLocalPosition.y + floatHeight, floatDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);

        _rotationTween = transform
            .DOLocalRotate(rotationAxis.normalized * 360f, rotationDuration, RotateMode.FastBeyond360)
            .SetEase(Ease.Linear)
            .SetRelative(true)
            .SetLoops(-1, LoopType.Restart);
    }

    private void StopIdleAnimation()
    {
        _floatTween?.Kill();
        _rotationTween?.Kill();
        _floatTween = null;
        _rotationTween = null;
    }
}
