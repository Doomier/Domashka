using UnityEngine;

public enum DistributionType
{
    Uniform,  
    Sequential 
}

public class OrbitSpawner : MonoBehaviour
{
    [SerializeField] private GameObject _cubePrefab;
    [SerializeField] private int _cubeCount = 5;
    [SerializeField] private float _radius = 5f;
    [SerializeField] private float _rotationSpeed = 50f;
    [SerializeField] private bool _clockwise = true;
    [SerializeField] private DistributionType _distribution = DistributionType.Uniform;
    [SerializeField] private float _stepAngle = 15f;
    private Transform[] _cubes;
    private float[] _angles;
    private void Awake()
    {
        if (_cubePrefab == null || _cubeCount <= 0) return;

        _cubes = new Transform[_cubeCount];
        _angles = new float[_cubeCount];

        for (int i = 0; i < _cubeCount; i++)
        {
            if (_distribution == DistributionType.Uniform)
            {
                _angles[i] = i * (360f / _cubeCount);
            }
            else
            {
                _angles[i] = i * _stepAngle;
            }

            GameObject cube = Instantiate(_cubePrefab, transform);
            _cubes[i] = cube.transform;

            UpdateCubePosition(i);
        }
    }

    private void Update()
    {
        if (_cubes == null) return;

        float direction = _clockwise ? -1f : 1f;
        float deltaAngle = direction * _rotationSpeed * Time.deltaTime;

        for (int i = 0; i < _cubes.Length; i++)
        {
            _angles[i] += deltaAngle;
            UpdateCubePosition(i);
        }
    }

    private void UpdateCubePosition(int index)
    {
        float rad = _angles[index] * Mathf.Deg2Rad;

        Vector3 localPos = new Vector3(Mathf.Cos(rad) * _radius, 0f, Mathf.Sin(rad) * _radius);

        _cubes[index].position = transform.position + localPos;
    }
}