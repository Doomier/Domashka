using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

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
    [SerializeField] private Transform spawnPoint;

    [Header("Настройки броска")]
    [SerializeField] private float minForce = 5f;
    [SerializeField] private float maxForce = 8f;
    [SerializeField] private float minTorque = 15f;
    [SerializeField] private float maxTorque = 30f;

    [Header("UI Ссылки (TextMeshPro)")]
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private TextMeshProUGUI targetScoreText;
    [SerializeField] private TextMeshProUGUI diceCountText;
    [SerializeField] private TMP_InputField inputDiceCount;

    [Header("События (UnityEvent)")]
    [SerializeField] private UnityEvent onRollStarted;
    [SerializeField] private UnityEvent<int> onRollFinished;

    private int defaultDiceCount = 3;
    private int currentDiceCount;
    private int winScoreThreshold = 12;
    private int drawScoreThreshold = 9;

    private List<GameObject> spawnedDice = new List<GameObject>();
    private List<int> currentResults = new List<int>();
    private int stoppedDiceCount = 0;
    private bool isRolling = false;

    private void Awake()
    {
        currentDiceCount = defaultDiceCount;
        GenerateRoundConditions();
        UpdateUI();
    }

    private void GenerateRoundConditions()
    {
        winScoreThreshold = Random.Range(10, 16);
        drawScoreThreshold = Random.Range(7, 10);
    }

    public void OnRollButtonPressed()
    {
        if (isRolling) return;

        if (inputDiceCount != null && int.TryParse(inputDiceCount.text, out int parsedCount) && parsedCount > 0)
        {
            currentDiceCount = parsedCount;
        }

        onRollStarted?.Invoke();
        StartCoroutine(RollAllDiceRoutine());
    }

    private IEnumerator RollAllDiceRoutine()
    {
        isRolling = true;
        currentResults.Clear();
        stoppedDiceCount = 0;

        GenerateRoundConditions();
        AdjustDicePool();

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

    private void AdjustDicePool()
    {
        while (spawnedDice.Count > currentDiceCount)
        {
            GameObject objToDestroy = spawnedDice[spawnedDice.Count - 1];
            spawnedDice.RemoveAt(spawnedDice.Count - 1);
            if (objToDestroy != null) Destroy(objToDestroy);
        }

        for (int i = 0; i < currentDiceCount; i++)
        {
            Vector3 spawnPos = spawnPoint != null
                ? spawnPoint.position + new Vector3(Random.Range(-0.2f, 0.2f), i * 0.2f, Random.Range(-0.2f, 0.2f))
                : new Vector3(0, 2f, 0);

            if (i < spawnedDice.Count)
            {
                if (spawnedDice[i] != null)
                {
                    spawnedDice[i].transform.position = spawnPos;
                    spawnedDice[i].transform.rotation = Random.rotation;
                }
                else
                {
                    spawnedDice[i] = Instantiate(dicePrefab, spawnPos, Random.rotation);
                }
            }
            else
            {
                GameObject dice = Instantiate(dicePrefab, spawnPos, Random.rotation);
                if (dice.GetComponent<DiceController>() == null)
                {
                    dice.AddComponent<DiceController>();
                }
                spawnedDice.Add(dice);
            }
        }
    }

    private void OnAllDiceStopped()
    {
        int totalSum = 0;
        foreach (int val in currentResults)
        {
            totalSum += val;
        }

        string status = "";
        if (totalSum >= winScoreThreshold)
            status = "ПОБЕДА!";
        else if (totalSum >= drawScoreThreshold)
            status = "НИЧЬЯ";
        else
            status = "ПОРАЖЕНИЕ";

        if (resultText != null)
        {
            resultText.text = $"Сумма: {totalSum} ({status})\nКости: [{string.Join(", ", currentResults)}]";
        }

        UpdateUI();
        onRollFinished?.Invoke(totalSum);
        isRolling = false;
    }

    private void UpdateUI()
    {
        if (diceCountText != null)
        {
            diceCountText.text = $"Число кубиков: {currentDiceCount}";
        }

        if (targetScoreText != null)
        {
            targetScoreText.text = $"Цели раунда:\n• Победа: от {winScoreThreshold}\n• Ничья: от {drawScoreThreshold}";
        }
    }
}