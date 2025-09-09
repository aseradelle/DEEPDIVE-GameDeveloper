using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class WaterTrailController : MonoBehaviour
{
    [Header("Riferimenti")]
    [SerializeField] private PlayerBehaviour player;       // drag dal tuo Player
    [SerializeField] private ParticleSystem trailPS;       // drag del prefab istanziato o riferimento nella scena

    [Header("Controllo emissivo")]
    [SerializeField] private float minSpeedToEmit = 0.1f;  // sotto questa velocit� niente scia
    [SerializeField] private float maxSpeedForScaling = 6f;// velocit� a cui raggiunge il rate massimo
    [SerializeField] private float maxRateOverTime = 40f;  // emissione massima
    [SerializeField] private float offsetBehind = 0.2f;    // quanto �dietro� posizionare l�emettitore

    private Rigidbody2D rb;
    private ParticleSystem.EmissionModule emission;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (player == null) player = GetComponent<PlayerBehaviour>();
        if (trailPS != null)
        {
            emission = trailPS.emission;
            emission.enabled = false; // start off
        }
    }

    void LateUpdate()
    {
        if (trailPS == null || rb == null || player == null) return;

        Vector2 v = rb.linearVelocity;
        float speed = v.magnitude;

        // Posiziona l�emettitore leggermente �dietro� rispetto alla direzione di movimento
        if (v.sqrMagnitude > 0.0001f)
        {
            Vector3 dir = ((Vector3)v).normalized;
            trailPS.transform.position = transform.position - dir * offsetBehind;

            // Orienta la particella (stretched billboard rende l�orientamento meno critico)
            // Qui allineiamo l�UP all direzione di moto (in 2D Z resta costante)
            trailPS.transform.rotation = Quaternion.LookRotation(Vector3.forward, dir);
        }
        else
        {
            trailPS.transform.position = transform.position;
        }

        // Attiva solo in acqua e sopra una certa velocit�
        bool active = player.IsInWater && speed > minSpeedToEmit;
        emission.enabled = active;

        if (active)
        {
            float t = Mathf.InverseLerp(minSpeedToEmit, maxSpeedForScaling, speed);
            emission.rateOverTime = Mathf.Lerp(0f, maxRateOverTime, t);
            // In alternativa: usare rateOverDistance costante (imposta da Inspector) e lasciare rateOverTime a 0
        }
    }
}
