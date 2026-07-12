using UnityEngine;

public class ZombieSpeed : Zombie
{
    protected override void Init()
    {
        state = StateCharacter.Idle;
        hp = 50;
        attack = 8;
        speed = 8;
        defend = 0;
        moanTimer = Random.Range(moanIntervalMin, moanIntervalMax);
    }
}
