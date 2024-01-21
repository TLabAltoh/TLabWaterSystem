using UnityEngine;

namespace TLab.WaterSystem
{
    public class Water : MonoBehaviour
    {
        [Tooltip("Material to which the computed wave normal texture is applied")]
        [SerializeField] public Material m_wave_mat;

        [Tooltip("Materials that calculate the wave equation")]
        [SerializeField] public Material m_wave_equation_mat;

        [Tooltip("Material that computes wave normals from the m_equation_result of the wave equation")]
        [SerializeField] public Material m_wave_normal_mat;

        [SerializeField] private Texture2D m_stamp_tex;

        [SerializeField, Range(128, 1024)] private int m_tex_width = 512;
        [SerializeField, Range(128, 1024)] private int m_tex_height = 512;

        private Texture2D m_input;
        private WaterInput m_water_input;

        private RenderTexture m_wave_normal;
        private RenderTexture m_prev_result;
        private RenderTexture m_prev_2_result;
        private RenderTexture m_equation_result;

        public static readonly int INPUT_TEX = Shader.PropertyToID("_InputTex");
        public static readonly int PREV_TEX = Shader.PropertyToID("_PrevTex");
        public static readonly int PREV_2_TEX = Shader.PropertyToID("_Prev2Tex");
        public static readonly int WAVE_TEX = Shader.PropertyToID("_WaveTex");
        public static readonly int WAVE_NORMAL = Shader.PropertyToID("_WaveNormal");

        public RenderTexture wave_normal => m_wave_normal;

        public RenderTexture prev_result => m_prev_result;

        public RenderTexture prev_2_result => m_prev_2_result;

        public RenderTexture equation_result => m_equation_result;

        public Texture2D input => m_input;

        public void TouchWater(Vector2 hit_uv_coord)
        {
            m_water_input.DrawPixelStamp(hit_uv_coord, ref m_input);
        }

        private void OnGUI()
        {
            // GUIでテクスチャバッファを可視化する
            int h = Screen.height / 2;
            int w = Screen.width;

            // m_wave_equation_matのDebug処理(コメントアウトにされてる箇所は気になったとき自由にDebugすること)
            GUI.DrawTexture(new Rect(0, 0 * h, h, h), m_equation_result);
            GUI.DrawTexture(new Rect(0, 1 * h, h, h), m_input);
            GUI.DrawTexture(new Rect(w - h, 0 * h, h, h), m_wave_normal);
            // GUI.DrawTexture(new Rect(w - h, 1 * h, h, h), m_stamp_tex);

            // 後述のGUI程重なった上に表示される
            // 第1,2引数 : GUIの位置. 第3,4引数 : GUIの大きさ
            const int text_scale = 3;    // 0 ~ 15
            GUI.Box(new Rect(0, 1 * h - h / (15 - text_scale), h, h / (15 - text_scale)), "EquationResult");
            GUI.Box(new Rect(0, 2 * h - h / (15 - text_scale), h, h / (15 - text_scale)), "Input");
            GUI.Box(new Rect(w - h, 1 * h - h / (15 - text_scale), h, h / (15 - text_scale)), "NormalResult");
            // GUI.Box(new Rect(w - h, 2 * h - h / (15 - text_scale), h, h / (15 - text_scale)), "StampTexture");
        }

        private void Start()
        {
            if (m_stamp_tex.isReadable == false)
            {
                Debug.LogError("m_stamp_tex is not readable");
                return;
            }

            m_water_input = new WaterInput(m_tex_width, m_tex_width, m_stamp_tex, ref m_input);

            var option = new RenderTextureOption
            {
                width = m_tex_width,
                height = m_tex_height,
                depth = 0,
                format = RenderTextureFormat.ARGB32,
                readWrite = RenderTextureReadWrite.Linear
            };
            RenderTextureUtil.CreateRenderTexture(option, ref m_equation_result);
            RenderTextureUtil.CreateRenderTexture(option, ref m_prev_result);
            RenderTextureUtil.CreateRenderTexture(option, ref m_prev_2_result);
            RenderTextureUtil.CreateRenderTexture(option, ref m_wave_normal);

            // バッファの初期化
            var color_init = new Texture2D(1, 1, TextureFormat.ARGB32, mipChain: false, linear: true);
            color_init.SetPixel(0, 0, new Color(0.5f, 0.5f, 0.5f, 0.5f));
            color_init.Apply();
            Graphics.Blit(color_init, m_prev_result);
            Graphics.Blit(color_init, m_prev_2_result);

            m_wave_equation_mat.SetTexture(INPUT_TEX, m_input);
            m_wave_equation_mat.SetTexture(PREV_TEX, m_prev_result);
            m_wave_equation_mat.SetTexture(PREV_2_TEX, m_prev_2_result);
        }

        private int count = 0;
        private int a = 1;

        private void Update()
        {
            //if (count++ > a)
            //{
            //    count = a + 1;
            //    return;
            //}

            // 波動方程式を Shaderで GPU演算してテクスチャに反映
            m_wave_equation_mat.SetTexture(INPUT_TEX, m_input);
            m_wave_equation_mat.SetTexture(PREV_TEX, m_prev_result);
            m_wave_equation_mat.SetTexture(PREV_2_TEX, m_prev_2_result);

            // 波動方程式で波を計算
            Graphics.Blit(null, m_equation_result, m_wave_equation_mat);

            // 波の法線を計算
            m_wave_normal_mat.SetTexture(WAVE_TEX, m_equation_result);
            Graphics.Blit(null, m_wave_normal, m_wave_normal_mat);

            // 法線の計算結果を最終結果のマテリアルに適応する
            m_wave_mat.SetTexture(WAVE_NORMAL, m_wave_normal);

            // RenderTextureを1つずつ入れ替える
            var tmp = m_prev_2_result;
            m_prev_2_result = m_prev_result;
            m_prev_result = m_equation_result;
            m_equation_result = tmp;
        }

        private void OnDestroy()
        {
            m_water_input.Dispose();
        }
    }
}
