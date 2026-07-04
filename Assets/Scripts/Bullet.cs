using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Bullet : MonoBehaviour
{
    [Header("Mermi Ayarları")]
    public float speed = 20f;
    public float lifeTime = 3f;
    public float damage = 25f;

    private Rigidbody2D rb;
    private float lifeTimer;
    private Vector2 travelDirection;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    private void OnEnable()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        lifeTimer = lifeTime;
        ApplyVelocity();
    }

    private void Update()
    {
        lifeTimer -= Time.deltaTime;
        if (lifeTimer <= 0f)
        {
            Destroy(gameObject);
        }
    }

    public void SetSpeed(float newSpeed)
    {
        speed = newSpeed;
        ApplyVelocity();
    }

    private void ApplyVelocity()
    {
        if (rb == null)
        {
            return;
        }

        travelDirection = transform.rotation * Vector2.right;
        rb.linearVelocity = travelDirection * speed;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleHit(collision.gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        HandleHit(other.gameObject);
    }

    private void HandleHit(GameObject hitObject)
    {
        BreakableBox box = hitObject.GetComponent<BreakableBox>()
                           ?? hitObject.GetComponentInParent<BreakableBox>();

        if (box != null)
        {
            box.TakeDamage(damage, travelDirection);
        }

        TargetBehaviour target = hitObject.GetComponent<TargetBehaviour>()
                                 ?? hitObject.GetComponentInParent<TargetBehaviour>();

        if (target != null)
        {
            target.Hit();
        }

        Destroy(gameObject);
    }
}