using UnityEngine;

public class voidbehaviour : MonoBehaviour
{

    private void OnCollisionEnter(Collision collision)
    {
        collision.transform.position = gamecore.instance.CurrentStage.Spawnpoint[0].position + new Vector3(0,10,0);
    }
}
