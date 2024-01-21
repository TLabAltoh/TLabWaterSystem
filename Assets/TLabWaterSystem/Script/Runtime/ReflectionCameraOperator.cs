using System.Collections.Generic;
using UnityEngine;

namespace TLab.WaterSystem
{
    public class ReflectionCameraOperator : MonoBehaviour
    {
        [SerializeField] Material m_wave_mat;

        private Transform m_wave_transform;
        private Dictionary<Camera, Camera> m_ref_camera_hash;
        private RenderTexture m_input_tex;
        private Vector4 m_near_clip_plane;
        private static readonly int REF_TEX = Shader.PropertyToID("_RefTex");

        private void GetReflectionCamera(out Camera reflectionCamera)
        {
            // Get current reflection camera.
            // Create a new reflection camera if it doesn't exist.
            // If the texture and camera already exist, skip the following steps

            if (m_input_tex == null)
            {
                const int width = 16;
                const int height = 9;
                const int screenSize = 20;
                m_input_tex = new RenderTexture(
                    width * screenSize,
                    height * screenSize,
                    32,
                    RenderTextureFormat.RGB565
                );
            }

            m_ref_camera_hash.TryGetValue(Camera.main, out reflectionCamera);

            if (reflectionCamera == null)
            {
                GameObject reflectionCameraObj = new GameObject();
                reflectionCameraObj.name = "ReflectionCamera for " + Camera.main.name;
                reflectionCamera = reflectionCameraObj.AddComponent<Camera>();

                // Don't render unless you call camera.Render()
                reflectionCamera.enabled = false;
                reflectionCamera.useOcclusionCulling = false;
                reflectionCamera.nearClipPlane = 0.3f;
                reflectionCamera.farClipPlane = 1000f;
                reflectionCamera.targetTexture = m_input_tex;

                m_ref_camera_hash[Camera.main] = reflectionCamera;
            }
        }

        private void CreateReflectionMatrix(ref Matrix4x4 mat)
        {
            // A matrix that flips coordinates along the normal in local space
            Vector4 plane = m_near_clip_plane;
            mat.m00 = (1f - 2f * plane.x * plane.x);
            mat.m01 = (-2f * plane.x * plane.y);
            mat.m02 = (-2f * plane.x * plane.z);
            mat.m03 = (-2f * plane.x * plane.w);
            mat.m10 = (-2f * plane.y * plane.x);
            mat.m11 = (1f - 2f * plane.y * plane.y);
            mat.m12 = (-2f * plane.y * plane.z);
            mat.m13 = (-2f * plane.y * plane.w);
            mat.m20 = (-2f * plane.z * plane.x);
            mat.m21 = (-2f * plane.z * plane.y);
            mat.m22 = (1f - 2f * plane.z * plane.z);
            mat.m23 = (-2f * plane.z * plane.w);
            mat.m30 = 0f;
            mat.m31 = 0f;
            mat.m32 = 0f;
            mat.m33 = 1f;
        }

        void Start()
        {
            m_wave_transform = GetComponent<Transform>();
            m_near_clip_plane = new Vector4(0f, 1f, 0f, 0f);
            m_ref_camera_hash = new Dictionary<Camera, Camera>();
        }

        void Update()
        {
            // --------------------------------------
            // Create or update reflection camera
            //

            Camera reflectionCamera;
            GetReflectionCamera(out reflectionCamera);

            // Flip camera position
            Vector3 mainCameraPos = Camera.main.transform.position;
            reflectionCamera.transform.position = new Vector3(
                mainCameraPos.x,
                m_wave_transform.position.y - (mainCameraPos.y - m_wave_transform.position.y),
                mainCameraPos.z
            );

            // Reverse camera direction
            Vector3 mainCameraEluer = Camera.main.transform.eulerAngles;
            reflectionCamera.transform.eulerAngles = new Vector3(
                -mainCameraEluer.x,
                 mainCameraEluer.y,
                -mainCameraEluer.z
            );

            // ------------------------------------------------------
            // Calculate clipping planes for reflection cameras
            //

            Matrix4x4 reflectionMatrix = new Matrix4x4();
            CreateReflectionMatrix(ref reflectionMatrix);

            // A matrix that reverses the coordinates with the reflective surface
            // as the origin (after World --> Local, returns to Local --> World)
            Matrix4x4 localReflectionMatrix =
                Camera.main.worldToCameraMatrix *
                m_wave_transform.localToWorldMatrix *
                reflectionMatrix *
                m_wave_transform.worldToLocalMatrix;

            // Reflection camera clipping plane orientation (local axis)
            Vector3 m_near_clip_planeXYZ = new Vector3(m_near_clip_plane.x, m_near_clip_plane.y, m_near_clip_plane.z);

            // Calculate the normal direction of a reflective surface in camera space
            Vector3 cnormal = localReflectionMatrix.MultiplyVector(m_near_clip_planeXYZ);
            Vector3 cpos = localReflectionMatrix.MultiplyPoint(Vector3.zero);

            Vector4 clipPlane = new Vector4(
                 cnormal.x,
                 cnormal.y,
                 cnormal.z,

                // Set m_near_clip_plane (Set a little lower so that the clipped
                // margin of the object is not visible due to wave fluctuation)
                -Vector3.Dot(cpos, cnormal) - m_wave_transform.position.y * 0.85f
            );

            reflectionCamera.worldToCameraMatrix = localReflectionMatrix;
            reflectionCamera.projectionMatrix = Camera.main.CalculateObliqueMatrix(clipPlane);

            // ------------------------------------------------------------------------------
            // Render the reflection camera footage to a texture and pass it to the shader.
            //

            // Flip the direction of the polygon normals to face the reflection camera
            GL.invertCulling = true;

            // Rendering with Reflection Camera
            reflectionCamera.Render();

            // Undo normal direction
            GL.invertCulling = false;

            // Pass the texture of the rendered reflection result to the shader
            m_wave_mat.SetTexture(REF_TEX, m_input_tex);
        }

        void OnGUI()
        {
#if false
        float h = Screen.height / 3;
        GUI.DrawTexture(new Rect(0, 0 * h, h, h), m_input_tex);
#endif
        }
    }
}
