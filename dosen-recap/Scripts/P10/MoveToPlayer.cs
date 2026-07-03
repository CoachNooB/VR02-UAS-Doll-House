using UnityEngine;

public class MoveToPlayer : MonoBehaviour
{
    [SerializeField] private Transform _playerTransform;
    [SerializeField] private float _moveSpeed = 3f;

    public bool isStart = false;

    public void StartMove() 
    {
        isStart = true;
    }

    private void Update()
    {
        if (_playerTransform == null)
        {
            return;
        }

        if (!isStart) return;


        transform.position = Vector3.MoveTowards(
            transform.position,
            _playerTransform.position,
            _moveSpeed * Time.deltaTime);
    }
}
