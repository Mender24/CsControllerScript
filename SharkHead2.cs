using UnityEngine;

public class SharkHead2 : MonoBehaviour
{
    [Header("Цель")]
    public Transform target;

    [Header("Скорость движения")]
    public float acceleration = 2f;
    public float deceleration = 1.5f;
    public float maxSpeed = 5f;
    private float currentSpeed = 0f;

    [Header("Параметры поворота")]
    public float turnSpeed = 90f;

    [Header("На каком расстоянии останавливаться")]
    public float stopDistance = 0.5f;

    [Header("Аквариум")]
    public Bounds aquariumBounds = new Bounds(Vector3.zero, new Vector3(10, 5, 10));

    [Header("Куб безопасности (точки не будут респауниться здесь)")]
    public Bounds safetyBounds = new Bounds(Vector3.zero, new Vector3(2, 2, 2));

    [Header("Параметры свободного плавания")]
    public float swimSpeed = 2f;
    public float wanderPointThreshold = 1f; // когда считать точку достигнутой

    [Header("Извилистое движение")]
    public float swayAngle = 10f;
    public float swayFrequency = 1.5f;

    private Vector3 wanderPoint;
    private float swayTimer;
    private GameObject player;

    void Start()
    {
        PickNewWanderPoint();
        swayTimer = Random.value * 10f;
        player = GameObject.FindGameObjectWithTag("Player");
    }

    void Update()
    {
        GameObject projectile = GameObject.FindGameObjectWithTag("Projectile");
        // Если цели нет, ищем снаряд через тупой файнд (да в каждом кадре, мне похуй)
        if (target == null)
        {
            // Предпочтение: сначала игрок, потом снаряд
            if (player != null &&
                aquariumBounds.Contains(player.transform.position) &&
                !safetyBounds.Contains(player.transform.position))
            {
                AssignTarget(player.transform);
            }
            else if (projectile != null &&
                     aquariumBounds.Contains(projectile.transform.position) &&
                     !safetyBounds.Contains(projectile.transform.position))
            {
                AssignTarget(projectile.transform);
            }
        }
        else
        {
            // Прерываем атаку, если цель вышла из аквариума или в зоне безопасности
            if (!aquariumBounds.Contains(target.position) || safetyBounds.Contains(target.position))
            {
                target = null;
                PickNewWanderPoint();
            }
        }

        if (target != null)
        {
            MoveToTarget();
        }
        else
        {
            Wander();
        }
    }

    void MoveToTarget()
    {
        Vector3 toTarget = target.position - transform.position;
        float distance = toTarget.magnitude;

        if (distance > stopDistance)
        {
            Vector3 direction = toTarget.normalized;
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }

        if (distance > stopDistance)
            currentSpeed = Mathf.MoveTowards(currentSpeed, maxSpeed, acceleration * Time.deltaTime);
        else
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, deceleration * Time.deltaTime);

        transform.position += transform.forward * currentSpeed * Time.deltaTime;
    }

    void Wander()
    {
        Vector3 toPoint = wanderPoint - transform.position;
        float distance = toPoint.magnitude;

        // Достигли точки — выбираем новую
        if (distance < wanderPointThreshold)
        {
            PickNewWanderPoint();
            toPoint = wanderPoint - transform.position;
        }

        Vector3 baseDirection = toPoint.normalized;

        // Колебание
        swayTimer += Time.deltaTime;
        float sway = Mathf.Sin(swayTimer * swayFrequency * 2f * Mathf.PI) * swayAngle;

        Quaternion swayRotation = Quaternion.AngleAxis(sway, Vector3.up);
        Vector3 swayedDirection = (Quaternion.LookRotation(baseDirection) * swayRotation) * Vector3.forward;

        // Плавный поворот к swayedDirection
        Quaternion targetRotation = Quaternion.LookRotation(swayedDirection);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);

        // Движение вперёд
        transform.position += transform.forward * swimSpeed * Time.deltaTime;
    }

    void PickNewWanderPoint()
    {
        Vector3 randomPoint;
        int safetyCheckMaxTries = 100;
        int tries = 0;

        do
        {
            randomPoint = new Vector3(
                Random.Range(aquariumBounds.min.x, aquariumBounds.max.x),
                Random.Range(aquariumBounds.min.y, aquariumBounds.max.y),
                Random.Range(aquariumBounds.min.z, aquariumBounds.max.z)
            );

            tries++;

            // Проверка: точка НЕ в safetyBounds и путь НЕ пересекает safetyBounds
            if (!safetyBounds.Contains(randomPoint) &&
                !PathIntersectsSafety(transform.position, randomPoint))
            {
                break;
            }

            if (tries > safetyCheckMaxTries)
            {
                Debug.LogWarning("Не удалось найти безопасную точку, выбираем любую");
                break;
            }

        } while (true);

        wanderPoint = randomPoint;
    }

    bool PathIntersectsSafety(Vector3 from, Vector3 to)
    {
        Ray ray = new Ray(from, (to - from).normalized);
        float distance = Vector3.Distance(from, to);
        return safetyBounds.IntersectRay(ray, out float hitDistance) && hitDistance <= distance;
    }

    public void AssignTarget(Transform newTarget)
    {
        target = newTarget;
    }

    void OnDrawGizmosSelected() //show Gizmos Aquarium
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(aquariumBounds.center, aquariumBounds.size);

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(safetyBounds.center, safetyBounds.size);

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(wanderPoint, 0.2f);
    }
}
