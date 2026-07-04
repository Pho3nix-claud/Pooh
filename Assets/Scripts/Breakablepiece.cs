using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BreakablePiece : MonoBehaviour
{
    public float minForce = 2f;
    public float maxForce = 6f;
    public float minTorque = -200f;
    public float maxTorque = 200f;
    public float pieceLifetime = 0.6f;

    private Rigidbody2D rb;
    private bool hasBeenHit = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.simulated = false;
        rb.gravityScale = 0f;
    }

    public void Activate(Vector2 hitDirection)
    {
        if (hasBeenHit)
        {
            return;
        }

        hasBeenHit = true;
        rb.simulated = true;

        Vector2 scatter = hitDirection + new Vector2(
            Random.Range(-0.5f, 0.5f),
            Random.Range(0.2f, 1f)
        );

        float force = Random.Range(minForce, maxForce);
        rb.AddForce(scatter.normalized * force, ForceMode2D.Impulse);
        rb.AddTorque(Random.Range(minTorque, maxTorque), ForceMode2D.Impulse);

        Destroy(gameObject, pieceLifetime);
    }
}