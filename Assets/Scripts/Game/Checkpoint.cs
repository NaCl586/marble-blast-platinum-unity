using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    public Transform spawn;

    public void OnCollisionEnter(Collision collision)
    {
        Movement movement = null;
        if (collision.gameObject.TryGetComponent<Movement>(out movement))
        {
            CollisionEnter();
        }
    }

    void CollisionEnter()
    {
        CancelInvoke();
        GameManager.onReachCheckpoint?.Invoke(spawn);
    }
}
