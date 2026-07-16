using UnityEngine;

public class ZombieHP : Zombie
{
    protected override void Init()
    {
        state = StateCharacter.Idle;
        hp = 200;
        attack = 15;
        speed = 6;
        defend = 0;
        moanTimer = Random.Range(moanIntervalMin, moanIntervalMax);
    }
}
