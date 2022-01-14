using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class PlayerMoveStripped : MonoBehaviour
{
    [Header("Components")]
    private Rigidbody2D rb;
    private Animator anim;
    private CapsuleCollider2D capsule;

    [Header("Layer Masks")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask stairLayer;
    [SerializeField] private LayerMask cornerCorrectLayer;

    [Header("Movement Variables")]
    [SerializeField] private float movementAcceleration = 70f;
    [SerializeField] private float maxMoveSpeed = 12f;
    [SerializeField] private float walkSpeed;
    [SerializeField] private float holdMaxMoveSpeed;
    [SerializeField] private float groundLinearDrag = 7f;
    private float horizontalDirection;

    private bool changingDirection => (rb.velocity.x > 0f && horizontalDirection < 0f) || (rb.velocity.x < 0f && horizontalDirection > 0f);

    [Header("Ground Collision Variables")]
    [SerializeField] private float groundRaycastLength;
    [SerializeField] private float slopeRaycastLength;
    [SerializeField] private Vector3 groundRaycastOffset;
    [SerializeField] private Vector3 slopeRaycastOffset;
    [SerializeField] private bool onGround;
    [SerializeField] private bool isOnSlope;
    private bool canWalkOnSlope;


    private float slopeDownAngle;
    private float slopeDownAngleOld;
    private float slopeSideAngle;
    [SerializeField] private float maxSlopeAngle;
    [SerializeField] private float slopeCheckDistance;


    private Vector2 slopeNormalPerp;
    private Vector2 colliderSize;

    [SerializeField] private PhysicsMaterial2D noFriction;
    [SerializeField] private PhysicsMaterial2D fullFriction;

    private Vector2 newVelocity;

    public TMP_Text notifierText;

    public GameObject dialogueUI;

    private NodeParser parser;
    public ResponseHandler handler;
    public GameManager manager;

    // Start is called before the first frame update
    void Start()
    {
        handler = GetComponent<ResponseHandler>();
        parser = manager.GetComponentInChildren<NodeParser>();
        holdMaxMoveSpeed = maxMoveSpeed;
        walkSpeed = maxMoveSpeed / 1.7f;
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        capsule = GetComponent<CapsuleCollider2D>();

        colliderSize = capsule.size;
    }

    // Update is called once per frame
    void Update()
    {
        //if (dialogueUI.IsOpen && dialogueUI != null) { return; }
        if (dialogueUI.activeInHierarchy)
        {
            anim.SetBool("Moving", false);
            return; 
        }
        anim.SetBool("IsGrounded", onGround);
        anim.SetBool("OnSlope", isOnSlope);
        Animation();

            /*if (Input.GetKeyDown(KeyCode.E) && dialogueUI.IsOpen == false)
            {
                if(Interactable != null)
                {
                    Interactable.Interact(this);
                }
            }*/
            horizontalDirection = GetInput().x;
    }

    public Vector2 GetPosition()
    {
        return this.transform.position;
    }

    public TMP_Text GetNotifierText()
    {
        return notifierText;
    }

    private Vector2 GetInput()
    {
        return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
    }

    private void MoveCharacter()
    {
        if(onGround && !isOnSlope)
        {
            maxMoveSpeed = holdMaxMoveSpeed;
            rb.AddForce(new Vector2(horizontalDirection, 0f) * movementAcceleration);

            if (Mathf.Abs(rb.velocity.x) > maxMoveSpeed)
                rb.velocity = new Vector2(Mathf.Sign(rb.velocity.x) * maxMoveSpeed, rb.velocity.y);

        }
        else if(onGround && isOnSlope && canWalkOnSlope)
        {
            maxMoveSpeed = walkSpeed;
            newVelocity.Set(maxMoveSpeed * slopeNormalPerp.x * -horizontalDirection, maxMoveSpeed * slopeNormalPerp.y * -horizontalDirection);
            rb.velocity = newVelocity;
        }
        else if (!onGround)
        {
            newVelocity.Set(maxMoveSpeed * horizontalDirection, rb.velocity.y);
            rb.velocity = newVelocity;
        }

    }

    private void Animation()
    {
        float input = horizontalDirection;
        if(input != 0)
        {
            anim.SetBool("Moving", true);
            anim.SetFloat("HorizontalDirection", input);
        }
        else
        {
            anim.SetBool("Moving", false);
        }
    }

    private void ApplyGroundLinearDrag()
    {
        if (Mathf.Abs(horizontalDirection) < 0.4f || changingDirection)
        {
            rb.drag = groundLinearDrag;
        }
        else
        {
            rb.drag = 0f;
        }
    }

    private void FixedUpdate()
    {
        CheckCollisions();
        StairCheck();
        MoveCharacter();
        ApplyGroundLinearDrag();
    }

    private void StairCheck()
    {
        Vector2 checkPosition = transform.position - new Vector3(0.0f, colliderSize.y/2);

        SlopeCheckHorizontal(checkPosition);
        SlopeCheckerVertical(checkPosition);
    
    }

    private void SlopeCheckHorizontal(Vector2 checkPosition)
    {
        RaycastHit2D slopeHitFront = Physics2D.Raycast(checkPosition, transform.right, slopeCheckDistance, groundLayer);
        RaycastHit2D slopeHitBack = Physics2D.Raycast(checkPosition, -transform.right, slopeCheckDistance ,groundLayer);

        if (slopeHitFront)
        {
            isOnSlope = true;
            slopeSideAngle = Vector2.Angle(slopeHitFront.normal, Vector2.up);
        }
        else if (slopeHitBack)
        {
            isOnSlope = true;
            slopeSideAngle = Vector2.Angle(slopeHitBack.normal, Vector2.up);
        }
        else
        {
            slopeSideAngle = 0f;
            isOnSlope = false;
        }
    }

    private void SlopeCheckerVertical(Vector2 checkPosition)
    {
        RaycastHit2D hit = Physics2D.Raycast(checkPosition, Vector2.down, slopeCheckDistance, groundLayer);

        if (hit)
        {
            slopeNormalPerp = Vector2.Perpendicular(hit.normal).normalized;

            slopeDownAngle = Vector2.Angle(hit.normal, Vector2.up);

            if(slopeDownAngle != slopeDownAngleOld)
            {
                isOnSlope = true;
            }

            slopeDownAngleOld = slopeDownAngle;

            Debug.DrawRay(hit.point, slopeNormalPerp, Color.red);

            Debug.DrawRay(hit.point, hit.normal, Color.green);
        }
        if(slopeDownAngle > maxSlopeAngle || slopeSideAngle > maxSlopeAngle)
        {
            canWalkOnSlope = false;
        }
        else
        {
            canWalkOnSlope = true;
        }

        if(isOnSlope && horizontalDirection == 0f && canWalkOnSlope == true)
        {
            rb.sharedMaterial = fullFriction;
        }
        else
        {
            rb.sharedMaterial = noFriction;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawLine(transform.position + groundRaycastOffset, transform.position + groundRaycastOffset + Vector3.down * groundRaycastLength);
        Gizmos.DrawLine(transform.position + slopeRaycastOffset, transform.position + slopeRaycastOffset + Vector3.down * slopeRaycastLength);
    }

    private void CheckCollisions()
    {
        onGround = Physics2D.Raycast(transform.position + groundRaycastOffset, Vector2.down, groundRaycastLength, groundLayer);
    }
}
