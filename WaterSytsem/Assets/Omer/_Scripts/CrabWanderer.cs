using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CrabWanderer : MonoBehaviour
{
    [Header("Hareket Ayarları")]
    public float moveSpeed = 0.2f;
    public float turnSpeed = 0.5f;

    // Yengeçlerin robot gibi aynı saniyede dönmemesi için sabit bir süre yerine aralık veriyoruz
    public float minDirectionTime = 3f;
    public float maxDirectionTime = 7f;
    private float currentDirectionChangeTime;

    [Header("Zemin Algılama ve Eğime Uyma")]
    public LayerMask groundLayer;
    public float rayLength = 1.0f;
    public float alignSpeed = 1.5f;

    [Header("Yüzme / Süzülme Ayarları")]
    public float jumpPushDuration = 1.2f;
    public float jumpForce = 15f;
    [Range(0f, 1f)]
    public float waterBuoyancy = 0.85f;
    public float minJumpWait = 5.0f;
    public float maxJumpWait = 12.0f;

    private Rigidbody rb;
    private Animator anim;

    private float directionTimer;
    private float jumpTimer;
    private float nextJumpTime;
    private Vector3 targetDirection;
    private bool isGrounded;
    private bool isJumping;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();

        rb.useGravity = true;
        rb.freezeRotation = true;

        // --- SÜRÜ PSİKOLOJİSİNİ BOZAN KISIM ---
        SetNewDirectionTimer();
        // Her yengecin sayacını farklı bir saniyeden başlatıyoruz ki aynı anda dönmesinler
        directionTimer = Random.Range(0f, currentDirectionChangeTime);

        ChooseNewDirection();
        CalculateNextJump();
    }

    void Update()
    {
        // 1. Rastgele Gezinme Zamanlayıcısı
        directionTimer += Time.deltaTime;
        if (directionTimer >= currentDirectionChangeTime)
        {
            ChooseNewDirection();
            SetNewDirectionTimer(); // Bir sonraki dönüş süresini yeniden rastgele belirle
            directionTimer = 0f;
        }

        // 2. Rastgele Zıplama Zamanlayıcısı
        jumpTimer += Time.deltaTime;
        if (jumpTimer >= nextJumpTime && isGrounded && !isJumping)
        {
            StartCoroutine(SmoothJumpRoutine());
            jumpTimer = 0f;
            CalculateNextJump();
        }

        if (anim != null)
        {
            anim.SetBool("isMoving", isGrounded && rb.linearVelocity.magnitude > 0.1f);
            anim.SetBool("isGrounded", isGrounded);
        }
    }

    void FixedUpdate()
    {
        AlignWithGround();

        if (isGrounded && !isJumping)
        {
            MoveForward();
            RotateTowardsTarget();
        }

        // Suyun kaldırma kuvveti (Süzülme)
        if (!isGrounded)
        {
            float counterGravityForce = Mathf.Abs(Physics.gravity.y) * waterBuoyancy;
            rb.AddForce(Vector3.up * counterGravityForce, ForceMode.Acceleration);
        }
    }

    void MoveForward()
    {
        Vector3 moveForce = transform.forward * moveSpeed;
        rb.MovePosition(rb.position + moveForce * Time.fixedDeltaTime);
    }

    void RotateTowardsTarget()
    {
        if (targetDirection != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(targetDirection, transform.up);
            rb.MoveRotation(Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.fixedDeltaTime));
        }
    }

    void AlignWithGround()
    {
        Vector3 rayStart = transform.position + (Vector3.up * 0.5f);

        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, rayLength, groundLayer))
        {
            isGrounded = true;
            Quaternion targetSurfaceRotation = Quaternion.FromToRotation(transform.up, hit.normal) * transform.rotation;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetSurfaceRotation, alignSpeed * Time.fixedDeltaTime);
        }
        else
        {
            isGrounded = false;
        }
    }

    // --- YÖN SEÇİMİ DÜZELTİLDİ ---
    void ChooseNewDirection()
    {
        // Unity'nin hazır rastgele daire formülünü kullanarak tam 360 derecelik kusursuz rastgele bir yön alıyoruz
        Vector2 randomCircle = Random.insideUnitCircle.normalized;

        // Bu yönü yengecin X ve Z (sağ/sol, ileri/geri) eksenine uyguluyoruz
        targetDirection = new Vector3(randomCircle.x, 0, randomCircle.y);
    }

    // Her seferinde kaç saniye düz yürüyeceğini rastgele belirler
    void SetNewDirectionTimer()
    {
        currentDirectionChangeTime = Random.Range(minDirectionTime, maxDirectionTime);
    }

    void CalculateNextJump()
    {
        nextJumpTime = Random.Range(minJumpWait, maxJumpWait);
    }

    IEnumerator SmoothJumpRoutine()
    {
        isJumping = true;
        float timer = 0f;

        if (anim != null) anim.SetTrigger("jump");

        while (timer < jumpPushDuration)
        {
            Vector3 jumpDirection = (transform.up * 1.5f + transform.forward * 1.0f).normalized;
            rb.AddForce(jumpDirection * jumpForce, ForceMode.Force);

            timer += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        isJumping = false;
    }
}