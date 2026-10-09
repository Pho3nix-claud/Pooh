using System.Collections;
using UnityEngine;

public class TargetBehaviour : MonoBehaviour
{
    [Header("Can Ayarları")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("Ölüm Ayarları")]
    public float deathRotationAngle = 90f;
    public float fallDuration = 0.2f;

    private bool isDead;

    public bool IsDead => isDead;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        if (isDead)
        {
            return;
        }

        currentHealth = Mathf.Max(0f, currentHealth - damage);

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;
        StartCoroutine(FallOver());
    }

    private IEnumerator FallOver()
    {
        Quaternion startRotation = transform.rotation;
        Quaternion endRotation = startRotation * Quaternion.Euler(0f, 0f, deathRotationAngle);
        float elapsed = 0f;

        while (elapsed < fallDuration)
        {
            elapsed += Time.deltaTime;
            transform.rotation = Quaternion.Slerp(startRotation, endRotation, elapsed / fallDuration);
            yield return null;
        }

        transform.rotation = endRotation;
    }
}
