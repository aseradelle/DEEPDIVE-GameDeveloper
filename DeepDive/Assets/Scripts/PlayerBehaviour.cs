using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerBehaviour : MonoBehaviour
{
    [Header("Movimento")]
    [SerializeField] private float moveSpeed = 5f;   // fuori dall'acqua (X)
    [SerializeField] private float swimSpeed = 2f;   // in acqua (X e Y)
    [SerializeField] private float accelInWater = 10f; // quanto rapidamente il player raggiunge la velocità di nuoto

    [Header("Fisica acqua")]
    [SerializeField] private float waterDrag = 3f;   // attrito in acqua
    [SerializeField] private float normalDrag = 0f;  // attrito fuori dall’acqua

    [Tooltip("Forza target di galleggiamento (coefficiente di molla). Più alto = risale più forte verso la superficie.")]
    [SerializeField] private float buoyancyK = 8f;

    [Tooltip("Smorzamento verticale in acqua. Più alto = meno rimbalzi.")]
    [SerializeField] private float buoyancyDamping = 3f;

    [Tooltip("Forza massima applicabile come spinta di galleggiamento (per evitare picchi).")]
    [SerializeField] private float maxBuoyancyForce = 12f;

    [Header("Affondamento iniziale")]
    [Tooltip("Durata in secondi della fase iniziale di affondamento.")]
    [SerializeField] private float sinkDuration = 0.6f;

    [Tooltip("Spinta verso il basso iniziale (valore positivo = spinta verso il basso).")]
    [SerializeField] private float initialSinkForce = 2.0f;

    [Tooltip("Tempo (secondi) per passare gradualmente da spinta verso il basso a pieno galleggiamento.")]
    [SerializeField] private float buoyancyRampTime = 0.8f;

    [Header("Effetti")]
    [SerializeField] private GameObject splashEffectPrefab;

    private Vector2 moveInput;
    private bool isInWater = false;
    private bool isSinking = false;
    public bool IsInWater => isInWater;
    private float sinkTimer = 0f;
    private float buoyancyT = 0f;        // 0 => spinta in giù, 1 => piena spinta in su
    private float waterSurfaceY = Mathf.NegativeInfinity;
    private Collider2D currentWaterTrigger;

    private Rigidbody2D rb;

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        if (isInWater)
        {
            // In acqua controlliamo noi la spinta verticale: disattiva gravità interna del Rigidbody2D
            rb.gravityScale = 0f;
            rb.linearDamping = waterDrag;

            // Movimento in acqua su X e Y (ammorbidito)
            Vector2 targetVel = moveInput * swimSpeed;
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, targetVel, accelInWater * Time.fixedDeltaTime);

            // --- Gestione affondamento -> galleggiamento ---
            if (isSinking)
            {
                sinkTimer += Time.fixedDeltaTime;

                // Spinta iniziale verso il basso
                float downward = initialSinkForce;

                // Porta gradualmente la "spinta netta" da giù a su
                buoyancyT = Mathf.Clamp01(sinkTimer / buoyancyRampTime);

                // Quando finisce la fase di affondamento nominale, consideralo in galleggiamento
                if (sinkTimer >= sinkDuration)
                    isSinking = false;

                // Applica la spinta verso il basso solo nella prima parte
                rb.AddForce(Vector2.down * downward, ForceMode2D.Force);
            }
            else
            {
                // Una volta terminato l'affondamento, assicurati che buoyancyT vada a 1 (piena spinta in su)
                buoyancyT = Mathf.MoveTowards(buoyancyT, 1f, Time.fixedDeltaTime / Mathf.Max(0.0001f, buoyancyRampTime));
            }

            // --- Galleggiamento tipo molla verso la superficie ---
            // Se non abbiamo una superficie valida (per sicurezza), salta
            if (currentWaterTrigger != null)
            {
                waterSurfaceY = currentWaterTrigger.bounds.max.y; // top AABB del trigger "Water"

                float depth = waterSurfaceY - rb.position.y; // > 0: sotto la superficie
                float k = buoyancyK * buoyancyT;             // cresce da 0 a K

                // Forza di molla verso la superficie solo se sotto (depth > 0)
                float spring = depth > 0f ? depth * k : 0f;

                // Smorzamento sulla velocità verticale
                float damping = -rb.linearVelocity.y * buoyancyDamping;

                // Somma e limita
                float F = Mathf.Clamp(spring + damping, -maxBuoyancyForce, maxBuoyancyForce);

                rb.AddForce(Vector2.up * F, ForceMode2D.Force);
            }
        }
        else
        {
            // Fuori dall’acqua: gravità "normale", controlli solo orizzontali
            rb.gravityScale = 1f; // regola a piacere (1–3)
            rb.linearDamping = normalDrag;
            rb.linearVelocity = new Vector2(moveInput.x * moveSpeed, rb.linearVelocity.y);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Water"))
        {
            isInWater = true;
            isSinking = true;
            sinkTimer = 0f;
            buoyancyT = 0f;
            currentWaterTrigger = other;

            // Effetto splash
            if (splashEffectPrefab != null)
                Instantiate(splashEffectPrefab, transform.position, Quaternion.identity);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other == currentWaterTrigger)
        {
            isInWater = false;
            isSinking = false;
            currentWaterTrigger = null;

            // (Opzionale) se vuoi evitare che continui a salire appena uscito
            // if (rb.velocity.y > 0f)
            //     rb.velocity = new Vector2(rb.velocity.x, 0f);
        }
    }
}
