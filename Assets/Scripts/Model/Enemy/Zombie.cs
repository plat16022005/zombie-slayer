using UnityEngine;

public class Zombie : Enemy
{
    protected override void Init()
    {
        state = StateCharacter.Idle;
        hp = 80;
        attack = 8;
        speed = 5;
        defend = 0;
        moanTimer = Random.Range(moanIntervalMin, moanIntervalMax);
    }
}
