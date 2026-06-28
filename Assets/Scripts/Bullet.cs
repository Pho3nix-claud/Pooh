using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Bullet : MonoBehaviour
{
    public float speed = 20f;
    public float lifeTime = 3f;

    private Rigidbody bulletRigidbody;
    private float lifeTimer;

    private void Awake()
    {
        bulletRigidbody = GetComponent<Rigidbody>();
        bulletRigidbody.useGravity = false;
    }

    private void OnEnable()
    {
        if (bulletRigidbody == null)
        {
            bulletRigidbody = GetComponent<Rigidbody>();
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
        if (bulletRigidbody == null)
        {
            return;
        }

        bulletRigidbody.linearVelocity = transform.right * speed;
    }
}
