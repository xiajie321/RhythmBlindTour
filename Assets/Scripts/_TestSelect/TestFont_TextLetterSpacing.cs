using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace TestRevolver
{
    [RequireComponent(typeof(Text))]
    public class TestFont_TextLetterSpacing : BaseMeshEffect
    {
        public enum TextAlignment
        {
            Left,
            Center,
            Right
        }

        public float spacing = 0f;
        public TextAlignment alignment = TextAlignment.Left;

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive()) return;

            List<UIVertex> verts = new List<UIVertex>();
            vh.GetUIVertexStream(verts);

            int count = verts.Count / 6;
            float totalSpacing = spacing * (count - 1);
            float offsetStart = 0f;

            switch (alignment)
            {
                case TextAlignment.Left:
                    offsetStart = 0f;
                    break;
                case TextAlignment.Center:
                    offsetStart = -totalSpacing / 2f;
                    break;
                case TextAlignment.Right:
                    offsetStart = -totalSpacing;
                    break;
            }

            for (int i = 0; i < count; i++)
            {
                float charOffset = offsetStart + spacing * i;

                for (int j = 0; j < 6; j++)
                {
                    int idx = i * 6 + j;
                    UIVertex v = verts[idx];
                    v.position += new Vector3(charOffset, 0, 0);
                    verts[idx] = v;
                }
            }

            vh.Clear();
            vh.AddUIVertexTriangleStream(verts);
        }
    }
}
