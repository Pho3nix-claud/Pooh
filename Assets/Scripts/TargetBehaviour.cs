using UnityEngine;

public class TargetBehaviour : MonoBehaviour
{
    public int hitPoints = 1;
    public Vector2 hitMoveOffset = Vector2.down;

    public void Hit()
    {
        hitPoints--;
        transform.Translate(hitMoveOffset);

        if (hitPoints <= 0)
        {
            Destroy(gameObject);
        }
    }
}
