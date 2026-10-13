using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyController : MonoBehaviour, IDamageable
{
    private enum EnemyState { Idle, Chase, Attack, Hit, Death }

    [Header("Parâmetros do Zumbi")]
    [SerializeField] private float maxHp = 5f;
    [SerializeField] private float chaseDistance = 20f;
    [SerializeField] private float attackDistance = 2.0f;
    [SerializeField] private float attackDuration = 1.5f;
    [SerializeField] private float attackDamageDelay = 0.5f;
    [SerializeField] private float attackDamage = 1f;
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float hitStunDuration = 0.3f;
    [SerializeField] private float deathDeactivateDelay = 3f;

    [Header("Referências")]
    [SerializeField] private NavMeshAgent navAgent;
    [SerializeField] private Animator anim;
    [SerializeField] private Transform attackPoint;
    [SerializeField] private GameObject attackFX;
    [SerializeField] private GameObject hitFX;

    private EnemyState currentState = EnemyState.Idle;
    private Transform targetPlayer;
    private float currentHp;
    private bool isActionLocked = false;
    private bool isDead = false;

    private void Awake()
    {
        if (!navAgent) navAgent = GetComponent<NavMeshAgent>();
    }

    // Chamado ao instanciar ou reaproveitar do Object Pool
    private void OnEnable()
    {
        ResetEnemyState();
    }

    // Permite ao Spawner injetar o alvo sem usar FindGameObjectWithTag
    public void InitializeTarget(Transform playerTransform)
    {
        targetPlayer = playerTransform;
    }

    private void ResetEnemyState()
    {
        currentHp = maxHp;
        isDead = false;
        isActionLocked = false;
        currentState = EnemyState.Idle;

        if (navAgent)
        {
            navAgent.enabled = true;
            navAgent.isStopped = false;
        }

        Collider col = GetComponent<Collider>();
        if (col) col.enabled = true;
    }

    private void Update()
    {
        if (isDead || isActionLocked || targetPlayer == null) return;

        float distanceToTarget = Vector3.Distance(transform.position, targetPlayer.position);
        UpdateStateMachine(distanceToTarget);
    }

    #region Máquina de Estados (FSM)

    private void UpdateStateMachine(float distance)
    {
        switch (currentState)
        {
            case EnemyState.Idle:
                if (distance <= chaseDistance)
                {
                    TransitionToState(EnemyState.Chase);
                }
                break;

            case EnemyState.Chase:
                if (distance > chaseDistance)
                {
                    TransitionToState(EnemyState.Idle);
                }
                else if (distance <= attackDistance)
                {
                    StartCoroutine(PerformAttackSequence());
                }
                else
                {
                    navAgent.SetDestination(targetPlayer.position);
                }
                break;

            case EnemyState.Attack:
                // O estado de ataque é mantido e travado pela Corrotina PerformAttackSequence
                break;
        }
    }

    private void TransitionToState(EnemyState newState)
    {
        currentState = newState;

        switch (newState)
        {
            case EnemyState.Idle:
                navAgent.isStopped = true;
                if (anim) anim.SetBool("IsRunning", false);
                break;

            case EnemyState.Chase:
                navAgent.isStopped = false;
                if (anim) anim.SetBool("IsRunning", true);
                break;
        }
    }

    #endregion

    #region Ataque e Combate

    private IEnumerator PerformAttackSequence()
    {
        currentState = EnemyState.Attack;
        isActionLocked = true;
        navAgent.isStopped = true;

        if (anim)
        {
            anim.SetBool("IsRunning", false);
            anim.SetTrigger("Attack");
        }

        if (attackFX && attackPoint)
        {
            Instantiate(attackFX, attackPoint.position, attackPoint.rotation);
        }

        // Aguarda o tempo exato do impacto na animação
        yield return new WaitForSeconds(attackDamageDelay);

        ExecuteDamageCheck();

        // Aguarda a animação de ataque terminar completamente
        yield return new WaitForSeconds(attackDuration - attackDamageDelay);

        isActionLocked = false;
        TransitionToState(EnemyState.Idle);
    }

    private void ExecuteDamageCheck()
    {
        if (attackPoint == null) return;

        Collider[] hitColliders = Physics.OverlapSphere(attackPoint.position, attackRange);
        foreach (var col in hitColliders)
        {
            if (col.CompareTag("Player"))
            {
                // Uso da Interface desacoplada
                IDamageable damageable = col.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(attackDamage);
                }
                break;
            }
        }
    }

    #endregion

    #region Sistema de Dano e Morte (IDamageable)

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHp -= amount;
        if (hitFX) Instantiate(hitFX, transform.position, transform.rotation);

        if (currentHp <= 0f)
        {
            Die();
        }
        else
        {
            StartCoroutine(ApplyHitStunRoutine());
        }
    }

    private IEnumerator ApplyHitStunRoutine()
    {
        isActionLocked = true;
        navAgent.isStopped = true;

        if (anim) anim.SetTrigger("Hit");

        yield return new WaitForSeconds(hitStunDuration);

        isActionLocked = false;
        TransitionToState(EnemyState.Chase);
    }

    private void Die()
    {
        isDead = true;
        currentState = EnemyState.Death;

        navAgent.isStopped = true;
        navAgent.enabled = false;

        Collider col = GetComponent<Collider>();
        if (col) col.enabled = false;

        if (anim) anim.SetTrigger("Death");

        StartCoroutine(DeactivateForPoolingRoutine());
    }

    private IEnumerator DeactivateForPoolingRoutine()
    {
        yield return new WaitForSeconds(deathDeactivateDelay);
        // Em vez de Destroy(), desativa o objeto para reaproveitamento no Object Pool
        gameObject.SetActive(false);
    }

    #endregion
}