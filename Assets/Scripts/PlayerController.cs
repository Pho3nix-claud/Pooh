using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    [System.Serializable]
    public class BuyMenuWeaponEntry
    {
        public string label;
        public int price;
        public WeaponControl weaponPrefab;
        public Sprite icon;
    }

    private enum HoldPose
    {
        Pistol,
        Rifle
    }

    public bool isGrounded;
    public float groundCheckDistance = 1.1f;
    public LayerMask groundLayer;
    public float speed = 5f;
    public Rigidbody2D rb;
    public Collider2D playerCollider;
    public Vector3 moveDirection;
    public float jumpForce;
    public int maxJumpCount = 2;
    public float walkFootRotation = 18f;
    public float walkAnimationSpeed = 10f;
    public float poseBlendSpeed = 10f;
    public GameObject pistolPosePrefab;
    public GameObject riflePosePrefab;
    public KeyCode swapPickupKey = KeyCode.E;
    public KeyCode buyMenuKey = KeyCode.B;
    public float verticalMoveMultiplier = 0.15f;
    public Sprite buyMenuBackgroundSprite;
    public Sprite buyMenuCharacterSprite;
    public List<BuyMenuWeaponEntry> buyMenuWeapons = new List<BuyMenuWeaponEntry>();

    private int jumpCount;
    private bool wasGrounded;
    private Transform leftHand;
    private Transform rightHand;
    private Transform leftFoot;
    private Transform rightFoot;
    private Transform gunPos;
    private WeaponControl equippedWeapon;
    private Animator animator;
    private HoldPose currentPose = HoldPose.Pistol;
    private float walkCycle;
    private WeaponControl nearbyWeapon;
    private Text ammoText;
    private Canvas hudCanvas;
    private GameObject buyMenuRoot;
    private GridLayoutGroup buyMenuGrid;
    private bool isBuyMenuOpen;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<Collider2D>();
        animator = GetComponent<Animator>();
        leftHand = transform.Find("body/left_hand") ?? transform.Find("left_hand");
        rightHand = transform.Find("body/right_hand") ?? transform.Find("right_hand");
        leftFoot = transform.Find("body/left_foot") ?? transform.Find("left_foot");
        rightFoot = transform.Find("body/right_foot") ?? transform.Find("right_foot");
        gunPos = EnsureGunPos();
        equippedWeapon = GetComponentInChildren<WeaponControl>();
        AttachWeaponToGunPos(true);
        UpdateAnimationState(false);
        CreateAmmoUIIfNeeded();
        RefreshAmmoUI();
    }

    void Update()
    {
        if (Input.GetKeyDown(buyMenuKey))
        {
            ToggleBuyMenu();
        }

        if (isBuyMenuOpen)
        {
            RefreshAmmoUI();
            return;
        }

        float horizontalInput = 0f;
        moveDirection = Vector3.zero;
        bool isMovingInput = false;

        if (Input.GetKey(KeyCode.D))
        {
            horizontalInput += 1f;
            moveDirection.x += 1f;
            transform.Translate(Vector2.right * speed);
            isMovingInput = true;
        }

        if (Input.GetKey(KeyCode.A))
        {
            horizontalInput -= 1f;
            moveDirection.x -= 1f;
            transform.Translate(Vector2.left * speed);
            isMovingInput = true;
        }

        if (Input.GetKey(KeyCode.W))
        {
            moveDirection.y += 1f;
            transform.Translate(Vector2.up * speed * verticalMoveMultiplier);
            isMovingInput = true;
        }

        if (Input.GetKey(KeyCode.S))
        {
            moveDirection.y -= 1f;
            transform.Translate(Vector2.down * speed * verticalMoveMultiplier);
            isMovingInput = true;
        }

        HandleWeaponPickupInput();
        UpdateWeaponReference();
        UpdateHoldPose();
        UpdateWalkAnimation(isMovingInput);
        RefreshAmmoUI();

        wasGrounded = isGrounded;
    }

    private void UpdateWeaponReference()
    {
        WeaponControl latestWeapon = GetComponentInChildren<WeaponControl>();
        if (latestWeapon != null && latestWeapon != equippedWeapon)
        {
            equippedWeapon = latestWeapon;
            AttachWeaponToGunPos(false);
        }
        else if (equippedWeapon != null && gunPos != null && equippedWeapon.transform.parent != gunPos)
        {
            AttachWeaponToGunPos(false);
        }
    }

    private void AttachWeaponToGunPos(bool forceRefresh)
    {
        gunPos = EnsureGunPos();
        if (equippedWeapon == null || gunPos == null)
        {
            return;
        }

        if (forceRefresh || equippedWeapon.transform.parent != gunPos)
        {
            equippedWeapon.AttachToMount(gunPos);
        }
    }

    private void HandleWeaponPickupInput()
    {
        if (nearbyWeapon == null || !nearbyWeapon.CanBePickedUp())
        {
            return;
        }

        if (equippedWeapon == null)
        {
            EquipNearbyWeapon();
            return;
        }

        if (Input.GetKeyDown(swapPickupKey))
        {
            Vector3 dropPosition = transform.position + new Vector3(transform.localScale.x >= 0f ? 0.8f : -0.8f, -0.1f, 0f);
            equippedWeapon.DropToWorld(dropPosition);
            EquipNearbyWeapon();
        }
    }

    private void EquipNearbyWeapon()
    {
        if (nearbyWeapon == null)
        {
            return;
        }

        equippedWeapon = nearbyWeapon;
        nearbyWeapon = null;
        AttachWeaponToGunPos(true);
        RefreshAmmoUI();
    }

    private Transform EnsureGunPos()
    {
        if (rightHand == null)
        {
            return null;
        }

        Transform existingGunPos = rightHand.Find("gunPos");
        if (existingGunPos != null)
        {
            return existingGunPos;
        }

        GameObject gunPosObject = new GameObject("gunPos");
        Transform gunPosTransform = gunPosObject.transform;
        gunPosTransform.SetParent(rightHand, false);
        gunPosTransform.localPosition = Vector3.zero;
        gunPosTransform.localRotation = Quaternion.identity;
        gunPosTransform.localScale = Vector3.one;
        return gunPosTransform;
    }

    private void UpdateHoldPose()
    {
        Transform poseRoot = GetActivePoseRoot();
        if (poseRoot == null)
        {
            return;
        }

        Transform poseLeftHand = FindPoseChild(poseRoot, "left_hand");
        Transform poseRightHand = FindPoseChild(poseRoot, "right_hand");

        if (leftHand != null && poseLeftHand != null)
        {
            leftHand.localPosition = Vector3.Lerp(leftHand.localPosition, poseLeftHand.localPosition, Time.deltaTime * poseBlendSpeed);
            leftHand.localRotation = Quaternion.Lerp(leftHand.localRotation, poseLeftHand.localRotation, Time.deltaTime * poseBlendSpeed);
        }

        if (rightHand != null && poseRightHand != null)
        {
            rightHand.localPosition = Vector3.Lerp(rightHand.localPosition, poseRightHand.localPosition, Time.deltaTime * poseBlendSpeed);
            rightHand.localRotation = Quaternion.Lerp(rightHand.localRotation, poseRightHand.localRotation, Time.deltaTime * poseBlendSpeed);
        }
    }

    private void UpdateWalkAnimation(bool isMovingInput)
    {
        Transform poseRoot = GetActivePoseRoot();
        Transform poseLeftFoot = FindPoseChild(poseRoot, "left_foot");
        Transform poseRightFoot = FindPoseChild(poseRoot, "right_foot");

        if (leftFoot == null || rightFoot == null || poseLeftFoot == null || poseRightFoot == null)
        {
            return;
        }

        bool isWalking = isMovingInput;
        float leftAngleOffset = 0f;
        float rightAngleOffset = 0f;

        if (isWalking)
        {
            walkCycle += Time.deltaTime * walkAnimationSpeed;
            leftAngleOffset = Mathf.Sin(walkCycle) * walkFootRotation;
            rightAngleOffset = Mathf.Sin(walkCycle + Mathf.PI) * walkFootRotation;
        }
        else
        {
            walkCycle = 0f;
        }

        Quaternion leftTargetRotation = poseLeftFoot.localRotation * Quaternion.Euler(0f, 0f, leftAngleOffset);
        Quaternion rightTargetRotation = poseRightFoot.localRotation * Quaternion.Euler(0f, 0f, rightAngleOffset);

        leftFoot.localPosition = Vector3.Lerp(leftFoot.localPosition, poseLeftFoot.localPosition, Time.deltaTime * poseBlendSpeed);
        rightFoot.localPosition = Vector3.Lerp(rightFoot.localPosition, poseRightFoot.localPosition, Time.deltaTime * poseBlendSpeed);
        leftFoot.localRotation = Quaternion.Lerp(leftFoot.localRotation, leftTargetRotation, Time.deltaTime * poseBlendSpeed);
        rightFoot.localRotation = Quaternion.Lerp(rightFoot.localRotation, rightTargetRotation, Time.deltaTime * poseBlendSpeed);

        UpdateAnimationState(isWalking);
    }

    private Transform GetActivePoseRoot()
    {
        currentPose = HoldPose.Pistol;
        if (equippedWeapon != null && equippedWeapon.weaponType == WeaponControl.WeaponType.Rifle)
        {
            currentPose = HoldPose.Rifle;
        }

        GameObject posePrefab = currentPose == HoldPose.Rifle ? riflePosePrefab : pistolPosePrefab;
        return posePrefab != null ? posePrefab.transform : null;
    }

    private void UpdateAnimationState(bool isWalking)
    {
        if (animator == null)
        {
            return;
        }

        string targetState = currentPose == HoldPose.Rifle ? "walkwithrifle" : "walk";
        AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);

        if (!currentState.IsName(targetState))
        {
            animator.Play(targetState, 0, 0f);
        }

        animator.speed = isWalking ? 1f : 0f;
    }

    private static Transform FindPoseChild(Transform poseRoot, string childName)
    {
        if (poseRoot == null)
        {
            return null;
        }

        Transform directChild = poseRoot.Find($"body/{childName}");
        return directChild != null ? directChild : poseRoot.Find(childName);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        UpdateGroundState(collision, true);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        UpdateGroundState(collision, true);
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        UpdateGroundState(collision, false);
    }

    private void UpdateGroundState(Collision2D collision, bool groundedState)
    {
        if (!collision.collider.CompareTag("Ground"))
        {
            return;
        }

        isGrounded = groundedState;

        if (groundedState)
        {
            jumpCount = 0;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        WeaponControl weapon = other.GetComponent<WeaponControl>();
        if (weapon != null && weapon.CanBePickedUp())
        {
            nearbyWeapon = weapon;
            if (equippedWeapon == null)
            {
                EquipNearbyWeapon();
            }
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        WeaponControl weapon = other.GetComponent<WeaponControl>();
        if (weapon != null && weapon.CanBePickedUp())
        {
            nearbyWeapon = weapon;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        WeaponControl weapon = other.GetComponent<WeaponControl>();
        if (weapon != null && weapon == nearbyWeapon)
        {
            nearbyWeapon = null;
        }
    }

    private void CreateAmmoUIIfNeeded()
    {
        Canvas existingCanvas = FindObjectOfType<Canvas>();
        if (existingCanvas != null)
        {
            Transform existingAmmo = existingCanvas.transform.Find("AmmoText");
            if (existingAmmo != null)
            {
                hudCanvas = existingCanvas;
                ammoText = existingAmmo.GetComponent<Text>();
                return;
            }
        }

        GameObject canvasObject = new GameObject("HUDCanvas");
        hudCanvas = canvasObject.AddComponent<Canvas>();
        hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject ammoObject = new GameObject("AmmoText");
        ammoObject.transform.SetParent(canvasObject.transform, false);
        ammoText = ammoObject.AddComponent<Text>();
        ammoText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        ammoText.fontSize = 28;
        ammoText.alignment = TextAnchor.LowerRight;
        ammoText.color = Color.white;

        RectTransform rectTransform = ammoText.rectTransform;
        rectTransform.anchorMin = new Vector2(1f, 0f);
        rectTransform.anchorMax = new Vector2(1f, 0f);
        rectTransform.pivot = new Vector2(1f, 0f);
        rectTransform.anchoredPosition = new Vector2(-24f, 24f);
        rectTransform.sizeDelta = new Vector2(240f, 60f);
    }

    private void ToggleBuyMenu()
    {
        if (buyMenuRoot == null)
        {
            CreateBuyMenuIfNeeded();
        }

        isBuyMenuOpen = !isBuyMenuOpen;
        buyMenuRoot.SetActive(isBuyMenuOpen);
        WeaponControl.InputBlockedByUI = isBuyMenuOpen;

        if (rb != null && isBuyMenuOpen)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    private void CreateBuyMenuIfNeeded()
    {
        if (hudCanvas == null)
        {
            CreateAmmoUIIfNeeded();
        }

        EnsureEventSystemExists();

        buyMenuRoot = new GameObject("BuyMenu");
        buyMenuRoot.transform.SetParent(hudCanvas.transform, false);

        RectTransform rootRect = buyMenuRoot.AddComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        Image dimmer = buyMenuRoot.AddComponent<Image>();
        dimmer.color = new Color(0f, 0f, 0f, 0.55f);

        GameObject panelObject = new GameObject("BuyPanel");
        panelObject.transform.SetParent(buyMenuRoot.transform, false);
        RectTransform panelRect = panelObject.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(1080f, 620f);

        Image panelImage = panelObject.AddComponent<Image>();
        panelImage.sprite = buyMenuBackgroundSprite;
        panelImage.color = buyMenuBackgroundSprite == null ? new Color(0.09f, 0.12f, 0.16f, 0.96f) : Color.white;

        GameObject characterObject = new GameObject("CharacterPreview");
        characterObject.transform.SetParent(panelObject.transform, false);
        RectTransform characterRect = characterObject.AddComponent<RectTransform>();
        characterRect.anchorMin = new Vector2(0f, 0f);
        characterRect.anchorMax = new Vector2(0f, 1f);
        characterRect.pivot = new Vector2(0f, 0.5f);
        characterRect.offsetMin = new Vector2(40f, 40f);
        characterRect.offsetMax = new Vector2(300f, -40f);
        characterRect.localPosition = new Vector3(characterRect.localPosition.x, characterRect.localPosition.y, -5f);

        Image characterImage = characterObject.AddComponent<Image>();
        characterImage.sprite = buyMenuCharacterSprite;
        characterImage.preserveAspect = true;
        characterImage.color = buyMenuCharacterSprite == null ? new Color(1f, 1f, 1f, 0f) : Color.white;

        GameObject weaponsObject = new GameObject("Weapons");
        weaponsObject.transform.SetParent(panelObject.transform, false);
        RectTransform weaponsRect = weaponsObject.AddComponent<RectTransform>();
        weaponsRect.anchorMin = new Vector2(0f, 0f);
        weaponsRect.anchorMax = new Vector2(1f, 1f);
        weaponsRect.offsetMin = new Vector2(360f, 40f);
        weaponsRect.offsetMax = new Vector2(-40f, -40f);

        buyMenuGrid = weaponsObject.AddComponent<GridLayoutGroup>();
        buyMenuGrid.cellSize = new Vector2(320f, 110f);
        buyMenuGrid.spacing = new Vector2(18f, 18f);
        buyMenuGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        buyMenuGrid.constraintCount = 2;
        buyMenuGrid.childAlignment = TextAnchor.UpperLeft;

        PopulateBuyMenuButtons();
        buyMenuRoot.SetActive(false);
    }

    private void PopulateBuyMenuButtons()
    {
        if (buyMenuGrid == null)
        {
            return;
        }

        foreach (BuyMenuWeaponEntry entry in buyMenuWeapons)
        {
            if (entry == null || entry.weaponPrefab == null)
            {
                continue;
            }

            GameObject buttonObject = new GameObject((string.IsNullOrEmpty(entry.label) ? entry.weaponPrefab.weaponName : entry.label) + "Button");
            buttonObject.transform.SetParent(buyMenuGrid.transform, false);

            RectTransform buttonRect = buttonObject.AddComponent<RectTransform>();
            buttonRect.sizeDelta = buyMenuGrid.cellSize;

            Image buttonImage = buttonObject.AddComponent<Image>();
            buttonImage.color = new Color(0.12f, 0.16f, 0.21f, 0.95f);

            Button button = buttonObject.AddComponent<Button>();

            GameObject iconObject = new GameObject("Icon");
            iconObject.transform.SetParent(buttonObject.transform, false);
            RectTransform iconRect = iconObject.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = new Vector2(14f, 0f);
            iconRect.sizeDelta = new Vector2(120f, 72f);
            iconRect.localPosition = new Vector3(iconRect.localPosition.x, iconRect.localPosition.y, -5f);

            Image iconImage = iconObject.AddComponent<Image>();
            iconImage.sprite = entry.icon;
            iconImage.preserveAspect = true;

            GameObject nameObject = new GameObject("Name");
            nameObject.transform.SetParent(buttonObject.transform, false);
            Text nameText = nameObject.AddComponent<Text>();
            nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nameText.fontSize = 24;
            nameText.alignment = TextAnchor.UpperLeft;
            nameText.color = Color.white;
            nameText.text = string.IsNullOrEmpty(entry.label) ? entry.weaponPrefab.weaponName : entry.label;

            RectTransform nameRect = nameText.rectTransform;
            nameRect.anchorMin = new Vector2(0f, 1f);
            nameRect.anchorMax = new Vector2(1f, 1f);
            nameRect.pivot = new Vector2(0f, 1f);
            nameRect.offsetMin = new Vector2(145f, -40f);
            nameRect.offsetMax = new Vector2(-12f, -8f);

            GameObject priceObject = new GameObject("Price");
            priceObject.transform.SetParent(buttonObject.transform, false);
            Text priceText = priceObject.AddComponent<Text>();
            priceText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            priceText.fontSize = 20;
            priceText.alignment = TextAnchor.LowerLeft;
            priceText.color = new Color(0.48f, 0.9f, 0.56f, 1f);
            priceText.text = "$" + entry.price;

            RectTransform priceRect = priceText.rectTransform;
            priceRect.anchorMin = new Vector2(0f, 0f);
            priceRect.anchorMax = new Vector2(1f, 0f);
            priceRect.pivot = new Vector2(0f, 0f);
            priceRect.offsetMin = new Vector2(145f, 8f);
            priceRect.offsetMax = new Vector2(-12f, 32f);

            BuyMenuWeaponEntry capturedEntry = entry;
            button.onClick.AddListener(() => BuyWeapon(capturedEntry));
        }
    }

    private void BuyWeapon(BuyMenuWeaponEntry entry)
    {
        if (entry == null || entry.weaponPrefab == null)
        {
            return;
        }

        Vector3 spawnPosition = transform.position + new Vector3(0.2f, 0f, 0f);
        WeaponControl newWeapon = Instantiate(entry.weaponPrefab, spawnPosition, Quaternion.identity);
        newWeapon.RefillAmmo();

        if (equippedWeapon != null)
        {
            Vector3 dropPosition = transform.position + new Vector3(transform.localScale.x >= 0f ? 0.8f : -0.8f, -0.1f, 0f);
            equippedWeapon.DropToWorld(dropPosition);
        }

        nearbyWeapon = newWeapon;
        EquipNearbyWeapon();
        ToggleBuyMenu();
    }

    private void EnsureEventSystemExists()
    {
        if (FindObjectOfType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
    }

    private void RefreshAmmoUI()
    {
        if (ammoText == null)
        {
            return;
        }

        if (equippedWeapon == null)
        {
            ammoText.text = "Silah yok";
            return;
        }

        string reloadSuffix = equippedWeapon.IsReloading() ? " | Reloading" : string.Empty;
        ammoText.text = equippedWeapon.GetCurrentAmmo() + " / " + equippedWeapon.GetMagazineSize() + reloadSuffix;
    }
}