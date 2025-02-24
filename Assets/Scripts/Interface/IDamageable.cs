using UnityEngine;

public interface IDamageable
{
    public void TakeDamage(int damage);

    //public void Die(); //Не факт что нужно в интерфейсе оставить, хотя по вайбу че нет
}
