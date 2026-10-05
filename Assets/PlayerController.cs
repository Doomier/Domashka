using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Настройки броска")]
    [SerializeField] private float minForce = 5f;
    [SerializeField] private float maxForce = 8f;
    [SerializeField] private float minTorque = 15f;
    [SerializeField] private float maxTorque = 30f;

    private InputSystem_Actions inputActions;
    private InputAction rollAction;
    private Rigidbody rb;
    private bool isRolling = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        inputActions = new InputSystem_Actions();
        rollAction = inputActions.Player.Roll;
    }

    private void OnEnable()
    {
        inputActions.Enable();
        if (rollAction != null)
        {
            rollAction.performed += OnRollPressed;
        }
    }

    private void OnDisable()
    {
        if (rollAction != null)
        {
            rollAction.performed -= OnRollPressed;
        }
        inputActions.Disable();
    }

    private void OnRollPressed(InputAction.CallbackContext context)
    {
        if (!isRolling)
        {
            StartCoroutine(RollAndDetectRoutine());
        }
    }

    private IEnumerator RollAndDetectRoutine()
    {
        isRolling = true;

        rb.isKinematic = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        Vector3 forceDirection = new Vector3(
            Random.Range(-0.3f, 0.3f),
            1f,
            Random.Range(-0.3f, 0.3f)
        ).normalized;

        float force = Random.Range(minForce, maxForce);
        rb.AddForce(forceDirection * force, ForceMode.Impulse);

        Vector3 randomTorque = new Vector3(
            Random.Range(-maxTorque, maxTorque),
            Random.Range(-maxTorque, maxTorque),
            Random.Range(-maxTorque, maxTorque)
        );
        rb.AddTorque(randomTorque, ForceMode.Impulse);

        yield return new WaitForSeconds(0.2f);

        while (rb.linearVelocity.sqrMagnitude > 0.01f || rb.angularVelocity.sqrMagnitude > 0.01f)
        {
            yield return null;
        }

        int diceResult = GetTopFaceNumber();
        Debug.Log($"Выпало число: {diceResult}");

        isRolling = false;
    }

    private int GetTopFaceNumber()
    {

        Vector3[] directions = new Vector3[]
        {
            transform.up,     
            -transform.up,     
            transform.right,    
            -transform.right,   
            transform.forward,  
            -transform.forward  
        };

        int[] values = new int[] { 6, 1, 2, 5, 3, 4 };

        float maxDot = -1f;
        int bestValue = 1;

        for (int i = 0; i < directions.Length; i++)
        {
            float dot = Vector3.Dot(directions[i], Vector3.up);
            if (dot > maxDot)
            {
                maxDot = dot;
                bestValue = values[i];
            }
        }

        return bestValue;
    }
}