using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class DiceController : MonoBehaviour
{
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public IEnumerator RollRoutine(float minForce, float maxForce, float minTorque, float maxTorque, System.Action<int> onStopped)
    {
        rb.isKinematic = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        Vector3 forceDirection = new Vector3(
            Random.Range(-0.4f, 0.4f),
            1f,
            Random.Range(-0.4f, 0.4f)
        ).normalized;

        float force = Random.Range(minForce, maxForce);
        rb.AddForce(forceDirection * force, ForceMode.Impulse);

        Vector3 randomTorque = new Vector3(
            Random.Range(-maxTorque, maxTorque),
            Random.Range(-maxTorque, maxTorque),
            Random.Range(-maxTorque, maxTorque)
        );
        rb.AddTorque(randomTorque, ForceMode.Impulse);

        yield return new WaitForSeconds(0.3f);

        while (rb.linearVelocity.sqrMagnitude > 0.01f || rb.angularVelocity.sqrMagnitude > 0.01f)
        {
            yield return null;
        }

        int result = GetTopFaceNumber();
        onStopped?.Invoke(result);
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

public class PlayerController : MonoBehaviour
{
    [Header("Настройки спавна")]
    [SerializeField] private GameObject dicePrefab; 
    [SerializeField] private int diceCount = 3;    
    [SerializeField] private Transform spawnPoint; 

    [Header("Настройки броска")]
    [SerializeField] private float minForce = 5f;
    [SerializeField] private float maxForce = 8f;
    [SerializeField] private float minTorque = 15f;
    [SerializeField] private float maxTorque = 30f;

    private InputSystem_Actions inputActions;
    private InputAction rollAction;
    private bool isRolling = false;

    private List<GameObject> spawnedDice = new List<GameObject>();
    private List<int> currentResults = new List<int>();
    private int stoppedDiceCount = 0;

    private void Awake()
    {
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
            StartCoroutine(RollAllDiceRoutine());
        }
    }

    private IEnumerator RollAllDiceRoutine()
    {
        isRolling = true;
        currentResults.Clear();
        stoppedDiceCount = 0;

        if (spawnedDice.Count == 0)
        {
            for (int i = 0; i < diceCount; i++)
            {
                Vector3 spawnPos = spawnPoint != null
                    ? spawnPoint.position + new Vector3(Random.Range(-0.2f, 0.2f), i * 0.2f, Random.Range(-0.2f, 0.2f))
                    : new Vector3(0, 2f, 0);

                GameObject dice = Instantiate(dicePrefab, spawnPos, Random.rotation);

                if (dice.GetComponent<DiceController>() == null)
                {
                    dice.AddComponent<DiceController>();
                }

                spawnedDice.Add(dice);
            }
        }
        else
        {
            foreach (var dice in spawnedDice)
            {
                if (dice != null)
                {
                    dice.transform.position = spawnPoint != null
                        ? spawnPoint.position + new Vector3(Random.Range(-0.2f, 0.2f), 0.5f, Random.Range(-0.2f, 0.2f))
                        : new Vector3(0, 2f, 0);
                    dice.transform.rotation = Random.rotation;
                }
            }
        }

        foreach (var diceObj in spawnedDice)
        {
            DiceController controller = diceObj.GetComponent<DiceController>();
            if (controller != null)
            {
                StartCoroutine(controller.RollRoutine(minForce, maxForce, minTorque, maxTorque, (result) =>
                {
                    currentResults.Add(result);
                    stoppedDiceCount++;

                    if (stoppedDiceCount >= spawnedDice.Count)
                    {
                        OnAllDiceStopped();
                    }
                }));
            }
        }

        yield return null;
    }

    private void OnAllDiceStopped()
    {
        int totalSum = 0;
        foreach (int val in currentResults)
        {
            totalSum += val;
        }

        Debug.Log($"Все кубики остановились! Результаты: [{string.Join(", ", currentResults)}]. Сумма: {totalSum}");
        isRolling = false;
    }
}