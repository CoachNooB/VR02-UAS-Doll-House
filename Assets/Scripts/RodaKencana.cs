using UnityEngine;

/// <summary>
/// Putar roda dekoratif Kereta Kencana proporsional kecepatan kereta —
/// diam saat parkir, makin cepat saat W ditahan. Dipasang di grup tiap roda
/// (child kereta); KeretaMover dicari di parent (pola auto-find).
/// </summary>
public class RodaKencana : MonoBehaviour
{
    [SerializeField] private float _radius = 0.28f; // radius roda visual (meter)

    private KeretaMover _kereta;

    private void Awake()
    {
        _kereta = GetComponentInParent<KeretaMover>();
    }

    private void Update()
    {
        if (_kereta == null || _radius <= 0f)
        {
            return;
        }

        // kecepatan linear / radius = kecepatan sudut (rad/dtk) -> derajat
        float derajat = _kereta.KecepatanSaat / _radius * Mathf.Rad2Deg * Time.deltaTime;
        transform.Rotate(Vector3.right, derajat, Space.Self);
    }
}
