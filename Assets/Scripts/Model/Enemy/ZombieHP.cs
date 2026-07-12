using UnityEngine;

public class ZombieHP : Zombie
{
    protected override void Init()
    {
        state = StateCharacter.Idle;
        hp = 120;
        attack = 8;
        speed = 5;
        defend = 0;
        moanTimer = Random.Range(moanIntervalMin, moanIntervalMax);
    }
}
