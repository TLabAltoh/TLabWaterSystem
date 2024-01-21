using UnityEngine;

namespace TLab.WaterSystem
{
    public class CharacterInput : MonoBehaviour
    {
        [SerializeField] private Water m_water;

        private Vector2 m_prev_hit;

        void Start()
        {
            m_prev_hit = Vector2.zero;
        }

        void Update()
        {
            RaycastHit hit;

            if (Physics.Raycast(new Ray(transform.position, -transform.up), out hit, 10))
            {
                if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Water"))
                {
                    m_water.TouchWater(hit.textureCoord);
                    m_prev_hit = hit.textureCoord;
                }
            }
        }
    }
}
