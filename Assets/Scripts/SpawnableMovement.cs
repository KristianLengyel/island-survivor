using UnityEngine;

public class SpawnableMovement : MonoBehaviour
{
    private bool isFollowingHook = false;
    private Transform hookTransform;

    [SerializeField] float x_speed;
    [SerializeField] float y_speed;

    private void FixedUpdate()
    {
        if (isFollowingHook)
        {
            transform.position = hookTransform.position;
        }
        else
        {
            transform.position = new Vector2(transform.position.x + x_speed, transform.position.y - y_speed);
        }
    }

    public void StartFollowingHook(Transform hook)
    {
        isFollowingHook = true;
        hookTransform = hook;
    }
}
