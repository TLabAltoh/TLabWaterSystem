using UnityEngine;
using TLab.WaterSystem;

public class Sample : MonoBehaviour
{
    [SerializeField] private Water m_water;

    private void Update()
    {
        if (Input.GetMouseButton(0))
        {
            RaycastHit hit;
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit))
            {
                m_water.TouchWater(hit.textureCoord);
            }
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            var target = m_water.equation_result;

            var color = RenderTextureUtil.GetPixel(0.1f, 0.1f, target);

            Debug.Log($"pixel value {color}");
        }
    }
}
