using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour, IDamageable
{
    [Header("Configurações de Movimentação")]
    [SerializeField] private float speed = 8f;
    [SerializeField] private float jumpForce = 6f;
    [SerializeField] private LayerMask floorLayers;
    [SerializeField] private float groundCheckDistance = 1.2f;

    [Header("Câmera FPS")]
    [SerializeField] private Transform cameraHolder; // Objeto pai da câmera (na altura dos olhos)
    [SerializeField] private Vector2 mouseSensitivity = new Vector2(0.1f, 0.1f);
    [SerializeField] private float maxPitchAngle = 80f;
    [SerializeField] private float minPitchAngle = -80f;

    [Header("Atributos do Jogador")]
    [SerializeField] private float maxHp = 5f;
    [SerializeField] private Image hpImage;
    [SerializeField] private GameObject hitFX;
    [SerializeField] private float hitDuration = 0.3f;
    [SerializeField] private float deathDuration = 3f;

    [Header("Componentes")]
    [SerializeField] private Animator anim;

    private Rigidbody rig;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private float cameraPitch = 0f;
    private float currentHp;
    private bool isGrounded = true;
    private bool isLocked = false;
    private bool isDead = false;

    private void Awake()
    {
        rig = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        currentHp = maxHp;
        UpdateUI();

        // Esconde e trava o cursor no centro da tela para FPS
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        if (isDead) return;

        CheckGrounded();
        HandleCameraRotation();
    }

    private void FixedUpdate()
    {
        if (isLocked || isDead) return;

        HandleMovement();
    }

    #region Movimentação e Câmera FPS

    private void HandleMovement()
    {
        // Converte o input local (frente/lado) para direção no espaço do mundo relativo ao olhar do player
        Vector3 moveDirection = transform.right * moveInput.x + transform.forward * moveInput.y;
        Vector3 targetVelocity = moveDirection * speed;

        // Mantém a velocidade Y original do Rigidbody (gravidade e pulo)
        rig.linearVelocity = new Vector3(targetVelocity.x, rig.linearVelocity.y, targetVelocity.z);

        if (anim != null)
        {
            anim.SetBool("isRunning", moveInput.sqrMagnitude > 0.01f);
        }
    }

    private void HandleCameraRotation()
    {
        if (isLocked) return;

        // Rotação Horizontal (Yaw) - Giro do corpo do jogador no eixo Y
        float bodyYaw = lookInput.x * mouseSensitivity.x;
        transform.Rotate(Vector3.up * bodyYaw);

        // Rotação Vertical (Pitch) - Giro da câmera no eixo X com trava (Clamp)
        cameraPitch -= lookInput.y * mouseSensitivity.y;
        cameraPitch = Mathf.Clamp(cameraPitch, minPitchAngle, maxPitchAngle);

        if (cameraHolder != null)
        {
            cameraHolder.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
        }
    }

    private void CheckGrounded()
    {
        isGrounded = Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, floorLayers);
        if (anim != null)
        {
            anim.SetBool("isGrounded", isGrounded);
        }
    }

    #endregion

    #region Callbacks do Input System

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed && isGrounded && !isLocked && !isDead)
        {
            rig.linearVelocity = new Vector3(rig.linearVelocity.x, jumpForce, rig.linearVelocity.z);
        }
    }

    #endregion

    #region Sistema de Saúde e Dano (IDamageable)

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHp -= amount;
        currentHp = Mathf.Max(currentHp, 0f);
        UpdateUI();

        if (hitFX) Instantiate(hitFX, transform.position, transform.rotation);

        if (currentHp <= 0f)
        {
            Die();
        }
        else
        {
            StartCoroutine(ApplyHitStun());
        }
    }

    private IEnumerator ApplyHitStun()
    {
        isLocked = true;
        rig.linearVelocity = Vector3.zero;

        if (anim != null) anim.SetTrigger("Hit");

        yield return new WaitForSeconds(hitDuration);

        isLocked = false;
    }

    private void Die()
    {
        isDead = true;
        rig.isKinematic = true;

        if (anim != null) anim.SetTrigger("Death");

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        StartCoroutine(ReloadSceneRoutine());
    }

    private IEnumerator ReloadSceneRoutine()
    {
        yield return new WaitForSeconds(deathDuration);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void UpdateUI()
    {
        if (hpImage != null)
        {
            hpImage.fillAmount = currentHp / maxHp;
        }
    }

    #endregion
}