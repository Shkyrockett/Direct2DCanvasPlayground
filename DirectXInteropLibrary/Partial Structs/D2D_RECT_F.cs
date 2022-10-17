// <copyright file="D2D_RECT_F.cs" company="Shkyrockett" >
//     Copyright © 2020 - 2022 Shkyrockett. All rights reserved.
// </copyright>
// <author id="shkyrockett">Shkyrockett</author>
// <license>
//     Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </license>
// <summary></summary>
// <remarks></remarks>

using System.Diagnostics;

namespace Windows.Win32
{
    namespace Graphics.Direct2D.Common
    {
        [DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
        public partial struct D2D_RECT_F
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="D2D_RECT_F"/> class.
            /// </summary>
            /// <param name="left">The left.</param>
            /// <param name="top">The top.</param>
            /// <param name="right">The right.</param>
            /// <param name="bottom">The bottom.</param>
            public D2D_RECT_F(float left, float top, float right, float bottom)
            {
                this.left = left;
                this.top = top;
                this.right = right;
                this.bottom = bottom;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="rect"></param>
            public static implicit operator D2D_RECT_F(RectangleF rect) => new(rect.Left, rect.Top, rect.Right, rect.Bottom);

            /// <summary>
            /// Gets the debugger display.
            /// </summary>
            /// <returns>A string? .</returns>
            private string? GetDebuggerDisplay() => ToString();

            /// <inheritdoc/>
            public override string? ToString() => $"{left}, {top}, {right}, {bottom}";
        }
    }
}
