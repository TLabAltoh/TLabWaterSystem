using UnityEngine;

public class TLabInputWithMoving : MonoBehaviour
{
    [SerializeField] TLabWaterManager waterManager;
    private Vector2 prevHit;

    void Start()
    {
        prevHit = Vector2.zero;
    }

    void Update()
    {
        RaycastHit hit;

        if (Physics.Raycast(new Ray(transform.position, -transform.up), out hit, 10))
        {
            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Water"))
            {
                waterManager.InputInWater(hit.textureCoord);
                prevHit = hit.textureCoord;
            }
            else
                waterManager.InputInWater(prevHit);
        }
    }
}
