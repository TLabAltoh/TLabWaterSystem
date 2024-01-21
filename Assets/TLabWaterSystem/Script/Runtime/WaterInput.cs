#define ZERO_ONE_INPUT

using UnityEngine;
using Unity.Collections;

namespace TLab.WaterSystem
{
    public unsafe class WaterInput : System.IDisposable
    {
        private Texture2D m_stamp_tex;
        private NativeArray<byte> m_stamp_buffer;
        private NativeArray<byte> m_color_buffer;
        private NativeArray<byte> m_reflesh_color_buffer;
        private Vector2 m_prev_hit_coord;
        private int m_tex_width;
        private int m_tex_height;
        private int m_stamp_tex_width;
        private int m_stamp_tex_half_width;
        private int m_stamp_tex_height;
        private int m_stamp_tex_half_height;

        private const int CHANNEL_SIZE = 4;

        public WaterInput(int tex_width, int tex_height, Texture2D stamp_tex, ref Texture2D input_tex)
        {
            m_tex_width = tex_width;
            m_tex_height = tex_height;
            m_stamp_tex = stamp_tex;
            m_stamp_tex_width = stamp_tex.width;
            m_stamp_tex_height = stamp_tex.height;
            m_stamp_tex_half_width = stamp_tex.width / 2;
            m_stamp_tex_half_height = stamp_tex.height / 2;

            var pixels = stamp_tex.GetPixels();

            m_reflesh_color_buffer = new NativeArray<byte>(m_tex_width * m_tex_height * CHANNEL_SIZE, Allocator.Persistent);
            m_color_buffer = new NativeArray<byte>(m_reflesh_color_buffer.Length, Allocator.Persistent);
            m_stamp_buffer = new NativeArray<byte>(pixels.Length * CHANNEL_SIZE, Allocator.Persistent);

            for (int i = 0; i < pixels.Length; i++)
            {
#if !ZERO_ONE_INPUT
                // 0 ~ 1 => 0.5 ~ 1
                m_stamp_buffer[i * CHANNEL_SIZE + 0] = (byte)((pixels[i].a * 0.5f + 0.5f) * byte.MaxValue);
                m_stamp_buffer[i * CHANNEL_SIZE + 1] = (byte)((pixels[i].a * 0.5f + 0.5f) * byte.MaxValue);
                m_stamp_buffer[i * CHANNEL_SIZE + 2] = (byte)((pixels[i].a * 0.5f + 0.5f) * byte.MaxValue);
                m_stamp_buffer[i * CHANNEL_SIZE + 3] = (byte)((pixels[i].a * 0.5f + 0.5f) * byte.MaxValue);
#else
                m_stamp_buffer[i * CHANNEL_SIZE + 0] = (byte)(pixels[i].a * byte.MaxValue);
                m_stamp_buffer[i * CHANNEL_SIZE + 1] = (byte)(pixels[i].a * byte.MaxValue);
                m_stamp_buffer[i * CHANNEL_SIZE + 2] = (byte)(pixels[i].a * byte.MaxValue);
                m_stamp_buffer[i * CHANNEL_SIZE + 3] = (byte)(pixels[i].a * byte.MaxValue);
#endif
            }

            for (int x = 0; x < m_tex_width; x++)
            {
                for (int y = 0; y < m_tex_height; y++)
                {
                    for (int c = 0; c < CHANNEL_SIZE; c++)
                    {
#if !ZERO_ONE_INPUT
                        m_reflesh_color_buffer[(x + m_tex_width * y) * CHANNEL_SIZE + c] = 128;
#else
                        m_reflesh_color_buffer[(x + m_tex_width * y) * CHANNEL_SIZE + c] = 0;
#endif
                    }
                }
            }

            m_reflesh_color_buffer.CopyTo(m_color_buffer);

            input_tex = new Texture2D(m_tex_width, m_tex_height, TextureFormat.ARGB32, mipChain: false, linear: true);
            input_tex.SetPixelData<byte>(m_color_buffer, 0);
            input_tex.Apply();

            m_prev_hit_coord = Vector2.zero;
        }

        public void Dispose()
        {
            if (m_color_buffer.IsCreated)
            {
                m_color_buffer.Dispose();
            }

            if (m_reflesh_color_buffer.IsCreated)
            {
                m_reflesh_color_buffer.Dispose();
            }

            if (m_stamp_buffer.IsCreated)
            {
                m_stamp_buffer.Dispose();
            }
        }

        public bool DrawPixelStamp(Vector2 p, ref Texture2D input_tex)
        {
            m_reflesh_color_buffer.CopyTo(m_color_buffer);

            if (m_prev_hit_coord.x != p.x && m_prev_hit_coord.y != p.y)
            {
                m_prev_hit_coord = p;

                int input_x = (int)(p.x * m_tex_width);
                int input_y = (int)(p.y * m_tex_height);
                int buffer_x_start = input_x - m_stamp_tex_half_width;
                int buffer_y_start = input_y - m_stamp_tex_half_height;
                int buffer_x_end = input_x + m_stamp_tex_half_width;
                int buffer_y_end = input_y + m_stamp_tex_half_height;

                int stamp_x_start = 0;
                int stamp_x = 0;
                int stamp_y = 0;

                if (buffer_x_start < 0)
                {
                    int over = 0 - buffer_x_start;
                    stamp_x_start = stamp_x_start + over;
                    buffer_x_start = 0;
                }
                if (buffer_x_end > m_tex_width)
                {
                    buffer_x_end = m_tex_width;
                }

                if (buffer_y_start < 0)
                {
                    int over = 0 - buffer_y_start;
                    stamp_y = stamp_y + over;
                    buffer_y_start = 0;
                }
                if (buffer_y_end > m_tex_height)
                {
                    buffer_y_end = m_tex_height;
                }

                //Debug.Log(
                //    $"input_x: {input_x}, " +
                //    $"input_y: {input_y}, " +
                //    $"buffer_x_start: {buffer_x_start}, " +
                //    $"buffer_x_end: {buffer_x_end}, " +
                //    $"buffer_y_start: {buffer_y_start}, " +
                //    $"buffer_y_end: {buffer_y_end}"
                //);

                int buffer_line_offset, stamp_line_offset;

                for (int y = buffer_y_start; y < buffer_y_end; y++)
                {
                    stamp_x = stamp_x_start * CHANNEL_SIZE;

                    buffer_line_offset = y * m_tex_height * CHANNEL_SIZE;
                    stamp_line_offset = (stamp_y++) * m_stamp_tex_width * CHANNEL_SIZE;

                    for (int x = buffer_x_start * CHANNEL_SIZE; x < buffer_x_end * CHANNEL_SIZE; x++)
                    {
                        m_color_buffer[buffer_line_offset + x] = m_stamp_buffer[stamp_line_offset + (stamp_x++)];
                    }
                }
            }

            input_tex.SetPixelData<byte>(m_color_buffer, 0);
            input_tex.Apply();

            return true;
        }
    }
}
