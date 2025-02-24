using UnityEngine;

public class HealthComponent : MonoBehaviour, IDamageable, IInitializable
{
    [SerializeField] private int _maxHealth = 100;
    private int _currentHealth;

    private bool _isInitialized = false;

    private void Start()
    {
        Initialize();
    }

    public void Initialize()
    {
        _currentHealth = _maxHealth;
        _isInitialized = true;
    }

    public void TakeDamage(int damage)
    {
        _currentHealth -= damage;
        _currentHealth = Mathf.Clamp(_currentHealth, 0, _maxHealth);

        if (_currentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        //Смерть
        Debug.Log("Чубрик умер");
    }

}
