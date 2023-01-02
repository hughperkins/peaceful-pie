using UnityEngine;

public class SimpleNPC : MonoBehaviour, INeedFixedUpdate
{

    public Transform target;
    public DungeonEscapeEnvController gameManager;

    private Rigidbody rb;

    public float walkSpeed = 1;
    private Vector3 dirToGo;
    Simulation simulation;

    void Awake()
    {
        simulation = gameManager.GetComponent<Simulation>();
        rb = GetComponent<Rigidbody>();
        simulation.RegisterNeedFixedUpdate(this);
    }

    public bool IsAlive() {
        return gameObject.activeSelf;
    }

    public void MyFixedUpdate(float deltaTime) {
        dirToGo = target.position - transform.position;
        dirToGo.y = 0;
        rb.rotation = Quaternion.LookRotation(dirToGo);
        // rb.AddForce(dirToGo.normalized * walkSpeed * Time.fixedDeltaTime, walkForceMode);
        // rb.MovePosition(rb.transform.TransformDirection(Vector3.forward * walkSpeed * Time.deltaTime));
        // rb.MovePosition(rb.transform.TransformVector() (Vector3.forward * walkSpeed * Time.deltaTime));
        rb.MovePosition(transform.position + transform.forward * walkSpeed * deltaTime);
    }

    public void SetRandomWalkSpeed()
    {
        walkSpeed = Random.Range(1f, 7f);
    }
}
