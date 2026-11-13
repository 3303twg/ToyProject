using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class PlatformerController : MonoBehaviour
{
    public float moveSpeed = 7f;
    public float jumpSpeed = 12f;
    public float fallRespawnY = -8f;

    Rigidbody2D body;
    BoxCollider2D box;
    SpriteRenderer spriteRenderer;
    Vector3 spawnPoint;

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        box = GetComponent<BoxCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        body.gravityScale = 3f;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        spawnPoint = transform.position;
    }

    void Update()
    {
        if (transform.position.y < fallRespawnY)
        {
            transform.position = spawnPoint;
            body.linearVelocity = Vector2.zero;
            return;
        }

        if (IsGrounded() && JumpPressed())
        {
            body.linearVelocity = new Vector2(body.linearVelocity.x, jumpSpeed);
        }

        float x = Input.GetAxisRaw("Horizontal");
        if (spriteRenderer != null && Mathf.Abs(x) > 0.01f)
            spriteRenderer.flipX = x < 0f;
    }

    void FixedUpdate()
    {
        float x = Input.GetAxisRaw("Horizontal");
        body.linearVelocity = new Vector2(x * moveSpeed, body.linearVelocity.y);
    }

    bool JumpPressed()
    {
        return Input.GetButtonDown("Jump")
            || Input.GetKeyDown(KeyCode.Space)
            || Input.GetKeyDown(KeyCode.W)
            || Input.GetKeyDown(KeyCode.UpArrow);
    }

    bool IsGrounded()
    {
        Bounds bounds = box.bounds;
        Vector2 origin = new Vector2(bounds.center.x, bounds.min.y - 0.02f);
        Vector2 size = new Vector2(bounds.size.x * 0.85f, 0.08f);
        Collider2D[] hits = Physics2D.OverlapBoxAll(origin, size, 0f);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] != null && hits[i].gameObject != gameObject)
                return true;
        }

        return false;
    }
}
