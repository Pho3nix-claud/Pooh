using UnityEngine;

public class WeaponControl : MonoBehaviour
{
    public static bool InputBlockedByUI;
    public enum WeaponType
    {
        Pistol,
        SMG,
        Rifle
    }

    [Header("References")]
    public GameObject bulletPrefab;
    public Transform firePoint;

    [Header("Weapon Settings")]
    public string weaponName = "Weapon";
    public WeaponType weaponType = WeaponType.Pistol;
    public float fireRate = 0.3f;
    public int bulletCountPerShot = 1;
    public float bulletSpeed = 20f;
    public float spreadAngle = 0f;
    public bool automaticFire;
    public int magazineSize = 12;
    public float reloadDuration = 1.2f;
    public KeyCode reloadKey = KeyCode.R;

    [Header("Mount Settings")]
    public Vector3 mountLocalPosition;
    public Vector3 mountLocalEulerAngles;

    private float nextFireTime;
    private int currentAmmo;
    private bool isReloading;
    private bool isEquipped;

    private void Awake()
    {
        currentAmmo = Mathf.Max(0, magazineSize);
        EnsurePickupTrigger();
    }

    private void Update()
    {
        if (!isEquipped || InputBlockedByUI)
        {
            return;
        }

        if (Input.GetKeyDown(reloadKey))
        {
            TryReload();
        }

        bool wantsToShoot = automaticFire ? Input.GetButton("Fire1") : Input.GetButtonDown("Fire1");

        if (!wantsToShoot || Time.time < nextFireTime || isReloading)
        {
            return;
        }

        if (currentAmmo <= 0)
        {
            TryReload();
            return;
        }

        Shoot();
        currentAmmo--;
        nextFireTime = Time.time + fireRate;
    }

    public void AttachToMount(Transform mountPoint)
    {
        if (mountPoint == null)
        {
            return;
        }

        transform.SetParent(mountPoint, false);
        Vector3 targetLocalPosition = HasCustomMountPosition() ? mountLocalPosition : GetDefaultMountLocalPosition();
        targetLocalPosition.z = -0.2f;
        transform.localPosition = targetLocalPosition;
        transform.localRotation = Quaternion.Euler(HasCustomMountRotation() ? mountLocalEulerAngles : GetDefaultMountLocalEulerAngles());
        isEquipped = true;
    }

    public void DropToWorld(Vector3 worldPosition)
    {
        isEquipped = false;
        isReloading = false;
        transform.SetParent(null, true);
        transform.position = new Vector3(worldPosition.x, worldPosition.y, 0f);
        transform.rotation = Quaternion.identity;
    }

    public bool CanBePickedUp()
    {
        return !isEquipped;
    }

    public bool IsReloading()
    {
        return isReloading;
    }

    public int GetCurrentAmmo()
    {
        return currentAmmo;
    }

    public int GetMagazineSize()
    {
        return magazineSize;
    }

    public void RefillAmmo()
    {
        currentAmmo = Mathf.Max(0, magazineSize);
    }

    private void Shoot()
    {
        if (bulletPrefab == null)
        {
            Debug.LogWarning($"{weaponName} için mermi prefabı eksik.", this);
            return;
        }

        Transform spawnPoint = GetFirePoint();

        for (int index = 0; index < Mathf.Max(1, bulletCountPerShot); index++)
        {
            float angleOffset = GetSpreadOffset(index);
            Quaternion spawnRotation = spawnPoint.rotation * Quaternion.Euler(0f, 0f, angleOffset);
            GameObject bulletObject = Instantiate(bulletPrefab, spawnPoint.position, spawnRotation);
            Bullet bullet = bulletObject.GetComponent<Bullet>();

            if (bullet != null)
            {
                bullet.SetSpeed(bulletSpeed);
            }
        }
    }

    private Transform GetFirePoint()
    {
        if (firePoint != null)
        {
            return firePoint;
        }

        Transform childFirePoint = transform.Find("firepoint");
        if (childFirePoint != null)
        {
            firePoint = childFirePoint;
            return firePoint;
        }

        return transform;
    }

    private float GetSpreadOffset(int bulletIndex)
    {
        if (bulletCountPerShot <= 1 || spreadAngle <= 0f)
        {
            return 0f;
        }

        float step = spreadAngle / (bulletCountPerShot - 1);
        return (-spreadAngle * 0.5f) + (step * bulletIndex);
    }

    private bool HasCustomMountPosition()
    {
        return mountLocalPosition != Vector3.zero;
    }

    private bool HasCustomMountRotation()
    {
        return mountLocalEulerAngles != Vector3.zero;
    }

    private Vector3 GetDefaultMountLocalPosition()
    {
        switch (weaponType)
        {
            case WeaponType.Rifle:
                return new Vector3(0.72f, -0.02f, 0f);
            case WeaponType.SMG:
                return new Vector3(0.45f, -0.03f, 0f);
            default:
                return new Vector3(0.18f, -0.05f, 0f);
        }
    }

    private Vector3 GetDefaultMountLocalEulerAngles()
    {
        switch (weaponType)
        {
            case WeaponType.Rifle:
                return new Vector3(0f, 0f, -4f);
            case WeaponType.SMG:
                return new Vector3(0f, 0f, -2f);
            default:
                return new Vector3(0f, 0f, -8f);
        }
    }

    private void TryReload()
    {
        if (isReloading || currentAmmo >= magazineSize)
        {
            return;
        }

        CancelInvoke(nameof(FinishReload));
        isReloading = true;
        Invoke(nameof(FinishReload), reloadDuration);
    }

    private void FinishReload()
    {
        currentAmmo = Mathf.Max(0, magazineSize);
        isReloading = false;
    }

    private void EnsurePickupTrigger()
    {
        Collider2D pickupCollider = GetComponent<Collider2D>();
        if (pickupCollider == null)
        {
            CircleCollider2D circleCollider = gameObject.AddComponent<CircleCollider2D>();
            circleCollider.radius = 0.9f;
            circleCollider.isTrigger = true;
            return;
        }

        pickupCollider.isTrigger = true;
    }
}
