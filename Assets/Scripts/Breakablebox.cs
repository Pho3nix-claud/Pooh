using UnityEngine;

public class BreakableBox : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth;
    public GameObject destroyEffectPrefab;

    private BreakablePiece[] pieces;
    private bool isBroken = false;

    private void Awake()
    {
        currentHealth = maxHealth;
        pieces = GetComponentsInChildren<BreakablePiece>();
    }

    public void TakeDamage(float damage, Vector2 hitDirection)
    {
        if (isBroken)
        {
            return;
        }

        currentHealth -= damage;

        if (currentHealth <= 0f)
        {
            Break(hitDirection);
        }
    }

    private void Break(Vector2 hitDirection)
    {
        isBroken = true;

        foreach (BreakablePiece piece in pieces)
        {
            if (piece == null)
            {
                continue;
            }

            piece.transform.SetParent(null);
            piece.Activate(hitDirection);
        }

        if (destroyEffectPrefab != null)
        {
            GameObject effect = Instantiate(destroyEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 0.5f);
        }

        Destroy(gameObject);
    }
}